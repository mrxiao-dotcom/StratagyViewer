using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using StrategyViewer.Models;
using StrategyViewer.Services;
using StrategyViewer.ViewModels;

namespace StrategyViewer.Views;

public partial class ContractSearchWindow : Window
{
    private readonly ContractSearchViewModel _viewModel;

    public ContractSearchWindow(IStrategyService strategyService, IMarketDataService marketDataService, IContractParserService contractParserService)
    {
        InitializeComponent();
        _viewModel = new ContractSearchViewModel(strategyService, marketDataService, contractParserService);
        DataContext = _viewModel;
        SearchTextBox.Focus();
    }

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.SearchCommand.Execute(null);
        }
    }

    private void ProductButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            var tag = button.Tag as string;
            if (!string.IsNullOrEmpty(tag))
            {
                System.Diagnostics.Debug.WriteLine($"[按钮点击] 搜索: {tag}");
                _viewModel.SearchByProduct(tag);
            }
        }
    }

    private void StrategyItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ContractHistory history)
        {
            _viewModel.SelectedItem = history;

            // 查找并设置对应的日期组选中状态
            foreach (var group in _viewModel.GroupedByDate)
            {
                if (group.Items.Contains(history))
                {
                    StrategiesList.SelectedItem = group;
                    break;
                }
            }
        }
    }

    public ContractHistory? SelectedContract => _viewModel.SelectedItem;
}
