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

    public List<StrategySummary> SummaryItems
    {
        get
        {
            if (string.IsNullOrEmpty(Summary))
                return new List<StrategySummary>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<StrategySummary>>(Summary)
                    ?? new List<StrategySummary>();
            }
            catch (System.Text.Json.JsonException ex)
            {
                // 尝试修复常见的 JSON 格式问题
                var fixedJson = FixJsonFormat(Summary);
                if (fixedJson != Summary)
                {
                    System.Diagnostics.Debug.WriteLine($"[JSON修复] 检测到格式问题，正在修复...");
                    try
                    {
                        return System.Text.Json.JsonSerializer.Deserialize<List<StrategySummary>>(fixedJson)
                            ?? new List<StrategySummary>();
                    }
                    catch (System.Text.Json.JsonException ex2)
                    {
                        System.Diagnostics.Debug.WriteLine($"[JSON修复失败] 异常: {ex2.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[JSON解析错误] Summary内容长度: {Summary?.Length ?? 0}");
                System.Diagnostics.Debug.WriteLine($"[JSON解析错误] 异常: {ex.Message}");
                return new List<StrategySummary>();
            }
        }
    }

    private static string FixJsonFormat(string json)
    {
        // 修复缺少引号的数值：如 "stopLoss":300" -> "stopLoss":"300"
        var fixedResult = System.Text.RegularExpressions.Regex.Replace(
            json,
            @"""(\w+)""?\s*:\s*(\d+)""?",
            @"""$1"": ""$2""");
        return fixedResult;
    }
}

public class StrategyListItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime TradeDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
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
                catch (System.Text.Json.JsonException)
                {
                    var fixedJson = FixJsonFormat(json);
                    if (fixedJson != json)
                    {
                        System.Diagnostics.Debug.WriteLine($"[JSON修复 StrategyListItem] 检测到格式问题，正在修复...");
                        try
                        {
                            return System.Text.Json.JsonSerializer.Deserialize<List<StrategySummary>>(fixedJson)
                                ?? _loadedSummaryItems;
                        }
                        catch { }
                    }
                }
            }
            return _loadedSummaryItems;
        }
    }

    private static string FixJsonFormat(string json)
    {
        var fixedResult = System.Text.RegularExpressions.Regex.Replace(
            json,
            @"""(\w+)""?\s*:\s*(\d+)""?",
            @"""$1"": ""$2""");
        return fixedResult;
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
    public string Contract { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string EntryRange { get; set; } = string.Empty;
    public string? StopLoss { get; set; }
    public string? TakeProfit { get; set; }
    public string? Logic { get; set; }
    public double? Confidence { get; set; }
}
