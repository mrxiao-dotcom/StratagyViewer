using System.Net.Http;
using System.Windows;
using StrategyViewer;
using StrategyViewer.Services;

namespace StrategyViewer.Views;

public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly ApiService _apiService;

    public SettingsWindow(ISettingsService settingsService, ApiService apiService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _apiService = apiService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        StrategyUrlTextBox.Text = _settingsService.Settings.StrategyServer.BaseUrl;
        StrategyTokenTextBox.Text = _settingsService.Settings.StrategyServer.Token;
        MarketUrlTextBox.Text = _settingsService.Settings.MarketDataServer.BaseUrl;
        AnalysisDaysTextBox.Text = _settingsService.Settings.ValidationSettings.AnalysisDays.ToString();
        StopLossAmountTextBox.Text = _settingsService.Settings.ValidationSettings.SingleTradeStopLossAmount.ToString("F0");

        // 飞书卡片消息
        FeishuEnabledCheckBox.IsChecked = _settingsService.Settings.FeishuSettings.IsEnabled;
        FeishuWebhookTextBox.Text = _settingsService.Settings.FeishuSettings.WebhookUrl;
        FeishuBotNameTextBox.Text = _settingsService.Settings.FeishuSettings.BotName;

        // 飞书图片发送
        FeishuImageEnabledCheckBox.IsChecked = _settingsService.Settings.FeishuImageSettings.IsEnabled;
        FeishuImageAppIdTextBox.Text = _settingsService.Settings.FeishuImageSettings.AppId;
        FeishuImageAppSecretTextBox.Text = _settingsService.Settings.FeishuImageSettings.AppSecret;
        FeishuImageChatIdTextBox.Text = _settingsService.Settings.FeishuImageSettings.ChatId;
        FeishuImageUseCardCheckBox.IsChecked = _settingsService.Settings.FeishuImageSettings.UseCardWithImage;

        TelegramEnabledCheckBox.IsChecked = _settingsService.Settings.TelegramSettings.IsEnabled;
        TelegramBotTokenTextBox.Text = _settingsService.Settings.TelegramSettings.BotToken;
        TelegramChatIdTextBox.Text = _settingsService.Settings.TelegramSettings.ChatIdsDisplay;
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        var strategyUrl = StrategyUrlTextBox.Text.Trim();
        var strategyToken = StrategyTokenTextBox.Text.Trim();

        if (!string.IsNullOrEmpty(strategyUrl) && !string.IsNullOrEmpty(strategyToken))
        {
            _apiService.Configure(strategyUrl, strategyToken);
            try
            {
                var cacheService = ((App)System.Windows.Application.Current).GetService<ICacheService>();
                var strategyService = new StrategyService(_apiService, cacheService);
                var stats = await strategyService.GetStatsAsync();
                if (stats != null)
                {
                    MessageBox.Show($"策略服务器连接成功！\n共有 {stats.TotalStrategies} 个策略",
                        "连接成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"策略服务器连接失败：\n{ex.Message}",
                    "连接失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        var marketUrl = MarketUrlTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(marketUrl))
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var response = await client.GetAsync($"{marketUrl}/api/health");
                MessageBox.Show("行情服务器连接成功！",
                    "连接成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show("行情服务器连接测试：无法连接到服务器。\n请确认地址正确且服务器已启动。",
                    "连接测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        if (string.IsNullOrEmpty(strategyUrl) && string.IsNullOrEmpty(marketUrl))
        {
            MessageBox.Show("请至少填写一个服务器配置后测试连接。",
                "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void TestTelegram_Click(object sender, RoutedEventArgs e)
    {
        var botToken = TelegramBotTokenTextBox.Text.Trim();
        var chatIdsText = TelegramChatIdTextBox.Text.Trim();

        if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(chatIdsText))
        {
            MessageBox.Show("请先填写 Bot Token 和 Chat ID",
                "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var chatIds = chatIdsText.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(c => c.Trim())
                                 .Where(c => !string.IsNullOrEmpty(c))
                                 .ToList();

        if (chatIds.Count == 0)
        {
            MessageBox.Show("请至少填写一个 Chat ID",
                "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var telegramService = new TelegramService();
            telegramService.Configure(botToken, chatIds);

            var results = await telegramService.SendTextToAllAsync("✅ *测试消息*\n\nTelegram 配置成功，机器人已正常工作！");

            var successCount = results.Count(r => r.Value);
            var failCount = results.Count(r => !r.Value);

            if (failCount == 0)
            {
                MessageBox.Show($"Telegram 连接测试成功！\n已成功发送到 {successCount} 个群。",
                    "测试成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Telegram 测试完成。\n成功: {successCount} 个群\n失败: {failCount} 个群",
                    "测试结果", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Telegram 测试失败：\n{ex.Message}",
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settingsService.UpdateStrategyServer(StrategyUrlTextBox.Text.Trim(), StrategyTokenTextBox.Text.Trim());
        _settingsService.UpdateMarketDataServer(MarketUrlTextBox.Text.Trim());

        decimal stopLossAmount = 10000m;
        int analysisDays = 5;

        if (decimal.TryParse(StopLossAmountTextBox.Text, out var sla) && sla > 0)
        {
            stopLossAmount = sla;
        }

        if (int.TryParse(AnalysisDaysTextBox.Text, out var days) && days > 0)
        {
            analysisDays = days;
        }

        _settingsService.UpdateValidationSettings(analysisDays, stopLossAmount);

        _settingsService.UpdateFeishuSettings(
            FeishuEnabledCheckBox.IsChecked == true,
            FeishuWebhookTextBox.Text.Trim(),
            FeishuBotNameTextBox.Text.Trim());

        _settingsService.UpdateFeishuImageSettings(
            FeishuImageEnabledCheckBox.IsChecked == true,
            FeishuImageAppIdTextBox.Text.Trim(),
            FeishuImageAppSecretTextBox.Text.Trim(),
            FeishuImageChatIdTextBox.Text.Trim(),
            FeishuImageUseCardCheckBox.IsChecked == true);

        _settingsService.UpdateTelegramSettings(
            TelegramEnabledCheckBox.IsChecked == true,
            TelegramBotTokenTextBox.Text.Trim(),
            TelegramChatIdTextBox.Text
                .Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList());

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
