using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Felweed.Models;
using Felweed.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace Felweed.Views;

public partial class GraphPage : Page, INavigableView<GraphPageViewModel>
{
    public GraphPageViewModel ViewModel { get; }

    public GraphPage(GraphPageViewModel viewModel)
    {
        InitializeComponent();
        
        ViewModel = viewModel;
        DataContext = viewModel;
        
        Loaded += PageLoaded;
    }
    
    private void PageLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Load();
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        LibFilter.SelectedIndex = -1;
        ViewModel.ApplyFilter(null);
    }

    private void LibFilter_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0)
        {
            ViewModel.ApplyFilter((e.AddedItems[0] as Solution).Id);
            ScrollToTop_Click(sender, null);
            SetExpansionStatus(MainTree, true);
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