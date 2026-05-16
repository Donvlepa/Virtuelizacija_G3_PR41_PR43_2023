using Service;
using System;
using System.ServiceModel;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = null;

            try
            {
                host = new ServiceHost(typeof(WeatherService));

                host.Open();

                Console.WriteLine("WeatherService je pokrenut.");
                Console.WriteLine("Server radi...");
                Console.WriteLine("Pritisni ENTER za gasenje servera.");

                Console.ReadLine();

                host.Close();

                Console.WriteLine("Server je ugasen.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greska pri pokretanju servera:");
                Console.WriteLine(ex.Message);

                if (host != null)
                {
                    host.Abort();
                }

                Console.ReadLine();
            }
        }
    }
}