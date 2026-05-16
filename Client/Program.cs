using Common;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            bool simulateBreak =
                args.Length > 0 &&
                args[0].Equals("--simulate-break", StringComparison.OrdinalIgnoreCase);

            ChannelFactory<IWeatherService> factory = null;
            IWeatherService proxy = null;
            IClientChannel clientChannel = null;

            bool aborted = false;

            try
            {
                factory = new ChannelFactory<IWeatherService>("WeatherService");
                proxy = factory.CreateChannel();
                clientChannel = (IClientChannel)proxy;

                SessionMeta meta = new SessionMeta
                {
                    Headers = new List<string>
                    {
                        "T",
                        "Pressure",
                        "Tpot",
                        "Tdew",
                        "VPmax",
                        "VPdef",
                        "VPact",
                        "Date"
                    },

                    Units = new Dictionary<string, string>
                    {
                        { "T", "C" },
                        { "Pressure", "hPa" },
                        { "Tpot", "K" },
                        { "Tdew", "C" },
                        { "VPmax", "mbar" },
                        { "VPdef", "mbar" },
                        { "VPact", "mbar" },
                        { "Date", "datetime" }
                    }
                };

                ServiceResponse startResponse = proxy.StartSession(meta);
                Console.WriteLine($"StartSession: {startResponse.Success}, {startResponse.Status}");

                List<WeatherSample> samples = CsvLoader.LoadWeatherSamples("cleaned_weather.csv");

                Console.WriteLine($"Učitano iz CSV: {samples.Count}");

                int breakAfter = Math.Max(1, samples.Count / 2);

                if (simulateBreak)
                {
                    int parsedBreakAfter;

                    if (args.Length > 1 && int.TryParse(args[1], out parsedBreakAfter))
                    {
                        breakAfter = parsedBreakAfter;
                    }

                    if (samples.Count > 0)
                    {
                        breakAfter = Math.Max(1, Math.Min(breakAfter, samples.Count));
                    }

                    Console.WriteLine($"[SIMULATION] Veza će biti prekinuta posle {breakAfter}. sample-a.");
                }

                int sampleNumber = 0;

                foreach (WeatherSample sample in samples)
                {
                    sampleNumber++;

                    try
                    {
                        ServiceResponse response = proxy.PushSample(sample);
                        Console.WriteLine($"Poslat sample {sampleNumber}/{samples.Count}: {response.Success}, {response.Status}");
                    }
                    catch (FaultException<ValidationFault> ex)
                    {
                        Console.WriteLine($"{ResponseStatus.NACK} | ValidationFault za sample {sampleNumber}: {ex.Detail.Message}");
                    }
                    catch (FaultException<DataFormatFault> ex)
                    {
                        Console.WriteLine($"{ResponseStatus.NACK} | DataFormatFault za sample {sampleNumber}: {ex.Detail.Message}");
                    }

                    if (simulateBreak && sampleNumber == breakAfter)
                    {
                        Console.WriteLine("[SIMULATION] Prekid veze usred prenosa. Klijent abortuje WCF kanal.");

                        clientChannel.Abort();
                        aborted = true;

                        throw new CommunicationException("SIMULACIJA: veza je prekinuta usred prenosa.");
                    }

                    Thread.Sleep(100);
                }

                ServiceResponse endResponse = proxy.EndSession();
                Console.WriteLine($"EndSession: {endResponse.Success}, {endResponse.Status}");
            }
            catch (CommunicationException ex)
            {
                Console.WriteLine($"Communication error: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"Timeout: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška: {ex.Message}");
            }
            finally
            {
                CloseOrAbort(clientChannel, "WCF channel", aborted);

                if (factory != null)
                {
                    CloseOrAbort(factory, "ChannelFactory", false);
                }

                if (aborted)
                {
                    Console.WriteLine("[DISPOSE] Klijent je prekinuo prenos i oslobodio lokalne WCF resurse.");
                    Console.WriteLine("[DISPOSE] EndSession nije pozvan jer se simulira pad veze.");
                }

                Console.ReadLine();
            }
        }

        private static void CloseOrAbort(ICommunicationObject communicationObject, string name, bool forceAbort)
        {
            if (communicationObject == null)
            {
                return;
            }

            try
            {
                if (forceAbort || communicationObject.State == CommunicationState.Faulted)
                {
                    communicationObject.Abort();
                    Console.WriteLine($"[DISPOSE] {name} abortovan.");
                }
                else
                {
                    communicationObject.Close();
                    Console.WriteLine($"[DISPOSE] {name} zatvoren.");
                }
            }
            catch
            {
                communicationObject.Abort();
                Console.WriteLine($"[DISPOSE] {name} abortovan nakon greške pri zatvaranju.");
            }
        }
    }
}