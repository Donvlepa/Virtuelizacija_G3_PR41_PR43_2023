using Common;
using System;

namespace Service
{
    public class TransferStartedEventArgs : EventArgs
    {
        public TransferStartedEventArgs(DateTime startedAt, string sessionFolder, SessionMeta meta)
        {
            StartedAt = startedAt;
            SessionFolder = sessionFolder;
            Meta = meta;
        }

        public DateTime StartedAt { get; private set; }
        public string SessionFolder { get; private set; }
        public SessionMeta Meta { get; private set; }
    }

    public class SampleReceivedEventArgs : EventArgs
    {
        public SampleReceivedEventArgs(WeatherSample sample, int sampleNumber, double pressureMean)
        {
            Sample = sample;
            SampleNumber = sampleNumber;
            PressureMean = pressureMean;
        }

        public WeatherSample Sample { get; private set; }
        public int SampleNumber { get; private set; }
        public double PressureMean { get; private set; }
    }

    public class TransferCompletedEventArgs : EventArgs
    {
        public TransferCompletedEventArgs(DateTime completedAt, int totalSamples)
        {
            CompletedAt = completedAt;
            TotalSamples = totalSamples;
        }

        public DateTime CompletedAt { get; private set; }
        public int TotalSamples { get; private set; }
    }

    public class WarningRaisedEventArgs : EventArgs
    {
        public WarningRaisedEventArgs(
            string warningType,
            string direction,
            string message,
            DateTime sampleDate,
            double currentValue,
            double referenceValue,
            double delta,
            double threshold)
        {
            WarningType = warningType;
            Direction = direction;
            Message = message;
            SampleDate = sampleDate;
            CurrentValue = currentValue;
            ReferenceValue = referenceValue;
            Delta = delta;
            Threshold = threshold;
        }

        public string WarningType { get; private set; }
        public string Direction { get; private set; }
        public string Message { get; private set; }
        public DateTime SampleDate { get; private set; }
        public double CurrentValue { get; private set; }
        public double ReferenceValue { get; private set; }
        public double Delta { get; private set; }
        public double Threshold { get; private set; }
    }
}
