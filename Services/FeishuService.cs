using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using StrategyViewer.Models;
using StrategyViewer.ViewModels;

namespace StrategyViewer.Services;

public interface IFeishuService
{
    Task<bool> SendMatrixAsync(DirectionMatrixViewModel viewModel, ISettingsService settingsService);
    Task<bool> SendMatrixImageAsync(string imagePath, DirectionMatrixViewModel viewModel, ISettingsService settingsService);
}

public class FeishuService : IFeishuService
{
    private readonly HttpClient _httpClient;
    private string? _cachedAccessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public FeishuService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public async Task<bool> SendMatrixAsync(DirectionMatrixViewModel viewModel, ISettingsService settingsService)
    {
        // 发送卡片消息（使用 Webhook）
        var feishuSettings = settingsService.Settings.FeishuSettings;
        if (!feishuSettings.IsEnabled || string.IsNullOrWhiteSpace(feishuSettings.WebhookUrl))
            return false;

        return await SendCardMessageAsync(viewModel, feishuSettings.WebhookUrl);
    }

    public async Task<bool> SendMatrixImageAsync(string imagePath, DirectionMatrixViewModel viewModel, ISettingsService settingsService)
    {
        var feishuSettings = settingsService.Settings.FeishuImageSettings;

        if (!feishuSettings.IsEnabled || !feishuSettings.IsConfigured)
        {
            System.Diagnostics.Debug.WriteLine("[飞书] 未配置飞书传图功能");
            return false;
        }

        if (!File.Exists(imagePath))
        {
            System.Diagnostics.Debug.WriteLine($"[飞书] 图片文件不存在: {imagePath}");
            return false;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"[飞书] 开始发送图片...");

            // 1. 获取 Access Token
            var accessToken = await GetAccessTokenAsync(feishuSettings.AppId, feishuSettings.AppSecret);
            if (string.IsNullOrEmpty(accessToken))
            {
                System.Diagnostics.Debug.WriteLine("[飞书] 获取 Access Token 失败");
                return false;
            }

            // 2. 上传图片获取 image_key
            var imageKey = await UploadImageAsync(imagePath, accessToken);
            if (string.IsNullOrEmpty(imageKey))
            {
                System.Diagnostics.Debug.WriteLine("[飞书] 图片上传失败");
                return false;
            }

            // 3. 发送图片消息
            if (feishuSettings.UseCardWithImage)
            {
                return await SendImageCardAsync(imageKey, viewModel, accessToken, feishuSettings.ChatId);
            }
            else
            {
                return await SendImageMessageAsync(imageKey, accessToken, feishuSettings.ChatId);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[飞书] 发送失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取飞书 Access Token
    /// </summary>
    private async Task<string?> GetAccessTokenAsync(string appId, string appSecret)
    {
        // 使用缓存的 token
        if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.Now < _tokenExpiry)
        {
            return _cachedAccessToken;
        }

        try
        {
            var url = "https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal";
            var payload = new { app_id = appId, app_secret = appSecret };
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"[飞书] 获取 Token 响应: {responseBody}");

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<FeishuTokenResponse>(responseBody);
                if (result?.Code == 0 && !string.IsNullOrEmpty(result.TenantAccessToken))
                {
                    _cachedAccessToken = result.TenantAccessToken;
                    _tokenExpiry = DateTime.Now.AddSeconds(result.ExpireIn - 60); // 提前1分钟过期
                    return _cachedAccessToken;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[飞书] 获取 Token 失败: {responseBody}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[飞书] 获取 Token 异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 上传图片到飞书
    /// </summary>
    private async Task<string?> UploadImageAsync(string imagePath, string accessToken)
    {
        try
        {
            var imageBytes = await File.ReadAllBytesAsync(imagePath);
            var fileName = Path.GetFileName(imagePath);

            using var content = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(imageContent, "image", fileName);
            content.Add(new StringContent("message"), "image_type");

            var uploadUrl = "https://open.feishu.cn/open-apis/im/v1/images";

            var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"[飞书] 图片上传响应: {responseBody}");

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<FeishuUploadResponse>(responseBody);
                if (result?.Code == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[飞书] 图片上传成功，image_key: {result.Data?.ImageKey}");
                    return result.Data?.ImageKey;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[飞书] 图片上传异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 发送图片消息
    /// </summary>
    private async Task<bool> SendImageMessageAsync(string imageKey, string accessToken, string chatId)
    {
        try
        {
            var url = "https://open.feishu.cn/open-apis/im/v1/messages?receive_id_type=chat_id";
            var payload = new
            {
                receive_id = chatId,
                msg_type = "image",
                content = JsonConvert.SerializeObject(new { image_key = imageKey })
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"[飞书] 发送图片响应: {responseBody}");

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<FeishuResponse>(responseBody);
                if (result?.Code == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[飞书] 图片发送成功");
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[飞书] 发送图片异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 发送包含图片的卡片消息
    /// </summary>
    private async Task<bool> SendImageCardAsync(string imageKey, DirectionMatrixViewModel viewModel, string accessToken, string chatId)
    {
        try
        {
            var cardContent = new
            {
                config = new { wide_screen_mode = true, enable_forward = true },
                header = new
                {
                    title = new { tag = "plain_text", content = "📊 品种多空矩阵" },
                    template = "blue"
                },
                elements = new object[]
                {
                    new { tag = "img", img_key = imageKey, alt = new { tag = "plain_text", content = "品种多空矩阵图片" } },
                    new { tag = "hr" },
                    new
                    {
                        tag = "div",
                        text = new { tag = "plain_text", content = $"🕐 更新时间: {DateTime.Now:yyyy-MM-dd HH:mm}  |  📈 品种: {viewModel.Products.Count}  |  📅 天数: {viewModel.Dates.Count}" }
                    },
                    new { tag = "hr" },
                    new
                    {
                        tag = "div",
                        text = new
                        {
                            tag = "plain_text",
                            content = "⚠️ 免责声明: 本频道提供的所有投研数据及多空方向仅供参考，不构成投资建议。金融市场存在风险，请谨慎决策。"
                        }
                    }
                }
            };

            return await SendCardMessageAsync(cardContent, accessToken, chatId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[飞书] 发送卡片失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 发送卡片消息（使用 Webhook）
    /// </summary>
    private async Task<bool> SendCardMessageAsync(DirectionMatrixViewModel viewModel, string webhookUrl)
    {
        try
        {
            var cardContent = BuildMatrixCard(viewModel);
            var message = new { msg_type = "interactive", card = cardContent };
            var json = JsonConvert.SerializeObject(message);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(webhookUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"[飞书] 卡片消息响应: {responseBody}");

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<FeishuResponse>(responseBody);
                if (result?.Code == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[飞书] 卡片消息发送成功");
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[飞书] 发送卡片异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 发送卡片消息（使用 API）
    /// </summary>
    private async Task<bool> SendCardMessageAsync(object cardContent, string accessToken, string chatId)
    {
        try
        {
            var url = "https://open.feishu.cn/open-apis/im/v1/messages?receive_id_type=chat_id";
            var payload = new
            {
                receive_id = chatId,
                msg_type = "interactive",
                content = JsonConvert.SerializeObject(cardContent)
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"[飞书] 卡片消息响应: {responseBody}");

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<FeishuResponse>(responseBody);
                if (result?.Code == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[飞书] 卡片消息发送成功");
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[飞书] 发送卡片异常: {ex.Message}");
            return false;
        }
    }

    private object BuildMatrixCard(DirectionMatrixViewModel viewModel)
    {
        var tableElements = new List<object>();

        // 表头行
        var headerCells = new List<object>
        {
            new { tag = "td", children = new[] { new { tag = "plain_text", content = "日期" } }, attrs = new { background_color = "#1a1a2e", align = "center" } }
        };
        foreach (var product in viewModel.Products)
        {
            headerCells.Add(new
            {
                tag = "td",
                children = new[] { new { tag = "plain_text", content = $"{product.Code}\n{product.Name}" } },
                attrs = new { background_color = "#1a1a2e", align = "center" }
            });
        }

        tableElements.Add(new { tag = "tr", children = headerCells });

        // 数据行
        foreach (var row in viewModel.Rows)
        {
            var dataCells = new List<object>
            {
                new { tag = "td", children = new[] { new { tag = "plain_text", content = row.DateString.Substring(5) } }, attrs = new { background_color = "#252536", align = "center" } }
            };

            foreach (var product in viewModel.Products)
            {
                var cellKey = $"{row.DateString}|{product.Code}";
                var symbol = viewModel.GetMatrixCellSymbol(cellKey);
                var (bgColor, text) = GetCellStyle(symbol);
                dataCells.Add(new
                {
                    tag = "td",
                    children = new[] { new { tag = "plain_text", content = text } },
                    attrs = new { background_color = bgColor, align = "center" }
                });
            }

            tableElements.Add(new { tag = "tr", children = dataCells });
        }

        return new
        {
            config = new { wide_screen_mode = true, enable_forward = true },
            header = new
            {
                title = new { tag = "plain_text", content = "📊 品种多空矩阵" },
                template = "blue"
            },
            elements = new object[]
            {
                new { tag = "div", text = new { tag = "plain_text", content = $"🕐 更新时间: {DateTime.Now:yyyy-MM-dd HH:mm}  |  📈 品种: {viewModel.Products.Count}  |  📅 天数: {viewModel.Dates.Count}" } },
                new { tag = "hr" },
                new { tag = "table", children = tableElements.ToArray() },
                new { tag = "hr" },
                new { tag = "div", text = new { tag = "plain_text", content = "📌 颜色说明: 🔴 多头 | 🟢 空头 | ⚪ 无数据\n\n⚠️ 免责声明: 本频道提供的所有投研数据及多空方向仅供参考，不构成投资建议。金融市场存在风险，请谨慎决策。" } }
            }
        };
    }

    private (string bgColor, string text) GetCellStyle(string symbol)
    {
        if (symbol == "🔴")
            return ("#166534", "🔴 多");
        else if (symbol == "🟢")
            return ("#f0fdf4", "🟢 空");
        else
            return ("#252536", "—");
    }

    private class FeishuResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("msg")]
        public string? Msg { get; set; }
    }

    private class FeishuTokenResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("msg")]
        public string? Msg { get; set; }

        [JsonProperty("tenant_access_token")]
        public string? TenantAccessToken { get; set; }

        [JsonProperty("expire")]
        public int ExpireIn { get; set; }
    }

    private class FeishuUploadResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("msg")]
        public string? Msg { get; set; }

        [JsonProperty("data")]
        public FeishuImageData? Data { get; set; }
    }

    private class FeishuImageData
    {
        [JsonProperty("image_key")]
        public string? ImageKey { get; set; }

        [JsonProperty("image_url")]
        public string? ImageUrl { get; set; }
    }
}
