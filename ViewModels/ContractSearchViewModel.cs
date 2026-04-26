using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StrategyViewer.Models;
using StrategyViewer.Services;

namespace StrategyViewer.ViewModels;

public partial class ContractSearchViewModel : ObservableObject
{
    private readonly IStrategyService _strategyService;
    private readonly IMarketDataService _marketDataService;
    private readonly IContractParserService _contractParserService;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ContractHistory> _results = new();

    [ObservableProperty]
    private ContractHistory? _selectedItem;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasSearched;

    // 按日期分组的数据
    [ObservableProperty]
    private ObservableCollection<DateGroup> _groupedByDate = new();

    // 按策略类型分组的数据
    [ObservableProperty]
    private ObservableCollection<StrategyTypeGroup> _groupedByStrategyType = new();

    [ObservableProperty]
    private DateGroup? _selectedDateGroup;

    // 分组模式: true=按策略类型, false=按日期
    [ObservableProperty]
    private bool _groupByStrategyType = true;

    // 图表数据（排除套利策略）
    [ObservableProperty]
    private ObservableCollection<ContractHistory> _chartSignals = new();

    // K线数据
    [ObservableProperty]
    private List<MarketData> _kLineData = new();

    // 当前选中品种的策略价格
    [ObservableProperty]
    private double _currentEntryPrice;

    [ObservableProperty]
    private double _currentStopLoss;

    [ObservableProperty]
    private double _currentTakeProfit;

    [ObservableProperty]
    private string _currentSymbol = string.Empty;

    private static bool IsArbitrageStrategy(ContractHistory item)
    {
        var title = item.StrategyTitle ?? "";
        var contract = item.Contract ?? "";
        return title.Contains("套利") || contract.Contains("套利");
    }

    public bool HasResults => Results.Count > 0;
    public bool ShowNoResults => HasSearched && !IsLoading && Results.Count == 0;
    public bool ShowSearchHint => !HasSearched && !IsLoading;

    // 品种分类按钮
    [ObservableProperty]
    private ObservableCollection<ProductCategory> _categories = new();

    public ContractSearchViewModel(IStrategyService strategyService, IMarketDataService marketDataService, IContractParserService contractParserService)
    {
        _strategyService = strategyService;
        _marketDataService = marketDataService;
        _contractParserService = contractParserService;
        InitializeCategories();
    }

    private void InitializeCategories()
    {
        Categories = new ObservableCollection<ProductCategory>
        {
            new ProductCategory
            {
                Name = "黑色系",
                Products = new ObservableCollection<ProductButton>
                {
                    new ProductButton { DisplayName = "螺纹钢", Code = "RB" },
                    new ProductButton { DisplayName = "铁矿石", Code = "I" },
                    new ProductButton { DisplayName = "热卷", Code = "HC" },
                    new ProductButton { DisplayName = "焦煤", Code = "JM" },
                    new ProductButton { DisplayName = "焦炭", Code = "J" },
                    new ProductButton { DisplayName = "动力煤", Code = "ZC" },
                    new ProductButton { DisplayName = "玻璃", Code = "FG" },
                    new ProductButton { DisplayName = "纯碱", Code = "SA" },
                }
            },
            new ProductCategory
            {
                Name = "化工系",
                Products = new ObservableCollection<ProductButton>
                {
                    new ProductButton { DisplayName = "甲醇", Code = "MA" },
                    new ProductButton { DisplayName = "尿素", Code = "UR" },
                    new ProductButton { DisplayName = "PTA", Code = "TA" },
                    new ProductButton { DisplayName = "乙二醇", Code = "EG" },
                    new ProductButton { DisplayName = "塑料", Code = "L" },
                    new ProductButton { DisplayName = "聚丙烯", Code = "PP" },
                    new ProductButton { DisplayName = "PVC", Code = "V" },
                    new ProductButton { DisplayName = "原油", Code = "SC" },
                    new ProductButton { DisplayName = "沥青", Code = "BU" },
                }
            },
            new ProductCategory
            {
                Name = "有色金属",
                Products = new ObservableCollection<ProductButton>
                {
                    new ProductButton { DisplayName = "铜", Code = "CU" },
                    new ProductButton { DisplayName = "铝", Code = "AL" },
                    new ProductButton { DisplayName = "锌", Code = "ZN" },
                    new ProductButton { DisplayName = "镍", Code = "NI" },
                    new ProductButton { DisplayName = "沪锡", Code = "SN" },
                }
            },
            new ProductCategory
            {
                Name = "贵金属",
                Products = new ObservableCollection<ProductButton>
                {
                    new ProductButton { DisplayName = "黄金", Code = "AU" },
                    new ProductButton { DisplayName = "白银", Code = "AG" },
                }
            },
            new ProductCategory
            {
                Name = "农产品",
                Products = new ObservableCollection<ProductButton>
                {
                    new ProductButton { DisplayName = "豆粕", Code = "M" },
                    new ProductButton { DisplayName = "豆油", Code = "Y" },
                    new ProductButton { DisplayName = "棕榈油", Code = "P" },
                    new ProductButton { DisplayName = "玉米", Code = "C" },
                    new ProductButton { DisplayName = "白糖", Code = "SR" },
                    new ProductButton { DisplayName = "棉花", Code = "CF" },
                    new ProductButton { DisplayName = "苹果", Code = "AP" },
                    new ProductButton { DisplayName = "红枣", Code = "CJ" },
                    new ProductButton { DisplayName = "粳米", Code = "JR" },
                }
            },
            new ProductCategory
            {
                Name = "金融期货",
                Products = new ObservableCollection<ProductButton>
                {
                    new ProductButton { DisplayName = "沪深300", Code = "IF" },
                    new ProductButton { DisplayName = "中证500", Code = "IC" },
                    new ProductButton { DisplayName = "中证1000", Code = "IM" },
                    new ProductButton { DisplayName = "上证50", Code = "IH" },
                }
            },
            new ProductCategory
            {
                Name = "国债期货",
                Products = new ObservableCollection<ProductButton>
                {
                    new ProductButton { DisplayName = "30年期国债", Code = "TL" },
                    new ProductButton { DisplayName = "10年期国债", Code = "T" },
                    new ProductButton { DisplayName = "5年期国债", Code = "TF" },
                    new ProductButton { DisplayName = "2年期国债", Code = "TS" },
                }
            },
        };
    }

