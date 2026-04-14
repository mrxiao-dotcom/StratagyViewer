using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace StrategyViewer.Models;

public class StrategySummary
{
    [JsonPropertyName("contract")]
    public string Contract { get; set; } = string.Empty;

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = string.Empty;

    [JsonPropertyName("entryRange")]
    public string EntryRange { get; set; } = string.Empty;

    [JsonPropertyName("stopLoss")]
    public string StopLoss { get; set; } = string.Empty;

    [JsonPropertyName("takeProfit")]
    public string TakeProfit { get; set; } = string.Empty;

    [JsonPropertyName("logic")]
    public string Logic { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }
}

public class Strategy
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public DateTime TradeDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int ItemCount { get; set; }

    public List<StrategySummary> SummaryItems =>
        string.IsNullOrEmpty(Summary)
            ? new List<StrategySummary>()
            : System.Text.Json.JsonSerializer.Deserialize<List<StrategySummary>>(Summary) ?? new List<StrategySummary>();
}

public class StrategyListItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime TradeDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }

    // 内部存储从详情接口加载的 SummaryItems
    private List<StrategySummary> _loadedSummaryItems = new();

    public List<StrategySummary> SummaryItems
    {
        get
        {
            var json = Summary;
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<StrategySummary>>(json) ?? _loadedSummaryItems;
                }
                catch { }
            }
            return _loadedSummaryItems;
        }
    }

    public void UpdateSummaryItems(List<StrategySummary> items)
    {
        System.Diagnostics.Debug.WriteLine($"[UpdateSummaryItems] items={items.Count}");
        _loadedSummaryItems = items;
        OnPropertyChanged(nameof(SummaryItems));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class StrategyStats
{
    public int TotalStrategies { get; set; }
    public int TotalItems { get; set; }
    public DateRange? DateRange { get; set; }
}

public class DateRange
{
    public string? Start { get; set; }
    public string? End { get; set; }
}

public class ContractHistory
{
    public DateTime TradeDate { get; set; }
    public int StrategyId { get; set; }
    public string StrategyTitle { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string EntryRange { get; set; } = string.Empty;
    public string? StopLoss { get; set; }
    public string? TakeProfit { get; set; }
    public string? Logic { get; set; }
    public double? Confidence { get; set; }
}
