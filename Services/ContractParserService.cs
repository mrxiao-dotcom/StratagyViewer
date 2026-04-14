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
        { "螺纹钢", "RB" },
        { "铁矿石", "I" },
        { "热卷", "HC" },
        { "焦煤", "JM" },
        { "焦炭", "J" },
        { "动力煤", "ZC" },
        { "玻璃", "FG" },
        { "纯碱", "SA" },
        { "甲醇", "MA" },
        { "尿素", "UR" },
        { "PTA", "TA" },
        { "乙二醇", "EG" },
        { "塑料", "L" },
        { "聚丙烯", "PP" },
        { "PVC", "V" },
        { "原油", "SC" },
        { "沥青", "BU" },
        { "铜", "CU" },
        { "铝", "AL" },
        { "锌", "ZN" },
        { "镍", "NI" },
        { "锡", "SN" },
        { "黄金", "AU" },
        { "白银", "AG" },
        { "螺纹", "RB" },
        { "豆粕", "M" },
        { "豆油", "Y" },
        { "棕榈油", "P" },
        { "玉米", "C" },
        { "白糖", "SR" },
        { "棉花", "CF" },
        { "苹果", "AP" },
        { "红枣", "CJ" },
        { "粳米", "JR" },
    };

    public List<string> ParseContracts(string contractText)
    {
        var contracts = new List<string>();

        if (string.IsNullOrWhiteSpace(contractText))
            return contracts;

        foreach (var kvp in SymbolMappings)
        {
            if (contractText.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                var normalized = NormalizeSymbol(kvp.Value);
                if (!contracts.Contains(normalized))
                    contracts.Add(normalized);
            }
        }

        var symbolPattern = @"\(([A-Za-z]+\d*)\)";
        var matches = System.Text.RegularExpressions.Regex.Matches(contractText, symbolPattern);
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var symbol = match.Groups[1].Value.ToUpper();
            var normalized = NormalizeSymbol(symbol);
            if (!contracts.Contains(normalized))
                contracts.Add(normalized);
        }

        var codePattern = @"\b([A-Z]{1,3})\d{0,4}\b";
        var codeMatches = System.Text.RegularExpressions.Regex.Matches(contractText, codePattern);
        foreach (System.Text.RegularExpressions.Match match in codeMatches)
        {
            var symbol = match.Groups[1].Value.ToUpper();
            if (symbol.Length <= 3 && !contracts.Any(c => c.StartsWith(symbol)))
            {
                var normalized = NormalizeSymbol(symbol);
                if (!contracts.Contains(normalized) && SymbolMappings.ContainsValue(symbol))
                    contracts.Add(normalized);
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
