using System.Text.Json.Serialization;

namespace StrategyViewer.Models;

public enum KLinePeriod
{
    Daily,    // 日K
    Min15     // 15分钟K
}

public class MarketData
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public decimal Settlement { get; set; }

    [JsonIgnore]
    public KLinePeriod Period { get; set; } = KLinePeriod.Daily;

    public decimal ChangePercent => Open != 0 ? (Close - Open) / Open * 100 : 0;
}

public class MarketDataRequest
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("startDate")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("endDate")]
    public string EndDate { get; set; } = string.Empty;

    [JsonPropertyName("period")]
    public string Period { get; set; } = "daily";
}

public class MarketDataResponse
{
    public bool Success { get; set; }
    public List<MarketData>? Data { get; set; }
    public string? Message { get; set; }
}

public class MarketDataApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public MarketDataResult? Data { get; set; }
}

public class MarketDataResult
{
    [JsonPropertyName("contract")]
    public string? Contract { get; set; }

    [JsonPropertyName("period")]
    public string? Period { get; set; }

    [JsonPropertyName("data")]
    public List<KLineItem>? Data { get; set; }
}

public class KLineItem
{
    [JsonPropertyName("StartTime")]
    public string? StartTime { get; set; }

    [JsonPropertyName("EndTime")]
    public string? EndTime { get; set; }

    [JsonPropertyName("TradeDate")]
    public string? TradeDate { get; set; }

    [JsonPropertyName("Open")]
    public decimal Open { get; set; }

    [JsonPropertyName("High")]
    public decimal High { get; set; }

    [JsonPropertyName("Low")]
    public decimal Low { get; set; }

    [JsonPropertyName("Close")]
    public decimal Close { get; set; }

    [JsonPropertyName("Volume")]
    public decimal Volume { get; set; }

    [JsonPropertyName("Amount")]
    public decimal Amount { get; set; }
}
