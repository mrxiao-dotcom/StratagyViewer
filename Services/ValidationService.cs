using StrategyViewer.Models;

namespace StrategyViewer.Services;

public class BacktestResult
{
    public string Contract { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public int Lots { get; set; }
    public decimal ContractMultiplier { get; set; }
    public bool Opened { get; set; }
    public string OpenReason { get; set; } = string.Empty;
    public DateTime? OpenTime { get; set; }
    public string Result { get; set; } = string.Empty;
    public decimal PnL { get; set; }
    public DateTime? CloseTime { get; set; }
    public List<KLineResult> KLineResults { get; set; } = new();
}

public class KLineResult
{
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public string Action { get; set; } = string.Empty;
    public decimal? PnL { get; set; }
    public decimal? FloatingPnL { get; set; }  // 开仓后到该K线收盘时的浮动盈亏
}

public interface IValidationService
{
    Task<BacktestResult> ValidateAsync(StrategySummary summary, DateTime tradeDate, CancellationToken cancellationToken = default);
}

public class ValidationService : IValidationService
{
    private readonly IMarketDataService _marketDataService;
    private decimal _singleTradeStopLossAmount = 10000m;  // 单笔止损金额
    
    public void SetStopLossAmount(decimal amount) => _singleTradeStopLossAmount = amount;
    
    // 合约乘数映射
    private static readonly Dictionary<string, decimal> ContractMultipliers = new(StringComparer.OrdinalIgnoreCase)
    {
        // 股指期货
        { "IF", 300m },    // 上证50
        { "IC", 200m },    // 中证500
        { "IM", 200m },    // 中证1000
        { "IH", 300m },    // 沪深300
        { "T", 10000m },   // 10年期国债
        { "TF", 10000m },  // 5年期国债
        { "TS", 20000m },  // 2年期国债
        { "TL", 10000m },  // 30年期国债
        
        // 商品期货
        { "AU", 1000m },   // 黄金
        { "AG", 15m },     // 白银
        { "CU", 5m },      // 铜
        { "AL", 5m },      // 铝
        { "ZN", 5m },      // 锌
        { "PB", 5m },      // 铅
        { "NI", 1m },      // 镍
        { "SN", 1m },      // 锡
        { "RB", 10m },     // 螺纹钢
        { "HC", 10m },     // 热轧卷板
        { "SS", 10m },     // 不锈钢
        { "FG", 20m },     // 玻璃
        { "SA", 20m },     // 纯碱
        { "JM", 60m },     // 焦煤
        { "J", 100m },     // 焦炭
        { "I", 100m },     // 铁矿石
        { "原油", 1000m },
        { "SC", 1000m },   // 原油
        { "BU", 10m },     // 沥青
        { "L", 5m },       // 聚乙烯
        { "V", 5m },       // 聚氯乙烯
        { "PP", 5m },      // 聚丙烯
        { "MA", 10m },     // 甲醇
        { "EG", 10m },     // 乙二醇
        { "TA", 5m },      // PTA
        { "ME", 10m },     // 甲醇
        { "RM", 10m },     // 菜粕
        { "M", 10m },      // 豆粕
        { "Y", 10m },      // 豆油
        { "A", 10m },      // 豆一
        { "B", 10m },      // 豆二
        { "CS", 10m },     // 玉米淀粉
        { "C", 10m },      // 玉米
        { "JD", 10m },     // 鸡蛋
        { "LH", 16m },     // 生猪
        { "P", 10m },      // 棕榈油
        { "OI", 10m },     // 菜油
        { "CF", 5m },      // 棉花
        { "SR", 10m },     // 白糖
        { "WH", 20m },     // 强麦
        { "PM", 50m },     // 普通小麦
        { "RI", 20m },     // 早籼稻
        { "LR", 20m },     // 晚籼稻
        { "JR", 20m },     // 粳稻
        { "SM", 5m },      // 锰硅
        { "SF", 5m },      // 硅铁
        { "UR", 10m },     // 尿素
        { "PK", 4m },      // 花生
        { "AP", 10m },     // 苹果
        { "CJ", 5m },      // 红枣
        { "ZC", 100m },    // 动力煤
        { "焦煤", 60m },
        { "焦炭", 100m },
        { "螺纹钢", 10m },
        { "热卷", 10m },
        { "铁矿石", 100m },
        { "铜", 5m },
        { "铝", 5m },
        { "锌", 5m },
        { "镍", 1m },
        { "锡", 1m },
        { "黄金", 1000m },
        { "白银", 15m },
        { "沥青", 10m },
        { "橡胶", 10m },
        { "RU", 10m },     // 橡胶
        { "NR", 10m },     // 20号胶
        
        // 期权
        { "沪铜期权", 5m },
        { "沪铝期权", 5m },
        { "沪锌期权", 5m },
        { "沪镍期权", 1m },
        { "沪锡期权", 1m },
        { "螺纹钢期权", 10m },
        { "热卷期权", 10m },
        { "铁矿石期权", 100m },
        { "豆粕期权", 10m },
        { "玉米期权", 10m },
        { "棉花期权", 5m },
        { "白糖期权", 10m },
        { "橡胶期权", 10m },
        { "黄金期权", 1000m },
        
        // 欧线集运
        { "EC", 50m },     // 欧线
        { "LC", 50m },     // 伦铜
        { "SI", 10m },     // 银
        { "PS", 50m },     // 巴油
    };

