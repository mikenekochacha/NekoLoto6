namespace NekoLoto6.Client.Models;

public class PredictionData
{
    public string LastUpdated { get; set; } = "";
    public int TotalDrawings { get; set; }
    public int RecentDraws { get; set; }
    public LatestDrawingData LatestDrawing { get; set; } = new();
    public RecommendationData Recommendation { get; set; } = new();
    public List<ScoreData> AllScores { get; set; } = new();
}

public class LatestDrawingData
{
    public int Round { get; set; }
    public string Date { get; set; } = "";
    public int[] Numbers { get; set; } = Array.Empty<int>();
    public int Bonus { get; set; }
}

public class RecommendationData
{
    public List<int> Numbers { get; set; } = new();
    public Dictionary<string, RecommendedScoreData> Scores { get; set; } = new();
}

public class RecommendedScoreData
{
    public double Total { get; set; }
    public double Frequency { get; set; }
    public double Trend { get; set; }
    public double Interval { get; set; }
    public double Balance { get; set; }
    public double Carryover { get; set; }
    public string Reason { get; set; } = "";
}

public class ScoreData
{
    public int Number { get; set; }
    public double TotalScore { get; set; }
    public double FrequencyScore { get; set; }
    public double TrendScore { get; set; }
    public double IntervalScore { get; set; }
    public double BalanceScore { get; set; }
    public double CarryoverScore { get; set; }
    public int TotalAppearances { get; set; }
    public int RecentAppearances { get; set; }
    public int DrawsSinceLastAppearance { get; set; }
    public bool IsCarriedOver { get; set; }
}
