# C# 应用程序接入飞书/Telegram 消息推送指南

## 一、飞书接入配置

### 1.1 创建飞书应用

1. 访问 [飞书开放平台](https://open.feishu.cn/app)
2. 点击「创建企业自建应用」
3. 填写应用名称、描述，上传应用图标
4. 获取 `App ID` 和 `App Secret`

### 1.2 启用机器人能力

> ⚠️ **重要**: 必须启用此能力，否则无法发送消息

1. 在应用详情页左侧菜单选择「添加应用能力」
2. 找到「机器人」能力，点击「添加」
3. 添加完成后需要**发布应用版本**才能生效

### 1.3 配置权限

1. 进入「权限管理」
2. 添加以下必要权限：
   - `im:message` - 发送消息
   - `im:message:send_as_bot` - 以机器人身份发送

### 1.4 获取 Chat ID

1. 在飞书桌面客户端中打开目标群聊
2. 点击群设置 → 群信息 → 群 ID
3. 或者使用 API 获取群信息

### 1.5 将机器人添加到群聊

1. 打开目标群聊
2. 点击群设置 → 群机器人 → 添加机器人
3. 选择刚才创建的应用

---

## 二、Telegram 接入配置

### 2.1 创建 Bot

1. 在 Telegram 中搜索 `@BotFather`
2. 发送 `/newbot` 创建新机器人
3. 按提示设置机器人名称和用户名
4. 获取 `Bot Token`（格式：`123456789:ABCdefGHIjklMNOpqrSTUvwxyz`）

### 2.2 获取 Chat ID

1. 先将机器人拉入目标群聊
2. 在群聊中发送任意消息
3. 访问 `https://api.telegram.org/bot<YOUR_TOKEN>/getUpdates`
4. 从返回的 JSON 中找到 `chat.id`（群 ID 通常为负数）

### 2.3 配置（可选）

支持配置多个 Chat ID，实现向多个群/用户发送

---

## 三、C# 代码示例

### 3.1 NuGet 依赖

```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

### 3.2 飞书服务代码

```csharp
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

public class FeishuService
{
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };
    private string? _cachedAccessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    /// <summary>
    /// 获取 Access Token（自动缓存）
    /// </summary>
    private async Task<string?> GetAccessTokenAsync(string appId, string appSecret)
    {
        if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.Now < _tokenExpiry)
            return _cachedAccessToken;

        var url = "https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal";
        var payload = new { app_id = appId, app_secret = appSecret };
        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        var result = JsonConvert.DeserializeObject<TokenResponse>(await response.Content.ReadAsStringAsync());

        if (result?.Code == 0)
        {
            _cachedAccessToken = result.TenantAccessToken;
            _tokenExpiry = DateTime.Now.AddSeconds(result.ExpireIn - 60);
            return _cachedAccessToken;
        }
        return null;
    }

    /// <summary>
    /// 上传图片获取 image_key
    /// </summary>
    public async Task<string?> UploadImageAsync(string imagePath, string accessToken)
    {
        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        using var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "image", Path.GetFileName(imagePath));
        content.Add(new StringContent("message"), "image_type");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://open.feishu.cn/open-apis/im/v1/images");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        var result = JsonConvert.DeserializeObject<UploadResponse>(await response.Content.ReadAsStringAsync());
        return result?.Code == 0 ? result.Data?.ImageKey : null;
    }

    /// <summary>
    /// 发送图片消息
    /// </summary>
    public async Task<bool> SendImageAsync(string imageKey, string accessToken, string chatId)
    {
        var url = "https://open.feishu.cn/open-apis/im/v1/messages?receive_id_type=chat_id";
        var payload = new
        {
            receive_id = chatId,
            msg_type = "image",
            content = JsonConvert.SerializeObject(new { image_key = imageKey })
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var result = JsonConvert.DeserializeObject<FeishuResponse>(await response.Content.ReadAsStringAsync());
        return result?.Code == 0;
    }

    private class TokenResponse
    {
        public int Code { get; set; }
        public string? TenantAccessToken { get; set; }
        public int ExpireIn { get; set; }
    }

    private class UploadResponse
    {
        public int Code { get; set; }
        public UploadData? Data { get; set; }
    }

    private class UploadData
    {
        public string? ImageKey { get; set; }
    }

    private class FeishuResponse
    {
        public int Code { get; set; }
        public string? Msg { get; set; }
    }
}
```

### 3.3 Telegram 服务代码

```csharp
using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

