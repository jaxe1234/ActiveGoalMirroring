namespace DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class StatHelpers
    {
        public static double StandardDeviation(this IEnumerable<double> sequence)
        {
            var list = sequence.ToList();

            double avg = list.Average();
            return Math.Sqrt(list.Average(v => (v - avg) * (v - avg)));
        }

        public static double StandardDeviation(this IEnumerable<int> sequence)
        {
            var list = sequence.ToList();

            double avg = list.Average();
            return Math.Sqrt(list.Average(v => (v - avg) * (v - avg)));
        }
    }
}