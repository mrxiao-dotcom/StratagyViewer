namespace StrategyViewer.Models;

public class ValidationResult
{
    public int StrategyId { get; set; }
    public string StrategyTitle { get; set; } = string.Empty;
    public DateTime TradeDate { get; set; }
    public ValidationSignal Signal { get; set; } = new();
    public List<DailyPerformance> DailyPerformances { get; set; } = new();
    public ValidationSummary Summary { get; set; } = new();
    public KLinePeriod Period { get; set; } = KLinePeriod.Daily;
}

public class ValidationSignal
{
    public string Contract { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public decimal EntryPrice { get; set; }
    public decimal? StopLoss { get; set; }
    public decimal? TakeProfit { get; set; }
    public DateTime SignalDate { get; set; }
}

public class DailyPerformance
{
    public DateTime Date { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal SignalPrice { get; set; }
    public decimal DailyPnL { get; set; }
    public decimal CumulativePnL { get; set; }
    public decimal MaxProfit { get; set; }
    public decimal MaxLoss { get; set; }
    public bool HitStopLoss { get; set; }
    public bool HitTakeProfit { get; set; }
    public decimal? SignalToCurrentChange { get; set; }
}

public class ValidationSummary
{
    public int AnalysisDays { get; set; }
    public decimal TotalPnL { get; set; }
    public decimal MaxProfitReached { get; set; }
    public decimal MaxLossReached { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal Volatility { get; set; }
    public bool HitStopLoss { get; set; }
    public bool HitTakeProfit { get; set; }
    public int ProfitableDays { get; set; }
    public int LossDays { get; set; }
    public decimal WinRate { get; set; }
    public decimal AverageGain { get; set; }
    public decimal AverageLoss { get; set; }
    public decimal ProfitFactor { get; set; }
    public SignalResult Result { get; set; }
    public string ResultDescription { get; set; } = string.Empty;
}

public enum SignalResult
{
    Excellent,
    Good,
    Neutral,
    Poor,
    Failed
}

public class ValidationReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public ValidationResult Result { get; set; } = new();
    public List<ChartPoint> PriceChartData { get; set; } = new();
    public List<ChartPoint> PnLChartData { get; set; } = new();
    public List<ChartPoint> CumulativePnLData { get; set; } = new();
}

public class ChartPoint
{
    public DateTime Date { get; set; }
    public double Value { get; set; }
    public string? Label { get; set; }
}

public class TradeSignalRecord
{
    public DateTime Time { get; set; }
    public string SignalType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? Lots { get; set; }
    public decimal? PnL { get; set; }
}
