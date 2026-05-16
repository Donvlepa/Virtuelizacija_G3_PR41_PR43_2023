using Common;
using System;
using System.Collections.Generic;

public class CsvLoader
{
    public static List<WeatherSample> LoadWeatherSamples(string path)
    {
        try
        {
            using (CsvFileReader reader = new CsvFileReader(path))
            {
                return reader.LoadWeatherSamples();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška pri čitanju CSV fajla: {ex.Message}");
            return new List<WeatherSample>();
        }
    }
}