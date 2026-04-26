# InfoServer API 接口文档

## 基本信息

| 项目 | 值 |
|------|-----|
| 服务器地址 | `http://localhost` |
| 默认端口 | `8082` |
| 认证方式 | Bearer Token |
| 数据格式 | JSON |

> **提示**：Token 在应用程序界面中查看和重置。

---

## 认证说明

所有API请求都需要在 HTTP Header 中添加 Authorization 字段：

```
Authorization: Bearer {你的Token}
```

### PowerShell 调用示例

```powershell
# 方式1：Invoke-WebRequest
$headers = @{"Authorization" = "Bearer YOUR_TOKEN"}
Invoke-WebRequest -Uri "http://localhost:8082/api/strategy" -Headers $headers

# 方式2：使用 curl.exe（绕过 PowerShell 别名）
curl.exe -H "Authorization: Bearer YOUR_TOKEN" "http://localhost:8082/api/strategy"
```

### curl 调用示例

```bash
curl -H "Authorization: Bearer YOUR_TOKEN" "http://localhost:8082/api/strategy"
```

---

## 信息管理 API

### 1. 获取所有信息

**接口地址**

```
GET /api/info
```

**返回参数**

| 字段 | 类型 | 说明 |
|------|------|------|
| success | boolean | 请求是否成功 |
| data | array | 信息列表 |

**响应示例**

```json
{
  "success": true,
  "message": "操作成功",
  "data": [
    {
      "id": 1,
      "title": "示例标题",
      "content": "# 内容\n\n这是详细信息...",
      "category": "技术",
      "tags": "标签1,标签2",
      "createdAt": "2024-01-15T10:30:00",
      "updatedAt": "2024-01-15T10:30:00"
    }
  ],
  "timestamp": "2024-01-15T10:30:00"
}
```

**调用示例**

```bash
curl -H "Authorization: Bearer YOUR_TOKEN" "http://localhost:8082/api/info"
```

---

### 2. 创建信息

**接口地址**

```
POST /api/info
```

**请求参数 (JSON Body)**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| title | string | 是 | 信息标题 |
| content | string | 是 | 信息内容 |
| category | string | 否 | 分类 |
| tags | array | 否 | 标签数组 |

**请求示例**

```json
{
  "title": "新建信息标题",
  "content": "# 内容\n\n详细信息...",
  "category": "技术",
  "tags": ["标签1", "标签2"]
}
```

**调用示例**

```bash
curl -X POST -H "Authorization: Bearer YOUR_TOKEN" -H "Content-Type: application/json" -d '{
  "title": "新建信息",
  "content": "# 内容",
  "category": "分类",
  "tags": ["标签1"]
}' "http://localhost:8082/api/info"
```

---

### 3. 更新信息

```
PUT /api/info/{id}
```

**路径参数**

| 参数 | 类型 | 说明 |
|------|------|------|
| id | integer | 信息ID |

**请求参数 (JSON Body)**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| title | string | 是 | 信息标题 |
| content | string | 是 | 信息内容 |
| category | string | 否 | 分类 |
| tags | array | 否 | 标签数组 |

**调用示例**

```bash
curl -X PUT -H "Authorization: Bearer YOUR_TOKEN" -H "Content-Type: application/json" -d '{
  "title": "更新后的标题",
  "content": "更新后的内容",
  "category": "新分类"
}' "http://localhost:8082/api/info/1"
```

---

### 4. 删除信息

```
DELETE /api/info/{id}
```

**路径参数**

| 参数 | 类型 | 说明 |
|------|------|------|
| id | integer | 信息ID |

**调用示例**

```bash
curl -X DELETE -H "Authorization: Bearer YOUR_TOKEN" "http://localhost:8082/api/info/1"
```

---

## 策略管理 API

### 1. 获取策略列表

```
GET /api/strategy
```

**查询参数**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| startDate | string | 否 | 开始日期 (yyyy-MM-dd)，不指定则返回所有 |
| endDate | string | 否 | 结束日期 (yyyy-MM-dd)，不指定则返回所有 |

> **说明**：不指定日期时，返回所有策略（按交易日期倒序排列）。

**返回参数**

| 字段 | 类型 | 说明 |
|------|------|------|
| success | boolean | 请求是否成功 |
| data | array | 策略列表 |

**data 数组元素**

