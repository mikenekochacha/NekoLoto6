using System.Globalization;
using NekoLoto6.Client.Models;

namespace NekoLoto6.Client.Services;

public class LotoCsvParser
{
    private readonly HttpClient _httpClient;

    public LotoCsvParser(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<LotoResult>> LoadResultsAsync()
    {
        var csv = await _httpClient.GetStringAsync("data/LOTO6_ALL.csv");
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var results = new List<LotoResult>();

        // 1行目はヘッダーなのでスキップ
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            var cols = line.Split(',');
            if (cols.Length < 20)
                continue;

            var result = new LotoResult
            {
                DrawNumber = int.Parse(cols[0]),
                DrawDate = DateTime.ParseExact(cols[1], "yyyy/MM/dd", CultureInfo.InvariantCulture),
                MainNumbers = new[]
                {
                    int.Parse(cols[2]),
                    int.Parse(cols[3]),
                    int.Parse(cols[4]),
                    int.Parse(cols[5]),
                    int.Parse(cols[6]),
                    int.Parse(cols[7])
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
            };

            results.Add(result);
        }

        return results;
    }
}
