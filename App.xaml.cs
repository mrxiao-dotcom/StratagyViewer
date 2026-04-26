using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using StrategyViewer.Services;
using StrategyViewer.ViewModels;

namespace StrategyViewer;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public T GetService<T>() where T : class
    {
        return _serviceProvider?.GetRequiredService<T>() ?? throw new InvalidOperationException($"Service {typeof(T).Name} not found");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // 初始化验证服务的参数
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var validationService = _serviceProvider.GetRequiredService<IValidationService>() as ValidationService;
        if (validationService != null)
        {
            validationService.SetStopLossAmount(settingsService.Settings.ValidationSettings.SingleTradeStopLossAmount);
        }

        var mainWindow = new Views.MainWindow();
        mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(sp => new HttpClient());
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ApiService>();
        services.AddSingleton<IStrategyService, StrategyService>();
        services.AddSingleton<IMarketDataService, MarketDataService>();
        services.AddSingleton<IContractParserService, ContractParserService>();
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddSingleton<IFeishuService, FeishuService>();
        services.AddSingleton<ITelegramService, TelegramService>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddTransient<MainViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
