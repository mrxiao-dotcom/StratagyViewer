using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using StrategyViewer.Models;

namespace StrategyViewer.Views;

public class KLineChartControl : Canvas
{
    public static readonly DependencyProperty KLineDataProperty =
        DependencyProperty.Register(nameof(KLineData), typeof(List<MarketData>), typeof(KLineChartControl),
            new PropertyMetadata(null, OnDataChanged));

    public static readonly DependencyProperty SignalsProperty =
        DependencyProperty.Register(nameof(Signals), typeof(List<ContractHistory>), typeof(KLineChartControl),
            new PropertyMetadata(null, OnDataChanged));

    public static readonly DependencyProperty EntryPriceProperty =
        DependencyProperty.Register(nameof(EntryPrice), typeof(double), typeof(KLineChartControl),
            new PropertyMetadata(0.0, OnDataChanged));

    public static readonly DependencyProperty StopLossProperty =
        DependencyProperty.Register(nameof(StopLoss), typeof(double), typeof(KLineChartControl),
            new PropertyMetadata(0.0, OnDataChanged));

    public static readonly DependencyProperty TakeProfitProperty =
        DependencyProperty.Register(nameof(TakeProfit), typeof(double), typeof(KLineChartControl),
            new PropertyMetadata(0.0, OnDataChanged));

    public List<MarketData>? KLineData
    {
        get => (List<MarketData>?)GetValue(KLineDataProperty);
        set => SetValue(KLineDataProperty, value);
    }

    public List<ContractHistory>? Signals
    {
        get => (List<ContractHistory>?)GetValue(SignalsProperty);
        set => SetValue(SignalsProperty, value);
    }

    public double EntryPrice
    {
        get => (double)GetValue(EntryPriceProperty);
        set => SetValue(EntryPriceProperty, value);
    }

    public double StopLoss
    {
        get => (double)GetValue(StopLossProperty);
        set => SetValue(StopLossProperty, value);
    }

    public double TakeProfit
    {
        get => (double)GetValue(TakeProfitProperty);
        set => SetValue(TakeProfitProperty, value);
    }

