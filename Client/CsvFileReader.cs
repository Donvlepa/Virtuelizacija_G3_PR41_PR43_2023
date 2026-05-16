using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public class CsvFileReader : IDisposable
{
    private StreamReader reader;
    private StreamWriter logWriter;
    private bool disposed = false;
    private readonly string path;
    private readonly string logPath;

    public CsvFileReader(string path, string logPath = "csv_log.txt")
    {
        this.path = path;
        this.logPath = logPath;
    }

    ~CsvFileReader()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                if (reader != null)
                    reader.Dispose();

                if (logWriter != null)
                    logWriter.Dispose();
            }

            disposed = true;
        }
    }

    public List<WeatherSample> LoadWeatherSamples()
    {
        List<WeatherSample> samples = new List<WeatherSample>();

        reader = new StreamReader(path);
        logWriter = new StreamWriter(logPath, false);

        reader.ReadLine();

        int lineNumber = 1;
        string line;

        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;

            string[] parts = line.Split(',');

            if (parts.Length < 21)
            {
                logWriter.WriteLine($"Line {lineNumber}: Nedovoljan broj kolona.");
                continue;
            }

            for (int j = 0; j < parts.Length; j++)
            {
                parts[j] = parts[j].Trim();
            }

            bool okDate = DateTime.TryParseExact(
                parts[0],
                new[]
                {
                    "dd-MM-yy H:mm",
                    "dd-MM-yy HH:mm",
                    "dd-MM-yyyy H:mm",
                    "dd-MM-yyyy HH:mm"
                },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime date);

            bool okPressure = double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double pressure);
            bool okT = double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double t);
            bool okTpot = double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double tpot);
            bool okTdew = double.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out double tdew);
            bool okVpmax = double.TryParse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture, out double vpmax);
            bool okVpact = double.TryParse(parts[7], NumberStyles.Any, CultureInfo.InvariantCulture, out double vpact);
            bool okVpdef = double.TryParse(parts[8], NumberStyles.Any, CultureInfo.InvariantCulture, out double vpdef);

            if (!okDate)
            {
                logWriter.WriteLine($"Line {lineNumber}: Nevalidan datum: '{parts[0]}'");
                continue;
            }

            if (!okPressure)
            {
                logWriter.WriteLine($"Line {lineNumber}: Nevalidan pressure: '{parts[1]}'");
                continue;
            }

            if (!okT)
            {
                logWriter.WriteLine($"Line {lineNumber}: Nevalidan T: '{parts[2]}'");
                continue;
            }

            if (!okTpot)
            {
                logWriter.WriteLine($"Line {lineNumber}: Nevalidan Tpot: '{parts[3]}'");
                continue;
            }

            if (!okTdew)
            {
                logWriter.WriteLine($"Line {lineNumber}: Nevalidan Tdew: '{parts[4]}'");
                continue;
            }

            if (!okVpmax)
            {
                logWriter.WriteLine($"Line {lineNumber}: Nevalidan VPmax: '{parts[6]}'");
                continue;
            }

            if (!okVpact)
            {
                logWriter.WriteLine($"Line {lineNumber}: Nevalidan VPact: '{parts[7]}'");
                continue;
            }

            if (!okVpdef)
            {
                logWriter.WriteLine($"Line {lineNumber}: Nevalidan VPdef: '{parts[8]}'");
                continue;
            }

            if (samples.Count < 107)
            {
                samples.Add(new WeatherSample(
                    t,
                    pressure,
                    tpot,
                    tdew,
                    vpmax,
                    vpdef,
                    vpact,
                    date
                ));
            }
            else
            {
                logWriter.WriteLine($"Line {lineNumber}: Višak preko 107 validno parsiranih redova.");
            }
        }

        return samples;
    }
}