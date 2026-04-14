using System.Collections.Concurrent;
using StrategyViewer.Models;

namespace StrategyViewer.Services;

public interface IStrategyService
{
    Task<List<StrategyListItem>> GetStrategiesAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<Strategy?> GetStrategyAsync(int id);
    Task<StrategyStats?> GetStatsAsync();
    Task<List<ContractHistory>> GetContractHistoryAsync(string contract, int days = 30);
    void ClearCache();
}

public class StrategyService : IStrategyService
{
    private readonly ApiService _apiService;
    private readonly ConcurrentDictionary<int, Strategy> _strategyCache = new();
    private readonly ConcurrentDictionary<string, List<ContractHistory>> _historyCache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

    public StrategyService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<StrategyListItem>> GetStrategiesAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var endpoint = "/api/strategy";
        var queryParams = new List<string>();

        if (startDate.HasValue)
            queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue)
            queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");

        if (queryParams.Count > 0)
            endpoint += "?" + string.Join("&", queryParams);

        var response = await _apiService.GetAsync<ApiResponse<List<StrategyListItem>>>(endpoint);
        return response?.Data ?? new List<StrategyListItem>();
    }

    public async Task<Strategy?> GetStrategyAsync(int id)
    {
        if (_strategyCache.TryGetValue(id, out var cached))
        {
            System.Diagnostics.Debug.WriteLine($"[缓存] 命中策略 {id}");
            return cached;
        }

        System.Diagnostics.Debug.WriteLine($"[缓存] 未命中策略 {id}, 从API获取");
        var response = await _apiService.GetAsync<ApiResponse<Strategy>>($"/api/strategy/{id}");
        var strategy = response?.Data;

        if (strategy != null)
        {
            _strategyCache[id] = strategy;
        }

        return strategy;
    }

    public async Task<StrategyStats?> GetStatsAsync()
    {
        var response = await _apiService.GetAsync<ApiResponse<StrategyStats>>("/api/strategy/stats");
        return response?.Data;
    }

    public async Task<List<ContractHistory>> GetContractHistoryAsync(string contract, int days = 30)
    {
        var cacheKey = $"{contract}_{days}";

        if (_historyCache.TryGetValue(cacheKey, out var cached))
        {
            System.Diagnostics.Debug.WriteLine($"[缓存] 命中历史 {cacheKey}");
            return cached;
        }

        System.Diagnostics.Debug.WriteLine($"[缓存] 未命中历史 {cacheKey}, 从API获取");
        var response = await _apiService.GetAsync<ApiResponse<List<ContractHistory>>>($"/api/strategy/contract/{Uri.EscapeDataString(contract)}?days={days}");
        var history = response?.Data ?? new List<ContractHistory>();

        if (history.Count > 0)
        {
            _historyCache[cacheKey] = history;
        }

        return history;
    }

    public void ClearCache()
    {
        _strategyCache.Clear();
        _historyCache.Clear();
        System.Diagnostics.Debug.WriteLine("[缓存] 已清空");
    }
}