    public ValidationService(IMarketDataService marketDataService)
    {
        _marketDataService = marketDataService;
    }

    public async Task<BacktestResult> ValidateAsync(StrategySummary summary, DateTime tradeDate, CancellationToken cancellationToken = default)
    {
        // 提取合约代码（移除中文名称和括号）
        var contractCode = ExtractContractCode(summary.Contract);
        System.Diagnostics.Debug.WriteLine($"[验证] 开始验证 {summary.Contract} -> {contractCode} ({summary.Direction}), 交易日期: {tradeDate:yyyy-MM-dd}");
        
        var result = new BacktestResult
        {
            Contract = summary.Contract,
            Direction = summary.Direction,
        };

        // 跳过套利策略（包含两个合约）
        if (summary.Contract.Contains(",") || summary.Contract.Contains("&") || summary.Contract.Contains("/"))
        {
            System.Diagnostics.Debug.WriteLine($"[验证] 跳过套利策略: {summary.Contract}");
            result.Result = "套利策略(暂不支持)";
            return result;
        }

        // 解析进场价、止损价、止盈价
        if (!TryParsePrice(summary.EntryRange, out var entryPrice) ||
            !TryParsePrice(summary.StopLoss, out var stopLoss) ||
            !TryParsePrice(summary.TakeProfit, out var takeProfit))
        {
            System.Diagnostics.Debug.WriteLine($"[验证] 价格解析失败: 进场={summary.EntryRange}, 止损={summary.StopLoss}, 止盈={summary.TakeProfit}");
            result.Result = "价格解析失败";
            return result;
        }

        result.EntryPrice = entryPrice;
        result.StopLoss = stopLoss;
        result.TakeProfit = takeProfit;
        result.ContractMultiplier = GetContractMultiplier(summary.Contract);
        
        System.Diagnostics.Debug.WriteLine($"[验证] 价格信息: 进场={entryPrice:F2}, 止损={stopLoss:F2}, 止盈={takeProfit:F2}, 乘数={result.ContractMultiplier}");

        // 获取K线数据
        // 期货交易时间：日盘9:00-15:00，夜盘21:00-次日2:30
        // 策略信号通常在收盘后发出，夜盘还没开始
        // 所以从策略日期当天21:00开始获取数据
        var startDate = tradeDate.Date.AddHours(21);   // 策略当天21:00（夜盘）
        var endDate = tradeDate.Date.AddDays(8).AddHours(3);  // 往后8天到凌晨3:00
        
        System.Diagnostics.Debug.WriteLine($"[验证] 请求K线时间范围: {startDate:yyyy-MM-dd HH:mm} ~ {endDate:yyyy-MM-dd HH:mm}");
        
        var kLines = await _marketDataService.GetMarketDataAsync(
            contractCode, 
            startDate, 
            endDate, 
            KLinePeriod.Min15,
            cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            result.Result = "已取消";
            return result;
        }

        if (kLines.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[验证] 无行情数据");
            result.Result = "无行情数据";
            return result;
        }

        System.Diagnostics.Debug.WriteLine($"[验证] 获取K线: 请求范围 {startDate:yyyy-MM-dd HH:mm}~{endDate:yyyy-MM-dd HH:mm}, 实际获取 {kLines.Count} 根, 数据范围 {kLines[0].Date:yyyy-MM-dd HH:mm}~{kLines.Last().Date:yyyy-MM-dd HH:mm}");

        result.KLineResults = kLines.Select(k => new KLineResult
        {
            Date = k.Date,
            Open = k.Open,
            High = k.High,
            Low = k.Low,
            Close = k.Close,
            Action = "未开仓"  // 初始状态，在验证方法中更新
        }).ToList();

        // 判断方向
        var isLong = summary.Direction.Contains("多") || summary.Direction == "long";
        var isShort = summary.Direction.Contains("空") || summary.Direction == "short";

        if (isLong)
        {
            System.Diagnostics.Debug.WriteLine($"[验证] 做多策略");
            return ValidateLong(result, kLines, entryPrice, stopLoss, takeProfit);
        }
        else if (isShort)
        {
            System.Diagnostics.Debug.WriteLine($"[验证] 做空策略");
            return ValidateShort(result, kLines, entryPrice, stopLoss, takeProfit);
        }

        System.Diagnostics.Debug.WriteLine($"[验证] 方向未知: {summary.Direction}");
        result.Result = "方向未知";
        return result;
    }

