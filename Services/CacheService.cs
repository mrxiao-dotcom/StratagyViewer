using System.IO;
using Newtonsoft.Json;
using StrategyViewer.Models;

namespace StrategyViewer.Services;

public interface ICacheService
{
    void SaveStrategies(List<StrategyListItem> strategies);
    List<StrategyListItem>? LoadStrategies();

    void SaveContracts(List<ContractHistory> contracts);
    List<ContractHistory>? LoadContracts();

    void SaveStrategyDetail(int id, Strategy strategy);
    Strategy? LoadStrategyDetail(int id);

    void ClearAll();
    bool HasCachedData();

    // 缓存元数据
    DateTime? GetLastRefreshTime();
    void SetLastRefreshTime(DateTime time);
}

public class CacheMetadata
{
    public DateTime LastRefreshTime { get; set; }
}

public class CacheService : ICacheService
{
    private readonly string _cacheDir;
    private readonly string _metadataPath;
    private CacheMetadata _metadata;

    public CacheService()
    {
        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StrategyViewer",
            "cache");

        _metadataPath = Path.Combine(_cacheDir, "metadata.json");

        if (!Directory.Exists(_cacheDir))
            Directory.CreateDirectory(_cacheDir);

        _metadata = LoadMetadata();
    }

    private CacheMetadata LoadMetadata()
    {
        try
        {
            if (File.Exists(_metadataPath))
            {
                var json = File.ReadAllText(_metadataPath);
                return JsonConvert.DeserializeObject<CacheMetadata>(json) ?? new CacheMetadata();
            }
        }
        catch { }
        return new CacheMetadata();
    }

    private void SaveMetadata()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_metadata, Formatting.Indented);
            File.WriteAllText(_metadataPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[本地缓存] 保存元数据失败: {ex.Message}");
        }
    }

    public DateTime? GetLastRefreshTime() => _metadata.LastRefreshTime == default ? null : _metadata.LastRefreshTime;

    public void SetLastRefreshTime(DateTime time)
    {
        _metadata.LastRefreshTime = time;
        SaveMetadata();
    }

    private string GetFilePath(string fileName) => Path.Combine(_cacheDir, fileName);

    public void SaveStrategies(List<StrategyListItem> strategies)
    {
        try
        {
            var json = JsonConvert.SerializeObject(strategies, Formatting.Indented);
            File.WriteAllText(GetFilePath("strategies.json"), json);
            System.Diagnostics.Debug.WriteLine($"[本地缓存] 已保存 {strategies.Count} 个策略");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[本地缓存] 保存策略失败: {ex.Message}");
        }
    }

    public List<StrategyListItem>? LoadStrategies()
    {
        try
        {
            var path = GetFilePath("strategies.json");
            if (!File.Exists(path)) return null;

            var json = File.ReadAllText(path);
            var strategies = JsonConvert.DeserializeObject<List<StrategyListItem>>(json);
            System.Diagnostics.Debug.WriteLine($"[本地缓存] 加载了 {strategies?.Count ?? 0} 个策略");
            return strategies;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[本地缓存] 加载策略失败: {ex.Message}");
            return null;
        }
    }

    public void SaveContracts(List<ContractHistory> contracts)
    {
        try
        {
            var json = JsonConvert.SerializeObject(contracts, Formatting.Indented);
            File.WriteAllText(GetFilePath("contracts.json"), json);
            System.Diagnostics.Debug.WriteLine($"[本地缓存] 已保存 {contracts.Count} 条合约历史");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[本地缓存] 保存合约历史失败: {ex.Message}");
        }
    }

    public List<ContractHistory>? LoadContracts()
    {
        try
        {
            var path = GetFilePath("contracts.json");
            if (!File.Exists(path)) return null;

            var json = File.ReadAllText(path);
            var contracts = JsonConvert.DeserializeObject<List<ContractHistory>>(json);
            System.Diagnostics.Debug.WriteLine($"[本地缓存] 加载了 {contracts?.Count ?? 0} 条合约历史");
            return contracts;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[本地缓存] 加载合约历史失败: {ex.Message}");
            return null;
        }
    }

    public void SaveStrategyDetail(int id, Strategy strategy)
    {
        try
        {
            var json = JsonConvert.SerializeObject(strategy, Formatting.Indented);
            File.WriteAllText(GetFilePath($"strategy_{id}.json"), json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[本地缓存] 保存策略详情 {id} 失败: {ex.Message}");
        }
    }

    public Strategy? LoadStrategyDetail(int id)
    {
        try
        {
            var path = GetFilePath($"strategy_{id}.json");
            if (!File.Exists(path)) return null;

            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Strategy>(json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[本地缓存] 加载策略详情 {id} 失败: {ex.Message}");
            return null;
        }
    }

    public void ClearAll()
    {
        try
        {
            if (Directory.Exists(_cacheDir))
            {
                foreach (var file in Directory.GetFiles(_cacheDir))
                {
                    File.Delete(file);
                }
            }
            System.Diagnostics.Debug.WriteLine("[本地缓存] 已清空所有缓存");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[本地缓存] 清空缓存失败: {ex.Message}");
        }
    }

    public bool HasCachedData()
    {
        return File.Exists(GetFilePath("strategies.json"));
    }
}
