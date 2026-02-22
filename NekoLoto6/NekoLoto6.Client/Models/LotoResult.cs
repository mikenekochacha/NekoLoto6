namespace NekoLoto6.Client.Models;

public class LotoResult
{
    public int DrawNumber { get; set; }
    public DateTime DrawDate { get; set; }
    public int[] MainNumbers { get; set; } = new int[6];
    public int BonusNumber { get; set; }
    public int Prize1Count { get; set; }
    public int Prize2Count { get; set; }
    public int Prize3Count { get; set; }
    public int Prize4Count { get; set; }
    public int Prize5Count { get; set; }
    public long Prize1Amount { get; set; }
    public long Prize2Amount { get; set; }
    public long Prize3Amount { get; set; }
    public long Prize4Amount { get; set; }
    public long Prize5Amount { get; set; }
    public long Carryover { get; set; }
    public long Sales { get; set; }
}
