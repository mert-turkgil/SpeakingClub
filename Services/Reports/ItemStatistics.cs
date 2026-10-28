using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeakingClub.Services.Reports
{
    /// <summary>
    /// Classical test theory, as used by Moodle's quiz statistics and Canvas's item analysis. Pure functions, no
    /// database: tested in tests/SpeakingClub.Tests. Variances are population variances (divide by n), like KR-20.
    /// </summary>
    public static class ItemStatistics
    {
        public static double Mean(IReadOnlyList<double> x) => x.Count == 0 ? double.NaN : x.Average();

        public static double Variance(IReadOnlyList<double> x)
        {
            if (x.Count == 0) return double.NaN;
            var mean = x.Average();
            return x.Sum(v => (v - mean) * (v - mean)) / x.Count;
        }

        public static double StandardDeviation(IReadOnlyList<double> x) => Math.Sqrt(Variance(x));

        /// <summary>Pearson correlation; null when there are fewer than 2 values or either side never varies.</summary>
        public static double? Pearson(IReadOnlyList<double> x, IReadOnlyList<double> y)
        {
            if (x.Count != y.Count) throw new ArgumentException("Both lists need the same length.");
            var n = x.Count;
            if (n < 2) return null;
            double mx = x.Average(), my = y.Average(), sxy = 0, sxx = 0, syy = 0;
            for (var i = 0; i < n; i++)
            {
                var dx = x[i] - mx;
                var dy = y[i] - my;
                sxy += dx * dy;
                sxx += dx * dx;
                syy += dy * dy;
            }
            if (sxx <= 1e-12 || syy <= 1e-12) return null;
            return sxy / Math.Sqrt(sxx * syy);
        }

        /// <summary>Quantile of sorted values with linear interpolation (R type 7, Excel's PERCENTILE.INC).</summary>
        public static double Quantile(IReadOnlyList<double> sorted, double q)
        {
            if (sorted.Count == 0) return double.NaN;
            if (sorted.Count == 1) return sorted[0];
            var h = (sorted.Count - 1) * Math.Clamp(q, 0, 1);
            var lo = (int)Math.Floor(h);
            var hi = Math.Min(lo + 1, sorted.Count - 1);
            return sorted[lo] + (h - lo) * (sorted[hi] - sorted[lo]);
        }

        public static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            return Quantile(sorted, 0.5);
        }

        public record BoxStats(int N, double Min, double Q1, double Median, double Q3, double Max, double Mean, double Sd);

        public static BoxStats? Box(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0) return null;
            return new BoxStats(sorted.Count, sorted[0], Quantile(sorted, 0.25), Quantile(sorted, 0.5), Quantile(sorted, 0.75),
                sorted[^1], sorted.Average(), StandardDeviation(sorted));
        }

        /// <summary>
        /// Kuder–Richardson 20 reliability of a test of right/wrong items: k/(k−1) · (1 − Σ p·q / σ²(total)).
        /// Rows are attempts, columns items (0 or 1). Null with fewer than 2 items or 2 attempts, or no score spread.
        /// </summary>
        public static double? Kr20(IReadOnlyList<int[]> rows)
        {
            if (rows.Count < 2) return null;
            var k = rows[0].Length;
            if (k < 2) return null;
            var totals = rows.Select(r => (double)r.Sum()).ToList();
            var totalVariance = Variance(totals);
            if (totalVariance <= 1e-12) return null;
            double sumPq = 0;
            for (var j = 0; j < k; j++)
            {
                var p = rows.Average(r => (double)r[j]);
                sumPq += p * (1 - p);
            }
            return k / (k - 1.0) * (1 - sumPq / totalVariance);
        }

        /// <summary>
        /// Standard error of measurement: σ · √(1 − reliability); in the unit of σ. A negative KR-20 (very inconsistent
        /// answers) counts as 0, so the error never exceeds the spread of the scores.
        /// </summary>
        public static double? Sem(double sd, double? reliability) =>
            reliability is { } r && !double.IsNaN(sd) ? sd * Math.Sqrt(1 - Math.Clamp(r, 0, 1)) : null;

        /// <summary>Indices of the top and bottom groups (27 % each) when attempts are ranked by total score.</summary>
        public static (List<int> Upper, List<int> Lower) UpperLowerGroups(IReadOnlyList<double> totals, IReadOnlyList<int> tieBreak)
        {
            var n = totals.Count;
            if (n < 2) return (new List<int>(), new List<int>());
            var size = Math.Max(1, (int)Math.Round(0.27 * n, MidpointRounding.AwayFromZero));
            var ranked = Enumerable.Range(0, n).OrderByDescending(i => totals[i]).ThenBy(i => tieBreak[i]).ToList();
            return (ranked.Take(size).ToList(), ranked.Skip(n - size).ToList());
        }

        /// <summary>
        /// An answer given faster than this many seconds is a rapid guess (Wise &amp; Kong 2005; normative threshold
        /// of Wise &amp; Ma 2012: 10 % of the question's mean time, never below 3 seconds).
        /// </summary>
        public static double RapidThreshold(double meanSeconds) => Math.Max(3, 0.10 * meanSeconds);

        /// <summary>
        /// Guttman errors of one answer pattern, as a share of the most that are possible: a harder question right
        /// while an easier one is wrong. 0 = perfectly consistent; high values suggest guessing or copied answers.
        /// <paramref name="byEasiness"/> holds 1/0 for the questions ordered from easiest to hardest.
        /// </summary>
        public static double? GuttmanErrorRate(IReadOnlyList<int> byEasiness)
        {
            var k = byEasiness.Count;
            var right = byEasiness.Sum();
            var max = right * (k - right);
            if (max == 0) return null;
            var errors = 0;
            var wrongSoFar = 0;
            foreach (var score in byEasiness)
            {
                if (score == 1) errors += wrongSoFar;   // right on this (harder) one after easier ones were wrong
                else wrongSoFar++;
            }
            return errors / (double)max;
        }
    }
}
