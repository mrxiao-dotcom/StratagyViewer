using System.IO;
using System.Net.Http;
using Newtonsoft.Json;

namespace StrategyViewer.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private string _baseUrl = string.Empty;
    private string _token = string.Empty;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
    }

    public void Configure(string baseUrl, string token)
    {
        var trimmedUrl = baseUrl.Trim();

        // 自动添加 http:// 前缀（如果用户没有输入）
        if (!string.IsNullOrEmpty(trimmedUrl) && !trimmedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !trimmedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            trimmedUrl = "http://" + trimmedUrl;
        }

        _baseUrl = trimmedUrl.TrimEnd('/');
        _token = token;
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_token}");
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_baseUrl) && !string.IsNullOrWhiteSpace(_token);

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new InvalidOperationException("API服务未配置。请先配置服务器地址和Token。");
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        EnsureConfigured();

        if (!Uri.TryCreate(_baseUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException($"无效的服务器地址：{_baseUrl}。请检查配置。");

        var url = endpoint.StartsWith("/")
            ? new Uri(baseUri, endpoint)
            : new Uri(baseUri, "/" + endpoint);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(content);
    }

    public async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        EnsureConfigured();

        if (!Uri.TryCreate(_baseUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException($"无效的服务器地址：{_baseUrl}。请检查配置。");

        var url = endpoint.StartsWith("/")
            ? new Uri(baseUri, endpoint)
            : new Uri(baseUri, "/" + endpoint);

        var json = JsonConvert.SerializeObject(data);
        var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, httpContent);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(content);
    }

    public async Task<T?> PutAsync<T>(string endpoint, object data)
    {
        EnsureConfigured();

        if (!Uri.TryCreate(_baseUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException($"无效的服务器地址：{_baseUrl}。请检查配置。");

        var url = endpoint.StartsWith("/")
            ? new Uri(baseUri, endpoint)
            : new Uri(baseUri, "/" + endpoint);

        var json = JsonConvert.SerializeObject(data);
        var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync(url, httpContent);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(content);
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        EnsureConfigured();

        if (!Uri.TryCreate(_baseUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException($"无效的服务器地址：{_baseUrl}。请检查配置。");

        var url = endpoint.StartsWith("/")
            ? new Uri(baseUri, endpoint)
            : new Uri(baseUri, "/" + endpoint);

        var response = await _httpClient.DeleteAsync(url);
        return response.IsSuccessStatusCode;
    }

    public async Task<Stream?> GetStreamAsync(string endpoint)
    {
        EnsureConfigured();

        if (!Uri.TryCreate(_baseUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException($"无效的服务器地址：{_baseUrl}。请检查配置。");

        var url = endpoint.StartsWith("/")
            ? new Uri(baseUri, endpoint)
            : new Uri(baseUri, "/" + endpoint);

        return await _httpClient.GetStreamAsync(url);
    }
}
