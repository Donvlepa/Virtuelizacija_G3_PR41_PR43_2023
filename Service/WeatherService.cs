using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;

namespace Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single)]
    public class WeatherService : IWeatherService
    {
        private static readonly object syncRoot = new object();
        private static readonly List<WeatherSample> samples = new List<WeatherSample>();
        private static readonly WeatherAnalyticsState analyticsState = new WeatherAnalyticsState();

        private static SessionFileStorage fileStorage;
        private static bool sessionStarted;
        private static AnalyticsSettings analyticsSettings = AnalyticsSettings.LoadFromConfiguration();

        private static ICommunicationObject activeClientChannel;

        public event EventHandler<TransferStartedEventArgs> OnTransferStarted;
        public event EventHandler<SampleReceivedEventArgs> OnSampleReceived;
        public event EventHandler<TransferCompletedEventArgs> OnTransferCompleted;
        public event EventHandler<WarningRaisedEventArgs> OnWarningRaised;

        public WeatherService()
        {
            OnTransferStarted += WeatherEventLogger.HandleTransferStarted;
            OnSampleReceived += WeatherEventLogger.HandleSampleReceived;
            OnTransferCompleted += WeatherEventLogger.HandleTransferCompleted;
            OnWarningRaised += WeatherEventLogger.HandleWarningRaised;
        }

        public ServiceResponse StartSession(SessionMeta meta)
        {
            ValidateMeta(meta);

            lock (syncRoot)
            {
                CloseCurrentSessionIfExists("Pokretanje nove sesije. Zatvaram prethodnu ako postoji.");

                samples.Clear();
                analyticsState.Reset();
                analyticsSettings = AnalyticsSettings.LoadFromConfiguration();

                fileStorage = new SessionFileStorage(meta.Headers);
                fileStorage.StartSession();

                sessionStarted = true;

                RegisterClientChannel();

                Console.WriteLine("prenos u toku...");
                Console.WriteLine("Kreiran measurements_session.csv: " + fileStorage.MeasurementsPath);
                Console.WriteLine("Kreiran rejects.csv: " + fileStorage.RejectsPath);

                RaiseTransferStarted(new TransferStartedEventArgs(DateTime.Now, fileStorage.SessionFolder, meta));
            }

            return new ServiceResponse
            {
                Success = ResponseStatus.ACK,
                Status = TransferStatus.IN_PROGRESS
            };
        }

        public ServiceResponse PushSample(WeatherSample sample)
        {
            lock (syncRoot)
            {
                EnsureSessionStarted();

                try
                {
                    ValidateSample(sample);
                }
                catch (FaultException<ValidationFault> ex)
                {
                    fileStorage.AppendReject(sample, ex.Detail.Message);
                    Console.WriteLine("Odbačeno merenje: " + ex.Detail.Message);
                    throw;
                }
                catch (FaultException<DataFormatFault> ex)
                {
                    fileStorage.AppendReject(sample, ex.Detail.Message);
                    Console.WriteLine("Odbačeno merenje: " + ex.Detail.Message);
                    throw;
                }

                samples.Add(sample);
                fileStorage.AppendMeasurement(sample);

                List<WarningRaisedEventArgs> warnings = analyticsState.AnalyzeSample(sample, analyticsSettings);

                Console.WriteLine("prenos u toku... primljen sample za " + sample.Date.ToString("yyyy-MM-dd HH:mm:ss"));

                RaiseSampleReceived(new SampleReceivedEventArgs(sample, samples.Count, analyticsState.PressureMean));

                foreach (WarningRaisedEventArgs warning in warnings)
                {
                    RaiseWarningRaised(warning);
                }
            }

            return new ServiceResponse
            {
                Success = ResponseStatus.ACK,
                Status = TransferStatus.IN_PROGRESS
            };
        }

        public ServiceResponse EndSession()
        {
            int totalSamples;

            lock (syncRoot)
            {
                totalSamples = samples.Count;

                CloseCurrentSessionIfExists("Normalan završetak prenosa preko EndSession.");

                RaiseTransferCompleted(new TransferCompletedEventArgs(DateTime.Now, totalSamples));
            }

            Console.WriteLine("završen prenos");

            return new ServiceResponse
            {
                Success = ResponseStatus.ACK,
                Status = TransferStatus.COMPLETED
            };
        }

        private static void EnsureSessionStarted()
        {
            if (!sessionStarted || fileStorage == null)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Sesija nije pokrenuta. Prvo pozovi StartSession."),
                    "Sesija nije pokrenuta.");
            }
        }

        private static void CloseCurrentSessionIfExists(string reason)
        {
            string measurementsPath = null;
            string rejectsPath = null;

            if (fileStorage != null)
            {
                measurementsPath = fileStorage.MeasurementsPath;
                rejectsPath = fileStorage.RejectsPath;

                fileStorage.Dispose();
                fileStorage = null;

                Console.WriteLine("[RESOURCE CLEANUP] SessionFileStorage je zatvoren.");
                Console.WriteLine("[RESOURCE CLEANUP] Razlog: " + reason);
            }

            sessionStarted = false;

            UnregisterClientChannel();

            ProveFileIsUnlocked(measurementsPath);
            ProveFileIsUnlocked(rejectsPath);
        }

        private static void RegisterClientChannel()
        {
            if (OperationContext.Current == null)
            {
                return;
            }

            ICommunicationObject channel = OperationContext.Current.Channel;

            if (channel == null)
            {
                return;
            }

            if (activeClientChannel != null)
            {
                activeClientChannel.Closed -= ActiveClientChannel_Closed;
                activeClientChannel.Faulted -= ActiveClientChannel_Faulted;
            }

            activeClientChannel = channel;

            activeClientChannel.Closed += ActiveClientChannel_Closed;
            activeClientChannel.Faulted += ActiveClientChannel_Faulted;
        }

        private static void UnregisterClientChannel()
        {
            if (activeClientChannel == null)
            {
                return;
            }

            activeClientChannel.Closed -= ActiveClientChannel_Closed;
            activeClientChannel.Faulted -= ActiveClientChannel_Faulted;
            activeClientChannel = null;
        }

        private static void ActiveClientChannel_Closed(object sender, EventArgs e)
        {
            lock (syncRoot)
            {
                if (sessionStarted || fileStorage != null)
                {
                    Console.WriteLine("[SIMULATION] Detektovano zatvaranje klijentskog kanala pre EndSession.");
                    CloseCurrentSessionIfExists("Klijentski kanal je zatvoren pre kraja prenosa.");
                }
            }
        }

        private static void ActiveClientChannel_Faulted(object sender, EventArgs e)
        {
            lock (syncRoot)
            {
                if (sessionStarted || fileStorage != null)
                {
                    Console.WriteLine("[SIMULATION] Detektovan prekid/fault klijentskog kanala pre EndSession.");
                    CloseCurrentSessionIfExists("Klijentski kanal je pukao usred prenosa.");
                }
            }
        }

        private static void ProveFileIsUnlocked(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }

                Console.WriteLine("[DOKAZ] Fajl nije zaključan i može ekskluzivno da se otvori: " + path);
            }
            catch (IOException ex)
            {
                Console.WriteLine("[DOKAZ NEUSPEO] Fajl je i dalje zaključan: " + path);
                Console.WriteLine(ex.Message);
            }
        }

        private void ValidateMeta(SessionMeta meta)
        {
            if (meta == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("SessionMeta ne sme biti null."),
                    "Nevalidan format sesije.");
            }

            if (meta.Headers == null || meta.Headers.Count == 0)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Meta headers ne smeju biti prazni."),
                    "Nedostaje meta-zaglavlje.");
            }

            string[] requiredHeaders =
            {
                "T",
                "Pressure",
                "Tpot",
                "Tdew",
                "VPmax",
                "VPdef",
                "VPact",
                "Date"
            };

            foreach (string header in requiredHeaders)
            {
                if (!meta.Headers.Contains(header))
                {
                    throw new FaultException<DataFormatFault>(
                        new DataFormatFault($"Nedostaje obavezno meta polje: {header}."),
                        "Nevalidno meta-zaglavlje.");
                }
            }
        }

        private void ValidateSample(WeatherSample sample)
        {
            if (sample == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Sample je null."),
                    "Nevalidan format.");
            }

            if (sample.Date == default(DateTime))
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Date je obavezno polje."),
                    "Nedostaje polje.");
            }

            if (double.IsNaN(sample.Pressure) || double.IsInfinity(sample.Pressure))
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Pressure nije validan broj."),
                    "Nevalidan tip.");
            }

            if (sample.Pressure <= 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Pressure mora biti > 0."),
                    "Nevalidan opseg.");
            }

            if (sample.VPmax < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("VPmax ne sme biti negativan."),
                    "Nevalidan opseg.");
            }

            if (sample.VPact < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("VPact ne sme biti negativan."),
                    "Nevalidan opseg.");
            }

            if (sample.VPdef < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("VPdef ne sme biti negativan."),
                    "Nevalidan opseg.");
            }

            if (sample.VPact > sample.VPmax)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("VPact ne sme biti veći od VPmax."),
                    "Nevalidan odnos.");
            }
        }

        private void RaiseTransferStarted(TransferStartedEventArgs args)
        {
            EventHandler<TransferStartedEventArgs> handler = OnTransferStarted;

            if (handler != null)
            {
                handler(this, args);
            }
        }

        private void RaiseSampleReceived(SampleReceivedEventArgs args)
        {
            EventHandler<SampleReceivedEventArgs> handler = OnSampleReceived;

            if (handler != null)
            {
                handler(this, args);
            }
        }

        private void RaiseTransferCompleted(TransferCompletedEventArgs args)
        {
            EventHandler<TransferCompletedEventArgs> handler = OnTransferCompleted;

            if (handler != null)
            {
                handler(this, args);
            }
        }

        private void RaiseWarningRaised(WarningRaisedEventArgs args)
        {
            EventHandler<WarningRaisedEventArgs> handler = OnWarningRaised;

            if (handler != null)
            {
                handler(this, args);
            }
        }
    }
}