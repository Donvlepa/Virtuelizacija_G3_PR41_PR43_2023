using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ChannelFactory<IWeatherService> factory =
                new ChannelFactory<IWeatherService>("WeatherService");

            IWeatherService proxy = factory.CreateChannel();

            try
            {
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

                var samples = CsvLoader.LoadWeatherSamples("cleaned_weather.csv");

                Console.WriteLine($"Učitano iz CSV: {samples.Count}");

                int sampleNumber = 0;

                foreach (var sample in samples)
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

            Console.ReadLine();
        }
    }
}