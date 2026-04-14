using System.IO;
using Newtonsoft.Json;
using StrategyViewer.Models;

namespace StrategyViewer.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }
    void Save();
    void Load();
    void UpdateStrategyServer(string baseUrl, string token);
    void UpdateMarketDataServer(string baseUrl, string? token = null);
    void UpdateValidationSettings(int analysisDays, decimal stopLossAmount = 10000m);
}

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    public AppSettings Settings { get; private set; } = new();

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StrategyViewer");

        if (!Directory.Exists(appDataPath))
            Directory.CreateDirectory(appDataPath);

        _settingsPath = Path.Combine(appDataPath, "settings.json");
        Load();
    }

    public void Save()
    {
        var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
        File.WriteAllText(_settingsPath, json);
    }

    public void Load()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
                Settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                Settings = new AppSettings();
            }
        }
        else
        {
            Settings = new AppSettings();
        }
    }

    public void UpdateStrategyServer(string baseUrl, string token)
    {
        Settings.StrategyServer.BaseUrl = baseUrl;
        Settings.StrategyServer.Token = token;
        Save();
    }

    public void UpdateMarketDataServer(string baseUrl, string? token = null)
    {
        Settings.MarketDataServer.BaseUrl = baseUrl;
        Settings.MarketDataServer.Token = token ?? string.Empty;
        Save();
    }

    public void UpdateValidationSettings(int analysisDays, decimal stopLossAmount = 10000m)
    {
        Settings.ValidationSettings.AnalysisDays = analysisDays;
        Settings.ValidationSettings.SingleTradeStopLossAmount = stopLossAmount;
        Save();
    }
}
