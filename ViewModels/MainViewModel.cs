using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using StrategyViewer.Models;
using StrategyViewer.Services;
using StrategyViewer.Views;

namespace StrategyViewer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly SKTypeface ChineseTypeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal)
        ?? SKTypeface.FromFamilyName("SimHei", SKFontStyle.Normal)
        ?? SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);

    private readonly IStrategyService _strategyService;
    private readonly IMarketDataService _marketDataService;
    private readonly IValidationService _validationService;
    private readonly ISettingsService _settingsService;
    private readonly ApiService _apiService;
    private CancellationTokenSource? _validationCts;

    [ObservableProperty]
    private ObservableCollection<StrategyListItem> _strategies = new();

    [ObservableProperty]
    private StrategyGroupCollection _strategyGroups = new();

    [ObservableProperty]
    private StrategyListItem? _selectedStrategy;

    [ObservableProperty]
    private ContractSelection? _selectedContract;

    [ObservableProperty]
    private Strategy? _currentStrategy;

    [ObservableProperty]
    private StrategySummary? _currentContractSummary;

    [ObservableProperty]
    private BacktestResult? _backtestResult;

    [ObservableProperty]
    private ValidationReport? _validationReport;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private bool _isStrategyServerConfigured;

    [ObservableProperty]
    private bool _isMarketServerConfigured;

    [ObservableProperty]
    private StrategyStats? _strategyStats;

    [ObservableProperty]
    private ObservableCollection<DailyPerformance> _dailyPerformances = new();

    [ObservableProperty]
    private ObservableCollection<TradeSignalRecord> _tradeSignals = new();

    [ObservableProperty]
    private ISeries[] _priceSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _dailyPnlSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _cumulativePnlSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _priceXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _priceYAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private ISeries[] _priceChartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _equityCurveSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _equityXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _equityYAxes = Array.Empty<Axis>();

    public MainViewModel(
        IStrategyService strategyService,
        IMarketDataService marketDataService,
        IValidationService validationService,
        ISettingsService settingsService,
        ApiService apiService)
    {
        _strategyService = strategyService;
        _marketDataService = marketDataService;
        _validationService = validationService;
        _settingsService = settingsService;
        _apiService = apiService;

        CheckServerConfiguration();
        _ = LoadStrategiesAsync();
    }

    public void CheckServerConfiguration()
    {
        var strategyConfig = _settingsService.Settings.StrategyServer;
        var marketConfig = _settingsService.Settings.MarketDataServer;

        IsStrategyServerConfigured = strategyConfig.IsConfigured;
        IsMarketServerConfigured = marketConfig.IsConfigured;

        if (IsStrategyServerConfigured)
        {
            _apiService.Configure(strategyConfig.BaseUrl, strategyConfig.Token);
        }
    }

    [RelayCommand]
    private async Task LoadStrategiesAsync()
    {
        if (!IsStrategyServerConfigured)
        {
            StatusMessage = "请先配置策略服务器";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "正在加载策略列表...";

            var strategyList = await _strategyService.GetStrategiesAsync();
            Strategies.Clear();
            foreach (var strategy in strategyList)
            {
                Strategies.Add(strategy);
            }

            StrategyGroups = StrategyGroupCollection.CreateFrom(Strategies);

            StrategyStats = await _strategyService.GetStatsAsync();
            StatusMessage = $"已加载 {Strategies.Count} 个策略";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedStrategyChanged(StrategyListItem? value)
    {
        // 策略选中时，不自动加载验证结果，等待用户点击品种
    }

    public void SelectStrategy(StrategyListItem strategy)
    {
        System.Diagnostics.Debug.WriteLine($"[ViewModel] SelectStrategy: {strategy.Title}");
        SelectedStrategy = strategy;
        SelectedContract = null;
        CurrentContractSummary = null;
        BacktestResult = null;
        _ = LoadStrategyDetailAsync(strategy);
    }

    public void SelectContract(ContractSelection contract)
    {
        // 取消之前的验证任务，防止竞态条件
        _validationCts?.Cancel();
        _validationCts = new CancellationTokenSource();
        var token = _validationCts.Token;

        SelectedContract = contract;

        // 如果当前策略没有 SummaryItems，需要先加载策略详情
        if (CurrentStrategy != null && CurrentStrategy.SummaryItems.Count == 0)
        {
            _ = LoadStrategyDetailAndSelectContractAsync(contract, token);
        }
        else
        {
            // 从已加载的策略中查找对应品种信息
            if (CurrentStrategy != null)
            {
                var summary = CurrentStrategy.SummaryItems.FirstOrDefault(s =>
                    s.Contract == contract.Contract && s.Direction == contract.Direction);
                CurrentContractSummary = summary;
            }
            // 触发品种验证
            _ = ValidateContractAsync(contract, token);
        }
    }

    private async Task LoadStrategyDetailAndSelectContractAsync(ContractSelection contract, CancellationToken token)
    {
        try
        {
            IsLoading = true;
            StatusMessage = $"正在加载策略详情 {contract.StrategyId}...";

            CurrentStrategy = await _strategyService.GetStrategyAsync(contract.StrategyId);

            if (token.IsCancellationRequested) return;

            if (CurrentStrategy != null)
            {
                var summary = CurrentStrategy.SummaryItems.FirstOrDefault(s =>
                    s.Contract == contract.Contract && s.Direction == contract.Direction);
                CurrentContractSummary = summary;

                // 刷新树视图以显示品种
                var strategy = Strategies.FirstOrDefault(s => s.Id == contract.StrategyId);
                if (strategy != null)
                {
                    strategy.UpdateSummaryItems(CurrentStrategy.SummaryItems);
                }

                // 触发品种验证
                await ValidateContractAsync(contract, token);
                if (!token.IsCancellationRequested)
                {
                    StatusMessage = $"策略已加载";
                }
            }
            else
            {
                StatusMessage = "策略加载失败";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "操作已取消";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadStrategyDetailAsync(StrategyListItem strategyListItem)
    {
        try
        {
            IsLoading = true;
            StatusMessage = $"正在加载策略详情 {strategyListItem.Id}...";

            CurrentStrategy = await _strategyService.GetStrategyAsync(strategyListItem.Id);

            if (CurrentStrategy != null)
            {
                strategyListItem.UpdateSummaryItems(CurrentStrategy.SummaryItems);
                StrategyGroups.RefreshContractNodes(strategyListItem.Id, CurrentStrategy.SummaryItems);
                StatusMessage = $"策略已加载，包含 {CurrentStrategy.SummaryItems.Count} 个品种";
            }
            else
            {
                StatusMessage = "策略加载失败";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败：{ex.Message}";
            CurrentStrategy = null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ValidateContractAsync(ContractSelection contractSelection, CancellationToken token)
    {
        if (CurrentStrategy == null)
        {
            StatusMessage = "请先选择一个策略";
            return;
        }

        if (!IsMarketServerConfigured)
        {
            StatusMessage = "请先配置行情服务器";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = $"正在回测 {contractSelection.Contract}...";

            // 从策略的 SummaryItems 中找到对应的合约信息
            var contractInfo = CurrentStrategy.SummaryItems.FirstOrDefault(s =>
                s.Contract == contractSelection.Contract && s.Direction == contractSelection.Direction);

            if (contractInfo == null)
            {
                StatusMessage = "未找到合约信息";
                return;
            }

            var backtestResult = await _validationService.ValidateAsync(contractInfo, CurrentStrategy.TradeDate);

            if (token.IsCancellationRequested) return;

            if (backtestResult != null)
            {
                BacktestResult = backtestResult;
                UpdateChartData();
                StatusMessage = $"回测完成：{BacktestResult.Result} ({BacktestResult.PnL:F2})";
            }
            else
            {
                StatusMessage = "无法获取行情数据，请检查配置";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "操作已取消";
        }
        catch (Exception ex)
        {
            var msg = ex.GetBaseException().Message;
            System.Diagnostics.Debug.WriteLine($"[错误] {ex.GetType().Name}: {msg}");
            StatusMessage = $"回测失败：{msg}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateChartData()
    {
        if (BacktestResult == null || BacktestResult.KLineResults.Count == 0)
            return;

        // 清空交易信号记录
        TradeSignals.Clear();
        DailyPerformances.Clear();
        decimal cumulativePnL = 0;

        // 记录开仓信号
        if (BacktestResult.Opened && BacktestResult.OpenTime.HasValue)
        {
            TradeSignals.Add(new TradeSignalRecord
            {
                Time = BacktestResult.OpenTime.Value,
                SignalType = "开仓",
                Price = BacktestResult.EntryPrice,
                Lots = BacktestResult.Lots,
                Description = BacktestResult.OpenReason
            });
        }

        foreach (var kLine in BacktestResult.KLineResults)
        {
            var perf = new DailyPerformance
            {
                Date = kLine.Date,
                OpenPrice = kLine.Open,
                HighPrice = kLine.High,
                LowPrice = kLine.Low,
                ClosePrice = kLine.Close,
                DailyPnL = kLine.PnL ?? 0,
                CumulativePnL = cumulativePnL + (kLine.PnL ?? 0)
            };
            cumulativePnL = perf.CumulativePnL;
            DailyPerformances.Add(perf);

            // 记录有Action的K线（开仓、止损、止盈）
            if (!string.IsNullOrEmpty(kLine.Action))
            {
                decimal signalPrice = kLine.Action switch
                {
                    "止损" => kLine.Low,
                    "止盈" => kLine.High,
                    "收盘" => kLine.Close,
                    _ => kLine.Close
                };

                TradeSignals.Add(new TradeSignalRecord
                {
                    Time = kLine.Date,
                    SignalType = kLine.Action,
                    Price = signalPrice,
                    PnL = kLine.PnL,
                    Description = $"{kLine.Action} @ {signalPrice:F2}"
                });
            }
        }

        System.Diagnostics.Debug.WriteLine($"[ViewModel] 更新图表数据: {DailyPerformances.Count} 根K线, {TradeSignals.Count} 个交易信号");

        // 使用Canvas绘制蜡烛图
        var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        mainWindow?.DrawCandlestickChart(BacktestResult);

        // 资金曲线（浮动盈亏）
        var equityKLines = BacktestResult.KLineResults.Where(k => k.FloatingPnL.HasValue).ToList();
        var equityValues = equityKLines.Select(k => (double)k.FloatingPnL!.Value).ToArray();
        var equityXLabels = equityKLines.Select(k => k.Date.ToString("MM-dd HH:mm")).ToArray();
        var cumulativeValues = new double[equityValues.Length];
        double cumSum = 0;
        for (int i = 0; i < equityValues.Length; i++)
        {
            cumSum += equityValues[i];
            cumulativeValues[i] = cumSum;
        }

        EquityCurveSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = equityValues,
                Name = "Floating PnL",
                Stroke = new SolidColorPaint(new SKColor(250, 176, 64)) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(new SKColor(250, 176, 64, 30)),
                GeometrySize = 4,
                GeometryStroke = new SolidColorPaint(new SKColor(250, 176, 64)) { StrokeThickness = 1 },
                GeometryFill = new SolidColorPaint(new SKColor(30, 30, 46)),
            }
        };

        // 资金曲线X轴
        EquityXAxes = new Axis[]
        {
            new Axis
            {
                Labels = equityXLabels,
                LabelsRotation = 45,
                TextSize = 10,
                LabelsPaint = new SolidColorPaint(new SKColor(108, 112, 134)),
            }
        };

        // 资金曲线Y轴
        if (equityValues.Length > 0)
        {
            EquityYAxes = new Axis[]
            {
                new Axis
                {
                    TextSize = 10,
                    LabelsPaint = new SolidColorPaint(new SKColor(108, 112, 134)),
                    MinLimit = equityValues.Min() * 1.2,
                    MaxLimit = equityValues.Max() * 1.2,
                }
            };
        }
    }

[RelayCommand]
private void OpenSettings()
{
    var settingsWindow = new SettingsWindow(_settingsService, _apiService);
    settingsWindow.Owner = System.Windows.Application.Current.MainWindow;
    if (settingsWindow.ShowDialog() == true)
    {
        CheckServerConfiguration();
        StatusMessage = "设置已保存";
    }
}
}
