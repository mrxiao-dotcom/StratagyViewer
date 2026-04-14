using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using StrategyViewer.ViewModels;
using StrategyViewer.Services;
using StrategyViewer.Models;

namespace StrategyViewer.Views;

public partial class MainWindow : Window
{
    private const double LeftMargin = 60;
    private const double RightMargin = 20;
    private BacktestResult? _lastResult;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        SizeChanged += MainWindow_SizeChanged;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_lastResult != null)
        {
            DrawCandlestickChart(_lastResult);
        }
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_lastResult != null)
        {
            DrawCandlestickChart(_lastResult);
        }
    }

    public void DrawCandlestickChart(BacktestResult result)
    {
        _lastResult = result;
        KlineCanvas.Children.Clear();

        if (result == null || result.KLineResults.Count == 0)
            return;

        var klines = result.KLineResults;
        var count = klines.Count;

        System.Diagnostics.Debug.WriteLine($"[Chart] 绘制蜡烛图: K线数量={count}, 时间范围 {klines[0].Date:yyyy-MM-dd HH:mm}~{klines.Last().Date:yyyy-MM-dd HH:mm}");
        System.Diagnostics.Debug.WriteLine($"[Chart] 价格信息: 进场={result.EntryPrice:F2}, 止损={result.StopLoss:F2}, 止盈={result.TakeProfit:F2}");

        // 计算价格范围
        double minPrice = klines.Min(k => (double)k.Low);
        double maxPrice = klines.Max(k => (double)k.High);

        if (result.StopLoss > 0) minPrice = Math.Min(minPrice, (double)result.StopLoss);
        if (result.TakeProfit > 0) maxPrice = Math.Max(maxPrice, (double)result.TakeProfit);
        if (result.EntryPrice > 0)
        {
            minPrice = Math.Min(minPrice, (double)result.EntryPrice);
            maxPrice = Math.Max(maxPrice, (double)result.EntryPrice);
        }

        // 添加5%边距
        double priceRange = maxPrice - minPrice;
        minPrice -= priceRange * 0.05;
        maxPrice += priceRange * 0.05;
        priceRange = maxPrice - minPrice;

        // 获取Canvas尺寸
        double canvasWidth = KlineCanvas.ActualWidth;
        double canvasHeight = KlineCanvas.ActualHeight;
        if (canvasWidth <= 0 || canvasHeight <= 0)
        {
            System.Diagnostics.Debug.WriteLine($"[Chart] Canvas尺寸为0: {canvasWidth}x{canvasHeight}");
            return;
        }
        
        System.Diagnostics.Debug.WriteLine($"[Chart] Canvas尺寸: {canvasWidth}x{canvasHeight}");

        double chartWidth = canvasWidth - LeftMargin - RightMargin;
        double chartHeight = canvasHeight - LeftMargin - 30;  // 底部留30像素给X轴标签

        // 每根K线的宽度
        double candleWidth = Math.Max(3, chartWidth / count * 0.7);
        double candleGap = chartWidth / count - candleWidth;

        // 价格到Y坐标的转换
        double chartTop = LeftMargin;
        double chartBottom = LeftMargin + chartHeight;
        // Y轴：价格高在上（chartTop），价格低在下（chartBottom）
        double PriceToY(double price) => chartBottom - ((price - minPrice) / priceRange) * chartHeight;

        System.Diagnostics.Debug.WriteLine($"[Chart] 价格范围: min={minPrice:F2}, max={maxPrice:F2}, range={priceRange:F2}");
        System.Diagnostics.Debug.WriteLine($"[Chart] 图表区域: top={chartTop:F0}, bottom={chartBottom:F0}, height={chartHeight:F0}");

        // 绘制Y轴刻度
        DrawYAxis(canvasHeight, chartHeight, minPrice, maxPrice, PriceToY);

        // 绘制K线
        for (int i = 0; i < count; i++)
        {
            var k = klines[i];
            double x = LeftMargin + i * (candleWidth + candleGap) + candleGap / 2;
            double open = (double)k.Open;
            double high = (double)k.High;
            double low = (double)k.Low;
            double close = (double)k.Close;

            bool isUp = close >= open;
            Brush fillBrush = isUp ? new SolidColorBrush(Color.FromRgb(166, 227, 161)) : new SolidColorBrush(Color.FromRgb(243, 139, 168));
            Brush strokeBrush = fillBrush;

            // 绘制上下影线
            double wickX = x + candleWidth / 2;
            var wick = new Line
            {
                X1 = wickX, Y1 = PriceToY(high),
                X2 = wickX, Y2 = PriceToY(low),
                Stroke = strokeBrush,
                StrokeThickness = 1
            };
            KlineCanvas.Children.Add(wick);

            // 绘制实体
            double bodyTop = PriceToY(Math.Max(open, close));
            double bodyBottom = PriceToY(Math.Min(open, close));
            double bodyHeight = Math.Max(1, bodyBottom - bodyTop);

            var body = new Rectangle
            {
                Width = candleWidth,
                Height = bodyHeight,
                Fill = fillBrush,
                Stroke = strokeBrush,
                StrokeThickness = 1
            };
            Canvas.SetLeft(body, x);
            Canvas.SetTop(body, bodyTop);
            KlineCanvas.Children.Add(body);
        }

        // 绘制水平线
        DrawHorizontalLine(result.StopLoss, "SL", Color.FromRgb(243, 139, 168), PriceToY, canvasWidth);
        DrawHorizontalLine(result.TakeProfit, "TP", Color.FromRgb(137, 180, 250), PriceToY, canvasWidth);
        if (result.Opened && result.EntryPrice > 0)
            DrawHorizontalLine(result.EntryPrice, "Entry", Color.FromRgb(166, 227, 161), PriceToY, canvasWidth);

        // 绘制X轴标签（每隔10根显示时间）
        DrawXAxis(klines, candleWidth, candleGap, canvasHeight);
    }

    private void DrawYAxis(double canvasHeight, double chartHeight, double minPrice, double maxPrice, Func<double, double> PriceToY)
    {
        int tickCount = 6;
        double priceStep = (maxPrice - minPrice) / tickCount;

        for (int i = 0; i <= tickCount; i++)
        {
            double price = minPrice + i * priceStep;
            double y = PriceToY(price);

            // 刻度线
            var tickLine = new Line
            {
                X1 = LeftMargin - 5, Y1 = y,
                X2 = LeftMargin, Y2 = y,
                Stroke = new SolidColorBrush(Color.FromRgb(108, 112, 134)),
                StrokeThickness = 1
            };
            KlineCanvas.Children.Add(tickLine);

            // 标签
            var label = new TextBlock
            {
                Text = price.ToString("F2"),
                Foreground = new SolidColorBrush(Color.FromRgb(108, 112, 134)),
                FontSize = 10
            };
            Canvas.SetLeft(label, 5);
            Canvas.SetTop(label, y - 7);
            KlineCanvas.Children.Add(label);
        }
    }

    private void DrawHorizontalLine(decimal price, string label, Color color, Func<double, double> PriceToY, double canvasWidth)
    {
        if (price <= 0) return;

        double y = PriceToY((double)price);
        double lineY = Math.Max(30, Math.Min(canvasWidth - 30, y));  // 确保在可见范围内
        
        System.Diagnostics.Debug.WriteLine($"[Chart] 绘制水平线: {label}={price:F2}, 价格范围 y=[30,{canvasWidth - 30}], 实际y={y:F1}, 限制后={lineY:F1}");

        var line = new Line
        {
            X1 = LeftMargin, Y1 = lineY,
            X2 = canvasWidth - RightMargin, Y2 = lineY,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 6, 3 }
        };
        KlineCanvas.Children.Add(line);

        var labelBg = new Border
        {
            Background = new SolidColorBrush(color),
            CornerRadius = new CornerRadius(2),
            Padding = new Thickness(4, 2, 4, 2)
        };
        var labelText = new TextBlock
        {
            Text = $"{label}: {price:F2}",
            Foreground = Brushes.White,
            FontSize = 9
        };
        labelBg.Child = labelText;
        Canvas.SetLeft(labelBg, canvasWidth - RightMargin - 80);
        Canvas.SetTop(labelBg, y - 10);
        KlineCanvas.Children.Add(labelBg);
    }

    private void DrawXAxis(List<KLineResult> klines, double candleWidth, double candleGap, double canvasHeight)
    {
        int labelInterval = Math.Max(1, klines.Count / 10);
        for (int i = 0; i < klines.Count; i += labelInterval)
        {
            double x = LeftMargin + i * (candleWidth + candleGap) + candleWidth / 2;
            var label = new TextBlock
            {
                Text = klines[i].Date.ToString("MM-dd\nHH:mm"),
                Foreground = new SolidColorBrush(Color.FromRgb(108, 112, 134)),
                FontSize = 9,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(label, x - 20);
            Canvas.SetTop(label, canvasHeight - 35);
            KlineCanvas.Children.Add(label);
        }
    }

    private void StrategyTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        // 双击处理，不需要这里处理
    }

    private void StrategyItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is Models.StrategyGroup group)
        {
            if (e.ClickCount == 2)
            {
                System.Diagnostics.Debug.WriteLine("[双击] StrategyItem");
                if (group.Tag is Models.StrategyListItem strategy)
                {
                    System.Diagnostics.Debug.WriteLine($"[双击] Strategy: {strategy.Title}");
                    if (DataContext is MainViewModel viewModel)
                    {
                        viewModel.SelectStrategy(strategy);
                    }
                }
            }
            else if (e.ClickCount == 1)
            {
                // 单击时展开/折叠节点
                System.Diagnostics.Debug.WriteLine("[单击] StrategyItem - 切换展开状态");
                var treeViewItem = GetTreeViewItem(element);
                if (treeViewItem != null)
                {
                    treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
                }
            }
        }
    }

    private void ContractItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            if (sender is FrameworkElement element && element.DataContext is Models.StrategyGroup group
                && group.Tag is Models.ContractSelection contract)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.SelectContract(contract);
                }
            }
        }
    }

    private TreeViewItem? GetTreeViewItem(FrameworkElement element)
    {
        var item = element;
        while (item != null && !(item is TreeViewItem))
        {
            item = System.Windows.Media.VisualTreeHelper.GetParent(item) as FrameworkElement;
        }
        return item as TreeViewItem;
    }
}
