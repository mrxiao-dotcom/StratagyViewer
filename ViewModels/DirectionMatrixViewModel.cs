using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StrategyViewer.Models;
using StrategyViewer.Services;

namespace StrategyViewer.ViewModels;

public partial class DirectionMatrixViewModel : ObservableObject
{
    private readonly IContractParserService _contractParserService;

    [ObservableProperty]
    private ObservableCollection<DirectionRow> _rows = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "加载中...";

    [ObservableProperty]
    private ObservableCollection<DateTime> _dates = new();

    [ObservableProperty]
    private ObservableCollection<ProductInfo> _products = new();

    [ObservableProperty]
    private Dictionary<string, DirectionCell> _matrixData = new();

    public DirectionMatrixViewModel(IContractParserService contractParserService, List<ContractHistory> cachedContracts)
    {
        _contractParserService = contractParserService;
        LoadDataFromCache(cachedContracts);
    }

    private void LoadDataFromCache(List<ContractHistory> allContracts)
    {
        try
        {
            IsLoading = false; // 数据已经缓存，直接显示
            System.Diagnostics.Debug.WriteLine($"[矩阵] 从缓存加载 {allContracts?.Count ?? 0} 条合约历史");

            if (allContracts == null || allContracts.Count == 0)
            {
                StatusMessage = "没有找到策略数据";
                return;
            }

            // 按品种分组，获取所有品种
            var productGroups = allContracts
                .GroupBy(c => _contractParserService.NormalizeSymbol(_contractParserService.ParseContracts(c.Contract ?? "").FirstOrDefault() ?? ""))
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .OrderBy(g => g.Key)
                .ToList();

            // 调试：输出 AL 和 AO 相关的合约
            var alAoContracts = allContracts
                .Where(c => (c.Contract ?? "").Contains("AL", StringComparison.OrdinalIgnoreCase) ||
                           (c.Contract ?? "").Contains("AO", StringComparison.OrdinalIgnoreCase) ||
                           (c.Contract ?? "").Contains("铝", StringComparison.OrdinalIgnoreCase) ||
                           (c.Contract ?? "").Contains("氧化铝", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (alAoContracts.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[矩阵] AL/AO 相关合约 ({alAoContracts.Count}条):");
                foreach (var contract in alAoContracts.Take(10))
                {
                    var parsed = _contractParserService.ParseContracts(contract.Contract ?? "");
                    System.Diagnostics.Debug.WriteLine($"  日期:{contract.TradeDate:yyyy-MM-dd} 原始:'{contract.Contract}' -> 解析:[{string.Join(",", parsed)}]");
                }
            }

            // 获取品种中文名映射
            var productNames = GetProductNames();

            Products.Clear();
            foreach (var group in productGroups)
            {
                var code = group.Key;
                var name = productNames.TryGetValue(code, out var n) ? n : code;
                Products.Add(new ProductInfo { Code = code, Name = name });
            }

            // 按日期分组（从近往远）
            var dateGroups = allContracts
                .GroupBy(c => c.TradeDate.Date)
                .OrderByDescending(g => g.Key)
                .ToList();

            Dates.Clear();
            foreach (var group in dateGroups)
            {
                Dates.Add(group.Key);
            }

            // 构建交叉数据
            MatrixData.Clear();
            var missingContracts = new List<string>();

            foreach (var contract in allContracts)
            {
                // 跳过套利相关的策略
                if (IsArbitrageStrategy(contract))
                {
                    continue;
                }

                var codes = _contractParserService.ParseContracts(contract.Contract ?? "");
                if (codes.Count == 0)
                {
                    // 记录无法解析的合约
                    if (!string.IsNullOrWhiteSpace(contract.Contract))
                    {
                        missingContracts.Add($"{contract.TradeDate:yyyy-MM-dd}: {contract.Contract}");
                    }
                    continue;
                }

                // 支持多合约策略
                foreach (var code in codes)
                {
                    var normalizedCode = _contractParserService.NormalizeSymbol(code);
                    if (string.IsNullOrEmpty(normalizedCode)) continue;

                    var dateKey = contract.TradeDate.Date.ToString("yyyy-MM-dd");
                    var cellKey = $"{dateKey}|{normalizedCode}";

                    var direction = ParseDirection(contract.Direction);

                    // 如果已有数据，保留第一个非空方向
                    if (!MatrixData.ContainsKey(cellKey) || string.IsNullOrEmpty(MatrixData[cellKey].Direction))
                    {
                        MatrixData[cellKey] = new DirectionCell
                        {
                            Direction = direction,
                            Contract = contract.Contract ?? "",
                            StrategyTitle = contract.StrategyTitle ?? "",
                            EntryRange = contract.EntryRange ?? "",
                            StopLoss = contract.StopLoss ?? "",
                            TakeProfit = contract.TakeProfit ?? "",
                            Logic = contract.Logic ?? ""
                        };
                    }
                }
            }

            // 调试：输出无法解析的合约
            if (missingContracts.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[矩阵] 无法解析的合约 ({missingContracts.Count}):");
                foreach (var item in missingContracts.Take(10))
                {
                    System.Diagnostics.Debug.WriteLine($"  {item}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[矩阵] 矩阵数据条目数: {MatrixData.Count}");
            var sampleKeys = MatrixData.Keys.Take(20).ToList();
            System.Diagnostics.Debug.WriteLine($"[矩阵] 示例键: {string.Join(", ", sampleKeys)}");

            // 显示矩阵中存在的所有品种
            var matrixProducts = MatrixData.Keys.Select(k => k.Split('|')[1]).Distinct().OrderBy(p => p).ToList();
            System.Diagnostics.Debug.WriteLine($"[矩阵] 矩阵中的品种: {string.Join(", ", matrixProducts)}");

            // 构建行数据（日期行）
            Rows.Clear();
            foreach (var date in Dates)
            {
                var row = new DirectionRow
                {
                    Date = date,
                    DateString = date.ToString("yyyy-MM-dd"),
                    Cells = new ObservableCollection<DirectionCell>()
                };

                foreach (var product in Products)
                {
                    var cellKey = $"{date:yyyy-MM-dd}|{product.Code}";
                    if (MatrixData.TryGetValue(cellKey, out var cell))
                    {
                        row.Cells.Add(cell);
                    }
                    else
                    {
                        row.Cells.Add(new DirectionCell { Direction = "" });
                    }
                }

                Rows.Add(row);
            }

            StatusMessage = $"加载完成：{Products.Count} 个品种，{Dates.Count} 个日期";
            System.Diagnostics.Debug.WriteLine($"[矩阵] 构建完成：{Rows.Count} 行，{Products.Count} 列");
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败：{ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[矩阵] 错误：{ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Dictionary<string, string> GetProductNames()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "I", "铁矿石" },
            { "RB", "螺纹钢" },
            { "HC", "热卷" },
            { "JM", "焦煤" },
            { "J", "焦炭" },
            { "ZC", "动力煤" },
            { "FG", "玻璃" },
            { "SA", "纯碱" },
            { "MA", "甲醇" },
            { "UR", "尿素" },
            { "TA", "PTA" },
            { "EG", "乙二醇" },
            { "L", "塑料" },
            { "PP", "聚丙烯" },
            { "V", "PVC" },
            { "SC", "原油" },
            { "BU", "沥青" },
            { "CU", "铜" },
        { "AL", "铝" },
        { "氧化铝", "AO" },
            { "ZN", "锌" },
            { "NI", "镍" },
            { "SN", "锡" },
            { "AU", "黄金" },
            { "AG", "白银" },
            { "M", "豆粕" },
            { "Y", "豆油" },
            { "P", "棕榈油" },
            { "C", "玉米" },
            { "SR", "白糖" },
            { "CF", "棉花" },
            { "AP", "苹果" },
            { "CJ", "红枣" },
            { "JR", "粳米" },
            { "IF", "沪深300" },
            { "IC", "中证500" },
            { "IM", "中证1000" },
            { "IH", "上证50" },
        };
    }

    public DirectionCell? GetCell(DateTime date, string productCode)
    {
        var key = $"{date:yyyy-MM-dd}|{productCode}";
        return MatrixData.TryGetValue(key, out var cell) ? cell : null;
    }

    public string GetMatrixCellSymbol(string cellKey)
    {
        if (MatrixData.TryGetValue(cellKey, out var cell))
        {
            if (cell.IsLong) return "🔴";
            if (cell.IsShort) return "🟢";
        }
        return "⚪";
    }

    // 按大类折叠相关
    [ObservableProperty]
    private bool _isCollapsedByCategory;

    public string CollapseButtonText => IsCollapsedByCategory ? "展开品种" : "折叠大类";

    partial void OnIsCollapsedByCategoryChanged(bool value)
    {
        OnPropertyChanged(nameof(CollapseButtonText));
        RebuildRows();
    }

    [RelayCommand]
    private void ToggleCollapse()
    {
        IsCollapsedByCategory = !IsCollapsedByCategory;
        RebuildRows();
    }

    private void RebuildRows()
    {
        // 重新构建行数据
        Rows.Clear();
        foreach (var date in Dates)
        {
            var row = new DirectionRow
            {
                Date = date,
                DateString = date.ToString("yyyy-MM-dd"),
                Cells = new ObservableCollection<DirectionCell>()
            };

            var displayProducts = GetDisplayProducts();
            foreach (var product in displayProducts)
            {
                var cellKey = $"{date:yyyy-MM-dd}|{product.Code}";
                if (MatrixData.TryGetValue(cellKey, out var cell))
                {
                    row.Cells.Add(cell);
                }
                else
                {
                    row.Cells.Add(new DirectionCell { Direction = "" });
                }
            }

            Rows.Add(row);
        }

        StatusMessage = IsCollapsedByCategory
            ? $"折叠模式：{GetDisplayProducts().Count} 个大类"
            : $"展开模式：{Products.Count} 个品种";
    }

    private List<ProductInfo> GetDisplayProducts()
    {
        if (!IsCollapsedByCategory)
        {
            return Products.ToList();
        }

        // 按大类分组，每个大类只取第一个品种
        var categoryMap = GetCategoryMapping();
        var result = new List<ProductInfo>();
        var seenCategories = new HashSet<string>();

        foreach (var product in Products)
        {
            var category = categoryMap.TryGetValue(product.Code, out var cat) ? cat : product.Code;
            if (!seenCategories.Contains(category))
            {
                seenCategories.Add(category);
                result.Add(new ProductInfo
                {
                    Code = category,
                    Name = GetCategoryName(category),
                    IsCategoryHeader = true
                });
            }
        }

        return result;
    }

    private Dictionary<string, string> GetCategoryMapping()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // 黑色系
            { "I", "黑色" }, { "RB", "黑色" }, { "HC", "黑色" }, { "JM", "黑色" }, { "J", "黑色" },
            // 化工
            { "FG", "化工" }, { "SA", "化工" }, { "MA", "化工" }, { "UR", "化工" },
            { "TA", "化工" }, { "EG", "化工" }, { "L", "化工" }, { "PP", "化工" }, { "V", "化工" },
            // 能源
            { "SC", "能源" }, { "BU", "能源" }, { "ZC", "能源" },
            // 有色
            { "CU", "有色" }, { "AL", "有色" }, { "ZN", "有色" }, { "NI", "有色" }, { "SN", "有色" }, { "PB", "有色" },
            // 贵金属
            { "AU", "贵金" }, { "AG", "贵金" },
            // 农产品
            { "M", "农产" }, { "Y", "农产" }, { "P", "农产" }, { "C", "农产" },
            { "SR", "农产" }, { "CF", "农产" }, { "AP", "农产" }, { "CJ", "农产" },
            { "JR", "农产" }, { "RM", "农产" }, { "A", "农产" }, { "B", "农产" },
            // 金融
            { "IF", "金融" }, { "IC", "金融" }, { "IM", "金融" }, { "IH", "金融" }, { "T", "金融" }, { "TF", "金融" }, { "TS", "金融" },
        };
    }

    private string GetCategoryName(string category)
    {
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "黑色", "黑色系" }, { "化工", "化工系" }, { "能源", "能源系" },
            { "有色", "有色系" }, { "贵金", "贵金属" }, { "农产", "农产品" }, { "金融", "金融期" }
        };
        return names.TryGetValue(category, out var name) ? name : category;
    }

    private static bool IsArbitrageStrategy(ContractHistory contract)
    {
        if (string.IsNullOrEmpty(contract.StrategyTitle)) return false;
        var title = contract.StrategyTitle.ToUpper();
        // 套利策略关键词
        return title.Contains("套利") || title.Contains("跨期") || title.Contains("跨品种") ||
               title.Contains("价差") || title.Contains("ARB") || title.Contains("SPREAD");
    }

    private static string ParseDirection(string? direction)
    {
        if (string.IsNullOrEmpty(direction)) return "";

        direction = direction.ToUpper();
        // 做多相关
        if (direction.Contains("多") || direction.Contains("买") || direction.Contains("做多") ||
            direction.Contains("LONG") || direction.Contains("BUY"))
            return "多";
        // 做空相关
        if (direction.Contains("空") || direction.Contains("卖") || direction.Contains("做空") ||
            direction.Contains("SHORT") || direction.Contains("SELL"))
            return "空";

        return "";
    }
}

public class ProductInfo
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsCategoryHeader { get; set; }
    public string DisplayName => IsCategoryHeader ? $"{Name}系" : $"{Code} ({Name})";
}

