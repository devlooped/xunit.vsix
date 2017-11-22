using System;
using Xunit.Sdk;

namespace Xunit
{
    [Serializable]
    internal class VsixRunSummary
    {
        /// <summary>
        /// The total number of tests run.
        /// </summary>
        public int Total;

        /// <summary>
        /// The number of failed tests.
        /// </summary>
        public int Failed;

        /// <summary>
        /// The number of skipped tests.
        /// </summary>
        public int Skipped;

        /// <summary>
        /// The total time taken to run the tests, in seconds.
        /// </summary>
        public decimal Time;

        /// <summary>
        /// The exception, if any, that occurred during the run.
        /// </summary>
        public Exception Exception;

        public RunSummary ToRunSummary()
        {
            return new RunSummary
            {
                Total = Total,
                Failed = Failed,
                Skipped = Skipped,
                Time = Time,
            };
        }
    }

    internal static class VsixRunSummaryExtensions
    {
        public static VsixRunSummary ToVsixRunSummary(this RunSummary summary)
        {
            return new VsixRunSummary
            {
                Total = summary.Total,
                Failed = summary.Failed,
                Skipped = summary.Skipped,
                Time = summary.Time,
            };
        }
    }
}
