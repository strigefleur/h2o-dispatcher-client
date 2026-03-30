using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Felweed.ViewModels;

namespace Felweed.Views;

public partial class GraphPage : Page
{
    private GraphPageViewModel Vm => (GraphPageViewModel)DataContext;

    public GraphPage()
    {
        InitializeComponent();

        var vm = new GraphPageViewModel();
        vm.Load();

        DataContext = vm;
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        LibFilter.SelectedIndex = -1;
        Vm.ApplyFilter(null);
    }

    private void LibFilter_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0)
        {
            Vm.ApplyFilter((e.AddedItems[0] as LevelNodeVm).Id);
            SetExpansionStatus(MainTree, true);
            ScrollToTop_Click(sender, null);
        }
    }

    private void ExpandAll_Click(object sender, RoutedEventArgs e)
    {
        SetExpansionStatus(MainTree, true);
    }

    private void CollapseAll_Click(object sender, RoutedEventArgs e)
    {
        SetExpansionStatus(MainTree, false);
    }

    private void SetExpansionStatus(ItemsControl container, bool isExpanded)
    {
        foreach (var item in container.Items)
        {
            // Get the visual container (TreeViewItem) for the data item
            if (container.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeItem)
            {
                treeItem.IsExpanded = isExpanded;

                // Recurse into children
                if (treeItem.HasItems)
                {
                    SetExpansionStatus(treeItem, isExpanded);
                }
            }
        }
    }

    private void ScrollToTop_Click(object sender, RoutedEventArgs e)
    {
        var scrollViewer = FindVisualChild<ScrollViewer>(MainTree);
        scrollViewer?.ScrollToTop();
    }

    // Helper to find the ScrollViewer inside the TreeView
    private T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if (child is T dependencyObject)
                return dependencyObject;

            return FindVisualChild<T>(child);
        }

        return null;
    }
}