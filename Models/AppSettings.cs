namespace StrategyViewer.Models;

public class AppSettings
{
    public ServerConfig StrategyServer { get; set; } = new();
    public ServerConfig MarketDataServer { get; set; } = new();
    public ValidationSettings ValidationSettings { get; set; } = new();
    public FeishuConfig FeishuSettings { get; set; } = new();
    public FeishuImageConfig FeishuImageSettings { get; set; } = new();
    public TelegramConfig TelegramSettings { get; set; } = new();
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

public class FeishuConfig
{
    public bool IsEnabled { get; set; } = false;
    public string WebhookUrl { get; set; } = string.Empty;
    public string BotName { get; set; } = "策略矩阵机器人";
}

public class FeishuImageConfig
{
    public bool IsEnabled { get; set; } = false;
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public string BotName { get; set; } = "策略矩阵机器人";
    public bool UseCardWithImage { get; set; } = true; // 是否在图片上方添加卡片标题

    public bool IsConfigured => !string.IsNullOrWhiteSpace(AppId) &&
                                 !string.IsNullOrWhiteSpace(AppSecret) &&
                                 !string.IsNullOrWhiteSpace(ChatId);
}

public class TelegramConfig
{
    public bool IsEnabled { get; set; } = false;
    public string BotToken { get; set; } = string.Empty;
    public List<string> ChatIds { get; set; } = new();
    public string ChatIdsDisplay => string.Join(";", ChatIds);
}