| 字段 | 类型 | 说明 |
|------|------|------|
| id | integer | 策略ID |
| title | string | 策略标题 |
| tradeDate | string | 交易日期 |
| createdAt | string | 创建时间 |
| itemCount | integer | 品种数量 |

**响应示例**

```json
{
  "success": true,
  "message": "获取成功，共5条策略",
  "data": [
    {
      "id": 1,
      "title": "2024-01-15 黑色系策略",
      "tradeDate": "2024-01-15T00:00:00",
      "createdAt": "2024-01-15T08:00:00",
      "itemCount": 5
    }
  ],
  "timestamp": "2024-01-15T10:00:00"
}
```

**调用示例**

```bash
# 获取所有策略（推荐）
curl -H "Authorization: Bearer YOUR_TOKEN" "http://localhost:8082/api/strategy"

# 获取指定日期范围
curl -H "Authorization: Bearer YOUR_TOKEN" "http://localhost:8082/api/strategy?startDate=2024-01-01&endDate=2024-01-31"
```

---

### 2. 获取单个策略

```
GET /api/strategy/{id}
```

**路径参数**

| 参数 | 类型 | 说明 |
|------|------|------|
| id | integer | 策略ID |

**返回参数**

| 字段 | 类型 | 说明 |
|------|------|------|
| success | boolean | 请求是否成功 |
| data | object | 策略详情对象 |

**data 对象结构**

| 字段 | 类型 | 说明 |
|------|------|------|
| id | integer | 策略ID |
| title | string | 策略标题 |
| summary | string | 摘要（JSON数组格式） |
| content | string | 内容（Markdown格式） |
| tradeDate | string | 交易日期 |
| createdAt | string | 创建时间 |
| updatedAt | string | 更新时间 |

**响应示例**

```json
{
  "success": true,
  "data": {
    "id": 1,
    "title": "2024-01-15 黑色系策略",
    "summary": "[{\"contract\":\"铁矿石 (i2405)\",\"direction\":\"做空\",\"entryRange\":\"815-820\",\"stopLoss\":\"850\",\"takeProfit\":\"750\",\"logic\":\"港口库存逼近历史高位...\"}]",
    "content": "## 标题\n\n## 摘要\n\n```json\n[...]\n```\n\n## 内容主体\n\n详细分析...",
    "tradeDate": "2024-01-15T00:00:00",
    "createdAt": "2024-01-15T08:00:00",
    "updatedAt": "2024-01-15T10:30:00"
  }
}
```

**调用示例**

```bash
curl -H "Authorization: Bearer YOUR_TOKEN" "http://localhost:8082/api/strategy/1"
```

---

### 3. 创建策略

```
POST /api/strategy
```

**请求参数 (JSON Body)**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| title | string | 是 | 策略标题 |
| summary | string | 是 | 摘要（JSON数组格式） |
| content | string | 是 | 内容（Markdown格式） |
| tradeDate | string | 是 | 交易日期 (yyyy-MM-dd) |

**summary 格式说明**

summary 是一个 JSON 数组，每个元素代表一个交易品种：

```json
[
  {
    "contract": "铁矿石 (i2405)",
    "direction": "做空",
    "entryRange": "815-820",
    "stopLoss": "850",
    "takeProfit": "750",
    "logic": "策略逻辑说明..."
  }
]
```

| 字段 | 类型 | 说明 |
|------|------|------|
| contract | string | 合约名称 |
| direction | string | 交易方向（做多/做空） |
| entryRange | string | 入场区间 |
| stopLoss | string | 止损位 |
| takeProfit | string | 止盈位 |
| logic | string | 策略逻辑 |

**请求示例**

```json
{
  "title": "2024-01-15 黑色系策略",
  "summary": "[{\"contract\":\"铁矿石\",\"direction\":\"做空\",\"entryRange\":\"815-820\",\"stopLoss\":\"850\",\"takeProfit\":\"750\",\"logic\":\"港口库存高位\"}]",
  "content": "## 标题\n\n## 摘要\n\n## 内容主体\n\n详细分析内容...",
  "tradeDate": "2024-01-15"
}
```

**调用示例**

