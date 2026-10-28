using SpeakingClub.Services.Reports;

namespace SpeakingClub.Tests;

// Expected values are worked out by hand (and match Excel / R where a function exists there)
public class ItemStatisticsTests
{
    private const int Digits = 10;

    [Fact]
    public void Mean_variance_and_sd_are_population_figures()
    {
        double[] x = { 2, 4, 4, 4, 5, 5, 7, 9 };
        Assert.Equal(5, ItemStatistics.Mean(x), Digits);
        Assert.Equal(4, ItemStatistics.Variance(x), Digits);          // Σ(x−5)² = 32, / 8
        Assert.Equal(2, ItemStatistics.StandardDeviation(x), Digits);
        Assert.True(double.IsNaN(ItemStatistics.Mean(Array.Empty<double>())));
    }

    [Fact]
    public void Pearson_handles_perfect_and_undefined_correlations()
    {
        Assert.Equal(1, ItemStatistics.Pearson(new double[] { 1, 2, 3, 4, 5 }, new double[] { 2, 4, 6, 8, 10 })!.Value, Digits);
        Assert.Equal(-1, ItemStatistics.Pearson(new double[] { 1, 2, 3 }, new double[] { 3, 2, 1 })!.Value, Digits);
        Assert.Null(ItemStatistics.Pearson(new double[] { 1, 2, 3 }, new double[] { 5, 5, 5 }));   // no spread
        Assert.Null(ItemStatistics.Pearson(new double[] { 1 }, new double[] { 2 }));
        Assert.Throws<ArgumentException>(() => ItemStatistics.Pearson(new double[] { 1, 2 }, new double[] { 1 }));
    }

    [Theory]
    [InlineData(0.00, 1.00)]
    [InlineData(0.25, 1.75)]   // Excel: PERCENTILE.INC({1;2;3;4}; 0,25)
    [InlineData(0.50, 2.50)]
    [InlineData(0.75, 3.25)]
    [InlineData(1.00, 4.00)]
    public void Quantile_interpolates_like_excel(double q, double expected) =>
        Assert.Equal(expected, ItemStatistics.Quantile(new double[] { 1, 2, 3, 4 }, q), Digits);

    [Fact]
    public void Box_summarises_unsorted_values()
    {
        var box = ItemStatistics.Box(new double[] { 9, 1, 5, 3, 7 })!;
        Assert.Equal(5, box.N);
        Assert.Equal((1.0, 3.0, 5.0, 7.0, 9.0), (box.Min, box.Q1, box.Median, box.Q3, box.Max));
        Assert.Equal(5, box.Mean, Digits);
        Assert.Null(ItemStatistics.Box(Array.Empty<double>()));
    }

    [Fact]
    public void Kr20_matches_the_worked_example()
    {
        // 4 attempts × 3 items: p = .75/.50/.25 → Σpq = .625; totals 3,2,1,0 → σ² = 1.25; KR-20 = 3/2 · (1 − .5) = .75
        var rows = new List<int[]> { new[] { 1, 1, 1 }, new[] { 1, 1, 0 }, new[] { 1, 0, 0 }, new[] { 0, 0, 0 } };
        Assert.Equal(0.75, ItemStatistics.Kr20(rows)!.Value, Digits);
    }

    [Fact]
    public void Kr20_is_undefined_without_spread_or_items()
    {
        Assert.Null(ItemStatistics.Kr20(new List<int[]> { new[] { 1, 0 }, new[] { 0, 1 } }));   // both totals 1
        Assert.Null(ItemStatistics.Kr20(new List<int[]> { new[] { 1 }, new[] { 0 } }));         // one item
        Assert.Null(ItemStatistics.Kr20(new List<int[]> { new[] { 1, 0 } }));                   // one attempt
    }

    [Fact]
    public void Sem_shrinks_with_reliability()
    {
        Assert.Equal(5, ItemStatistics.Sem(10, 0.75)!.Value, Digits);   // 10 · √(1 − .75)
        Assert.Equal(10, ItemStatistics.Sem(10, -0.2)!.Value, Digits);  // negative reliability counts as 0
        Assert.Null(ItemStatistics.Sem(10, null));
    }

    [Fact]
    public void Upper_and_lower_groups_take_27_percent_each()
    {
        var totals = new double[] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
        var ids = Enumerable.Range(0, 10).ToList();
        var (upper, lower) = ItemStatistics.UpperLowerGroups(totals, ids);
        Assert.Equal(new[] { 0, 1, 2 }, upper);   // round(2.7) = 3
        Assert.Equal(new[] { 7, 8, 9 }, lower);
    }

    [Fact]
    public void Upper_and_lower_groups_break_ties_by_the_given_order()
    {
        var (upper, lower) = ItemStatistics.UpperLowerGroups(new double[] { 5, 5, 5, 5 }, new[] { 40, 10, 30, 20 });
        Assert.Equal(new[] { 1 }, upper);   // round(1.08) = 1; lowest tie-break first
        Assert.Equal(new[] { 0 }, lower);
    }

    [Theory]
    [InlineData(20, 3)]    // 10 % of 20 s is 2 s, but never below 3 s
    [InlineData(60, 6)]
    public void Rapid_threshold_is_ten_percent_of_mean_time_at_least_three_seconds(double mean, double expected) =>
        Assert.Equal(expected, ItemStatistics.RapidThreshold(mean), Digits);

    [Theory]
    [InlineData(new[] { 1, 1, 0, 0 }, 0.0)]    // easy ones right, hard ones wrong: consistent
    [InlineData(new[] { 0, 0, 1, 1 }, 1.0)]    // the reverse: every possible error
    [InlineData(new[] { 1, 0, 1, 0 }, 0.25)]   // one error out of 2 · 2
    public void Guttman_error_rate(int[] easiestFirst, double expected) =>
        Assert.Equal(expected, ItemStatistics.GuttmanErrorRate(easiestFirst)!.Value, Digits);

    [Fact]
    public void Guttman_error_rate_is_undefined_for_all_right_or_all_wrong()
    {
        Assert.Null(ItemStatistics.GuttmanErrorRate(new[] { 1, 1, 1 }));
        Assert.Null(ItemStatistics.GuttmanErrorRate(new[] { 0, 0, 0 }));
    }
}
