using NekoLoto6.Client.Models;

namespace NekoLoto6.Client.Services;

public class PredictionResult
{
    public List<int> RecommendedNumbers { get; set; } = new();
    public List<NumberScore> AllScores { get; set; } = new();
    public string AnalysisDate { get; set; } = "";
    public int TotalDraws { get; set; }
    public int RecentDraws { get; set; }
}

public class NumberScore
{
    public int Number { get; set; }
    public double FrequencyScore { get; set; }
    public double TrendScore { get; set; }
    public double IntervalScore { get; set; }
    public double BalanceScore { get; set; }
    public double CarryoverScore { get; set; }
    public double TotalScore { get; set; }
    public int TotalAppearances { get; set; }
    public int RecentAppearances { get; set; }
    public int DrawsSinceLastAppearance { get; set; }
    public bool IsCarriedOver { get; set; }
}

public static class LotoPredictionService
{
    private const int RecentCount = 50;
    private const double WeightFrequency = 0.20;
    private const double WeightTrend = 0.30;
    private const double WeightInterval = 0.20;
    private const double WeightBalance = 0.10;
    private const double WeightCarryover = 0.20;

    public static PredictionResult GeneratePrediction(List<LotoResult> results)
    {
        var totalDraws = results.Count;
        var recentResults = results.OrderByDescending(r => r.DrawNumber).Take(RecentCount).ToList();
        var latestDraw = results.MaxBy(r => r.DrawNumber);

        var scores = new NumberScore[44]; // index 1〜43
        for (var num = 1; num <= 43; num++)
            scores[num] = new NumberScore { Number = num };

        // 全期間の出現回数
        foreach (var result in results)
            foreach (var num in result.MainNumbers)
                if (num >= 1 && num <= 43)
                    scores[num].TotalAppearances++;

        // 直近50回の出現回数
        foreach (var result in recentResults)
            foreach (var num in result.MainNumbers)
                if (num >= 1 && num <= 43)
                    scores[num].RecentAppearances++;

        // 最終出現からの間隔
        var orderedResults = results.OrderByDescending(r => r.DrawNumber).ToList();
        for (var num = 1; num <= 43; num++)
        {
            var found = false;
            for (var i = 0; i < orderedResults.Count; i++)
            {
                if (orderedResults[i].MainNumbers.Contains(num))
                {
                    scores[num].DrawsSinceLastAppearance = i;
                    found = true;
                    break;
                }
            }
            if (!found)
                scores[num].DrawsSinceLastAppearance = totalDraws;
        }

        // スコア1: 出現頻度スコア（正規化）
        var allAppearances = Enumerable.Range(1, 43).Select(n => scores[n].TotalAppearances).ToArray();
        var minFreq = allAppearances.Min();
        var maxFreq = allAppearances.Max();
        for (var num = 1; num <= 43; num++)
            scores[num].FrequencyScore = maxFreq > minFreq
                ? (double)(scores[num].TotalAppearances - minFreq) / (maxFreq - minFreq)
                : 0.5;

        // スコア2: 直近トレンドスコア（正規化）
        var recentAppearances = Enumerable.Range(1, 43).Select(n => scores[n].RecentAppearances).ToArray();
        var minRecent = recentAppearances.Min();
        var maxRecent = recentAppearances.Max();
        for (var num = 1; num <= 43; num++)
            scores[num].TrendScore = maxRecent > minRecent
                ? (double)(scores[num].RecentAppearances - minRecent) / (maxRecent - minRecent)
                : 0.5;

        // スコア3: 出目間隔スコア（正規化、間隔が大きいほど高い）
        var intervals = Enumerable.Range(1, 43).Select(n => scores[n].DrawsSinceLastAppearance).ToArray();
        var minInterval = intervals.Min();
        var maxInterval = intervals.Max();
        for (var num = 1; num <= 43; num++)
            scores[num].IntervalScore = maxInterval > minInterval
                ? (double)(scores[num].DrawsSinceLastAppearance - minInterval) / (maxInterval - minInterval)
                : 0.5;

        // スコア4: バランス補正（初期値0.5、選出後に調整）
        for (var num = 1; num <= 43; num++)
            scores[num].BalanceScore = 0.5;

        // スコア5: 引き継ぎ傾向スコア
        var lastNumbers = new HashSet<int>(latestDraw?.MainNumbers ?? Array.Empty<int>());
        var carryCount = new int[44];
        var carryAppear = new int[44];
        for (var i = 0; i < orderedResults.Count - 1; i++)
        {
            var prev = new HashSet<int>(orderedResults[i + 1].MainNumbers);
            var curr = orderedResults[i].MainNumbers;
            foreach (var n in prev)
            {
                if (n >= 1 && n <= 43)
                {
                    carryAppear[n]++;
                    if (curr.Contains(n))
                        carryCount[n]++;
                }
            }
        }

        var carryRates = new double[44];
        for (var num = 1; num <= 43; num++)
            carryRates[num] = carryAppear[num] > 0 ? (double)carryCount[num] / carryAppear[num] : 0;
        var avgCarryRate = Enumerable.Range(1, 43).Average(n => carryRates[n]);

        for (var num = 1; num <= 43; num++)
        {
            scores[num].IsCarriedOver = lastNumbers.Contains(num);
            if (scores[num].IsCarriedOver && avgCarryRate > 0)
                scores[num].CarryoverScore = Math.Clamp(0.7 * (carryRates[num] / avgCarryRate), 0.0, 1.0);
            else
                scores[num].CarryoverScore = 0.0;
        }

        // 仮の合計スコア（バランス補正前）
        for (var num = 1; num <= 43; num++)
            scores[num].TotalScore =
                scores[num].FrequencyScore * WeightFrequency +
                scores[num].TrendScore * WeightTrend +
                scores[num].IntervalScore * WeightInterval +
                scores[num].BalanceScore * WeightBalance +
                scores[num].CarryoverScore * WeightCarryover;

        // 上位候補から6数字を選出（バランス補正を適用）
        var selected = SelectWithBalance(scores);

        // 選出された数字のバランススコアを更新
        UpdateBalanceScores(scores, selected);

        // 最終スコア再計算
        for (var num = 1; num <= 43; num++)
            scores[num].TotalScore =
                scores[num].FrequencyScore * WeightFrequency +
                scores[num].TrendScore * WeightTrend +
                scores[num].IntervalScore * WeightInterval +
                scores[num].BalanceScore * WeightBalance +
                scores[num].CarryoverScore * WeightCarryover;

        var allScores = Enumerable.Range(1, 43)
            .Select(n => scores[n])
            .OrderByDescending(s => s.TotalScore)
            .ToList();

        return new PredictionResult
        {
            RecommendedNumbers = selected.OrderBy(n => n).ToList(),
            AllScores = allScores,
            AnalysisDate = latestDraw?.DrawDate.ToString("yyyy/MM/dd") ?? "",
            TotalDraws = totalDraws,
            RecentDraws = Math.Min(RecentCount, totalDraws)
        };
    }