```bash
curl -X POST -H "Authorization: Bearer YOUR_TOKEN" -H "Content-Type: application/json" -d '{
  "title": "2024-01-15 黑色系策略",
  "summary": "[{\"contract\":\"铁矿石 (i2405)\",\"direction\":\"做空\",\"entryRange\":\"815-820\",\"stopLoss\":\"850\",\"takeProfit\":\"750\",\"logic\":\"港口库存高位\"}]",
  "content": "## 标题\n\n## 摘要\n\n## 内容主体\n\n详细分析...",
  "tradeDate": "2024-01-15"
}' "http://localhost:8082/api/strategy"
```

---

### 4. 更新策略

```
PUT /api/strategy/{id}
```

**路径参数**

| 参数 | 类型 | 说明 |
|------|------|------|
| id | integer | 策略ID |

**请求参数 (JSON Body)**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| title | string | 是 | 策略标题 |
| summary | string | 是 | 摘要（JSON数组格式） |
| content | string | 是 | 内容（Markdown格式） |
| tradeDate | string | 是 | 交易日期 (yyyy-MM-dd) |

**调用示例**

```bash
curl -X PUT -H "Authorization: Bearer YOUR_TOKEN" -H "Content-Type: application/json" -d '{
  "title": "更新后的策略标题",
  "summary": "[{\"contract\":\"铁矿石\",\"direction\":\"做多\"}]",
  "content": "## 更新后的内容",
  "tradeDate": "2024-01-16"
}' "http://localhost:8082/api/strategy/1"
```

---

### 5. 删除策略

```
DELETE /api/strategy/{id}
```

**路径参数**

| 参数 | 类型 | 说明 |
|------|------|------|
| id | integer | 策略ID |

**调用示例**

```bash
curl -X DELETE -H "Authorization: Bearer YOUR_TOKEN" "http://localhost:8082/api/strategy/1"
```

---

### 6. 获取策略统计

```
GET /api/strategy/stats
```

**响应示例**

```json
{
  "success": true,
  "data": {
    "totalStrategies": 25,
    "totalItems": 120,
    "dateRange": {
      "start": "2024-01-01",
      "end": "2024-01-31"
    }
  }
}
```

**调用示例**

```bash
curl -H "Authorization: Bearer YOUR_TOKEN" "http://localhost:8082/api/strategy/stats"
```

---

### 7. 根据品种查询策略历史

```
GET /api/strategy/contract/{contract}
```

**说明**：根据品种名称查询该品种在历史策略中的记录。支持三种匹配方式：
- 品种名称：如 `螺纹钢`、`铁矿石`
- 具体合约：如 `rb2605`、`i2405`
- 合约代码：如 `rb`、`i`

**路径参数**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| contract | string | 是 | 品种名称/合约代码 |

**查询参数**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| days | integer | 否 | 查询天数，默认30天，最大365天 |

**返回参数**

| 字段 | 类型 | 说明 |
|------|------|------|
| success | boolean | 请求是否成功 |
| data | array | 品种历史记录列表 |

**data 数组元素**

| 字段 | 类型 | 说明 |
|------|------|------|
| tradeDate | string | 交易日期 |
| strategyId | integer | 策略ID |
| strategyTitle | string | 策略标题 |
| direction | string | 多空方向（做多/做空） |
| entryRange | string | 进场价格区间 |
| stopLoss | string | 止损价格 |
| takeProfit | string | 止盈价格 |
| logic | string | 进场逻辑 |
| confidence | number | 置信度分数（0-1） |

**响应示例**

```json
{
  "success": true,
  "message": "查询到 6 条记录",
  "data": [
    {
      "tradeDate": "2024-01-15T00:00:00",
      "strategyId": 5,
      "strategyTitle": "2024-01-15 黑色系策略",
      "direction": "做空",
      "entryRange": "3800-3850",
      "stopLoss": "3950",
      "takeProfit": "3600",
      "logic": "库存持续累积，需求走弱",
      "confidence": 0.85
    },
    {
      "tradeDate": "2024-01-12T00:00:00",
      "strategyId": 3,
      "strategyTitle": "2024-01-12 螺纹钢策略",
      "direction": "做多",
      "entryRange": "3750-3780",
      "stopLoss": "3700",
      "takeProfit": "3900",
      "logic": "基差修复，终端采购增加",
      "confidence": 0.72
    }
  ],
  "timestamp": "2024-01-15T10:00:00"
}
```

**调用示例**

