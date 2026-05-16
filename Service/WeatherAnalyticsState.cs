using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;

namespace Service
{
    internal class AnalyticsSettings
    {
        public double PThreshold { get; private set; }
        public double VPactThreshold { get; private set; }
        public double VPdefThreshold { get; private set; }
        public double OutOfBandPercentage { get; private set; }

        public double LowerOutOfBandFactor
        {
            get { return 1.0 - (OutOfBandPercentage / 100.0); }
        }

        public double UpperOutOfBandFactor
        {
            get { return 1.0 + (OutOfBandPercentage / 100.0); }
        }

        public static AnalyticsSettings LoadFromConfiguration()
        {
            return new AnalyticsSettings
            {
                PThreshold = ReadDoubleFromConfig("P_threshold", 5.0),
                VPactThreshold = ReadDoubleFromConfig("VPact_threshold", 2.0),
                VPdefThreshold = ReadDoubleFromConfig("VPdef_threshold", 2.0),
                OutOfBandPercentage = ReadDoubleFromConfig("OutOfBandPercentage", 25.0)
            };
        }

        private static double ReadDoubleFromConfig(string key, double defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            double result;
            if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                return defaultValue;
            }

            return result;
        }
    }

    internal class WeatherAnalyticsState
    {
        private WeatherSample previousSample;
        private int pressureCount;
        private double pressureMean;

        public double PressureMean
        {
            get { return pressureMean; }
        }

        public void Reset()
        {
            previousSample = null;
            pressureCount = 0;
            pressureMean = 0;
        }

        public List<WarningRaisedEventArgs> AnalyzeSample(WeatherSample sample, AnalyticsSettings settings)
        {
            List<WarningRaisedEventArgs> warnings = new List<WarningRaisedEventArgs>();

            if (sample == null || settings == null)
            {
                return warnings;
            }

            AnalyzePressureSpike(sample, settings, warnings);
            AnalyzePressureOutOfBand(sample, settings, warnings);
            AnalyzeVPactSpike(sample, settings, warnings);
            AnalyzeVPdefSpike(sample, settings, warnings);

            UpdatePressureMean(sample.Pressure);
            previousSample = sample;

            return warnings;
        }

        private void AnalyzePressureSpike(WeatherSample sample, AnalyticsSettings settings, List<WarningRaisedEventArgs> warnings)
        {
            if (previousSample == null)
            {
                return;
            }

            double deltaP = sample.Pressure - previousSample.Pressure;

            if (Math.Abs(deltaP) > settings.PThreshold)
            {
                string direction = GetDirection(deltaP);
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Nagla promena pritiska: ΔP = {0}, prag = {1}, smer = {2}.",
                    deltaP,
                    settings.PThreshold,
                    direction);

                warnings.Add(new WarningRaisedEventArgs(
                    "PressureSpike",
                    direction,
                    message,
                    sample.Date,
                    sample.Pressure,
                    previousSample.Pressure,
                    deltaP,
                    settings.PThreshold));
            }
        }

        private void AnalyzePressureOutOfBand(WeatherSample sample, AnalyticsSettings settings, List<WarningRaisedEventArgs> warnings)
        {
            if (pressureCount == 0)
            {
                return;
            }

            double lowerLimit = pressureMean * settings.LowerOutOfBandFactor;
            double upperLimit = pressureMean * settings.UpperOutOfBandFactor;

            if (sample.Pressure < lowerLimit || sample.Pressure > upperLimit)
            {
                string direction = sample.Pressure < lowerLimit ? "ispod očekivane vrednosti" : "iznad očekivane vrednosti";
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Pritisak je van opsega: P = {0}, Pmean = {1}, dozvoljeno = [{2}, {3}], smer = {4}.",
                    sample.Pressure,
                    pressureMean,
                    lowerLimit,
                    upperLimit,
                    direction);

                warnings.Add(new WarningRaisedEventArgs(
                    "OutOfBandWarning",
                    direction,
                    message,
                    sample.Date,
                    sample.Pressure,
                    pressureMean,
                    sample.Pressure - pressureMean,
                    settings.OutOfBandPercentage));
            }
        }

        private void AnalyzeVPactSpike(WeatherSample sample, AnalyticsSettings settings, List<WarningRaisedEventArgs> warnings)
        {
            if (previousSample == null)
            {
                return;
            }

            double deltaVPact = sample.VPact - previousSample.VPact;

            if (Math.Abs(deltaVPact) > settings.VPactThreshold)
            {
                string direction = GetDirection(deltaVPact);
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Nagla promena stvarne parne vrednosti: ΔVPact = {0}, prag = {1}, smer = {2}.",
                    deltaVPact,
                    settings.VPactThreshold,
                    direction);

                warnings.Add(new WarningRaisedEventArgs(
                    "VPactSpike",
                    direction,
                    message,
                    sample.Date,
                    sample.VPact,
                    previousSample.VPact,
                    deltaVPact,
                    settings.VPactThreshold));
            }
        }

        private void AnalyzeVPdefSpike(WeatherSample sample, AnalyticsSettings settings, List<WarningRaisedEventArgs> warnings)
        {
            if (previousSample == null)
            {
                return;
            }

            double deltaVPdef = sample.VPdef - previousSample.VPdef;

            if (Math.Abs(deltaVPdef) > settings.VPdefThreshold)
            {
                string direction = GetDirection(deltaVPdef);
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Nagla promena suvoće vazduha: ΔVPdef = {0}, prag = {1}, smer = {2}.",
                    deltaVPdef,
                    settings.VPdefThreshold,
                    direction);

                warnings.Add(new WarningRaisedEventArgs(
                    "VPdefSpike",
                    direction,
                    message,
                    sample.Date,
                    sample.VPdef,
                    previousSample.VPdef,
                    deltaVPdef,
                    settings.VPdefThreshold));
            }
        }

        private void UpdatePressureMean(double pressure)
        {
            pressureCount++;
            pressureMean = pressureMean + ((pressure - pressureMean) / pressureCount);
        }

        private static string GetDirection(double delta)
        {
            return delta > 0 ? "iznad očekivanog" : "ispod očekivanog";
        }
    }
}
