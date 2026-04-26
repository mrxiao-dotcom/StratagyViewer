using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using StrategyViewer.Models;

namespace StrategyViewer.Views;

public class SignalChartControl : Canvas
{
    public static readonly DependencyProperty SignalsProperty =
        DependencyProperty.Register(nameof(Signals), typeof(ObservableCollection<ContractHistory>), typeof(SignalChartControl),
            new PropertyMetadata(null, OnSignalsChanged));

    public ObservableCollection<ContractHistory>? Signals
    {
        get => (ObservableCollection<ContractHistory>?)GetValue(SignalsProperty);
        set => SetValue(SignalsProperty, value);
    }

    private static void OnSignalsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SignalChartControl control)
        {
            // 取消旧集合的事件订阅
            if (e.OldValue is ObservableCollection<ContractHistory> oldCollection)
            {
                oldCollection.CollectionChanged -= control.OnCollectionChanged;
            }
            // 订阅新集合的事件
            if (e.NewValue is ObservableCollection<ContractHistory> newCollection)
            {
                newCollection.CollectionChanged += control.OnCollectionChanged;
            }
            control.InvalidateVisual();
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
        InvalidateArrange();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        // 背景
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x31, 0x32, 0x44)), null,
            new Rect(0, 0, ActualWidth, ActualHeight));

        var signals = Signals?.ToList() ?? new List<ContractHistory>();
        if (signals == null || signals.Count == 0)
        {
            DrawNoDataMessage(dc);
            return;
        }

        var chartMargin = new Thickness(60, 30, 30, 50);
        var chartWidth = ActualWidth - chartMargin.Left - chartMargin.Right;
        var chartHeight = ActualHeight - chartMargin.Top - chartMargin.Bottom;

        if (chartWidth <= 0 || chartHeight <= 0) return;

        // 解析价格数据，排除套利策略
        var validSignals = signals
            .Where(s => TryParsePrice(s.EntryRange, out _) && !IsArbitrageStrategy(s))
            .OrderBy(s => s.TradeDate)
            .ToList();

        if (validSignals.Count == 0)
        {
            DrawNoDataMessage(dc);
            return;
        }

        // 计算价格范围
        double minPrice = double.MaxValue;
        double maxPrice = double.MinValue;

        foreach (var signal in validSignals)
        {
            if (TryParsePrice(signal.EntryRange, out var entry))
            {
                minPrice = Math.Min(minPrice, entry);
                maxPrice = Math.Max(maxPrice, entry);
            }
            if (!string.IsNullOrEmpty(signal.StopLoss) && TryParsePrice(signal.StopLoss, out var sl))
            {
                minPrice = Math.Min(minPrice, sl);
                maxPrice = Math.Max(maxPrice, sl);
            }
            if (!string.IsNullOrEmpty(signal.TakeProfit) && TryParsePrice(signal.TakeProfit, out var tp))
            {
                minPrice = Math.Min(minPrice, tp);
                maxPrice = Math.Max(maxPrice, tp);
            }
        }

        // 添加价格边距
        var priceRange = maxPrice - minPrice;
        if (priceRange < 1) priceRange = 10;
        minPrice -= priceRange * 0.1;
        maxPrice += priceRange * 0.1;

        // 绘制网格和轴
        DrawGrid(dc, chartMargin, chartWidth, chartHeight, validSignals, minPrice, maxPrice);

        // 绘制每个信号
        var dateRange = validSignals.Max(s => s.TradeDate) - validSignals.Min(s => s.TradeDate);
        if (dateRange.TotalDays < 1) dateRange = TimeSpan.FromDays(1);

        for (int i = 0; i < validSignals.Count; i++)
        {
            var signal = validSignals[i];
            DrawSignal(dc, signal, chartMargin, chartWidth, chartHeight, minPrice, maxPrice,
                validSignals.Min(s => s.TradeDate), dateRange);
        }
    }

    private void DrawNoDataMessage(DrawingContext dc)
    {
        var text = new FormattedText(
            "选择策略查看信号图",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            14,
            new SolidColorBrush(Color.FromRgb(0x6c, 0x70, 0x86)),
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        dc.DrawText(text, new Point((ActualWidth - text.Width) / 2, (ActualHeight - text.Height) / 2));
    }

    private void DrawGrid(DrawingContext dc, Thickness margin, double chartWidth, double chartHeight,
        List<ContractHistory> signals, double minPrice, double maxPrice)
    {
        var gridBrush = new SolidColorBrush(Color.FromRgb(0x45, 0x47, 0x5a));
        var axisBrush = new SolidColorBrush(Color.FromRgb(0x6c, 0x70, 0x86));
        var textBrush = new SolidColorBrush(Color.FromRgb(0xcd, 0xd6, 0xf4));

        var chartLeft = margin.Left;
        var chartTop = margin.Top;
        var chartRight = margin.Left + chartWidth;
        var chartBottom = margin.Top + chartHeight;

        // 水平网格线（5条）
        for (int i = 0; i <= 4; i++)
        {
            var y = chartTop + (chartHeight * i / 4);
            var pen = new Pen(gridBrush, 1);
            pen.Freeze();
            dc.DrawLine(pen, new Point(chartLeft, y), new Point(chartRight, y));

            // 价格标签
            var price = maxPrice - (maxPrice - minPrice) * i / 4;
            var priceText = new FormattedText(
                price.ToString("F2"),
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                11,
                textBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(priceText, new Point(margin.Left - priceText.Width - 8, y - priceText.Height / 2));
        }

        // 垂直网格线
        var uniqueDates = signals.Select(s => s.TradeDate.Date).Distinct().OrderBy(d => d).ToList();
        var dateSpacing = uniqueDates.Count > 1 ? chartWidth / (uniqueDates.Count - 1) : chartWidth;

        for (int i = 0; i < uniqueDates.Count; i++)
        {
            var x = chartLeft + (uniqueDates.Count > 1 ? dateSpacing * i : chartWidth / 2);
            var pen = new Pen(gridBrush, 1);
            pen.Freeze();
            dc.DrawLine(pen, new Point(x, chartTop), new Point(x, chartBottom));

            // 日期标签
            var dateText = new FormattedText(
                uniqueDates[i].ToString("MM-dd"),
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                10,
                textBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(dateText, new Point(x - dateText.Width / 2, chartBottom + 8));
        }

        // 轴线
        var axisPen = new Pen(axisBrush, 2);
        axisPen.Freeze();
        dc.DrawLine(axisPen, new Point(chartLeft, chartTop), new Point(chartLeft, chartBottom));
        dc.DrawLine(axisPen, new Point(chartLeft, chartBottom), new Point(chartRight, chartBottom));

        // 轴标签
        var yLabel = new FormattedText(
            "价格",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            12,
            textBrush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(yLabel, new Point(15, chartTop - 5));

        var xLabel = new FormattedText(
            "日期",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            12,
            textBrush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(xLabel, new Point(chartRight - xLabel.Width, chartBottom + 25));
    }

    private void DrawSignal(DrawingContext dc, ContractHistory signal, Thickness margin,
        double chartWidth, double chartHeight, double minPrice, double maxPrice,
        DateTime minDate, TimeSpan dateRange)
    {
        if (!TryParsePrice(signal.EntryRange, out var entryPrice)) return;
        var stopLoss = TryParsePrice(signal.StopLoss ?? "", out var sl) ? sl : (double?)null;
        var takeProfit = TryParsePrice(signal.TakeProfit ?? "", out var tp) ? tp : (double?)null;

        var chartLeft = margin.Left;
        var chartTop = margin.Top;

        // 计算X位置
        var dayOffset = (signal.TradeDate.Date - minDate).TotalDays;
        var totalDays = dateRange.TotalDays > 0 ? dateRange.TotalDays : 1;
        var x = chartLeft + (chartWidth * dayOffset / totalDays);

        // 计算Y位置
        double Y(double price) => chartTop + chartHeight * (1 - (price - minPrice) / (maxPrice - minPrice));

        var entryY = Y(entryPrice);

        // 判断做多还是做空
        var isLong = signal.Direction.Contains("多") || signal.Direction.Contains("买");
        var arrowColor = isLong
            ? new SolidColorBrush(Color.FromRgb(0xa6, 0xe3, 0xa1))  // 绿色
            : new SolidColorBrush(Color.FromRgb(0xf3, 0x8b, 0xa8)); // 红色

        // 绘制入场点箭头
        DrawArrow(dc, x, entryY, isLong, arrowColor);

        // 绘制水平虚线延伸
        var dashPen = new Pen(new SolidColorBrush(Color.FromArgb(80, arrowColor.Color.R, arrowColor.Color.G, arrowColor.Color.B)), 1);
        dashPen.DashStyle = DashStyles.Dash;
        dashPen.Freeze();

        // 绘制止损和止盈点
        if (stopLoss.HasValue)
        {
            var slY = Y(stopLoss.Value);
            DrawPoint(dc, x, slY, new SolidColorBrush(Color.FromRgb(0xf3, 0x8b, 0xa8)));

            // 绘制连接线
            dc.DrawLine(dashPen, new Point(x, entryY), new Point(x, slY));

            // 标签
            DrawLabel(dc, "止损:" + stopLoss.Value.ToString("F2"), x + 5, slY - 10,
                new SolidColorBrush(Color.FromRgb(0xf3, 0x8b, 0xa8)));
        }

        if (takeProfit.HasValue)
        {
            var tpY = Y(takeProfit.Value);
            DrawPoint(dc, x, tpY, new SolidColorBrush(Color.FromRgb(0x89, 0xdc, 0xeb)));

            // 绘制连接线
            dc.DrawLine(dashPen, new Point(x, entryY), new Point(x, tpY));

            // 标签
            DrawLabel(dc, "止盈:" + takeProfit.Value.ToString("F2"), x + 5, tpY - 10,
                new SolidColorBrush(Color.FromRgb(0x89, 0xdc, 0xeb)));
        }

        // 入场点标签
        var directionText = isLong ? "做多" : "做空";
        DrawLabel(dc, directionText + " " + entryPrice.ToString("F2"), x + 5, entryY - 25, arrowColor);
    }

    private void DrawArrow(DrawingContext dc, double x, double y, bool isLong, SolidColorBrush color)
    {
        var size = 10.0;
        var geometry = new StreamGeometry();

        using (var ctx = geometry.Open())
        {
            if (isLong) // 向上箭头
            {
                ctx.BeginFigure(new Point(x, y - size), true, true);
                ctx.LineTo(new Point(x + size * 0.7, y - size * 0.3), true, false);
                ctx.LineTo(new Point(x - size * 0.7, y - size * 0.3), true, false);
            }
            else // 向下箭头
            {
                ctx.BeginFigure(new Point(x, y + size), true, true);
                ctx.LineTo(new Point(x + size * 0.7, y + size * 0.3), true, false);
                ctx.LineTo(new Point(x - size * 0.7, y + size * 0.3), true, false);
            }
        }

        geometry.Freeze();
        dc.DrawGeometry(color, new Pen(color, 1.5), geometry);

        // 绘制中心点
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x2e)),
            new Pen(color, 2), new Point(x, y), 5, 5);
    }

    private void DrawPoint(DrawingContext dc, double x, double y, SolidColorBrush color)
    {
        dc.DrawEllipse(color, null, new Point(x, y), 4, 4);
    }

    private void DrawLabel(DrawingContext dc, string text, double x, double y, SolidColorBrush color)
    {
        var formattedText = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            10,
            color,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        // 背景
        var bgBrush = new SolidColorBrush(Color.FromArgb(200, 0x31, 0x32, 0x44));
        dc.DrawRectangle(bgBrush, null, new Rect(x - 2, y - 2, formattedText.Width + 4, formattedText.Height + 4));

        dc.DrawText(formattedText, new Point(x, y));
    }

    private bool TryParsePrice(string? priceStr, out double price)
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

    private static bool IsArbitrageStrategy(ContractHistory signal)
    {
        var title = signal.StrategyTitle ?? "";
        var contract = signal.Contract ?? "";
        return title.Contains("套利") || contract.Contains("套利");
    }
}
