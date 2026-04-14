using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StrategyViewer.Models;

public class ContractSelection
{
    public int StrategyId { get; set; }
    public string Contract { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
}

public partial class StrategyGroup : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private StrategyGroupChildren _children = new();

    [ObservableProperty]
    private object? _tag;

    public int Count
    {
        get
        {
            int count = 0;
            foreach (var child in Children)
            {
                if (child.Tag is StrategyListItem)
                    count++;
                else if (child is StrategyGroup group)
                    count += group.Count;
            }
            return count;
        }
    }
}

public class StrategyGroupChildren : ObservableCollection<StrategyGroup>
{
    public StrategyGroupChildren() { }
}

public class StrategyGroupCollection : ObservableCollection<StrategyGroup>
{
    public StrategyGroupCollection() { }

    public void RefreshContractNodes(int strategyId, List<StrategySummary> items)
    {
        // 遍历所有节点找到对应的策略节点
        foreach (var dateGroup in this)
        {
            foreach (var strategyNode in dateGroup.Children)
            {
                if (strategyNode.Tag is StrategyListItem strategy && strategy.Id == strategyId)
                {
                    System.Diagnostics.Debug.WriteLine($"[RefreshContractNodes] 找到节点 {strategyId}, 更新 {items.Count} 个品种");
                    strategyNode.Children.Clear();
                    foreach (var summary in items)
                    {
                        strategyNode.Children.Add(new StrategyGroup
                        {
                            Key = $"contract_{strategyId}_{summary.Contract}",
                            Name = $"{summary.Contract} ({summary.Direction})",
                            Tag = new ContractSelection { StrategyId = strategyId, Contract = summary.Contract, Direction = summary.Direction }
                        });
                    }
                    // 展开节点以显示品种
                    strategyNode.IsExpanded = true;
                    return;
                }
            }
        }
        System.Diagnostics.Debug.WriteLine($"[RefreshContractNodes] 未找到节点 {strategyId}");
    }

    public static StrategyGroupCollection CreateFrom(IEnumerable<StrategyListItem> strategies)
    {
        var result = new StrategyGroupCollection();

        var dateGroups = new Dictionary<string, List<StrategyListItem>>();

        foreach (var strategy in strategies)
        {
            var dateKey = strategy.TradeDate.ToString("yyyy-MM-dd");

            if (!dateGroups.ContainsKey(dateKey))
                dateGroups[dateKey] = new List<StrategyListItem>();

            dateGroups[dateKey].Add(strategy);
        }

        foreach (var dateGroup in dateGroups.OrderByDescending(d => d.Key))
        {
            var group = new StrategyGroup
            {
                Key = dateGroup.Key,
                Name = dateGroup.Key,
                IsExpanded = true
            };

            foreach (var strategy in dateGroup.Value)
            {
                var strategyNode = new StrategyGroup
                {
                    Key = $"strategy_{strategy.Id}",
                    Name = strategy.Title,
                    Tag = strategy,
                    IsExpanded = true
                };

                group.Children.Add(strategyNode);
            }

            if (group.Children.Count > 0)
            {
                result.Add(group);
            }
        }

        return result;
    }
}
