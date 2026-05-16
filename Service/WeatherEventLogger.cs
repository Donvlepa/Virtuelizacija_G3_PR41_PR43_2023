using System;
using System.Globalization;
using System.IO;

namespace Service
{
    internal static class WeatherEventLogger
    {
        private static readonly object locker = new object();
        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "events.log");

        public static void HandleTransferStarted(object sender, TransferStartedEventArgs e)
        {
            WriteEvent(string.Format(
                CultureInfo.InvariantCulture,
                "OnTransferStarted | vreme = {0:yyyy-MM-dd HH:mm:ss} | folder = {1}",
                e.StartedAt,
                e.SessionFolder));
        }

        public static void HandleSampleReceived(object sender, SampleReceivedEventArgs e)
        {
            WriteEvent(string.Format(
                CultureInfo.InvariantCulture,
                "OnSampleReceived | sample = {0} | datum = {1:yyyy-MM-dd HH:mm:ss} | P = {2} | Pmean = {3}",
                e.SampleNumber,
                e.Sample.Date,
                e.Sample.Pressure,
                e.PressureMean));
        }

        public static void HandleTransferCompleted(object sender, TransferCompletedEventArgs e)
        {
            WriteEvent(string.Format(
                CultureInfo.InvariantCulture,
                "OnTransferCompleted | vreme = {0:yyyy-MM-dd HH:mm:ss} | ukupno validnih sample-ova = {1}",
                e.CompletedAt,
                e.TotalSamples));
        }

        public static void HandleWarningRaised(object sender, WarningRaisedEventArgs e)
        {
            WriteEvent(string.Format(
                CultureInfo.InvariantCulture,
                "OnWarningRaised | {0} | datum = {1:yyyy-MM-dd HH:mm:ss} | smer = {2} | {3}",
                e.WarningType,
                e.SampleDate,
                e.Direction,
                e.Message));
        }

        private static void WriteEvent(string message)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " | " + message;

            lock (locker)
            {
                Console.WriteLine(line);
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
        }
    }
}
