namespace StrategyViewer.Services;

public interface IContractParserService
{
    List<string> ParseContracts(string contractText);
    string NormalizeSymbol(string symbol);
}

public class ContractParserService : IContractParserService
{
    private static readonly Dictionary<string, string> SymbolMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // 黑色系
        { "螺纹钢", "RB" },
        { "螺纹", "RB" },
        { "铁矿石", "I" },
        { "热卷", "HC" },
        { "冷轧", "HC" },
        { "焦煤", "JM" },
        { "焦炭", "J" },
        { "动力煤", "ZC" },
        { "澳煤", "TL" },
        { "锰硅", "SM" },
        { "硅铁", "SF" },
        { "不锈钢", "SS" },

        // 化工系
        { "玻璃", "FG" },
        { "纯碱", "SA" },
        { "甲醇", "MA" },
        { "尿素", "UR" },
        { "PTA", "TA" },
        { "乙二醇", "EG" },
        { "塑料", "L" },
        { "聚乙烯", "L" },
        { "聚丙烯", "PP" },
        { "PVC", "V" },
        { "沥青", "BU" },

        // 原油系
        { "原油", "SC" },
        { "燃料油", "FU" },
        { "低硫燃料油", "LU" },
        { "LU", "LU" },

        // 有色金属
        { "铜", "CU" },
        { "铝", "AL" },
        { "锌", "ZN" },
        { "镍", "NI" },
        { "锡", "SN" },
        { "黄金", "AU" },
        { "白银", "AG" },
        { "铝锭", "AL" },
        { "电解铜", "CU" },
        { "碳酸锂", "LC" },
        { "工业硅", "SI" },
        { "硅", "SI" },

        // 贵金属
        { "铂金", "PT" },
        { "钯金", "PD" },

        // 农产品
        { "豆粕", "M" },
        { "豆油", "Y" },
        { "棕榈油", "P" },
        { "菜粕", "RM" },
        { "菜油", "OI" },
        { "玉米", "C" },
        { "淀粉", "CS" },
        { "白糖", "SR" },
        { "棉花", "CF" },
        { "棉纱", "CY" },
        { "苹果", "AP" },
        { "红枣", "CJ" },
        { "粳米", "JR" },
        { "花生", "PK" },
        { "鸡蛋", "JD" },
        { "生猪", "LH" },
        { "多晶硅", "PS" },
        { "乙醇", "AO" },
        { "酒精", "AO" },
        { "氧化铝", "AO" },
        { "液化石油气", "PG" },
        { "LPG", "PG" },

        // 油脂油料
        { "豆一", "A" },
        { "豆二", "B" },
        { "菜籽", "RS" },

        // 金融期货
        { "国债", "TL" },
        { "三十年期国债", "TL" },
        { "十年期国债", "T" },
        { "五年期国债", "TF" },
        { "二年期国债", "TS" },
        { "中证500", "IM" },
        { "中证1000", "IM" },
        { "沪深300", "IF" },
        { "上证50", "IH" },
        { "上证", "IF" },

        // 其他
        { "橡胶", "RU" },
        { "20号胶", "NR" },
        { "纸浆", "SP" },
        { "线材", "WR" },
        { "集运欧线", "EC" },
        { "欧线", "EC" },
    };

    // 所有标准品种代码集合（用于快速判断是否为有效品种）
    private static readonly HashSet<string> StandardSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        // 黑色系
        "RB", "I", "HC", "JM", "J", "ZC", "TL", "SM", "SF", "SS",
        // 化工系
        "FG", "SA", "MA", "UR", "TA", "EG", "L", "PP", "V", "BU", "SC", "FU", "LU",
        // 有色金属
        "CU", "AL", "ZN", "NI", "SN", "AU", "AG", "PT", "PD", "LC", "SI",
        // 农产品
        "M", "Y", "P", "RM", "OI", "C", "CS", "SR", "CF", "CY", "AP", "CJ", "JR", "PK", "JD", "LH", "PS", "AO", "PG",
        // 油脂油料
        "A", "B", "RS",
        // 金融期货
        "T", "TF", "TS", "IM", "IH", "IC", "IF",
        // 其他
        "RU", "NR", "SP", "WR", "EC",
    };

    public List<string> ParseContracts(string contractText)
    {
        var contracts = new List<string>();

        if (string.IsNullOrWhiteSpace(contractText))
            return contracts;

        // 0. 预处理：移除常见干扰词，如"主力"、"连续"、"近月"、"远月"等
        var cleanedText = contractText
            .Replace("主力", "")
            .Replace("连续", "")
            .Replace("近月", "")
            .Replace("远月", "")
            .Replace("当月", "")
            .Replace("次月", "")
            .Replace("组合", "")
            .Replace("跨期", "")
            .Replace("跨品种", "")
            .Replace("/", " ");

        // 1. 先处理中文品种名映射（使用清理后的文本）
        foreach (var kvp in SymbolMappings)
        {
            if (cleanedText.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                var normalized = NormalizeSymbol(kvp.Value);
                if (!contracts.Contains(normalized))
                    contracts.Add(normalized);
            }
        }

        // 2. 处理括号中的品种代码，如 (CU) 或 (cu)
        var symbolPattern = @"\(([A-Za-z]+[A-Za-z0-9]*)\)";
        var matches = System.Text.RegularExpressions.Regex.Matches(cleanedText, symbolPattern);
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var symbol = match.Groups[1].Value.ToUpper();
            var normalized = NormalizeSymbol(symbol);
            if (!contracts.Contains(normalized))
                contracts.Add(normalized);
        }

        // 3. 处理纯品种代码（如 CU2606、cu2606、LH2607 等）
        // 匹配 1-4 个字母后跟 0-4 个数字的组合
        var codePattern = @"\b([A-Za-z]{1,4})\d{0,4}\b";
        var codeMatches = System.Text.RegularExpressions.Regex.Matches(cleanedText, codePattern);
        foreach (System.Text.RegularExpressions.Match match in codeMatches)
        {
            var symbol = match.Groups[1].Value.ToUpper();
            var normalized = NormalizeSymbol(symbol);

            // 如果已经是标准品种代码，直接添加
            if (StandardSymbols.Contains(normalized) && !contracts.Contains(normalized))
            {
                contracts.Add(normalized);
            }
            // 否则尝试通过映射转换
            else if (!contracts.Contains(normalized))
            {
                var mapped = NormalizeSymbol(symbol);
                if (StandardSymbols.Contains(mapped) && !contracts.Contains(mapped))
                    contracts.Add(mapped);
            }
        }

        return contracts;
    }

    public string NormalizeSymbol(string symbol)
    {
        symbol = symbol.ToUpper().Trim();

        if (SymbolMappings.TryGetValue(symbol, out var standardSymbol))
            return standardSymbol;

        var digits = System.Text.RegularExpressions.Regex.Match(symbol, @"\d+");
        if (digits.Success)
        {
            return System.Text.RegularExpressions.Regex.Replace(symbol, @"\d+", "");
        }

        return symbol;
    }
}