```bash
# 查询螺纹钢最近30天的策略记录
curl -H "Authorization: Bearer YOUR_TOKEN" "http://localhost:8082/api/strategy/contract/螺纹钢"

# 查询螺纹钢最近60天的策略记录
curl -H "Authorization: Bearer YOUR_TOKEN" "http://localhost:8082/api/strategy/contract/螺纹钢?days=60"

# 使用合约代码查询（如rb代表螺纹钢）
curl -H "Authorization: Bearer YOUR_TOKEN" "http://localhost:8082/api/strategy/contract/rb?days=90"

# 查询铁矿石（i）
curl -H "Authorization: Bearer YOUR_TOKEN" "http://localhost:8082/api/strategy/contract/铁矿石?days=30"
```

---

## 备份接口

### 导出备份

```
GET /api/strategy/backup
```

**说明**：导出所有策略数据（用于备份迁移）。

**响应示例**

```json
{
  "success": true,
  "data": [
    {
      "title": "2024-01-15 黑色系策略",
      "summary": "[{\"contract\":\"铁矿石\",\"direction\":\"做空\"}]",
      "content": "## 内容...",
      "tradeDate": "2024-01-15T00:00:00",
      "createdAt": "2024-01-15T08:00:00",
      "updatedAt": "2024-01-15T10:30:00"
    }
  ]
}
```

**调用示例**

```bash
curl -H "Authorization: Bearer YOUR_TOKEN" "http://localhost:8082/api/strategy/backup"
```

---

## 错误响应

```json
{
  "success": false,
  "message": "错误信息描述",
  "timestamp": "2024-01-15T10:00:00"
}
```

### 常见错误码

| HTTP状态码 | 说明 |
|-----------|------|
| 400 | 请求参数错误 |
| 401 | 未授权（Token无效或缺失） |
| 404 | 资源不存在 |
| 500 | 服务器内部错误 |

---

## 完整调用示例

### PowerShell

```powershell
$token = "YOUR_TOKEN"
$baseUrl = "http://localhost:8082"
$headers = @{"Authorization" = "Bearer $token"}

# 获取策略列表
$response = Invoke-WebRequest -Uri "$baseUrl/api/strategy" -Headers $headers
$strategies = ($response.Content | ConvertFrom-Json).data

# 创建策略
$newStrategy = @{
    title = "测试策略"
    summary = '[{"contract":"铁矿石","direction":"做空","entryRange":"815","stopLoss":"850","takeProfit":"750","logic":"测试"}]'
    content = "## 标题\n\n## 内容"
    tradeDate = "2024-01-15"
} | ConvertTo-Json

Invoke-WebRequest -Uri "$baseUrl/api/strategy" -Headers $headers -Method POST -Body $newStrategy -ContentType "application/json"
```

### curl

```bash
TOKEN="YOUR_TOKEN"
BASE_URL="http://localhost:8082"

# 获取策略列表（返回所有策略）
curl -H "Authorization: Bearer $TOKEN" "$BASE_URL/api/strategy"

# 获取单个策略
curl -H "Authorization: Bearer $TOKEN" "$BASE_URL/api/strategy/1"

# 创建策略
curl -X POST \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "2024-01-15 黑色系策略",
    "summary": "[{\"contract\":\"铁矿石\",\"direction\":\"做空\",\"entryRange\":\"815-820\",\"stopLoss\":\"850\",\"takeProfit\":\"750\",\"logic\":\"港口库存高位\"}]",
    "content": "## 标题\n\n## 摘要\n\n## 内容",
    "tradeDate": "2024-01-15"
  }' \
  "$BASE_URL/api/strategy"

# 更新策略
curl -X PUT \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "更新后的策略",
    "summary": "[{\"contract\":\"铁矿石\",\"direction\":\"做多\"}]",
    "content": "更新后的内容",
    "tradeDate": "2024-01-16"
  }' \
  "$BASE_URL/api/strategy/1"

# 删除策略
curl -X DELETE -H "Authorization: Bearer $TOKEN" "$BASE_URL/api/strategy/1"
```

---

## 注意事项

1. **Token安全**：请妥善保管Token，不要泄露给他人
2. **日期格式**：所有日期使用 `yyyy-MM-dd` 格式
3. **Content-Type**：POST/PUT 请求必须设置 `Content-Type: application/json`
4. **JSON转义**：在命令行中使用时，注意对特殊字符进行转义
5. **端口修改**：如果修改了默认端口，请相应更改URL中的端口号
6. **策略列表**：建议先调用 `GET /api/strategy` 获取所有策略及其ID，再根据ID查询详情
