namespace StrategyViewer.Models;

public class AppSettings
{
    public ServerConfig StrategyServer { get; set; } = new();
    public ServerConfig MarketDataServer { get; set; } = new();
    public ValidationSettings ValidationSettings { get; set; } = new();
}

public class ServerConfig
{
    public string BaseUrl { get; set; } = "http://localhost";
    public string Token { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);
}

public class ValidationSettings
{
    public int AnalysisDays { get; set; } = 5;
    public string? DefaultStartDate { get; set; }
    public string? DefaultEndDate { get; set; }
    public decimal SingleTradeStopLossAmount { get; set; } = 10000m;  // 单笔止损金额（元）
}
