using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using NekoLoto6.Client.Models;
using NekoLoto6.Client.Services;

if (args.Length < 3)
{
    Console.Error.WriteLine("Usage: ScoreCalculator <csv-path> <output-json-path> <wwwroot-dir>");
    return 1;
}

var csvPath = args[0];
var outputPath = args[1];
var wwwrootDir = args[2];

if (!File.Exists(csvPath))
{
    Console.Error.WriteLine($"[ERROR] CSVファイルが見つかりません: {csvPath}");
    return 1;
}

// --- CSV読み込み ---
Console.WriteLine($"[INFO] CSV読み込み: {csvPath}");
var lines = File.ReadAllLines(csvPath);
var results = new List<LotoResult>();

for (var i = 1; i < lines.Length; i++)
{
    var line = lines[i].Trim();
    if (string.IsNullOrEmpty(line)) continue;
    var cols = line.Split(',');
    if (cols.Length < 20) continue;

    results.Add(new LotoResult
    {
        DrawNumber = int.Parse(cols[0]),
        DrawDate = DateTime.ParseExact(cols[1], "yyyy/MM/dd", CultureInfo.InvariantCulture),
        MainNumbers = new[]
        {
            int.Parse(cols[2]), int.Parse(cols[3]), int.Parse(cols[4]),
            int.Parse(cols[5]), int.Parse(cols[6]), int.Parse(cols[7])
        },
        BonusNumber = int.Parse(cols[8]),
        Prize1Count = int.Parse(cols[9]),
        Prize2Count = int.Parse(cols[10]),
        Prize3Count = int.Parse(cols[11]),
        Prize4Count = int.Parse(cols[12]),
        Prize5Count = int.Parse(cols[13]),
        Prize1Amount = long.Parse(cols[14]),
        Prize2Amount = long.Parse(cols[15]),
        Prize3Amount = long.Parse(cols[16]),
        Prize4Amount = long.Parse(cols[17]),
        Prize5Amount = long.Parse(cols[18]),
        Carryover = long.Parse(cols[19]),
        Sales = cols.Length > 20 ? long.Parse(cols[20]) : 0
    });
}

Console.WriteLine($"[INFO] {results.Count} 件の抽選データを読み込み");

// --- スコア計算 ---
var prediction = LotoPredictionService.GeneratePrediction(results);
var latest = results.MaxBy(r => r.DrawNumber)!;

Console.WriteLine($"[INFO] 最新回号: 第{latest.DrawNumber}回 ({latest.DrawDate:yyyy/MM/dd})");
Console.WriteLine($"[INFO] おすすめ数字: {string.Join(", ", prediction.RecommendedNumbers)}");

// --- JSON出力 ---
var scores = new Dictionary<string, object>();
foreach (var num in prediction.RecommendedNumbers)
{
    var s = prediction.AllScores.First(x => x.Number == num);
    scores[num.ToString()] = new
    {
        total = Math.Round(s.TotalScore, 3),
        frequency = Math.Round(s.FrequencyScore, 3),
        trend = Math.Round(s.TrendScore, 3),
        interval = Math.Round(s.IntervalScore, 3),
        balance = Math.Round(s.BalanceScore, 3),
        carryover = Math.Round(s.CarryoverScore, 3),
        reason = LotoPredictionService.GetReasonText(s)
    };
}

var allScores = prediction.AllScores.Select(s => new
{
    number = s.Number,
    totalScore = Math.Round(s.TotalScore, 3),
    frequencyScore = Math.Round(s.FrequencyScore, 3),
    trendScore = Math.Round(s.TrendScore, 3),
    intervalScore = Math.Round(s.IntervalScore, 3),
    balanceScore = Math.Round(s.BalanceScore, 3),
    carryoverScore = Math.Round(s.CarryoverScore, 3),
    totalAppearances = s.TotalAppearances,
    recentAppearances = s.RecentAppearances,
    drawsSinceLastAppearance = s.DrawsSinceLastAppearance,
    isCarriedOver = s.IsCarriedOver
}).ToList();

var lastUpdated = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(9)).ToString("yyyy-MM-ddTHH:mm:sszzz");

var output = new
{
    lastUpdated,
    totalDrawings = results.Count,
    recentDraws = prediction.RecentDraws,
    latestDrawing = new
    {
        round = latest.DrawNumber,
        date = latest.DrawDate.ToString("yyyy-MM-dd"),
        numbers = latest.MainNumbers,
        bonus = latest.BonusNumber
    },
    recommendation = new
    {
        numbers = prediction.RecommendedNumbers,
        scores
    },
    allScores
};

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never
};

var json = JsonSerializer.Serialize(output, jsonOptions);
var dir = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
File.WriteAllText(outputPath, json);

Console.WriteLine($"[INFO] prediction.json 出力完了: {outputPath}");

// --- HTML生成 ---
Console.WriteLine($"[INFO] HTML生成開始: {wwwrootDir}");
HtmlGenerator.GenerateAll(wwwrootDir, results, prediction, latest, lastUpdated);
Console.WriteLine("[INFO] HTML生成完了: index.html, frequency.html, statistics.html, algorithm.html, 404.html");

return 0;
