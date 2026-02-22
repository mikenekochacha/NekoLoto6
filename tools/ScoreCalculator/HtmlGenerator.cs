using System.Globalization;
using System.Text;
using NekoLoto6.Client.Models;
using NekoLoto6.Client.Services;

public static class HtmlGenerator
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private const string SiteBase = "https://nekocode.tech/";

    public static void GenerateAll(
        string wwwrootDir,
        List<LotoResult> results,
        PredictionResult prediction,
        LotoResult latest,
        string lastUpdated)
    {
        var totalDrawings = results.Count;

        File.WriteAllText(
            Path.Combine(wwwrootDir, "index.html"),
            GenerateIndexPage(results, prediction, latest, lastUpdated),
            Encoding.UTF8);

        File.WriteAllText(
            Path.Combine(wwwrootDir, "frequency.html"),
            GenerateFrequencyPage(results),
            Encoding.UTF8);

        File.WriteAllText(
            Path.Combine(wwwrootDir, "statistics.html"),
            GenerateStatisticsPage(results),
            Encoding.UTF8);

        File.WriteAllText(
            Path.Combine(wwwrootDir, "algorithm.html"),
            GenerateAlgorithmPage(results),
            Encoding.UTF8);

        File.WriteAllText(
            Path.Combine(wwwrootDir, "404.html"),
            Generate404Page(),
            Encoding.UTF8);

        var lastmod = DateTime.Now.ToString("yyyy-MM-dd");
        File.WriteAllText(
            Path.Combine(wwwrootDir, "sitemap.xml"),
            GenerateSitemap(lastmod),
            Encoding.UTF8);

        File.WriteAllText(
            Path.Combine(wwwrootDir, "robots.txt"),
            GenerateRobotsTxt(),
            Encoding.UTF8);

        File.WriteAllText(
            Path.Combine(wwwrootDir, ".nojekyll"), "");
    }

    // ========== Page Generators ==========

    private static string GenerateIndexPage(
        List<LotoResult> results,
        PredictionResult prediction,
        LotoResult latest,
        string lastUpdated)
    {
        var sb = new StringBuilder();
        var recommended = prediction.RecommendedNumbers;
        var allScores = prediction.AllScores;
        var reasons = GenerateUniqueReasons(recommended, allScores);

        // Mascot + title
        sb.AppendLine("<img src=\"images/neko-mascot.png\" class=\"neko-avatar-large\" alt=\"NekoLoto6\" />");
        sb.AppendLine("<h1 class=\"page-title\">\U0001F3AF NekoLoto6 次回予想</h1>");
        sb.AppendLine($"<p class=\"page-subtitle\">全 <strong>{results.Count}</strong> 回の抽選データに基づく統計分析予想</p>");

        // Meta info
        var numbersStr = string.Join(" ", latest.MainNumbers.Select(n => n.ToString("D2")));
        var formattedDate = FormatLastUpdated(lastUpdated);
        sb.AppendLine("<div class=\"predict-meta\">");
        sb.AppendLine($"    <span>最新: 第{latest.DrawNumber}回 ({latest.DrawDate:yyyy-MM-dd}) 【{numbersStr} + bonus {latest.BonusNumber:D2}】</span>");
        sb.AppendLine($"    <span>更新: {formattedDate}</span>");
        sb.AppendLine("</div>");

        // Main card: recommended 6 numbers
        sb.AppendLine("<div class=\"card-dark predict-main-card\">");
        sb.AppendLine("    <h2 class=\"card-dark-title\">おすすめ6数字</h2>");
        sb.AppendLine("    <div class=\"predict-balls\">");
        foreach (var num in recommended)
            sb.AppendLine($"        <span class=\"predict-ball\">{num:D2}</span>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <p class=\"predict-note\">※ 統計的傾向に基づく参考値です。当選を保証するものではありません。</p>");
        sb.AppendLine("</div>");

        // Reason card
        sb.AppendLine("<div class=\"card-dark\">");
        sb.AppendLine("    <h2 class=\"card-dark-title\">選定理由</h2>");
        foreach (var num in recommended)
        {
            sb.AppendLine("    <div class=\"predict-reason-row\">");
            sb.AppendLine($"        <span class=\"num-badge\">{num:D2}</span>");
            sb.AppendLine("        <span class=\"predict-reason-arrow\">\u2192</span>");
            sb.AppendLine($"        <span class=\"predict-reason-text\">{reasons[num]}</span>");
            sb.AppendLine("    </div>");
        }
        sb.AppendLine("</div>");

        // Score detail card
        sb.AppendLine("<div class=\"card-dark\">");
        sb.AppendLine("    <h2 class=\"card-dark-title\">選出根拠 スコア詳細</h2>");
        foreach (var num in recommended)
        {
            var s = allScores.First(x => x.Number == num);
            sb.AppendLine("    <div class=\"predict-detail\">");
            sb.AppendLine("        <div class=\"predict-detail-header\">");
            sb.AppendLine($"            <span class=\"num-badge\">{num:D2}</span>");
            sb.AppendLine($"            <span class=\"predict-detail-total\">合計スコア: <strong>{s.TotalScore.ToString("F3", Inv)}</strong></span>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class=\"predict-score-bars\">");

            AppendScoreBar(sb, "出現頻度 (20%)", "", s.FrequencyScore);
            AppendScoreBar(sb, "直近トレンド (30%)", " predict-score-bar-trend", s.TrendScore);
            AppendScoreBar(sb, "出目間隔 (20%)", " predict-score-bar-interval", s.IntervalScore);
            AppendScoreBar(sb, "バランス (10%)", " predict-score-bar-balance", s.BalanceScore);
            AppendScoreBar(sb, "引き継ぎ傾向 (20%)", " predict-score-bar-carryover", s.CarryoverScore);

            sb.AppendLine("        </div>");
            sb.Append($"        <p class=\"predict-detail-info\">全期間 {s.TotalAppearances} 回出現 / 直近{prediction.RecentDraws}回で {s.RecentAppearances} 回出現 / 最後の出現から {s.DrawsSinceLastAppearance} 回前");
            if (s.IsCarriedOver) sb.Append(" / 前回から引き継ぎ");
            sb.AppendLine("</p>");
            sb.AppendLine("    </div>");
        }
        sb.AppendLine("</div>");

        // Score ranking card (collapsible)
        sb.AppendLine("<div class=\"card-dark\">");
        sb.AppendLine("    <details>");
        sb.AppendLine("        <summary class=\"card-dark-title predict-ranking-summary\">全43数字 スコアランキング</summary>");
        sb.AppendLine("        <div class=\"predict-ranking\">");
        var topScore = allScores.Count > 0 ? allScores[0].TotalScore : 0;
        for (var rank = 0; rank < allScores.Count; rank++)
        {
            var score = allScores[rank];
            var isSelected = recommended.Contains(score.Number);
            var rowClass = isSelected ? " predict-ranking-highlight" : "";
            var badgeClass = isSelected ? "" : " num-badge-dim";
            var barWidth = topScore > 0
                ? (score.TotalScore / topScore * 100).ToString("F1", Inv)
                : "0.0";

            sb.AppendLine($"            <div class=\"predict-ranking-row{rowClass}\">");
            sb.AppendLine($"                <span class=\"predict-ranking-rank\">{rank + 1}</span>");
            sb.AppendLine($"                <span class=\"num-badge{badgeClass}\">{score.Number:D2}</span>");
            sb.AppendLine("                <div class=\"predict-score-bar-bg predict-ranking-bar\">");
            sb.AppendLine($"                    <div class=\"predict-score-bar\" style=\"width: {barWidth}%\"></div>");
            sb.AppendLine("                </div>");
            sb.AppendLine($"                <span class=\"predict-ranking-score\">{score.TotalScore.ToString("F3", Inv)}</span>");
            sb.AppendLine("            </div>");
        }
        sb.AppendLine("        </div>");
        sb.AppendLine("    </details>");
        sb.AppendLine("</div>");

        return WrapPage(new PageSeo(
            Title: $"ロト6予想 次回おすすめ数字 | NekoLoto6 統計分析",
            Description: $"ロト6の過去全{results.Count}回の抽選データを統計分析し、5つの独自指標で次回のおすすめ数字を予想。出現頻度・直近トレンド・引き継ぎ傾向などから注目の6数字を毎回自動更新でお届けします。",
            Keywords: "ロト6,予想,次回,おすすめ数字,統計分析,出現頻度,トレンド,当選番号,loto6",
            FileName: "",
            ActivePage: "index",
            JsonLdType: "WebSite",
            JsonLdName: "NekoLoto6",
            JsonLdDescription: "ロト6の統計分析に基づく次回予想サイト"
        ), sb.ToString());
    }

    private static void AppendScoreBar(StringBuilder sb, string label, string extraClass, double value)
    {
        var widthPct = (value * 100).ToString("F1", Inv);
        sb.AppendLine("            <div class=\"predict-score-row\">");
        sb.AppendLine($"                <span class=\"predict-score-label\">{label}</span>");
        sb.AppendLine("                <div class=\"predict-score-bar-bg\">");
        sb.AppendLine($"                    <div class=\"predict-score-bar{extraClass}\" style=\"width: {widthPct}%\"></div>");
        sb.AppendLine("                </div>");
        sb.AppendLine($"                <span class=\"predict-score-value\">{value.ToString("F2", Inv)}</span>");
        sb.AppendLine("            </div>");
    }

    private static string GenerateFrequencyPage(List<LotoResult> results)
    {
        var sb = new StringBuilder();
        var frequency = new int[44];
        foreach (var r in results)
            foreach (var num in r.MainNumbers)
                if (num >= 1 && num <= 43)
                    frequency[num]++;
        var maxCount = frequency.Skip(1).Max();

        sb.AppendLine("<h1 class=\"page-title\">\U0001F3B1 ロト6 出現回数分析</h1>");
        sb.AppendLine($"<p class=\"page-subtitle\">全 <strong>{results.Count}</strong> 回の抽選データを分析</p>");

        sb.AppendLine("<div class=\"card-dark\">");
        sb.AppendLine("    <h2 class=\"card-dark-title\">番号別 出現回数（本数字）</h2>");
        sb.AppendLine("    <div class=\"frequency-chart\">");
        for (var num = 1; num <= 43; num++)
        {
            var count = frequency[num];
            var pct = maxCount > 0 ? (double)count / maxCount * 100 : 0;
            sb.AppendLine("        <div class=\"freq-row\">");
            sb.AppendLine($"            <span class=\"num-badge\">{num:D2}</span>");
            sb.AppendLine("            <div class=\"freq-bar-bg\">");
            sb.AppendLine($"                <div class=\"freq-bar\" style=\"width: {pct.ToString("F1", Inv)}%\"></div>");
            sb.AppendLine("            </div>");
            sb.AppendLine($"            <span class=\"freq-count\">{count} 回</span>");
            sb.AppendLine("        </div>");
        }
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");

        return WrapPage(new PageSeo(
            Title: "ロト6 数字別出現回数・出現頻度 | NekoLoto6",
            Description: "ロト6の1〜43全数字の出現回数・出現頻度をグラフで可視化。過去全抽選データに基づく統計で、よく出る数字・出にくい数字が一目でわかります。",
            Keywords: "ロト6,出現回数,出現頻度,統計,よく出る数字,データ分析,loto6",
            FileName: "frequency.html",
            ActivePage: "frequency",
            JsonLdType: "WebPage",
            JsonLdName: "ロト6 数字別出現回数・出現頻度",
            JsonLdDescription: "ロト6の1〜43全数字の出現回数・出現頻度をグラフで可視化"
        ), sb.ToString());
    }

    private static string GenerateStatisticsPage(List<LotoResult> results)
    {
        var sb = new StringBuilder();
        var evenOdd = LotoStatisticsService.AnalyzeEvenOdd(results);
        var highLow = LotoStatisticsService.AnalyzeHighLow(results);
        var sumRange = LotoStatisticsService.AnalyzeSumDistribution(results);
        var consecutive = LotoStatisticsService.AnalyzeConsecutive(results);

        sb.AppendLine("<h1 class=\"page-title\">\U0001F4CA ロト6 統計分析</h1>");
        sb.AppendLine($"<p class=\"page-subtitle\">全 <strong>{results.Count}</strong> 回の抽選データを分析</p>");

        // Section 1: Even/Odd
        sb.AppendLine("<div class=\"card-dark\">");
        sb.AppendLine("    <h2 class=\"card-dark-title\">偶数・奇数バランス</h2>");
        sb.AppendLine("    <div class=\"stat-chart\">");
        AppendPatternBars(sb, evenOdd.Patterns, evenOdd.MaxIndex);
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");

        // Section 2: High/Low
        sb.AppendLine("<div class=\"card-dark\">");
        sb.AppendLine("    <h2 class=\"card-dark-title\">高低バランス（低1-21 / 高22-43）</h2>");
        sb.AppendLine("    <div class=\"stat-chart\">");
        AppendPatternBars(sb, highLow.Patterns, highLow.MaxIndex);
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");

        // Section 3: Sum distribution
        sb.AppendLine("<div class=\"card-dark\">");
        sb.AppendLine("    <h2 class=\"card-dark-title\">本数字 合計値の分布</h2>");
        sb.AppendLine("    <div class=\"stat-summary\">");
        sb.AppendLine($"        <span>平均: <strong>{sumRange.Average.ToString("F1", Inv)}</strong></span>");
        sb.AppendLine($"        <span>中央値: <strong>{sumRange.Median.ToString("F1", Inv)}</strong></span>");
        sb.AppendLine($"        <span>最小: <strong>{sumRange.Min}</strong></span>");
        sb.AppendLine($"        <span>最大: <strong>{sumRange.Max}</strong></span>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div class=\"stat-chart\">");
        var sumMaxCount = sumRange.Ranges.Max(x => x.Count);
        foreach (var p in sumRange.Ranges)
        {
            var barPct = sumMaxCount > 0 ? (double)p.Count / sumMaxCount * 100 : 0;
            sb.AppendLine("        <div class=\"stat-row\">");
            sb.AppendLine($"            <span class=\"stat-label stat-label-wide\">{p.Label}</span>");
            sb.AppendLine("            <div class=\"stat-bar-bg\">");
            sb.AppendLine($"                <div class=\"stat-bar\" style=\"width: {barPct.ToString("F1", Inv)}%\"></div>");
            sb.AppendLine("            </div>");
            sb.AppendLine($"            <span class=\"stat-count\">{p.Count} 回 ({p.Percentage.ToString("F1", Inv)}%)</span>");
            sb.AppendLine("        </div>");
        }
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");

        // Section 4: Consecutive
        sb.AppendLine("<div class=\"card-dark\">");
        sb.AppendLine("    <h2 class=\"card-dark-title\">連番（連続数字）の出現分析</h2>");
        sb.AppendLine("    <p class=\"stat-note\">連番とは、隣り合う数字（例: 5,6 や 12,13,14）のことです</p>");
        sb.AppendLine("    <div class=\"stat-chart\">");
        AppendPatternBars(sb, consecutive.Patterns, consecutive.MaxIndex);
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");

        return WrapPage(new PageSeo(
            Title: "ロト6 統計分析・データ傾向 | NekoLoto6",
            Description: "ロト6の統計分析ページ。直近の出現トレンド、出目間隔、数字帯別の偏りなど、多角的なデータ分析で傾向を把握できます。",
            Keywords: "ロト6,統計,分析,トレンド,データ,傾向,出目間隔,loto6",
            FileName: "statistics.html",
            ActivePage: "statistics",
            JsonLdType: "WebPage",
            JsonLdName: "ロト6 統計分析・データ傾向",
            JsonLdDescription: "ロト6の統計分析。直近の出現トレンド、出目間隔、数字帯別の偏りなど多角的なデータ分析"
        ), sb.ToString());
    }

    private static void AppendPatternBars(StringBuilder sb, List<PatternCount> patterns, int maxIndex)
    {
        var maxCount = patterns.Max(x => x.Count);
        for (var i = 0; i < patterns.Count; i++)
        {
            var p = patterns[i];
            var isMax = i == maxIndex;
            var barPct = maxCount > 0 ? (double)p.Count / maxCount * 100 : 0;
            var highlightClass = isMax ? " stat-bar-highlight" : "";
            sb.AppendLine("        <div class=\"stat-row\">");
            sb.AppendLine($"            <span class=\"stat-label\">{p.Label}</span>");
            sb.AppendLine("            <div class=\"stat-bar-bg\">");
            sb.AppendLine($"                <div class=\"stat-bar{highlightClass}\" style=\"width: {barPct.ToString("F1", Inv)}%\"></div>");
            sb.AppendLine("            </div>");
            sb.AppendLine($"            <span class=\"stat-count\">{p.Count} 回 ({p.Percentage.ToString("F1", Inv)}%)</span>");
            sb.AppendLine("        </div>");
        }
    }

    private static string Generate404Page()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<h1 class=\"page-title\">ページが見つかりません</h1>");
        sb.AppendLine("<p class=\"page-subtitle\">お探しのページは存在しないか、移動した可能性があります。</p>");
        sb.AppendLine("<p><a href=\"\">トップページに戻る</a></p>");
        return WrapPage(new PageSeo(
            Title: "NekoLoto6 - ページが見つかりません",
            Description: "",
            Keywords: "",
            FileName: "404.html",
            ActivePage: "",
            JsonLdType: "",
            JsonLdName: "",
            JsonLdDescription: ""
        ), sb.ToString());
    }

    private static string GenerateAlgorithmPage(List<LotoResult> results)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<h1 class=\"page-title\">\U0001F9E0 予想の仕組み</h1>");
        sb.AppendLine($"<p class=\"page-subtitle\">NekoLoto6 の予想アルゴリズムを解説します</p>");

        // Section 1: NekoLoto6の予想とは
        sb.AppendLine("<div class=\"card-dark\">");
        sb.AppendLine("    <h2 class=\"card-dark-title\">NekoLoto6 の予想とは</h2>");
        sb.AppendLine($"    <p class=\"algo-text\">NekoLoto6 は、過去 <strong style=\"color:#f5a623\">{results.Count}</strong> 回分のロト6抽選データを統計的に分析し、次回の予想数字を算出しています。</p>");
        sb.AppendLine("    <p class=\"algo-text\">各数字（01〜43）に対して 5 つの指標からスコアを計算し、総合スコアが高い上位 6 つの数字を「おすすめ数字」として選出します。</p>");
        sb.AppendLine("    <p class=\"algo-note\">※ 統計的な傾向分析であり、乱数である抽選結果の予測を保証するものではありません。</p>");
        sb.AppendLine("</div>");

        // Section 2: 5つのスコア指標
        sb.AppendLine("<div class=\"card-dark\">");
        sb.AppendLine("    <h2 class=\"card-dark-title\">5つのスコア指標</h2>");
        sb.AppendLine("    <p class=\"algo-text\">各数字は以下の 5 つの指標で評価され、重み付けして合計スコアを算出します。</p>");

        var indicators = new[]
        {
            ("linear-gradient(90deg, #4a9eff, #2d7cd6)", "出現頻度スコア（重み 20%）",
             "全期間を通じた出現回数を評価します。出現回数が多い数字ほどスコアが高くなります。"),
            ("linear-gradient(90deg, #ff6b35, #e05520)", "直近トレンドスコア（重み 30%）",
             "直近 50 回での出現頻度を評価します。最近よく出ている「勢いのある」数字を重視します。最も高い重みが設定されています。"),
            ("linear-gradient(90deg, #26b050, #1e8a3e)", "出目間隔スコア（重み 20%）",
             "最後に出現してからの経過回数を評価します。長く出ていない数字は「そろそろ出る」可能性として加点されます。"),
            ("linear-gradient(90deg, #a855f7, #7c3aed)", "バランススコア（重み 10%）",
             "偶奇バランス・高低バランス・数字帯の分散など、選出全体の偏りを抑えるための調整指標です。"),
            ("linear-gradient(90deg, #f59e0b, #d97706)", "引き継ぎ傾向スコア（重み 20%）",
             "前回の当選番号が次回も出現する「引き継ぎ」傾向を評価します。前回出た数字に加点されます。")
        };

        foreach (var (gradient, title, desc) in indicators)
        {
            sb.AppendLine("    <div class=\"algo-indicator\">");
            sb.AppendLine($"        <div class=\"algo-indicator-bar\" style=\"background: {gradient}\"></div>");
            sb.AppendLine("        <div class=\"algo-indicator-content\">");
            sb.AppendLine($"            <div class=\"algo-indicator-title\">{title}</div>");
            sb.AppendLine($"            <div class=\"algo-indicator-desc\">{desc}</div>");
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
        }

        sb.AppendLine("</div>");

        // Section 3: 引き継ぎ傾向
        sb.AppendLine("<div class=\"card-dark\">");
        sb.AppendLine("    <h2 class=\"card-dark-title\">引き継ぎ傾向の分析</h2>");
        sb.AppendLine("    <p class=\"algo-text\">前回の当選番号 6 個のうち、次回も再び出現する数字の個数を集計しました。</p>");

        // 動的計算
        var ordered = results.OrderBy(r => r.DrawNumber).ToList();
        var countsAll = new int[5]; // [0個, 1個, 2個, 3個, 4個以上]
        for (int i = 1; i < ordered.Count; i++)
        {
            var prev = new HashSet<int>(ordered[i - 1].MainNumbers);
            var carry = ordered[i].MainNumbers.Count(n => prev.Contains(n));
            countsAll[Math.Min(carry, 4)]++;
        }
        var totalTransitions = ordered.Count - 1;

        // 直近100回
        var recentStart = Math.Max(1, ordered.Count - 100);
        var countsRecent = new int[5];
        for (int i = recentStart; i < ordered.Count; i++)
        {
            var prev = new HashSet<int>(ordered[i - 1].MainNumbers);
            var carry = ordered[i].MainNumbers.Count(n => prev.Contains(n));
            countsRecent[Math.Min(carry, 4)]++;
        }
        var totalRecent = ordered.Count - recentStart;

        // 理論値（超幾何分布 C(6,k)*C(37,6-k)/C(43,6)）
        var theoretical = new[] { 28.1, 42.4, 22.6, 6.1, 0.8 };

        sb.AppendLine("    <table class=\"algo-table\">");
        sb.AppendLine("        <thead><tr>");
        sb.AppendLine("            <th>引き継ぎ数</th><th>理論値</th><th>全期間実績</th><th>直近100回</th>");
        sb.AppendLine("        </tr></thead>");
        sb.AppendLine("        <tbody>");

        var labels = new[] { "0 個", "1 個", "2 個", "3 個", "4 個以上" };
        for (int k = 0; k < 5; k++)
        {
            var allPct = totalTransitions > 0
                ? (countsAll[k] * 100.0 / totalTransitions).ToString("F1", Inv)
                : "0.0";
            var recentPct = totalRecent > 0
                ? (countsRecent[k] * 100.0 / totalRecent).ToString("F1", Inv)
                : "0.0";
            sb.AppendLine("        <tr>");
            sb.AppendLine($"            <td>{labels[k]}</td>");
            sb.AppendLine($"            <td>{theoretical[k].ToString("F1", Inv)}%</td>");
            sb.AppendLine($"            <td>{allPct}%（{countsAll[k]} 回）</td>");
            sb.AppendLine($"            <td>{recentPct}%（{countsRecent[k]} 回）</td>");
            sb.AppendLine("        </tr>");
        }

        sb.AppendLine("        </tbody>");
        sb.AppendLine("    </table>");
        sb.AppendLine("    <p class=\"algo-note\">※ 理論値は超幾何分布 C(6,k)×C(37,6−k)÷C(43,6) に基づく確率です。</p>");
        sb.AppendLine("</div>");

        // Section 4: スコア計算の流れ
        sb.AppendLine("<div class=\"card-dark\">");
        sb.AppendLine("    <h2 class=\"card-dark-title\">スコア計算の流れ</h2>");

        var steps = new[]
        {
            "過去の全抽選データを読み込む",
            "各数字（01〜43）について 5 指標のスコアを 0〜1 に正規化して算出",
            "重み付けして合計スコアを計算",
            "合計スコアの上位 6 数字を「おすすめ」として選出"
        };

        for (int i = 0; i < steps.Length; i++)
        {
            sb.AppendLine("    <div class=\"algo-step\">");
            sb.AppendLine($"        <span class=\"algo-step-badge\">{i + 1}</span>");
            sb.AppendLine($"        <span class=\"algo-step-text\">{steps[i]}</span>");
            sb.AppendLine("    </div>");
            if (i < steps.Length - 1)
                sb.AppendLine("    <div class=\"algo-step-arrow\">\u2193</div>");
        }

        sb.AppendLine("    <div class=\"algo-formula\">合計スコア = 出現頻度×0.2 + 直近トレンド×0.3 + 出目間隔×0.2 + バランス×0.1 + 引き継ぎ傾向×0.2</div>");
        sb.AppendLine("</div>");

        // Section 5: 免責事項
        sb.AppendLine("<div class=\"card-dark\">");
        sb.AppendLine("    <h2 class=\"card-dark-title\">免責事項</h2>");
        sb.AppendLine("    <p class=\"algo-text\">ロト6 の抽選は完全な乱数であり、過去の結果が将来の結果に影響を与えることはありません。</p>");
        sb.AppendLine("    <p class=\"algo-text\">NekoLoto6 の予想はあくまで統計的な傾向分析に基づく参考情報であり、当選を保証するものではありません。</p>");
        sb.AppendLine("    <p class=\"algo-note\">※ 宝くじは適度に楽しみましょう。</p>");
        sb.AppendLine("</div>");

        return WrapPage(new PageSeo(
            Title: "ロト6予想の仕組み・アルゴリズム解説 | NekoLoto6",
            Description: "NekoLoto6のロト6予想アルゴリズムを解説。出現頻度・直近トレンド・出目間隔・バランス・引き継ぎ傾向の5指標によるスコアリング方式と、超幾何分布に基づく独自分析の詳細。",
            Keywords: "ロト6,予想,アルゴリズム,仕組み,スコアリング,超幾何分布,引き継ぎ,統計分析,loto6",
            FileName: "algorithm.html",
            ActivePage: "algorithm",
            JsonLdType: "WebPage",
            JsonLdName: "ロト6予想の仕組み・アルゴリズム解説",
            JsonLdDescription: "NekoLoto6の予想アルゴリズム解説。5指標によるスコアリング方式と超幾何分布に基づく独自分析"
        ), sb.ToString());
    }

    // ========== SEO Data ==========

    private record PageSeo(
        string Title,
        string Description,
        string Keywords,
        string FileName,
        string ActivePage,
        string JsonLdType,
        string JsonLdName,
        string JsonLdDescription);

    // ========== Layout ==========

    private static string WrapPage(PageSeo seo, string content)
    {
        var canonicalUrl = SiteBase + seo.FileName;
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"ja\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"utf-8\" />");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
        sb.AppendLine($"    <title>{seo.Title}</title>");
        sb.AppendLine("    <base href=\"/\" />");
        sb.AppendLine("    <link rel=\"icon\" type=\"image/png\" href=\"favicon.png\" />");
        sb.AppendLine("    <link rel=\"stylesheet\" href=\"css/app.css\" />");
        sb.AppendLine("    <meta name=\"robots\" content=\"index, follow\" />");
        sb.AppendLine("    <meta name=\"author\" content=\"NekoLoto6\" />");
        sb.AppendLine("    <meta name=\"theme-color\" content=\"#1a1a2e\" />");
        sb.AppendLine($"    <link rel=\"canonical\" href=\"{canonicalUrl}\" />");

        if (!string.IsNullOrEmpty(seo.Description))
        {
            sb.AppendLine($"    <meta name=\"description\" content=\"{EscapeAttr(seo.Description)}\" />");
            sb.AppendLine($"    <meta name=\"keywords\" content=\"{EscapeAttr(seo.Keywords)}\" />");

            // OGP
            sb.AppendLine($"    <meta property=\"og:title\" content=\"{EscapeAttr(seo.Title)}\" />");
            sb.AppendLine($"    <meta property=\"og:description\" content=\"{EscapeAttr(seo.Description)}\" />");
            sb.AppendLine($"    <meta property=\"og:url\" content=\"{canonicalUrl}\" />");
            sb.AppendLine("    <meta property=\"og:site_name\" content=\"NekoLoto6\" />");
            sb.AppendLine("    <meta property=\"og:type\" content=\"website\" />");
            sb.AppendLine("    <meta property=\"og:locale\" content=\"ja_JP\" />");
            sb.AppendLine($"    <meta property=\"og:image\" content=\"{SiteBase}images/neko-mascot.png\" />");
            sb.AppendLine("    <meta property=\"og:image:width\" content=\"512\" />");
            sb.AppendLine("    <meta property=\"og:image:height\" content=\"512\" />");

            // Twitter Card
            sb.AppendLine("    <meta name=\"twitter:card\" content=\"summary\" />");
            sb.AppendLine($"    <meta name=\"twitter:title\" content=\"{EscapeAttr(seo.Title)}\" />");
            sb.AppendLine($"    <meta name=\"twitter:description\" content=\"{EscapeAttr(seo.Description)}\" />");
            sb.AppendLine($"    <meta name=\"twitter:image\" content=\"{SiteBase}images/neko-mascot.png\" />");
        }

        // JSON-LD
        if (!string.IsNullOrEmpty(seo.JsonLdType))
        {
            if (seo.JsonLdType == "WebSite")
            {
                sb.AppendLine("    <script type=\"application/ld+json\">");
                sb.AppendLine("    {");
                sb.AppendLine("        \"@context\": \"https://schema.org\",");
                sb.AppendLine("        \"@type\": \"WebSite\",");
                sb.AppendLine($"        \"name\": \"{EscapeJson(seo.JsonLdName)}\",");
                sb.AppendLine($"        \"description\": \"{EscapeJson(seo.JsonLdDescription)}\",");
                sb.AppendLine($"        \"url\": \"{SiteBase}\",");
                sb.AppendLine("        \"inLanguage\": \"ja\"");
                sb.AppendLine("    }");
                sb.AppendLine("    </script>");
            }
            else
            {
                sb.AppendLine("    <script type=\"application/ld+json\">");
                sb.AppendLine("    {");
                sb.AppendLine("        \"@context\": \"https://schema.org\",");
                sb.AppendLine("        \"@type\": \"WebPage\",");
                sb.AppendLine($"        \"name\": \"{EscapeJson(seo.JsonLdName)}\",");
                sb.AppendLine($"        \"description\": \"{EscapeJson(seo.JsonLdDescription)}\",");
                sb.AppendLine($"        \"url\": \"{canonicalUrl}\",");
                sb.AppendLine("        \"inLanguage\": \"ja\",");
                sb.AppendLine("        \"isPartOf\": {");
                sb.AppendLine("            \"@type\": \"WebSite\",");
                sb.AppendLine("            \"name\": \"NekoLoto6\",");
                sb.AppendLine($"            \"url\": \"{SiteBase}\"");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine("    </script>");
            }
        }

        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"page\">");
        sb.AppendLine("        <div class=\"sidebar\">");
        sb.Append(GenerateSidebar(seo.ActivePage));
        sb.AppendLine("        </div>");
        sb.AppendLine("        <main>");
        sb.AppendLine("            <article class=\"content px-4\">");
        sb.Append(content);
        sb.AppendLine();
        sb.AppendLine("            </article>");
        sb.AppendLine("        </main>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string EscapeAttr(string s) => s.Replace("\"", "&quot;").Replace("&", "&amp;");
    private static string EscapeJson(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    // ========== Sitemap & Robots ==========

    private static string GenerateSitemap(string lastmod)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        var pages = new (string url, string priority, string changefreq)[]
        {
            (SiteBase, "1.0", "weekly"),
            ($"{SiteBase}frequency.html", "0.8", "weekly"),
            ($"{SiteBase}statistics.html", "0.8", "weekly"),
            ($"{SiteBase}algorithm.html", "0.6", "monthly")
        };

        foreach (var (url, priority, changefreq) in pages)
        {
            sb.AppendLine("    <url>");
            sb.AppendLine($"        <loc>{url}</loc>");
            sb.AppendLine($"        <lastmod>{lastmod}</lastmod>");
            sb.AppendLine($"        <changefreq>{changefreq}</changefreq>");
            sb.AppendLine($"        <priority>{priority}</priority>");
            sb.AppendLine("    </url>");
        }

        sb.AppendLine("</urlset>");
        return sb.ToString();
    }

    private static string GenerateRobotsTxt()
    {
        return $"User-agent: *\nAllow: /\nSitemap: {SiteBase}sitemap.xml\n";
    }

    private static string GenerateSidebar(string activePage)
    {
        var sb = new StringBuilder();
        sb.AppendLine("            <div class=\"top-row navbar\">");
        sb.AppendLine("                <div class=\"container-fluid\">");
        sb.AppendLine("                    <a class=\"navbar-brand\" href=\"\"><img src=\"images/neko-mascot.png\" class=\"neko-avatar-small\" alt=\"NekoLoto6\" /> NekoLoto6</a>");
        sb.AppendLine("                </div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("            <input type=\"checkbox\" title=\"Navigation menu\" class=\"navbar-toggler\" />");
        sb.AppendLine("            <div class=\"nav-scrollable\">");
        sb.AppendLine("                <nav class=\"nav flex-column\">");

        var navItems = new[]
        {
            ("index", "", "bi-crosshair-nav-menu", "次回予想"),
            ("frequency", "frequency.html", "bi-bar-chart-fill-nav-menu", "出現回数"),
            ("statistics", "statistics.html", "bi-graph-up-nav-menu", "統計分析"),
            ("algorithm", "algorithm.html", "bi-question-circle-nav-menu", "予想の仕組み")
        };

        foreach (var (page, href, icon, label) in navItems)
        {
            var activeClass = page == activePage ? " active" : "";
            sb.AppendLine($"                    <div class=\"nav-item px-3\">");
            sb.AppendLine($"                        <a class=\"nav-link{activeClass}\" href=\"{href}\">");
            sb.AppendLine($"                            <span class=\"bi {icon}\" aria-hidden=\"true\"></span> {label}");
            sb.AppendLine("                        </a>");
            sb.AppendLine("                    </div>");
        }

        sb.AppendLine("                </nav>");
        sb.AppendLine("            </div>");
        return sb.ToString();
    }

    // ========== Reason Text Generation (ported from Home.razor) ==========

    private static Dictionary<int, string> GenerateUniqueReasons(
        List<int> recommendedNumbers,
        List<NumberScore> allScores)
    {
        var result = new Dictionary<int, string>();
        var used = new HashSet<string>();

        foreach (var num in recommendedNumbers)
        {
            var score = allScores.First(x => x.Number == num);
            var candidates = GetReasonCandidates(score);
            var text = candidates.FirstOrDefault(c => !used.Contains(c)) ?? candidates[0];
            result[num] = text;
            used.Add(text);
        }

        return result;
    }

    private static List<string> GetReasonCandidates(NumberScore s)
    {
        var candidates = new List<string>();
        var indicators = new (string key, double value)[]
        {
            ("frequency", s.FrequencyScore),
            ("trend", s.TrendScore),
            ("interval", s.IntervalScore),
            ("balance", s.BalanceScore),
            ("carryover", s.CarryoverScore)
        };
        var sorted = indicators.OrderByDescending(x => x.value).ToArray();

        if (sorted[1].value >= 0.7)
            candidates.AddRange(GetComboTexts(sorted[0].key, sorted[1].key));

        candidates.AddRange(GetSingleTexts(sorted[0].key));

        if (sorted[2].value >= 0.7)
            candidates.AddRange(GetComboTexts(sorted[0].key, sorted[2].key));

        candidates.AddRange(GetSingleTexts(sorted[1].key));

        for (var i = 2; i < sorted.Length; i++)
            candidates.AddRange(GetSingleTexts(sorted[i].key));

        return candidates;
    }

    private static string[] GetSingleTexts(string indicator) => indicator switch
    {
        "frequency" => [
            "全期間での出現率が高く、安定した数字",
            "全期間を通じて安定感のある数字",
            "長期的に見て信頼度の高い数字"
        ],
        "trend" => [
            "直近50回で特に勢いがある注目数字",
            "最近の抽選で好調な数字",
            "ここ最近の出現率が上昇中"
        ],
        "interval" => [
            "しばらく出ていないため、そろそろ出る可能性",
            "出目間隔が広がっており、反発に期待",
            "前回から間が空いており、出現タイミングか"
        ],
        "balance" => [
            "バランスが非常に良く、総合力が高い数字",
            "各指標がまんべんなく高い優等生タイプ",
            "偏りが少なく、安定した総合評価"
        ],
        "carryover" => [
            "前回も出現しており、引き継がれやすい傾向の数字",
            "連続出現の可能性が統計的に高い数字",
            "前回の流れを引き継ぐ勢いのある数字"
        ],
        _ => []
    };

    private static string[] GetComboTexts(string a, string b) => (a, b) switch
    {
        ("trend", "frequency") or ("frequency", "trend") => [
            "直近50回で勢いがあり、全期間でも安定して出現",
            "最近の好調さに加え、長期的な安定感も兼備"
        ],
        ("trend", "interval") or ("interval", "trend") => [
            "直近の勢いがあり、出目間隔的にも好タイミング",
            "最近の好調と出目間隔の好条件が重なる数字"
        ],
        ("frequency", "interval") or ("interval", "frequency") => [
            "安定した出現率で、出目間隔的にも期待大",
            "高い出現率に加え、間隔的にも好タイミング"
        ],
        ("frequency", "balance") or ("balance", "frequency") => [
            "全期間で安定しており、バランスにも優れた数字",
            "出現率の高さとバランスの良さを兼備"
        ],
        ("trend", "balance") or ("balance", "trend") => [
            "直近の勢いとバランスを兼ね備えた数字",
            "最近好調でバランスも良い期待の数字"
        ],
        ("interval", "balance") or ("balance", "interval") => [
            "しばらく出ていないが、バランスが良く反発が期待される",
            "出目間隔とバランスの両面で期待できる数字"
        ],
        ("carryover", "trend") or ("trend", "carryover") => [
            "前回からの引き継ぎで、直近の勢いも継続中",
            "連続出現の勢いと最近の好調さが重なる数字"
        ],
        ("carryover", "frequency") or ("frequency", "carryover") => [
            "前回から引き継がれやすく、全期間でも安定した数字",
            "引き継ぎ傾向と長期的な安定性を兼備"
        ],
        ("carryover", "interval") or ("interval", "carryover") => [
            "前回からの引き継ぎと出目間隔の両面で期待大",
            "引き継ぎの流れに加え、間隔的にも出やすい状況"
        ],
        ("carryover", "balance") or ("balance", "carryover") => [
            "前回からの引き継ぎ傾向とバランスを兼備",
            "連続出現の可能性が高く、バランスも良好"
        ],
        _ => []
    };

    private static string FormatLastUpdated(string isoDate)
    {
        if (DateTimeOffset.TryParse(isoDate, out var dto))
            return dto.ToString("yyyy/MM/dd HH:mm");
        return isoDate;
    }
}