    partial void OnResultsChanged(ObservableCollection<ContractHistory> value)
    {
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(ShowNoResults));
        OnPropertyChanged(nameof(ShowSearchHint));

        // 更新图表数据（排除套利策略）
        ChartSignals.Clear();
        foreach (var item in value.Where(r => !IsArbitrageStrategy(r)))
        {
            ChartSignals.Add(item);
        }

        UpdateGroupedByDate();

        // 自动选中第一个结果并加载K线数据
        if (value.Count > 0)
        {
            SelectedItem = value[0];
        }
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(ShowNoResults));
        OnPropertyChanged(nameof(ShowSearchHint));
    }

    partial void OnHasSearchedChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowNoResults));
        OnPropertyChanged(nameof(ShowSearchHint));
    }

    partial void OnSelectedItemChanged(ContractHistory? value)
    {
        // 当选中项改变时，刷新图表
        OnPropertyChanged(nameof(ChartSignals));

        // 加载选中品种的K线数据
        if (value != null)
        {
            _ = LoadKLineDataAsync(value);

            // 确保选中的项也在 ChartSignals 中（用于显示标记）
            if (!ChartSignals.Contains(value) && !IsArbitrageStrategy(value))
            {
                ChartSignals.Add(value);
            }
        }
    }

    private async Task LoadKLineDataAsync(ContractHistory item)
    {
        try
        {
            // 使用 ContractParserService 解析品种代码（支持中文品种名）
            var contracts = _contractParserService.ParseContracts(item.Contract ?? "");
            if (contracts.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[K线] 无法解析品种代码: {item.Contract}");
                return;
            }

            var symbol = contracts.First();
            CurrentSymbol = symbol;
            System.Diagnostics.Debug.WriteLine($"[K线] 开始加载 {symbol} 的K线数据...");

            // 设置时间范围：策略日期前后各取几天
            var strategyDate = item.TradeDate.Date;
            var startDate = strategyDate.AddDays(-5);
            var endDate = strategyDate.AddDays(10);

            // 获取15分钟K线数据
            var kLineData = await _marketDataService.GetMarketDataAsync(
                symbol, startDate, endDate, KLinePeriod.Min15);

            if (kLineData.Count > 0)
            {
                KLineData = kLineData;
                System.Diagnostics.Debug.WriteLine($"[K线] 加载成功，共 {kLineData.Count} 根K线");

                // 设置策略价格
                if (TryParsePrice(item.EntryRange, out var entry))
                    CurrentEntryPrice = entry;
                if (TryParsePrice(item.StopLoss, out var sl))
                    CurrentStopLoss = sl;
                if (TryParsePrice(item.TakeProfit, out var tp))
                    CurrentTakeProfit = tp;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[K线] 未获取到K线数据");
                KLineData = new List<MarketData>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[K线] 加载失败: {ex.Message}");
        }
    }

    private static bool TryParsePrice(string? priceStr, out double price)
    {
        price = 0;
        if (string.IsNullOrEmpty(priceStr)) return false;

        var cleaned = priceStr.Trim();
        var match = System.Text.RegularExpressions.Regex.Match(cleaned, @"[\d.]+");
        if (match.Success && double.TryParse(match.Value, out price))
        {
            return true;
        }
        return false;
    }

    public void SearchByProduct(string productCode)
    {
        System.Diagnostics.Debug.WriteLine($"[SearchByProduct] 开始搜索: {productCode}");
        SearchText = productCode;
        HasSearched = true;
        IsLoading = true;

        // 清除该品种的缓存以确保获取最新数据
        var cacheKey = $"{productCode}_30";
        if (_strategyService is StrategyService service)
        {
            // 手动触发搜索，不使用缓存
        }

        SearchCommand.Execute(null);
    }

    private void UpdateGroupedByDate()
    {
        GroupedByDate.Clear();
        var groups = Results
            .GroupBy(r => r.TradeDate.Date)
            .OrderByDescending(g => g.Key);

        foreach (var group in groups)
        {
            GroupedByDate.Add(new DateGroup
            {
                Date = group.Key,
                Items = new ObservableCollection<ContractHistory>(group.OrderByDescending(i => i.Confidence ?? 0))
            });
        }

        UpdateGroupedByStrategyType();
    }

    private void UpdateGroupedByStrategyType()
    {
        GroupedByStrategyType.Clear();

        // 定义策略类型顺序
        var strategyTypeOrder = new Dictionary<string, int>
        {
            { "黑色系策略", 1 },
            { "有色金属策略", 2 },
            { "贵金属策略", 3 },
            { "农产品策略", 4 },
            { "化工系策略", 5 },
            { "股指国债策略", 6 },
            { "能源策略", 7 },
            { "软商品策略", 8 },
        };

        var groups = Results
            .GroupBy(r => GetStrategyType(r.StrategyTitle ?? ""))
            .OrderBy(g => strategyTypeOrder.TryGetValue(g.Key, out var order) ? order : 99)
            .ThenBy(g => g.Key);

        foreach (var group in groups)
        {
            GroupedByStrategyType.Add(new StrategyTypeGroup
            {
                StrategyType = group.Key,
                Items = new ObservableCollection<ContractHistory>(group.OrderByDescending(i => i.Confidence ?? 0))
            });
        }
    }

    private string GetStrategyType(string strategyTitle)
    {
        if (string.IsNullOrEmpty(strategyTitle)) return "其他策略";

        if (strategyTitle.Contains("黑色系")) return "黑色系策略";
        if (strategyTitle.Contains("有色金属")) return "有色金属策略";
        if (strategyTitle.Contains("贵金属")) return "贵金属策略";
        if (strategyTitle.Contains("农产品")) return "农产品策略";
        if (strategyTitle.Contains("化工")) return "化工系策略";
        if (strategyTitle.Contains("股指") || strategyTitle.Contains("国债")) return "股指国债策略";
        if (strategyTitle.Contains("能源")) return "能源策略";
        if (strategyTitle.Contains("软商品")) return "软商品策略";

        return "其他策略";
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Results.Clear();
            HasSearched = false;
            return;
        }

        try
        {
            IsLoading = true;
            HasSearched = true;
            System.Diagnostics.Debug.WriteLine($"[搜索] 开始搜索品种: {SearchText}");

            var searchResults = await _strategyService.GetContractHistoryAsync(SearchText.Trim(), 30);

            System.Diagnostics.Debug.WriteLine($"[搜索] API返回数据: {searchResults?.Count ?? 0} 条记录");

            Results.Clear();
            if (searchResults != null)
            {
                foreach (var item in searchResults)
                {
                    Results.Add(item);
                }
            }

            System.Diagnostics.Debug.WriteLine($"[搜索] 搜索完成: Results.Count={Results.Count}");

            // 更新图表数据（排除套利策略）
            ChartSignals.Clear();
            foreach (var item in Results.Where(r => !IsArbitrageStrategy(r)))
            {
                ChartSignals.Add(item);
            }
            System.Diagnostics.Debug.WriteLine($"[搜索] 图表数据: ChartSignals.Count={ChartSignals.Count}");

            // 手动更新分组数据
            UpdateGroupedByDate();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[搜索] 搜索失败: {ex.Message}\n{ex.StackTrace}");
            Results.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ProductSearchAsync(string productCode)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            return;

        SearchText = productCode;
        await SearchAsync();
    }
}

public class DateGroup
{
    public DateTime Date { get; set; }
    public ObservableCollection<ContractHistory> Items { get; set; } = new();
    public string DateString => Date.ToString("yyyy-MM-dd");
}

public class StrategyTypeGroup
{
    public string StrategyType { get; set; } = "";
    public ObservableCollection<ContractHistory> Items { get; set; } = new();
    public int ItemCount => Items.Count;
}

public class ProductCategory
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<ProductButton> Products { get; set; } = new();
}

public class ProductButton
{
    public string DisplayName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