    private BacktestResult ValidateLong(BacktestResult result, List<MarketData> kLines, 
        decimal entryPrice, decimal stopLoss, decimal takeProfit)
    {
        // 检查开仓条件：最低价 < 进场价
        var openKLine = kLines.FirstOrDefault(k => k.Low < entryPrice);
        
        if (openKLine == null)
        {
            System.Diagnostics.Debug.WriteLine($"[验证] 未触发开仓: 首根K线最低价({kLines[0].Low:F2})未低于进场价({entryPrice:F2})");
            result.Result = "未触发开仓";
            result.Opened = false;
            result.OpenReason = $"首根K线最低价({kLines[0].Low:F2})未低于进场价({entryPrice:F2})";
            return result;
        }

        result.Opened = true;
        result.OpenTime = openKLine.Date;
        result.OpenReason = $"K线({openKLine.Date:yyyy-MM-dd HH:mm})最低价({openKLine.Low:F2})低于进场价({entryPrice:F2})";
        System.Diagnostics.Debug.WriteLine($"[验证] 【开仓信号】时间: {openKLine.Date:yyyy-MM-dd HH:mm}, 价格: {openKLine.Low:F2}");

        // 计算手数 (以损定量)
        var priceDiff = entryPrice - stopLoss;
        if (priceDiff <= 0)
        {
            System.Diagnostics.Debug.WriteLine($"[验证] 参数错误: 止损价({stopLoss:F2}) >= 进场价({entryPrice:F2})");
            result.Result = "参数错误";
            return result;
        }
        
        // 以损定量：手数 = 单笔止损金额 / (止损距离 * 合约乘数)
        result.Lots = Math.Max(1, (int)(_singleTradeStopLossAmount / (priceDiff * result.ContractMultiplier)));
        System.Diagnostics.Debug.WriteLine($"[验证] 开仓手数: {result.Lots}, 单笔止损金额: {_singleTradeStopLossAmount:F0}, 止损距离: {priceDiff:F2}");

        // 从开仓K线之后开始，先检查止损/止盈，再计算浮动盈亏
        var openedKLines = kLines.Where(k => k.Date >= openKLine.Date).OrderBy(k => k.Date).ToList();
        DateTime? closeTime = null;

        // 先处理开仓K线本身（标记为开仓，不计算浮动盈亏）
        var firstKLineResult = result.KLineResults.First(r => r.Date == openKLine.Date);
        firstKLineResult.Action = "开仓";
        firstKLineResult.FloatingPnL = 0;  // 开仓时浮动盈亏为0

        // 从第二根K线开始检查
        for (int i = 1; i < openedKLines.Count; i++)
        {
            var kLine = openedKLines[i];
            var kLineResult = result.KLineResults.First(r => r.Date == kLine.Date);
            kLineResult.Action = "持仓";

            // 先检查止损
            if (kLine.Low <= stopLoss)
            {
                System.Diagnostics.Debug.WriteLine($"[验证] 【止损信号】时间: {kLine.Date:yyyy-MM-dd HH:mm}, 价格: {kLine.Low:F2}");
                result.Result = "止损";
                closeTime = kLine.Date;
                result.PnL = -result.Lots * (entryPrice - stopLoss) * result.ContractMultiplier;
                kLineResult.Action = "止损";
                kLineResult.PnL = result.PnL;
                System.Diagnostics.Debug.WriteLine($"[验证] 止损完成: 盈亏={result.PnL:F2}");
                break;
            }

            // 再检查止盈
            if (kLine.High >= takeProfit)
            {
                System.Diagnostics.Debug.WriteLine($"[验证] 【止盈信号】时间: {kLine.Date:yyyy-MM-dd HH:mm}, 价格: {kLine.High:F2}");
                result.Result = "止盈";
                closeTime = kLine.Date;
                result.PnL = result.Lots * (takeProfit - entryPrice) * result.ContractMultiplier;
                kLineResult.Action = "止盈";
                kLineResult.PnL = result.PnL;
                System.Diagnostics.Debug.WriteLine($"[验证] 止盈完成: 盈亏={result.PnL:F2}");
                break;
            }

            // 计算浮动盈亏（以收盘价计算）
            kLineResult.FloatingPnL = result.Lots * (kLine.Close - entryPrice) * result.ContractMultiplier;
        }

        // 如果没有触发止损/止盈，持有到最后
        if (closeTime == null)
        {
            var lastKLine = openedKLines.Last();
            result.Result = "持仓中";
            closeTime = lastKLine.Date;
            result.PnL = result.Lots * (lastKLine.Close - entryPrice) * result.ContractMultiplier;
            
            var lastResult = result.KLineResults.First(r => r.Date == lastKLine.Date);
            lastResult.Action = "收盘";
            lastResult.PnL = result.PnL;
            System.Diagnostics.Debug.WriteLine($"[验证] 持仓中: 收盘价={lastKLine.Close:F2}, 盈亏={result.PnL:F2}");
        }

        result.CloseTime = closeTime;
        return result;
    }

