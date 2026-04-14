using System.Net.Http;
using System.Windows;
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
                var strategyService = new StrategyService(_apiService);
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

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
