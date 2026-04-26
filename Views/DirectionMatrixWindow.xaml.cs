using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using StrategyViewer.Services;
using StrategyViewer.ViewModels;

namespace StrategyViewer.Views;

public partial class DirectionMatrixWindow : Window
{
    private readonly DirectionMatrixViewModel _viewModel;
    private readonly IFeishuService _feishuService;
    private readonly ITelegramService _telegramService;
    private readonly ISettingsService _settingsService;

    public DirectionMatrixWindow(
        IContractParserService contractParserService,
        List<Models.ContractHistory> cachedContracts,
        IFeishuService feishuService,
        ITelegramService telegramService,
        ISettingsService settingsService)
    {
        InitializeComponent();
        _viewModel = new DirectionMatrixViewModel(contractParserService, cachedContracts);
        _feishuService = feishuService;
        _telegramService = telegramService;
        _settingsService = settingsService;
        DataContext = _viewModel;

        Loaded += DirectionMatrixWindow_Loaded;
        PreviewMouseDown += Window_PreviewMouseDown;
    }

    private void Window_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // 点击窗口空白区域时关闭弹窗
        if (_currentPopup != null && _currentPopupAnchor != null)
        {
            // 检查是否点击在弹窗内
            var popup = _currentPopup;
            if (popup.Child != null)
            {
                var position = e.GetPosition(popup.Child);
                if (position.X >= 0 && position.Y >= 0 &&
                    position.X <= popup.Child.RenderSize.Width &&
                    position.Y <= popup.Child.RenderSize.Height)
                {
                    return; // 点击在弹窗内，不关闭
                }
            }

            // 检查是否点击在单元格内
            var anchorPosition = e.GetPosition(_currentPopupAnchor);
            if (anchorPosition.X >= 0 && anchorPosition.Y >= 0 &&
                anchorPosition.X <= _currentPopupAnchor.ActualWidth &&
                anchorPosition.Y <= _currentPopupAnchor.ActualHeight)
            {
                return; // 点击在单元格内，不关闭
            }

            CloseCurrentPopup();
        }
    }

    private async void DirectionMatrixWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 等待数据加载完成后再构建表格
        await System.Threading.Tasks.Task.Delay(100);
        BuildMatrixGrid();
    }

    private void BuildMatrixGrid()
    {
        MatrixGrid.Children.Clear();
        MatrixGrid.RowDefinitions.Clear();
        MatrixGrid.ColumnDefinitions.Clear();

        if (_viewModel.Products.Count == 0 || _viewModel.Rows.Count == 0)
        {
            var noDataText = new TextBlock
            {
                Text = "没有数据",
                Foreground = new SolidColorBrush(Color.FromRgb(108, 112, 134)),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 100, 0, 0)
            };
            MatrixGrid.Children.Add(noDataText);
            return;
        }

        int colCount = _viewModel.Products.Count + 1; // 日期列 + 品种列
        int rowCount = _viewModel.Rows.Count + 1;     // 表头行 + 数据行

        // 添加行定义
        MatrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 表头行

        foreach (var row in _viewModel.Rows)
        {
            MatrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        // 添加列定义
        MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 日期列

        foreach (var product in _viewModel.Products)
        {
            MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        // 绘制表头行
        var dateHeader = new Border
        {
            Style = (Style)FindResource("HeaderCell"),
            Child = new TextBlock
            {
                Text = "日期",
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(137, 180, 250))
            }
        };
        SetPosition(dateHeader, 0, 0);
        MatrixGrid.Children.Add(dateHeader);

        int col = 1;
        foreach (var product in _viewModel.Products)
        {
            var header = new Border
            {
                Style = (Style)FindResource("HeaderCell"),
                MinWidth = 50,
                Child = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };

            var codeText = new TextBlock
            {
                Text = product.Code,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(137, 180, 250)),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var nameText = new TextBlock
            {
                Text = product.Name,
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 112, 134)),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            ((StackPanel)header.Child).Children.Add(codeText);
            ((StackPanel)header.Child).Children.Add(nameText);

            SetPosition(header, 0, col);
            MatrixGrid.Children.Add(header);
            col++;
        }

        // 绘制数据行
        int rowIndex = 1;
        foreach (var row in _viewModel.Rows)
        {
            // 日期单元格
            var dateCell = new Border
            {
                Style = (Style)FindResource("DateCell"),
                MinWidth = 70,
                Child = new TextBlock
                {
                    Text = row.DateString,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(205, 214, 244)),
                    FontWeight = FontWeights.SemiBold
                }
            };
            SetPosition(dateCell, rowIndex, 0);
            MatrixGrid.Children.Add(dateCell);

            // 品种数据单元格
            int cellCol = 1;
            foreach (var cell in row.Cells)
            {
                Border cellBorder;

                if (cell.IsEmpty)
                {
                    cellBorder = new Border
                    {
                        Style = (Style)FindResource("EmptyCell"),
                        MinWidth = 50,
                        MinHeight = 30,
                        Child = new TextBlock
                        {
                            Text = "",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };
                }
                else if (cell.IsLong)
                {
                    cellBorder = CreateCellWithPopup(cell, true);
                }
                else
                {
                    cellBorder = CreateCellWithPopup(cell, false);
                }

                SetPosition(cellBorder, rowIndex, cellCol);
                MatrixGrid.Children.Add(cellBorder);
                cellCol++;
            }

            rowIndex++;
        }
    }

    private static void SetPosition(UIElement element, int row, int column)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, column);
    }

    // 跟踪当前打开的弹窗
    private Popup? _currentPopup;
    private Border? _currentPopupAnchor;

    private Border CreateCellWithPopup(DirectionCell cell, bool isLong)
    {
        var style = isLong ? (Style)FindResource("LongCell") : (Style)FindResource("ShortCell");
        var textColor = isLong ? Brushes.White : new SolidColorBrush(Color.FromRgb(30, 30, 46));
        var text = isLong ? "多" : "空";

        // 创建弹窗内容
        var popupContent = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(48, 48, 60)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(137, 180, 250)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            MinWidth = 250,
            MaxWidth = 350,
            Child = new StackPanel { Orientation = Orientation.Vertical }
        };

        var contentPanel = (StackPanel)popupContent.Child;

        // 策略标题
        if (!string.IsNullOrEmpty(cell.StrategyTitle))
        {
            contentPanel.Children.Add(new TextBlock
            {
                Text = cell.StrategyTitle,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(137, 180, 250)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        // 方向和合约
        contentPanel.Children.Add(CreateInfoRow("方向", cell.Direction));
        contentPanel.Children.Add(CreateInfoRow("合约", cell.Contract));

        // 进场价
        if (!string.IsNullOrEmpty(cell.EntryRange))
        {
            contentPanel.Children.Add(CreateInfoRow("进场", cell.EntryRange));
        }

        // 止损
        if (!string.IsNullOrEmpty(cell.StopLoss))
        {
            contentPanel.Children.Add(CreateInfoRow("止损", cell.StopLoss, "#f38ba8"));
        }

        // 止盈
        if (!string.IsNullOrEmpty(cell.TakeProfit))
        {
            contentPanel.Children.Add(CreateInfoRow("止盈", cell.TakeProfit, "#a6e3a1"));
        }

        // 说明
        if (!string.IsNullOrEmpty(cell.Logic))
        {
            contentPanel.Children.Add(new TextBlock
            {
                Text = "说明",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 112, 134)),
                Margin = new Thickness(0, 8, 0, 2)
            });
            contentPanel.Children.Add(new TextBlock
            {
                Text = cell.Logic,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(205, 214, 244)),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 320
            });
        }

        // 创建弹窗
        var popup = new Popup
        {
            AllowsTransparency = true,
            Placement = PlacementMode.Relative,
            StaysOpen = false,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 10,
                ShadowDepth = 2,
                Opacity = 0.5
            }
        };

        // 单击事件控制弹窗
        var mainBorder = new Border
        {
            Style = style,
            MinWidth = 50,
            MinHeight = 30,
            CornerRadius = new CornerRadius(4),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        mainBorder.MouseLeftButtonDown += (s, e) => TogglePopup(popup, mainBorder, e);

        popup.Child = popupContent;

        mainBorder.Child = new TextBlock
        {
            Text = text,
            FontSize = 12,
            Foreground = textColor,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        return mainBorder;
    }

    private void TogglePopup(Popup popup, Border anchor, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (popup.IsOpen && _currentPopup == popup)
        {
            // 点击同一单元格，关闭弹窗
            popup.IsOpen = false;
            _currentPopup = null;
            _currentPopupAnchor = null;
        }
        else
        {
            // 关闭之前的弹窗
            if (_currentPopup != null)
            {
                _currentPopup.IsOpen = false;
            }

            // 打开新弹窗，使用 Mouse 模式
            popup.PlacementTarget = anchor;
            popup.Placement = PlacementMode.Mouse;
            popup.IsOpen = true;
            _currentPopup = popup;
            _currentPopupAnchor = anchor;
        }
    }

    private void CloseCurrentPopup()
    {
        if (_currentPopup != null)
        {
            _currentPopup.IsOpen = false;
            _currentPopup = null;
            _currentPopupAnchor = null;
        }
    }

    private StackPanel CreateInfoRow(string label, string value, string? valueColor = null)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
        panel.Children.Add(new TextBlock
        {
            Text = $"{label}：",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(108, 112, 134)),
            MinWidth = 40
        });
        panel.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 12,
            Foreground = string.IsNullOrEmpty(valueColor)
                ? new SolidColorBrush(Color.FromRgb(205, 214, 244))
                : (SolidColorBrush)new BrushConverter().ConvertFromString(valueColor)!,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 260
        });
        return panel;
    }

    private async void SendFeishu_Click(object sender, RoutedEventArgs e)
    {
        if (_feishuService == null || _settingsService == null)
        {
            MessageBox.Show("飞书服务未配置", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 检查是否配置了飞书传图
        if (!_settingsService.Settings.FeishuImageSettings.IsEnabled)
        {
            MessageBox.Show("请先在设置中启用飞书传图功能并配置 App ID、App Secret 和 Chat ID", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SendFeishuButton.IsEnabled = false;
        _viewModel.StatusMessage = "正在发送到飞书...";

        // 创建临时图片文件
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"矩阵_{DateTime.Now:yyyyMMdd_HHmmss}.png");

        try
        {
            // 生成矩阵图片（在 UI 线程）
            var matrixBorder = FindMatrixBorder();
            if (matrixBorder != null)
            {
                var wrapper = CreateImageWrapper(matrixBorder);
                SaveElementToImage(wrapper, tempImagePath);
            }
            else
            {
                SaveElementToImage(CreateImageWrapper(MatrixGrid), tempImagePath);
            }

            System.Diagnostics.Debug.WriteLine($"[飞书] 图片已保存到: {tempImagePath}");

            // 发送图片到飞书（在后台线程）
            var success = await Task.Run(() => _feishuService.SendMatrixImageAsync(tempImagePath, _viewModel, _settingsService));

            if (success)
            {
                _viewModel.StatusMessage = "已发送到飞书";
                MessageBox.Show("矩阵图片已成功发送到飞书！", "发送成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                _viewModel.StatusMessage = "发送到飞书失败";
                MessageBox.Show("发送到飞书失败，请检查配置和网络", "发送失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = $"发送失败：{ex.Message}";
            MessageBox.Show($"发送失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SendFeishuButton.IsEnabled = true;
            // 清理临时文件
            if (File.Exists(tempImagePath))
            {
                try { File.Delete(tempImagePath); } catch { }
            }
        }
    }

    private async void SendTelegram_Click(object sender, RoutedEventArgs e)
    {
        if (_telegramService == null || _settingsService == null)
        {
            MessageBox.Show("Telegram 服务未配置", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!_settingsService.Settings.TelegramSettings.IsEnabled)
        {
            MessageBox.Show("请先在设置中启用 Telegram 通知并配置 Bot Token", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 配置 Telegram 服务
        var telegramConfig = _settingsService.Settings.TelegramSettings;
        _telegramService.Configure(telegramConfig.BotToken, telegramConfig.ChatIds);

        SendTelegramButton.IsEnabled = false;
        _viewModel.StatusMessage = "正在发送到 Telegram...";

        // 创建临时图片文件
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"矩阵_{DateTime.Now:yyyyMMdd_HHmmss}.png");

        try
        {
            // 生成图片
            var matrixBorder = FindMatrixBorder();
            if (matrixBorder != null)
            {
                var wrapper = CreateImageWrapper(matrixBorder);
                SaveElementToImage(wrapper, tempImagePath);
            }
            else
            {
                var wrapper = CreateImageWrapper(MatrixGrid);
                SaveElementToImage(wrapper, tempImagePath);
            }

            // 构建图片说明
            var caption = $"📊 *品种多空矩阵*\n" +
                         $"更新时间: {DateTime.Now:yyyy-MM-dd HH:mm}\n" +
                         $"品种: {_viewModel.Products.Count} | 天数: {_viewModel.Dates.Count}";

            // 发送图片到所有群组
            var results = await _telegramService.SendPhotoToAllWithErrorAsync(tempImagePath, caption);

            var successCount = results.Count(r => r.Value.Success);
            var failCount = results.Count(r => !r.Value.Success);

            if (failCount == 0)
            {
                _viewModel.StatusMessage = $"已发送到 {successCount} 个 Telegram 群";
                MessageBox.Show($"矩阵图片已成功发送到 {successCount} 个 Telegram 群！", "发送成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var failedMessages = results
                    .Where(r => !r.Value.Success)
                    .Select(r => $"  • {r.Key}\n    错误: {r.Value.Error}")
                    .ToList();
                var failedInfo = string.Join("\n", failedMessages);

                _viewModel.StatusMessage = "部分发送到 Telegram 失败";
                MessageBox.Show($"发送到 Telegram 部分失败\n\n成功: {successCount} 个群\n失败: {failCount} 个群\n\n失败的详情:\n{failedInfo}",
                    "发送结果", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = $"发送失败：{ex.Message}";
            MessageBox.Show($"发送失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SendTelegramButton.IsEnabled = true;
            // 清理临时文件
            if (File.Exists(tempImagePath))
            {
                try { File.Delete(tempImagePath); } catch { }
            }
        }
    }

    private void SaveImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PNG 图片|*.png|JPEG 图片|*.jpg",
            DefaultExt = ".png",
            FileName = $"矩阵_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                SaveImageButton.IsEnabled = false;
                _viewModel.StatusMessage = "正在保存图片...";

                // 获取矩阵Grid的边界
                var matrixBorder = FindMatrixBorder();
                if (matrixBorder != null)
                {
                    // 创建包含矩阵和免责声明的包装
                    var wrapper = CreateImageWrapper(matrixBorder);
                    SaveElementToImage(wrapper, dialog.FileName);
                }
                else
                {
                    // 创建包含矩阵和免责声明的包装
                    var wrapper = CreateImageWrapper(MatrixGrid);
                    SaveElementToImage(wrapper, dialog.FileName);
                }

                _viewModel.StatusMessage = "图片已保存";
                MessageBox.Show($"图片已保存至：\n{dialog.FileName}", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _viewModel.StatusMessage = $"保存失败：{ex.Message}";
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SaveImageButton.IsEnabled = true;
            }
        }
    }

    private Border CreateImageWrapper(FrameworkElement content)
    {
        // 创建包装容器
        var wrapper = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 46)),
            Padding = new Thickness(20),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        var mainStack = new StackPanel { Orientation = Orientation.Vertical };

        // 添加矩阵内容
        var contentClone = CloneElement(content);
        if (contentClone != null)
        {
            mainStack.Children.Add(contentClone);
        }

        // 添加分隔线和免责声明
        var disclaimer = new TextBlock
        {
            Text = "⚠️ 免责声明 (Disclaimer)\n" +
                   "本频道/群组提供的所有投研数据、策略简报及多空方向，均基于大模型算法与公开数据生成，仅供学术研究与交流参考，不构成任何具体的投资、理财或交易建议。\n" +
                   "金融衍生品交易具有极高风险，您可能会损失全部初始本金。请独立思考，自主决策，盈亏自负。",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(137, 180, 250)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 15, 0, 0),
            MaxWidth = 1200
        };
        mainStack.Children.Add(disclaimer);

        wrapper.Child = mainStack;
        return wrapper;
    }

    private FrameworkElement? CloneElement(FrameworkElement original)
    {
        try
        {
            // 序列化并反序列化来克隆元素
            var xaml = System.Windows.Markup.XamlWriter.Save(original);
            return System.Windows.Markup.XamlReader.Parse(xaml) as FrameworkElement;
        }
        catch
        {
            return null;
        }
    }

    private Border? FindMatrixBorder()
    {
        // 尝试找到包含矩阵内容的外层Border
        var parent = MatrixGrid.Parent;
        while (parent != null)
        {
            if (parent is Border border)
                return border;
            if (parent is Window)
                break;
            parent = (parent as FrameworkElement)?.Parent;
        }
        return null;
    }

    private void SaveElementToImage(FrameworkElement element, string filePath)
    {
        // 等待布局更新
        element.UpdateLayout();

        double width = element.ActualWidth;
        double height = element.ActualHeight;

        if (width == 0 || height == 0)
        {
            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            element.Arrange(new Rect(element.DesiredSize));
            width = element.ActualWidth;
            height = element.ActualHeight;
        }

        // 提高分辨率：使用2倍缩放和192 DPI
        double scale = 2.0;
        int pixelWidth = (int)(width * scale);
        int pixelHeight = (int)(height * scale);

        var renderBitmap = new RenderTargetBitmap(
            pixelWidth, pixelHeight, 192, 192, PixelFormats.Pbgra32);

        var drawingVisual = new DrawingVisual();
        using (var context = drawingVisual.RenderOpen())
        {
            var brush = new VisualBrush(element)
            {
                Stretch = Stretch.None
            };
            context.DrawRectangle(brush, null, new Rect(0, 0, width, height));
        }

        renderBitmap.Render(drawingVisual);

        var encoder = filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            ? (BitmapEncoder)new JpegBitmapEncoder { QualityLevel = 95 }
            : new PngBitmapEncoder();

        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

        using var stream = File.Create(filePath);
        encoder.Save(stream);
    }

    private void SaveScrollViewerToImage(ScrollViewer scrollViewer, string filePath)
    {
        // 等待布局更新
        scrollViewer.UpdateLayout();

        // 保存当前的滚动位置
        var savedHorizontalOffset = scrollViewer.HorizontalOffset;
        var savedVerticalOffset = scrollViewer.VerticalOffset;

        // 滚动到最左和最上
        scrollViewer.ScrollToHorizontalOffset(0);
        scrollViewer.ScrollToVerticalOffset(0);

        // 获取内容大小
        var contentWidth = scrollViewer.ExtentWidth;
        var contentHeight = scrollViewer.ExtentHeight;

        // 提高分辨率：使用2倍缩放和192 DPI
        double scale = 2.0;
        int pixelWidth = (int)(contentWidth * scale);
        int pixelHeight = (int)(contentHeight * scale);

        // 创建渲染位图
        var renderBitmap = new RenderTargetBitmap(
            pixelWidth, pixelHeight, 192, 192, PixelFormats.Pbgra32);

        // 创建临时Canvas来绘制内容
        var canvas = new Canvas
        {
            Width = contentWidth,
            Height = contentHeight,
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 46))
        };

        // 克隆矩阵内容到Canvas
        CloneVisualToCanvas(MatrixGrid, canvas, 0, 0);

        // 渲染
        var drawingVisual = new DrawingVisual();
        using (var context = drawingVisual.RenderOpen())
        {
            context.DrawRectangle(
                new SolidColorBrush(Color.FromRgb(30, 30, 46)),
                null,
                new Rect(0, 0, contentWidth, contentHeight));
        }

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var brush = new VisualBrush(canvas);
            context.DrawRectangle(brush, null, new Rect(0, 0, contentWidth, contentHeight));
        }

        renderBitmap.Render(visual);

        // 恢复滚动位置
        scrollViewer.ScrollToHorizontalOffset(savedHorizontalOffset);
        scrollViewer.ScrollToVerticalOffset(savedVerticalOffset);

        // 保存
        var encoder = filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            ? (BitmapEncoder)new JpegBitmapEncoder { QualityLevel = 95 }
            : new PngBitmapEncoder();

        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

        using var stream = File.Create(filePath);
        encoder.Save(stream);
    }

    private void CloneVisualToCanvas(FrameworkElement source, Canvas target, double offsetX, double offsetY)
    {
        source.UpdateLayout();

        var clone = new Border
        {
            Width = source.ActualWidth,
            Height = source.ActualHeight,
            Background = Brushes.Transparent
        };

        // 克隆子元素
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(source); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(source, i);
            if (child is UIElement uiChild)
            {
                var clonedChild = CloneElement(uiChild);
                if (clonedChild != null)
                {
                    Canvas.SetLeft(clonedChild, offsetX);
                    Canvas.SetTop(clonedChild, offsetY);
                    clone.Child = clonedChild;
                    break;
                }
            }
        }

        Canvas.SetLeft(clone, offsetX);
        Canvas.SetTop(clone, offsetY);
        target.Children.Add(clone);
    }

    private UIElement? CloneElement(UIElement element)
    {
        if (element is TextBlock textBlock)
        {
            return new TextBlock
            {
                Text = textBlock.Text,
                FontFamily = textBlock.FontFamily,
                FontSize = textBlock.FontSize,
                FontWeight = textBlock.FontWeight,
                Foreground = textBlock.Foreground,
                Background = textBlock.Background,
                Padding = textBlock.Padding,
                Margin = textBlock.Margin
            };
        }

        if (element is Border border)
        {
            var newBorder = new Border
            {
                Width = border.ActualWidth,
                Height = border.ActualHeight,
                Background = border.Background,
                BorderBrush = border.BorderBrush,
                BorderThickness = border.BorderThickness,
                CornerRadius = border.CornerRadius,
                Padding = border.Padding,
                Margin = border.Margin
            };

            if (border.Child != null)
            {
                var clonedChild = CloneElement(border.Child);
                if (clonedChild != null)
                    newBorder.Child = clonedChild;
            }

            return newBorder;
        }

        return null;
    }
}