public class TelegramService
{
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };
    private string? _botToken;
    private List<string> _chatIds = new();

    public void Configure(string botToken, List<string> chatIds)
    {
        _botToken = botToken;
        _chatIds = chatIds ?? new();
    }

    /// <summary>
    /// 发送图片
    /// </summary>
    public async Task<bool> SendPhotoAsync(string imagePath, string? caption = null)
    {
        if (string.IsNullOrEmpty(_botToken) || _chatIds.Count == 0)
            return false;

        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        var fileName = Path.GetFileName(imagePath);

        foreach (var chatId in _chatIds)
        {
            if (!await SendPhotoToChatAsync(chatId, imageBytes, fileName, caption))
                return false;
        }
        return true;
    }

    private async Task<bool> SendPhotoToChatAsync(string chatId, byte[] imageBytes, string fileName, string? caption)
    {
        var apiUrl = $"https://api.telegram.org/bot{_botToken}/sendPhoto";

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(chatId), "chat_id");

        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "photo", fileName);

        if (!string.IsNullOrWhiteSpace(caption))
            content.Add(new StringContent(caption), "caption");

        var response = await _httpClient.PostAsync(apiUrl, content);
        var result = JsonConvert.DeserializeObject<TelegramResult>(await response.Content.ReadAsStringAsync());
        return result?.ok == true;
    }

    /// <summary>
    /// 发送文本消息
    /// </summary>
    public async Task<bool> SendTextAsync(string message)
    {
        if (string.IsNullOrEmpty(_botToken) || _chatIds.Count == 0)
            return false;

        foreach (var chatId in _chatIds)
        {
            if (!await SendTextToChatAsync(chatId, message))
                return false;
        }
        return true;
    }

    private async Task<bool> SendTextToChatAsync(string chatId, string message)
    {
        var apiUrl = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        var payload = new { chat_id = chatId, text = message, parse_mode = "Markdown" };
        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(apiUrl, content);
        var result = JsonConvert.DeserializeObject<TelegramResult>(await response.Content.ReadAsStringAsync());
        return result?.ok == true;
    }

    private class TelegramResult
    {
        public bool ok { get; set; }
        public string? description { get; set; }
    }
}
```

---

## 四、WPF 注意事项

### 4.1 UI 操作必须在主线程

```csharp
// ❌ 错误：UI 操作放在 Task.Run 中会报错
await Task.Run(() => SaveElementToImage(element, path));

// ✅ 正确：UI 操作在主线程，网络请求在后台线程
SaveElementToImage(element, path);  // 主线程
await Task.Run(() => SendToFeishu());  // 后台线程
```

错误信息：`调用线程必须为 STA，因为许多 UI 组件都需要`

### 4.2 WPF 元素转图片

```csharp
public void SaveElementToImage(FrameworkElement element, string filePath)
{
    var width = (int)element.ActualWidth;
    var height = (int)element.ActualHeight;
    if (width == 0 || height == 0) return;

    var dpi = 96;
    var pixelWidth = (int)(width * dpi / 96);
    var pixelHeight = (int)(height * dpi / 96);

    var renderBitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, dpi, dpi, PixelFormats.Pbgra32);
    renderBitmap.Render(element);

    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

    using var stream = File.Create(filePath);
    encoder.Save(stream);
}
```

---

## 五、常见错误排查

### 飞书错误码

| 错误码 | 说明 | 解决方案 |
|--------|------|----------|
| 230002 | 参数错误 | 检查 JSON 格式和必填字段 |
| 230029 | 权限不足 | 在开放平台添加对应权限 |
| 230038 | 应用未发布 | 发布应用新版本 |
| 230044 | 应用未启用机器人能力 | 添加机器人能力 |
| 230107 | Token 过期 | 重新获取 Access Token |
| 99991672 | receive_id_type 缺失 | URL 添加 `?receive_id_type=chat_id` |

### Telegram 错误

| 错误 | 说明 | 解决方案 |
|------|------|----------|
| Bot was blocked by the user | 机器人被拉黑 | 与用户重新建立联系 |
| Chat not found | Chat ID 错误 | 检查 Chat ID 是否正确 |
| Bad Request: chat not found | 机器人不在群中 | 将机器人添加进群聊 |
| unauthorized | Token 错误 | 检查 Bot Token 是否正确 |

---

## 六、飞书卡片消息进阶

飞书支持富文本卡片消息，以下是卡片元素说明：

### 6.1 卡片结构

```csharp
var card = new
{
    config = new { wide_screen_mode = true },
    header = new
    {
        title = new { tag = "plain_text", content = "标题" },  // header 只支持 plain_text
        template = "blue"  // 颜色：blue/red/green/yellow/purple/orange/grey
    },
    elements = new object[]
    {
        // 图片
        new { tag = "img", img_key = "xxx", alt = new { tag = "plain_text", content = "alt" } },
        
        // 分割线
        new { tag = "hr" },
        
        // 文本（div 内的 text 只支持 plain_text，不支持 markdown）
        new { tag = "div", text = new { tag = "plain_text", content = "文本内容" } },
        
        // 表格
        new { tag = "table", children = new[] {
            new { tag = "tr", children = new[] {
                new { tag = "td", children = new[] { new { tag = "plain_text", content = "Cell" } },
                    attrs = new { background_color = "#f0f0f0", align = "center" }
            }}
        }}
    }
};
```

### 6.2 注意事项

- `header.title` 只支持 `plain_text`，不支持 `markdown`
- `div` 内的 `text` 只支持 `plain_text`，**不支持 markdown**
- 只有顶层元素支持 `markdown` 标签
- 使用 `table` 元素可以发送表格数据

---

## 七、配置清单

### 飞书配置
- [ ] 创建自建应用
- [ ] 启用「机器人」能力
- [ ] 添加消息权限 (`im:message`)
- [ ] 发布应用版本
- [ ] 将机器人添加到目标群
- [ ] 获取 App ID、App Secret、Chat ID

### Telegram 配置
- [ ] 通过 @BotFather 创建 Bot
- [ ] 获取 Bot Token
- [ ] 将机器人添加到目标群
- [ ] 获取 Chat ID
- [ ] 在群中发送消息触发更新
