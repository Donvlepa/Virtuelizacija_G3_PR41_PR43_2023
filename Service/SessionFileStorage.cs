using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Service
{
    internal class SessionFileStorage : IDisposable
    {
        private readonly List<string> headers;
        private readonly object locker = new object();

        private StreamWriter measurementsWriter;
        private StreamWriter rejectsWriter;
        private bool disposed;

        public string SessionFolder { get; private set; }
        public string MeasurementsPath { get; private set; }
        public string RejectsPath { get; private set; }

        public SessionFileStorage(IEnumerable<string> headers)
        {
            this.headers = new List<string>(headers);
        }

        public void StartSession()
        {
            string folderName = "Session_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

            SessionFolder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "ServerData",
                folderName);

            Directory.CreateDirectory(SessionFolder);

            MeasurementsPath = Path.Combine(SessionFolder, "measurements_session.csv");
            RejectsPath = Path.Combine(SessionFolder, "rejects.csv");

            measurementsWriter = new StreamWriter(new FileStream(
                MeasurementsPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read));

            rejectsWriter = new StreamWriter(new FileStream(
                RejectsPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read));

            measurementsWriter.WriteLine(BuildHeaderLine(includeReason: false));
            rejectsWriter.WriteLine(BuildHeaderLine(includeReason: true));

            measurementsWriter.Flush();
            rejectsWriter.Flush();
        }

        public void AppendMeasurement(WeatherSample sample)
        {
            lock (locker)
            {
                EnsureWritersAreOpen();

                measurementsWriter.WriteLine(BuildSampleLine(sample, null));
                measurementsWriter.Flush();
            }
        }

        public void AppendReject(WeatherSample sample, string reason)
        {
            lock (locker)
            {
                EnsureWritersAreOpen();

                rejectsWriter.WriteLine(BuildSampleLine(sample, reason));
                rejectsWriter.Flush();
            }
        }

        private void EnsureWritersAreOpen()
        {
            if (measurementsWriter == null || rejectsWriter == null)
            {
                throw new InvalidOperationException("CSV fajlovi za sesiju nisu otvoreni.");
            }
        }

        private string BuildHeaderLine(bool includeReason)
        {
            List<string> columns = new List<string>(headers);

            if (includeReason)
            {
                columns.Add("Reason");
            }

            return string.Join(",", columns.ConvertAll(EscapeCsv));
        }

        private string BuildSampleLine(WeatherSample sample, string reason)
        {
            List<string> values = new List<string>();

            foreach (string header in headers)
            {
                values.Add(GetValueByHeader(sample, header));
            }

            if (reason != null)
            {
                values.Add(reason);
            }

            return string.Join(",", values.ConvertAll(EscapeCsv));
        }

        private string GetValueByHeader(WeatherSample sample, string header)
        {
            if (sample == null)
            {
                return string.Empty;
            }

            switch ((header ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "t":
                    return sample.T.ToString(CultureInfo.InvariantCulture);
                case "pressure":
                    return sample.Pressure.ToString(CultureInfo.InvariantCulture);
                case "tpot":
                    return sample.Tpot.ToString(CultureInfo.InvariantCulture);
                case "tdew":
                    return sample.Tdew.ToString(CultureInfo.InvariantCulture);
                case "vpmax":
                    return sample.VPmax.ToString(CultureInfo.InvariantCulture);
                case "vpdef":
                    return sample.VPdef.ToString(CultureInfo.InvariantCulture);
                case "vpact":
                    return sample.VPact.ToString(CultureInfo.InvariantCulture);
                case "date":
                    return sample.Date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                default:
                    return string.Empty;
            }
        }

        private static string EscapeCsv(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            bool mustQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\r") || value.Contains("\n");

            value = value.Replace("\"", "\"\"");

            if (mustQuote)
            {
                return "\"" + value + "\"";
            }

            return value;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (measurementsWriter != null)
            {
                measurementsWriter.Dispose();
                measurementsWriter = null;
            }

            if (rejectsWriter != null)
            {
                rejectsWriter.Dispose();
                rejectsWriter = null;
            }

            disposed = true;
        }
    }
}