    private static List<int> SelectWithBalance(NumberScore[] scores)
    {
        var candidates = Enumerable.Range(1, 43)
            .OrderByDescending(n => scores[n].TotalScore)
            .ToList();

        var selected = new List<int>();
        var evenCount = 0;
        var oddCount = 0;
        var lowCount = 0;  // 1-21
        var highCount = 0; // 22-43

        foreach (var num in candidates)
        {
            if (selected.Count >= 6) break;

            var isEven = num % 2 == 0;
            var isLow = num <= 21;

            // バランスチェック: 偶数3:奇数3、低3:高3 に近づける
            if (isEven && evenCount >= 4) continue;
            if (!isEven && oddCount >= 4) continue;
            if (isLow && lowCount >= 4) continue;
            if (!isLow && highCount >= 4) continue;

            selected.Add(num);
            if (isEven) evenCount++; else oddCount++;
            if (isLow) lowCount++; else highCount++;
        }

        // 6個に満たない場合は残りから補充
        if (selected.Count < 6)
        {
            foreach (var num in candidates)
            {
                if (selected.Count >= 6) break;
                if (!selected.Contains(num))
                    selected.Add(num);
            }
        }

        return selected;
    }

    public static string GetReasonText(NumberScore score)
    {
        var indicators = new (string key, double value)[]
        {
            ("frequency", score.FrequencyScore),
            ("trend", score.TrendScore),
            ("interval", score.IntervalScore),
            ("balance", score.BalanceScore),
            ("carryover", score.CarryoverScore)
        };

        var sorted = indicators.OrderByDescending(s => s.value).ToArray();
        var top = sorted[0].key;
        var second = sorted[1].key;
        var hasStrongSecond = sorted[0].value - sorted[1].value < 0.15;

        if (hasStrongSecond)
        {
            var pair = (top, second);
            if (pair is ("trend", "frequency") or ("frequency", "trend"))
                return "直近50回で勢いがあり、全期間でも安定して出現";
            if (pair is ("trend", "interval") or ("interval", "trend"))
                return "直近の勢いがあり、出目間隔的にも好タイミング";
            if (pair is ("frequency", "interval") or ("interval", "frequency"))
                return "安定した出現率で、出目間隔的にも期待大";
            if (pair is ("frequency", "balance") or ("balance", "frequency"))
                return "全期間で安定しており、バランスにも優れた数字";
            if (pair is ("trend", "balance") or ("balance", "trend"))
                return "直近の勢いとバランスを兼ね備えた数字";
            if (pair is ("interval", "balance") or ("balance", "interval"))
                return "出目間隔とバランスの両面で期待できる数字";
            if (pair is ("carryover", "trend") or ("trend", "carryover"))
                return "前回からの引き継ぎ傾向が強く、直近でも勢いあり";
            if (pair is ("carryover", "frequency") or ("frequency", "carryover"))
                return "前回から引き継がれやすく、全期間でも安定した数字";
            if (pair is ("carryover", "interval") or ("interval", "carryover"))
                return "前回からの引き継ぎと出目間隔の両面で期待大";
            if (pair is ("carryover", "balance") or ("balance", "carryover"))
                return "前回からの引き継ぎ傾向とバランスを兼備";
        }

        return top switch
        {
            "frequency" => "全期間での出現率が高く、安定した数字",
            "trend" => "直近50回で特に勢いがある注目数字",
            "interval" => "しばらく出ていないため、そろそろ出る可能性",
            "balance" => "バランスが非常に良く、総合力が高い数字",
            "carryover" => "前回も出現しており、引き継がれやすい傾向の数字",
            _ => ""
        };
    }

    private static void UpdateBalanceScores(NumberScore[] scores, List<int> selected)
    {
        var evenInSelected = selected.Count(n => n % 2 == 0);
        var lowInSelected = selected.Count(n => n <= 21);

        for (var num = 1; num <= 43; num++)
        {
            var bonus = 0.0;
            var isEven = num % 2 == 0;
            var isLow = num <= 21;

            // 偶奇バランス: 3:3が理想
            if (isEven && evenInSelected <= 3) bonus += 0.25;
            if (!isEven && (6 - evenInSelected) <= 3) bonus += 0.25;

            // 高低バランス: 3:3が理想
            if (isLow && lowInSelected <= 3) bonus += 0.25;
            if (!isLow && (6 - lowInSelected) <= 3) bonus += 0.25;

            scores[num].BalanceScore = Math.Min(1.0, 0.5 + bonus);
        }
    }
}