    private BacktestResult ValidateShort(BacktestResult result, List<MarketData> kLines, 
        decimal entryPrice, decimal stopLoss, decimal takeProfit)
    {
        // 检查开仓条件：最高价 > 进场价
        var openKLine = kLines.FirstOrDefault(k => k.High > entryPrice);
        
        if (openKLine == null)
        {
            System.Diagnostics.Debug.WriteLine($"[验证] 未触发开仓: 首根K线最高价({kLines[0].High:F2})未高于进场价({entryPrice:F2})");
            result.Result = "未触发开仓";
            result.Opened = false;
            result.OpenReason = $"首根K线最高价({kLines[0].High:F2})未高于进场价({entryPrice:F2})";
            return result;
        }

        result.Opened = true;
        result.OpenTime = openKLine.Date;
        result.OpenReason = $"K线({openKLine.Date:yyyy-MM-dd HH:mm})最高价({openKLine.High:F2})高于进场价({entryPrice:F2})";
        System.Diagnostics.Debug.WriteLine($"[验证] 【开仓信号】时间: {openKLine.Date:yyyy-MM-dd HH:mm}, 价格: {openKLine.High:F2}");

        // 计算手数 (以损定量)
        var priceDiff = stopLoss - entryPrice;
        if (priceDiff <= 0)
        {
            System.Diagnostics.Debug.WriteLine($"[验证] 参数错误: 止损价({stopLoss:F2}) <= 进场价({entryPrice:F2})");
            result.Result = "参数错误";
            return result;
        }
        
        // 以损定量：手数 = 单笔止损金额 / (止损距离 * 合约乘数)
        result.Lots = Math.Max(1, (int)(_singleTradeStopLossAmount / (priceDiff * result.ContractMultiplier)));
        System.Diagnostics.Debug.WriteLine($"[验证] 开仓手数: {result.Lots}, 单笔止损金额: {_singleTradeStopLossAmount:F0}, 止损距离: {priceDiff:F2}");

        // 从开仓K线之后开始，先检查止损/止盈，再计算浮动盈亏
        var openedKLines = kLines.Where(k => k.Date >= openKLine.Date).OrderBy(k => k.Date).ToList();
        DateTime? closeTime = null;

        // 先处理开仓K线本身（标记为开仓，不计算浮动盈亏）
        var firstKLineResult = result.KLineResults.First(r => r.Date == openKLine.Date);
        firstKLineResult.Action = "开仓";
        firstKLineResult.FloatingPnL = 0;  // 开仓时浮动盈亏为0

        // 从第二根K线开始检查
        for (int i = 1; i < openedKLines.Count; i++)
        {
            var kLine = openedKLines[i];
            var kLineResult = result.KLineResults.First(r => r.Date == kLine.Date);
            kLineResult.Action = "持仓";

            // 先检查止损（做空时止损价 > 进场价）
            if (kLine.High >= stopLoss)
            {
                System.Diagnostics.Debug.WriteLine($"[验证] 【止损信号】时间: {kLine.Date:yyyy-MM-dd HH:mm}, 价格: {kLine.High:F2}");
                result.Result = "止损";
                closeTime = kLine.Date;
                result.PnL = -result.Lots * (stopLoss - entryPrice) * result.ContractMultiplier;
                kLineResult.Action = "止损";
                kLineResult.PnL = result.PnL;
                System.Diagnostics.Debug.WriteLine($"[验证] 止损完成: 盈亏={result.PnL:F2}");
                break;
            }

            // 再检查止盈（做空时止盈价 < 进场价）
            if (kLine.Low <= takeProfit)
            {
                System.Diagnostics.Debug.WriteLine($"[验证] 【止盈信号】时间: {kLine.Date:yyyy-MM-dd HH:mm}, 价格: {kLine.Low:F2}");
                result.Result = "止盈";
                closeTime = kLine.Date;
                result.PnL = result.Lots * (entryPrice - takeProfit) * result.ContractMultiplier;
                kLineResult.Action = "止盈";
                kLineResult.PnL = result.PnL;
                System.Diagnostics.Debug.WriteLine($"[验证] 止盈完成: 盈亏={result.PnL:F2}");
                break;
            }

            // 计算浮动盈亏（以收盘价计算，做空时方向相反）
            kLineResult.FloatingPnL = result.Lots * (entryPrice - kLine.Close) * result.ContractMultiplier;
        }

        // 如果没有触发止损/止盈，持有到最后
        if (closeTime == null)
        {
            var lastKLine = openedKLines.Last();
            result.Result = "持仓中";
            closeTime = lastKLine.Date;
            result.PnL = result.Lots * (entryPrice - lastKLine.Close) * result.ContractMultiplier;
            
            var lastResult = result.KLineResults.First(r => r.Date == lastKLine.Date);
            lastResult.Action = "收盘";
            lastResult.PnL = result.PnL;
            System.Diagnostics.Debug.WriteLine($"[验证] 持仓中: 收盘价={lastKLine.Close:F2}, 盈亏={result.PnL:F2}");
        }

        result.CloseTime = closeTime;
        return result;
    }

