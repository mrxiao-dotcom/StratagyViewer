using System.Collections.Concurrent;
using StrategyViewer.Models;

namespace StrategyViewer.Services;

public interface IStrategyService
{
    Task<List<StrategyListItem>> GetStrategiesAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<Strategy?> GetStrategyAsync(int id);
    Task<StrategyStats?> GetStatsAsync();
    Task<List<ContractHistory>> GetContractHistoryAsync(string contract, int days = 30);
    Task<List<ContractHistory>> GetAllContractHistoryAsync(int days = 30);
    void ClearCache();
    void SaveCache();
    void UpdateStrategiesCache(List<StrategyListItem> strategies);
}

public class StrategyService : IStrategyService
{
    private readonly ApiService _apiService;
    private readonly ICacheService _cacheService;
    private readonly ConcurrentDictionary<int, Strategy> _strategyCache = new();
    private readonly ConcurrentDictionary<string, List<ContractHistory>> _historyCache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

    // 内存缓存标志
    private List<StrategyListItem>? _strategiesCache;
    private bool _strategiesLoaded = false;

    public StrategyService(ApiService apiService, ICacheService cacheService)
    {
        _apiService = apiService;
        _cacheService = cacheService;
    }

    public async Task<List<StrategyListItem>> GetStrategiesAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        // 如果有日期过滤，必须从API获取
        if (startDate.HasValue || endDate.HasValue)
        {
            var url = "/api/strategy";
            var queryParams = new List<string>();
            if (startDate.HasValue)
                queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
            if (endDate.HasValue)
                queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            System.Diagnostics.Debug.WriteLine($"[策略] 带日期过滤获取策略: {url}");
            var response = await _apiService.GetAsync<ApiResponse<List<StrategyListItem>>>(url);
            var strategies = response?.Data ?? new List<StrategyListItem>();
            return strategies;
        }

        // 无日期过滤时，优先返回内存缓存
        if (_strategiesLoaded && _strategiesCache != null)
        {
            System.Diagnostics.Debug.WriteLine($"[策略] 命中内存缓存，返回 {_strategiesCache.Count} 个策略");
            return _strategiesCache;
        }

        // 尝试从本地缓存加载
        var cached = _cacheService.LoadStrategies();
        if (cached != null && cached.Count > 0)
        {
            _strategiesCache = cached;
            _strategiesLoaded = true;
            System.Diagnostics.Debug.WriteLine($"[策略] 从本地缓存加载了 {cached.Count} 个策略");
            return cached;
        }

        // 从API获取
        System.Diagnostics.Debug.WriteLine("[策略] 本地缓存未命中，从API获取");
        var response2 = await _apiService.GetAsync<ApiResponse<List<StrategyListItem>>>("/api/strategy");
        var strategies2 = response2?.Data ?? new List<StrategyListItem>();

        // 保存到内存和本地缓存
        _strategiesCache = strategies2;
        _strategiesLoaded = true;
        if (strategies2.Count > 0)
        {
            _cacheService.SaveStrategies(strategies2);
        }

        return strategies2;
    }

    public async Task<Strategy?> GetStrategyAsync(int id)
    {
        if (_strategyCache.TryGetValue(id, out var cached))
        {
            System.Diagnostics.Debug.WriteLine($"[缓存] 命中策略 {id}");
            return cached;
        }

        // 尝试从本地缓存加载
        var localCached = _cacheService.LoadStrategyDetail(id);
        if (localCached != null)
        {
            System.Diagnostics.Debug.WriteLine($"[缓存] 从本地文件命中策略 {id}");
            _strategyCache[id] = localCached;
            return localCached;
        }

        System.Diagnostics.Debug.WriteLine($"[缓存] 未命中策略 {id}, 从API获取");
        var response = await _apiService.GetAsync<ApiResponse<Strategy>>($"/api/strategy/{id}");
        var strategy = response?.Data;

        if (strategy != null)
        {
            _strategyCache[id] = strategy;
            _cacheService.SaveStrategyDetail(id, strategy);
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
            System.Diagnostics.Debug.WriteLine($"[缓存] 命中历史 {cacheKey}, 返回 {cached.Count} 条");
            return cached;
        }

        System.Diagnostics.Debug.WriteLine($"[缓存] 未命中历史 {cacheKey}, 从API获取");
        var response = await _apiService.GetAsync<ApiResponse<List<ContractHistory>>>($"/api/strategy/contract/{Uri.EscapeDataString(contract)}?days={days}");
        var history = response?.Data ?? new List<ContractHistory>();

        System.Diagnostics.Debug.WriteLine($"[API] 返回 {history.Count} 条记录");

        _historyCache[cacheKey] = history;
        return history;
    }

    public void ClearCache()
    {
        _strategyCache.Clear();
        _historyCache.Clear();
        _strategiesCache = null;
        _strategiesLoaded = false;
        _cacheService.ClearAll();
        System.Diagnostics.Debug.WriteLine("[缓存] 已清空所有缓存");
    }

    public void SaveCache()
    {
        if (_strategiesCache != null)
        {
            _cacheService.SaveStrategies(_strategiesCache);
        }

        foreach (var kvp in _strategyCache)
        {
            _cacheService.SaveStrategyDetail(kvp.Key, kvp.Value);
        }

        System.Diagnostics.Debug.WriteLine("[缓存] 已保存到本地");
    }

    public void UpdateStrategiesCache(List<StrategyListItem> strategies)
    {
        _strategiesCache = strategies;
        _strategiesLoaded = true;
        _cacheService.SaveStrategies(strategies);
        System.Diagnostics.Debug.WriteLine($"[策略] 已更新内存缓存，共 {strategies.Count} 个策略");
    }

    public async Task<List<ContractHistory>> GetAllContractHistoryAsync(int days = 30)
    {
        var result = new List<ContractHistory>();
        var cutoffDate = DateTime.Now.Date.AddDays(-days);

        System.Diagnostics.Debug.WriteLine($"[矩阵] 开始获取最近 {days} 天的所有策略...");

        // 获取所有策略
        var strategies = await GetStrategiesAsync(cutoffDate, null);

        System.Diagnostics.Debug.WriteLine($"[矩阵] 获取到 {strategies.Count} 个策略");

        foreach (var strategy in strategies)
        {
            // 获取策略详情（包含 SummaryItems）
            var strategyDetail = await GetStrategyAsync(strategy.Id);
            if (strategyDetail?.SummaryItems == null) continue;

            foreach (var item in strategyDetail.SummaryItems)
            {
                result.Add(new ContractHistory
                {
                    TradeDate = strategyDetail.TradeDate,
                    StrategyId = strategy.Id,
                    StrategyTitle = strategy.Title,
                    Contract = item.Contract ?? "",
                    Direction = item.Direction ?? "",
                    EntryRange = item.EntryRange ?? "",
                    StopLoss = item.StopLoss,
                    TakeProfit = item.TakeProfit,
                    Logic = item.Logic
                });
            }
        }

        System.Diagnostics.Debug.WriteLine($"[矩阵] 共提取 {result.Count} 条合约历史");
        return result;
    }
}