    private const double LeftMargin = 60;
    private const double RightMargin = 30;
    private const double TopMargin = 20;
    private const double BottomMargin = 40;

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KLineChartControl control)
        {
            control.InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        // 背景
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x31, 0x32, 0x44)), null,
            new Rect(0, 0, ActualWidth, ActualHeight));

        var klines = KLineData;
        if (klines == null || klines.Count == 0)
        {
            DrawNoDataMessage(dc, "加载K线数据中...");
            return;
        }

        double canvasWidth = ActualWidth;
        double canvasHeight = ActualHeight;
        if (canvasWidth <= LeftMargin + RightMargin || canvasHeight <= TopMargin + BottomMargin)
            return;

        double chartWidth = canvasWidth - LeftMargin - RightMargin;
        double chartHeight = canvasHeight - TopMargin - BottomMargin;

        // 计算价格范围
        double minPrice = klines.Min(k => (double)k.Low);
        double maxPrice = klines.Max(k => (double)k.High);

        // 包含策略价格
        if (EntryPrice > 0)
        {
            minPrice = Math.Min(minPrice, EntryPrice);
            maxPrice = Math.Max(maxPrice, EntryPrice);
        }
        if (StopLoss > 0)
        {
            minPrice = Math.Min(minPrice, StopLoss);
            maxPrice = Math.Max(maxPrice, StopLoss);
        }
        if (TakeProfit > 0)
        {
            minPrice = Math.Min(minPrice, TakeProfit);
            maxPrice = Math.Max(maxPrice, TakeProfit);
        }

        // 添加边距
        double priceRange = maxPrice - minPrice;
        if (priceRange < 1) priceRange = 10;
        minPrice -= priceRange * 0.05;
        maxPrice += priceRange * 0.05;
        priceRange = maxPrice - minPrice;

        // 价格转Y坐标
        double PriceToY(double price) => TopMargin + chartHeight * (1 - (price - minPrice) / priceRange);

        // 绘制网格
        DrawGrid(dc, chartWidth, chartHeight, minPrice, maxPrice, PriceToY);

        // 每根K线宽度
        int count = klines.Count;
        double candleWidth = Math.Max(2, chartWidth / count * 0.7);
        double candleGap = chartWidth / count - candleWidth;

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
            Brush color = isUp
                ? new SolidColorBrush(Color.FromRgb(166, 227, 161))
                : new SolidColorBrush(Color.FromRgb(243, 139, 168));

            // 影线
            double wickX = x + candleWidth / 2;
            var wick = new Line
            {
                X1 = wickX, Y1 = PriceToY(high),
                X2 = wickX, Y2 = PriceToY(low),
                Stroke = color,
                StrokeThickness = 1
            };
            Children.Add(wick);

            // 实体
            double bodyTop = PriceToY(Math.Max(open, close));
            double bodyBottom = PriceToY(Math.Min(open, close));
            double bodyHeight = Math.Max(1, bodyBottom - bodyTop);

            var body = new Rectangle
            {
                Width = candleWidth,
                Height = bodyHeight,
                Fill = color,
                Stroke = color,
                StrokeThickness = 1
            };
            Canvas.SetLeft(body, x);
            Canvas.SetTop(body, bodyTop);
            Children.Add(body);
        }

        // 绘制策略价格线
        DrawHorizontalLine(dc, EntryPrice, "入场", Color.FromRgb(166, 227, 161), PriceToY, canvasWidth);
        DrawHorizontalLine(dc, StopLoss, "止损", Color.FromRgb(243, 139, 168), PriceToY, canvasWidth);
        DrawHorizontalLine(dc, TakeProfit, "止盈", Color.FromRgb(137, 180, 250), PriceToY, canvasWidth);

        // 绘制信号标记
        DrawSignalMarkers(dc, chartWidth, chartHeight, klines, minPrice, maxPrice, PriceToY);

        // X轴
        DrawXAxis(dc, klines, candleWidth, candleGap, canvasHeight);
    }

    private void DrawNoDataMessage(DrawingContext dc, string message)
    {
        var text = new FormattedText(
            message,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            14,
            new SolidColorBrush(Color.FromRgb(0x6c, 0x70, 0x86)),
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        dc.DrawText(text, new Point((ActualWidth - text.Width) / 2, (ActualHeight - text.Height) / 2));
    }

    private void DrawGrid(DrawingContext dc, double chartWidth, double chartHeight,
        double minPrice, double maxPrice, Func<double, double> PriceToY)
    {
        var gridBrush = new SolidColorBrush(Color.FromRgb(0x45, 0x47, 0x5a));
        var textBrush = new SolidColorBrush(Color.FromRgb(0xcd, 0xd6, 0xf4));

        // 水平网格线
        for (int i = 0; i <= 5; i++)
        {
            double y = TopMargin + chartHeight * i / 5;
            var pen = new Pen(gridBrush, 1);
            pen.Freeze();
            dc.DrawLine(pen, new Point(LeftMargin, y), new Point(LeftMargin + chartWidth, y));

            // 价格标签
            double price = maxPrice - (maxPrice - minPrice) * i / 5;
            var priceText = new FormattedText(
                price.ToString("F2"),
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                10,
                textBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(priceText, new Point(LeftMargin - priceText.Width - 8, y - priceText.Height / 2));
        }

        // 边框
        var axisPen = new Pen(new SolidColorBrush(Color.FromRgb(0x6c, 0x70, 0x86)), 1);
        axisPen.Freeze();
        dc.DrawLine(axisPen, new Point(LeftMargin, TopMargin), new Point(LeftMargin, TopMargin + chartHeight));
        dc.DrawLine(axisPen, new Point(LeftMargin, TopMargin + chartHeight), new Point(LeftMargin + chartWidth, TopMargin + chartHeight));
    }

    private void DrawHorizontalLine(DrawingContext dc, double price, string label, Color color,
        Func<double, double> PriceToY, double canvasWidth)
    {
        if (price <= 0) return;

        double y = PriceToY(price);
        var linePen = new Pen(new SolidColorBrush(color), 2)
        {
            DashStyle = DashStyles.Dash
        };
        linePen.Freeze();
        dc.DrawLine(linePen, new Point(LeftMargin, y), new Point(canvasWidth - RightMargin, y));

        // 标签
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

        // 手动绘制标签背景和文字
        var bgBrush = new SolidColorBrush(color);
        var formattedText = new FormattedText(
            $"{label}: {price:F2}",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            9,
            Brushes.White,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        dc.DrawRectangle(bgBrush, null, new Rect(canvasWidth - RightMargin - formattedText.Width - 8, y - formattedText.Height / 2 - 2, formattedText.Width + 8, formattedText.Height + 4));
        dc.DrawText(formattedText, new Point(canvasWidth - RightMargin - formattedText.Width - 4, y - formattedText.Height / 2));
    }

    private void DrawXAxis(DrawingContext dc, List<MarketData> klines, double candleWidth, double candleGap, double canvasHeight)
    {
        int labelInterval = Math.Max(1, klines.Count / 8);
        var textBrush = new SolidColorBrush(Color.FromRgb(0x6c, 0x70, 0x86));

        for (int i = 0; i < klines.Count; i += labelInterval)
        {
            double x = LeftMargin + i * (candleWidth + candleGap) + candleWidth / 2;
            var label = new TextBlock
            {
                Text = klines[i].Date.ToString("MM-dd\nHH:mm"),
                Foreground = textBrush,
                FontSize = 9,
                TextAlignment = TextAlignment.Center
            };
            var ft = new FormattedText(
                klines[i].Date.ToString("MM-dd\nHH:mm"),
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                9,
                textBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(ft, new Point(x - 20, canvasHeight - BottomMargin + 5));
        }
    }

    private void DrawSignalMarkers(DrawingContext dc, double chartWidth, double chartHeight,
        List<MarketData> klines, double minPrice, double maxPrice, Func<double, double> PriceToY)
    {
        if (Signals == null || Signals.Count == 0) return;

        var firstKlineTime = klines.First().Date.Date;
        var lastKlineTime = klines.Last().Date.Date;
        var klineRange = lastKlineTime - firstKlineTime;
        if (klineRange.TotalDays < 1) klineRange = TimeSpan.FromDays(1);

        int count = klines.Count;
        double candleWidth = chartWidth / count * 0.7;
        double candleGap = chartWidth / count - candleWidth;

        foreach (var signal in Signals)
        {
            // 检查信号日期是否在K线范围内
            var signalDate = signal.TradeDate.Date;
            if (signalDate < firstKlineTime || signalDate > lastKlineTime)
                continue;

            // 计算X位置（基于日期）
            double dayOffset = (signalDate - firstKlineTime).TotalDays;
            double x = LeftMargin + (dayOffset / klineRange.TotalDays) * chartWidth;

            // 解析价格并绘制标记
            if (TryParsePrice(signal.EntryRange, out var entry))
            {
                var isLong = signal.Direction.Contains("多") || signal.Direction.Contains("买");
                var color = isLong
                    ? Color.FromRgb(166, 227, 161)
                    : Color.FromRgb(243, 139, 168);

                double y = PriceToY(entry);
                DrawArrowMarker(dc, x, y, isLong, color);
            }
        }
    }

    private void DrawArrowMarker(DrawingContext dc, double x, double y, bool isLong, Color color)
    {
        var size = 8.0;
        var geometry = new StreamGeometry();
        var brush = new SolidColorBrush(color);

        using (var ctx = geometry.Open())
        {
            if (isLong)
            {
                ctx.BeginFigure(new Point(x, y - size), true, true);
                ctx.LineTo(new Point(x + size * 0.7, y - size * 0.3), true, false);
                ctx.LineTo(new Point(x - size * 0.7, y - size * 0.3), true, false);
            }
            else
            {
                ctx.BeginFigure(new Point(x, y + size), true, true);
                ctx.LineTo(new Point(x + size * 0.7, y + size * 0.3), true, false);
                ctx.LineTo(new Point(x - size * 0.7, y + size * 0.3), true, false);
            }
        }

        geometry.Freeze();
        dc.DrawGeometry(brush, new Pen(brush, 1.5), geometry);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x2e)),
            new Pen(brush, 2), new Point(x, y), 5, 5);
    }

    private static bool TryParsePrice(string? priceStr, out double price)
    {
        price = 0;
        if (string.IsNullOrEmpty(priceStr)) return false;

        var cleaned = priceStr.Trim();
        var match = System.Text.RegularExpressions.Regex.Match(cleaned, @"[\d.]+");
        if (match.Success && double.TryParse(match.Value, out price))
        {
            return true;
        }
        return false;
    }
}
