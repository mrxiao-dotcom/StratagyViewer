using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using StrategyViewer.Models;

namespace StrategyViewer.Services;

public interface ITelegramService
{
    void Configure(string botToken, List<string> chatIds);
    Task<bool> SendPhotoAsync(string imagePath, string? caption = null);
    Task<bool> SendTextAsync(string message);
    Task<Dictionary<string, bool>> SendPhotoToAllAsync(string imagePath, string? caption = null);
    Task<Dictionary<string, bool>> SendTextToAllAsync(string message);
    Task<Dictionary<string, (bool Success, string? Error)>> SendPhotoToAllWithErrorAsync(string imagePath, string? caption = null);
}

public class TelegramService : ITelegramService
{
    private readonly HttpClient _httpClient;
    private string? _botToken;
    private List<string> _chatIds = new();

    public TelegramService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public void Configure(string botToken, List<string> chatIds)
    {
        _botToken = botToken;
        _chatIds = chatIds ?? new List<string>();
    }

    public async Task<bool> SendPhotoAsync(string imagePath, string? caption = null)
    {
        if (string.IsNullOrWhiteSpace(_botToken) || _chatIds.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("[Telegram] 未配置 Bot Token 或 Chat ID");
            return false;
        }

        if (!File.Exists(imagePath))
        {
            System.Diagnostics.Debug.WriteLine($"[Telegram] 图片文件不存在: {imagePath}");
            return false;
        }

        try
        {
            var fileName = Path.GetFileName(imagePath);
            var imageBytes = await File.ReadAllBytesAsync(imagePath);

            foreach (var chatId in _chatIds)
            {
                var success = await SendPhotoToChatAsync(_botToken, chatId, imageBytes, fileName, caption);
                if (!success)
                {
                    return false;
                }
            }

            System.Diagnostics.Debug.WriteLine("[Telegram] 发送成功");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Telegram] 发送失败: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> SendPhotoToChatAsync(string botToken, string chatId, byte[] imageBytes, string fileName, string? caption)
    {
        try
        {
            var apiUrl = $"https://api.telegram.org/bot{botToken}/sendPhoto";

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(chatId), "chat_id");

            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(imageContent, "photo", fileName);

            if (!string.IsNullOrWhiteSpace(caption))
            {
                content.Add(new StringContent(caption), "caption");
            }

            var response = await _httpClient.PostAsync(apiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<TelegramResponse>(responseBody);
                if (result?.ok == true)
                {
                    System.Diagnostics.Debug.WriteLine($"[Telegram] 发送到 {chatId} 成功");
                    return true;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[Telegram] 发送到 {chatId} 失败: {responseBody}");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Telegram] 发送到 {chatId} 失败: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendTextAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(_botToken) || _chatIds.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("[Telegram] 未配置 Bot Token 或 Chat ID");
            return false;
        }

        foreach (var chatId in _chatIds)
        {
            var success = await SendTextToChatAsync(_botToken, chatId, message);
            if (!success)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> SendTextToChatAsync(string botToken, string chatId, string message)
    {
        try
        {
            var apiUrl = $"https://api.telegram.org/bot{botToken}/sendMessage";

            var payload = new
            {
                chat_id = chatId,
                text = message,
                parse_mode = "Markdown"
            };

            var json = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(apiUrl, httpContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<TelegramResponse>(responseBody);
                return result?.ok == true;
            }

            System.Diagnostics.Debug.WriteLine($"[Telegram] 发送失败: {responseBody}");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Telegram] 发送失败: {ex.Message}");
            return false;
        }
    }

    public async Task<Dictionary<string, bool>> SendPhotoToAllAsync(string imagePath, string? caption = null)
    {
        var results = new Dictionary<string, bool>();

        if (string.IsNullOrWhiteSpace(_botToken) || _chatIds.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("[Telegram] 未配置 Bot Token 或 Chat ID");
            return results;
        }

        if (!File.Exists(imagePath))
        {
            System.Diagnostics.Debug.WriteLine($"[Telegram] 图片文件不存在: {imagePath}");
            return results;
        }

        try
        {
            var fileName = Path.GetFileName(imagePath);
            var imageBytes = await File.ReadAllBytesAsync(imagePath);

            foreach (var chatId in _chatIds)
            {
                var success = await SendPhotoToChatAsync(_botToken, chatId, imageBytes, fileName, caption);
                results[chatId] = success;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Telegram] 发送失败: {ex.Message}");
            foreach (var chatId in _chatIds)
            {
                results[chatId] = false;
            }
        }

        return results;
    }

    public async Task<Dictionary<string, bool>> SendTextToAllAsync(string message)
    {
        var results = new Dictionary<string, bool>();

        if (string.IsNullOrWhiteSpace(_botToken) || _chatIds.Count == 0)
        {
            return results;
        }

        foreach (var chatId in _chatIds)
        {
            var success = await SendTextToChatAsync(_botToken, chatId, message);
            results[chatId] = success;
        }

        return results;
    }

    public async Task<Dictionary<string, (bool Success, string? Error)>> SendPhotoToAllWithErrorAsync(string imagePath, string? caption = null)
    {
        var results = new Dictionary<string, (bool Success, string? Error)>();

        if (string.IsNullOrWhiteSpace(_botToken) || _chatIds.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("[Telegram] 未配置 Bot Token 或 Chat ID");
            return results;
        }

        if (!File.Exists(imagePath))
        {
            System.Diagnostics.Debug.WriteLine($"[Telegram] 图片文件不存在: {imagePath}");
            foreach (var chatId in _chatIds)
            {
                results[chatId] = (false, "图片文件不存在");
            }
            return results;
        }

        try
        {
            var fileName = Path.GetFileName(imagePath);
            var imageBytes = await File.ReadAllBytesAsync(imagePath);

            foreach (var chatId in _chatIds)
            {
                var (success, error) = await SendPhotoToChatWithErrorAsync(_botToken, chatId, imageBytes, fileName, caption);
                results[chatId] = (success, error);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Telegram] 发送失败: {ex.Message}");
            foreach (var chatId in _chatIds)
            {
                results[chatId] = (false, ex.Message);
            }
        }

        return results;
    }

    private async Task<(bool Success, string? Error)> SendPhotoToChatWithErrorAsync(string botToken, string chatId, byte[] imageBytes, string fileName, string? caption)
    {
        try
        {
            var apiUrl = $"https://api.telegram.org/bot{botToken}/sendPhoto";

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(chatId), "chat_id");

            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(imageContent, "photo", fileName);

            if (!string.IsNullOrWhiteSpace(caption))
            {
                content.Add(new StringContent(caption), "caption");
            }

            var response = await _httpClient.PostAsync(apiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<TelegramResponse>(responseBody);
                if (result?.ok == true)
                {
                    System.Diagnostics.Debug.WriteLine($"[Telegram] 发送到 {chatId} 成功");
                    return (true, null);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Telegram] 发送到 {chatId} 失败: {result?.description}");
                    return (false, result?.description ?? "未知错误");
                }
            }

            var errorResult = JsonConvert.DeserializeObject<TelegramResponse>(responseBody);
            System.Diagnostics.Debug.WriteLine($"[Telegram] 发送到 {chatId} 失败: {responseBody}");
            return (false, errorResult?.description ?? responseBody);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Telegram] 发送到 {chatId} 失败: {ex.Message}");
            return (false, ex.Message);
        }
    }

    private class TelegramResponse
    {
        public bool ok { get; set; }
        public string? description { get; set; }
    }
}
