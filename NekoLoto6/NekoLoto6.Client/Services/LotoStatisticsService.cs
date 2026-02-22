using NekoLoto6.Client.Models;

namespace NekoLoto6.Client.Services;

public record PatternCount(string Label, int Count, double Percentage);

public record EvenOddResult(List<PatternCount> Patterns, int MaxIndex);

public record HighLowResult(List<PatternCount> Patterns, int MaxIndex);

public record SumRangeResult(
    List<PatternCount> Ranges,
    double Average,
    double Median,
    int Min,
    int Max);

public record ConsecutiveResult(List<PatternCount> Patterns, int MaxIndex);

public static class LotoStatisticsService
{
    /// <summary>偶数・奇数バランス分析</summary>
    public static EvenOddResult AnalyzeEvenOdd(List<LotoResult> results)
    {
        var counts = new int[7]; // index 0〜6: 偶数の個数

        foreach (var r in results)
        {
            var evenCount = r.MainNumbers.Count(n => n % 2 == 0);
            counts[evenCount]++;
        }

        var total = results.Count;
        var patterns = new List<PatternCount>();
        for (var i = 0; i <= 6; i++)
        {
            var label = $"偶{i}:奇{6 - i}";
            var pct = total > 0 ? (double)counts[i] / total * 100 : 0;
            patterns.Add(new PatternCount(label, counts[i], pct));
        }

        var maxIndex = Array.IndexOf(counts, counts.Max());
        return new EvenOddResult(patterns, maxIndex);
    }

    /// <summary>高低バランス分析</summary>
    public static HighLowResult AnalyzeHighLow(List<LotoResult> results)
    {
        var counts = new int[7]; // index 0〜6: 低(1-21)の個数

        foreach (var r in results)
        {
            var lowCount = r.MainNumbers.Count(n => n <= 21);
            counts[lowCount]++;
        }

        var total = results.Count;
        var patterns = new List<PatternCount>();
        for (var i = 0; i <= 6; i++)
        {
            var label = $"低{i}:高{6 - i}";
            var pct = total > 0 ? (double)counts[i] / total * 100 : 0;
            patterns.Add(new PatternCount(label, counts[i], pct));
        }

        var maxIndex = Array.IndexOf(counts, counts.Max());
        return new HighLowResult(patterns, maxIndex);
    }

    /// <summary>合計値の分布分析</summary>
    public static SumRangeResult AnalyzeSumDistribution(List<LotoResult> results)
    {
        var ranges = new (int From, int To)[]
        {
            (21, 50), (51, 80), (81, 110), (111, 140),
            (141, 170), (171, 200), (201, 230), (231, 258)
        };

        var rangeCounts = new int[ranges.Length];
        var sums = new List<int>();

        foreach (var r in results)
        {
            var sum = r.MainNumbers.Sum();
            sums.Add(sum);

            for (var i = 0; i < ranges.Length; i++)
            {
                if (sum >= ranges[i].From && sum <= ranges[i].To)
                {
                    rangeCounts[i]++;
                    break;
                }
            }
        }

        var total = results.Count;
        var patterns = new List<PatternCount>();
        for (var i = 0; i < ranges.Length; i++)
        {
            var label = $"{ranges[i].From}-{ranges[i].To}";
            var pct = total > 0 ? (double)rangeCounts[i] / total * 100 : 0;
            patterns.Add(new PatternCount(label, rangeCounts[i], pct));
        }

        sums.Sort();
        var average = sums.Count > 0 ? sums.Average() : 0;
        var median = sums.Count > 0
            ? sums.Count % 2 == 1
                ? sums[sums.Count / 2]
                : (sums[sums.Count / 2 - 1] + sums[sums.Count / 2]) / 2.0
            : 0;
        var min = sums.Count > 0 ? sums.Min() : 0;
        var max = sums.Count > 0 ? sums.Max() : 0;

        return new SumRangeResult(patterns, average, median, min, max);
    }

    /// <summary>連番（連続数字）分析</summary>
    public static ConsecutiveResult AnalyzeConsecutive(List<LotoResult> results)
    {
        var counts = new int[6]; // index 0〜5: 連番の組数

        foreach (var r in results)
        {
            var sorted = r.MainNumbers.OrderBy(n => n).ToArray();
            var pairs = 0;
            for (var i = 1; i < sorted.Length; i++)
            {
                if (sorted[i] - sorted[i - 1] == 1)
                    pairs++;
            }

            if (pairs < counts.Length)
                counts[pairs]++;
        }

        var total = results.Count;
        var patterns = new List<PatternCount>();
        for (var i = 0; i < counts.Length; i++)
        {
            var label = $"{i}組";
            var pct = total > 0 ? (double)counts[i] / total * 100 : 0;
            patterns.Add(new PatternCount(label, counts[i], pct));
        }

        var maxIndex = Array.IndexOf(counts, counts.Max());
        return new ConsecutiveResult(patterns, maxIndex);
    }
}
