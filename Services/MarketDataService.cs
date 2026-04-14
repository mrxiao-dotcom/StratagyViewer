using System.Net.Http;
using System.Text;
using System.Text.Json;
using StrategyViewer.Models;

namespace StrategyViewer.Services;

public interface IMarketDataService
{
    Task<List<MarketData>> GetMarketDataAsync(string symbol, DateTime startDate, DateTime endDate, KLinePeriod period = KLinePeriod.Daily);
    Task<MarketData?> GetLatestDataAsync(string symbol, KLinePeriod period = KLinePeriod.Daily);
    List<string> ParseContracts(string contractText);
}

public class MarketDataService : IMarketDataService
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;

    public MarketDataService(HttpClient httpClient, ISettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    public async Task<List<MarketData>> GetMarketDataAsync(string symbol, DateTime startDate, DateTime endDate, KLinePeriod period = KLinePeriod.Min15)
    {
        var baseUrl = _settingsService.Settings.MarketDataServer.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            System.Diagnostics.Debug.WriteLine($"[行情API] 服务器地址未配置");
            return new List<MarketData>();
        }

        var periodStr = period == KLinePeriod.Min15 ? "15m" : "1d";
        // 使用完整的时间格式，包含小时分钟
        var startTime = startDate.ToString("yyyy-MM-dd-HH-mm");
        var endTime = endDate.ToString("yyyy-MM-dd-HH-mm");
        var fullUrl = $"http://{baseUrl}/api/data/{symbol}/{periodStr}?startDate={startTime}&endDate={endTime}";

        System.Diagnostics.Debug.WriteLine($"[行情API] 请求 {symbol} K线 {periodStr}, 时间范围: {startDate:yyyy-MM-dd HH:mm} ~ {endDate:yyyy-MM-dd HH:mm}");

        try
        {
            System.Diagnostics.Debug.WriteLine($"[行情API] 请求URL: {fullUrl}");

            var response = await _httpClient.GetAsync(fullUrl);

            System.Diagnostics.Debug.WriteLine($"[行情API] 响应状态: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[行情API] 响应内容: {responseJson}");
                
                var marketResponse = JsonSerializer.Deserialize<MarketDataApiResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (marketResponse?.Success == true && marketResponse.Data?.Data != null)
                {
                    var result = marketResponse.Data.Data.Select(k => new MarketData
                    {
                        Symbol = symbol,
                        Date = DateTime.TryParse(k.TradeDate, out var dt) ? dt : DateTime.Now,
                        Open = k.Open,
                        High = k.High,
                        Low = k.Low,
                        Close = k.Close,
                        Volume = k.Volume,
                        Settlement = k.Amount,
                        Period = period
                    }).ToList();

                    System.Diagnostics.Debug.WriteLine($"[行情API] 获取 {symbol} K线成功, 共 {result.Count} 根");
                    return result;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[行情API] {symbol} 返回失败: Success={marketResponse?.Success}, Message={marketResponse?.Message}, 完整响应: {responseJson}");
                }
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[行情API] {symbol} 请求失败: HTTP {response.StatusCode}, Body: {errorBody}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[行情API] {symbol} 请求异常: {ex.Message}");
        }

        return new List<MarketData>();
    }

    public async Task<MarketData?> GetLatestDataAsync(string symbol, KLinePeriod period = KLinePeriod.Min15)
    {
        var endDate = DateTime.Today;
        var startDate = period == KLinePeriod.Min15 ? endDate.AddDays(-3) : endDate.AddDays(-5);
        var data = await GetMarketDataAsync(symbol, startDate, endDate, period);
        return data.OrderByDescending(d => d.Date).FirstOrDefault();
    }

    public List<string> ParseContracts(string contractText)
    {
        var contracts = new List<string>();

        if (string.IsNullOrWhiteSpace(contractText))
            return contracts;

        var patterns = new[]
        {
            @"\(([^)]+)\)",
            @"([A-Za-z]+\d+)",
            @"([A-Za-z]{1,3})"
        };

        foreach (var pattern in patterns)
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(contractText, pattern);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var contract = match.Groups[1].Value.ToUpper();
                if (!contracts.Contains(contract) && contract.Length <= 10)
                {
                    contracts.Add(contract);
                }
            }
        }

        return contracts;
    }
}
