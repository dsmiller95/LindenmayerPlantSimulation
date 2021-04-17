using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dman.LSystem.Packages.Tests.EditMode
{
    public struct Stats
    {
        public double max;
        public double MaxRel => max / domainSize;
        public double min;
        public double MinRel => min / domainSize;
        public double stdDev;
        public double StdDevRel => stdDev / domainSize;
        public double mean;
        public double MeanRel => mean / domainSize;

        public double domainSize;

        public override string ToString()
        {
            return $"min: {MinRel:P1}, max: {MaxRel:P1}, stdev {StdDevRel:P1}, mean {MeanRel:P1}";
        }
    }

    public struct MetaStats
    {
        public Stats minStats;
        public Stats maxStats;
        public Stats stdDevStats;
        public Stats meanStats;

        public double domainSize;
        public override string ToString()
        {
            return $"min: {minStats}\nmax: {maxStats}\nstdev: {stdDevStats}\nmean: {meanStats}";
        }
    }

    public static class StatsUtilities
    {

        public static MetaStats GetMetaStats(this IEnumerable<Stats> values)
        {
            var domainSize = values.First().domainSize;
            return new MetaStats
            {
                minStats = values.Select(x => x.min).GetStats(domainSize),
                maxStats = values.Select(x => x.max).GetStats(domainSize),
                stdDevStats = values.Select(x => x.stdDev).GetStats(domainSize),
                meanStats = values.Select(x => x.mean).GetStats(domainSize),
                domainSize = domainSize
            };
        }

        public static Stats GetStats(this IEnumerable<double> values, double domainSize)
        {
            var min = double.MaxValue;
            var max = double.MinValue;
            foreach (var val in values)
            {
                if (val < min)
                {
                    min = val;
                }
                if (val > max)
                {
                    max = val;
                }
            }

            var stdDev = values.StdDev(out var mean);

            return new Stats
            {
                max = max,
                min = min,
                stdDev = stdDev,
                mean = mean,
                domainSize = domainSize
            };
        }
        public static Stats GetStats(this IEnumerable<uint> values)
        {
            var min = uint.MaxValue;
            var max = uint.MinValue;
            foreach (var val in values)
            {
                if(val < min)
                {
                    min = val;
                }
                if(val > max)
                {
                    max = val;
                }
            }

            var stdDev = values.Select(x => (double)x).StdDev(out var mean);

            return new Stats
            {
                max = max,
                min = min,
                stdDev = stdDev,
                mean = mean,
                domainSize = uint.MaxValue
            };
        }

        // Return the standard deviation of an array of Doubles.
        //
        // If the second argument is True, evaluate as a sample.
        // If the second argument is False, evaluate as a population.
        public static double StdDev(this IEnumerable<double> values,
            out double mean)
        {
            // Get the mean.
            var myMean = mean = values.Sum(x => x) / values.Count();

            // Get the sum of the squares of the differences
            // between the values and the mean.
            var squares_query = values.Select(value => (value - myMean) * (value - myMean));
            double sum_of_squares = squares_query.Sum();

            return Math.Sqrt(sum_of_squares / values.Count());
        }
    }
}