    private bool TryParsePrice(string? priceText, out decimal price)
    {
        price = 0;
        if (string.IsNullOrWhiteSpace(priceText))
            return false;

        // 先移除所有逗号（处理 "1,980" 格式）
        var cleanText = priceText.Replace(",", "").Trim();

        // 提取数字
        var numberMatch = System.Text.RegularExpressions.Regex.Match(cleanText, @"[\d.]+");
        if (numberMatch.Success && decimal.TryParse(numberMatch.Value, out price))
        {
            System.Diagnostics.Debug.WriteLine($"[价格解析] 从 '{priceText}' 提取数字: {price}");
            return true;
        }

        // 尝试解析 "3800-3900" 格式，取中间值
        if (priceText.Contains("-"))
        {
            var parts = priceText.Split('-');
            if (parts.Length == 2 && 
                decimal.TryParse(parts[0].Trim().Replace(",", ""), out var low) && 
                decimal.TryParse(parts[1].Trim().Replace(",", ""), out var high))
            {
                price = (low + high) / 2;
                return true;
            }
        }

        return false;
    }

    private decimal GetContractMultiplier(string contract)
    {
        // 尝试精确匹配
        if (ContractMultipliers.TryGetValue(contract, out var multiplier))
            return multiplier;

        // 尝试匹配前缀（如 IC2401 -> IC）
        foreach (var key in ContractMultipliers.Keys)
        {
            if (contract.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                return ContractMultipliers[key];
        }

        // 默认值
        return 10m;
    }

    private string ExtractContractCode(string contractText)
    {
        if (string.IsNullOrWhiteSpace(contractText))
            return contractText;

        // 先提取括号中的内容作为合约代码
        var match = System.Text.RegularExpressions.Regex.Match(contractText, @"\(([^)]+)\)");
        if (match.Success)
        {
            var code = match.Groups[1].Value;
            System.Diagnostics.Debug.WriteLine($"[合约提取] 从 '{contractText}' 提取合约代码: {code}");
            return code;
        }

        // 如果没有括号，尝试提取字母+数字组合
        var codeMatch = System.Text.RegularExpressions.Regex.Match(contractText, @"([A-Za-z]+\d+)", System.Text.RegularExpressions.RegexOptions.RightToLeft);
        if (codeMatch.Success)
        {
            System.Diagnostics.Debug.WriteLine($"[合约提取] 从 '{contractText}' 提取合约代码: {codeMatch.Value}");
            return codeMatch.Value;
        }

        // 直接返回原始文本
        return contractText;
    }
}