public class DirectionRow
{
    public DateTime Date { get; set; }
    public string DateString { get; set; } = "";
    public ObservableCollection<DirectionCell> Cells { get; set; } = new();
    public bool IsExpanded { get; set; } = true;
}

public class DirectionCell
{
    public string Direction { get; set; } = "";  // "多", "空", ""
    public string Contract { get; set; } = "";
    public string StrategyTitle { get; set; } = "";
    // 摘要信息
    public string EntryRange { get; set; } = "";
    public string StopLoss { get; set; } = "";
    public string TakeProfit { get; set; } = "";
    public string Logic { get; set; } = "";

    public bool IsLong => Direction == "多";
    public bool IsShort => Direction == "空";
    public bool IsEmpty => string.IsNullOrEmpty(Direction);

    public string TooltipContent => IsEmpty ? "" :
        $"策略：{StrategyTitle}\n" +
        $"方向：{Direction}\n" +
        $"合约：{Contract}\n" +
        (string.IsNullOrEmpty(EntryRange) ? "" : $"进场：{EntryRange}\n") +
        (string.IsNullOrEmpty(StopLoss) ? "" : $"止损：{StopLoss}\n") +
        (string.IsNullOrEmpty(TakeProfit) ? "" : $"止盈：{TakeProfit}\n") +
        (string.IsNullOrEmpty(Logic) ? "" : $"\n说明：{Logic}");
}
