using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Models;
using Felweed.Models.Enumerators;
using Felweed.Models.Graph;
using Felweed.Services;
using Felweed.Services.Graph;

namespace Felweed.ViewModels;

public partial class GraphPageViewModel : ObservableObject
{
    private DependencyGraph? _graph;
    
    [ObservableProperty] private ObservableCollection<LevelVm> _allLevels = [];
    [ObservableProperty] private ObservableCollection<LevelVm> _filteredLevels = [];
    [ObservableProperty] private ObservableCollection<Solution> _filterListLibraries = [];
    
    [RelayCommand]
    private void PackSolution(Solution? solution)
    {
        solution?.Pack();
    }
    
    [RelayCommand]
    private void InvalidateLibraryCache(Solution? solution)
    {
        solution?.InvalidateCache();
    }

    public void Load()
    {
        _graph = DependencyGraphBuilder.Build(MainViewModel.GraphPageSelector == SolutionKind.CSharp
            ? SolutionScanner.CsharpSolutions
            : SolutionScanner.AngularSolutions);
        var layers = GraphLayering.BuildLayers(_graph);

        AllLevels.Clear();
        FilterListLibraries.Clear();

        for (var i = 0; i < layers.Count; i++)
        {
            var level = new LevelVm { Level = i };

            foreach (var id in layers[i])
            {
                var solution = _graph.Nodes[id].Solution;

                level.Nodes.Add(solution);

                if (solution.Type == SolutionType.Library)
                    FilterListLibraries.Add(solution);
            }

            AllLevels.Add(level);
        }

        ApplyFilter(null); // show all
    }
    
    public void ApplyFilter(Guid? libraryId)
    {
        FilteredLevels.Clear();

        if (_graph is null)
            return;

        var visible = libraryId is null
            ? null
            : GraphQueries.GetDownstreamInclusive(_graph, libraryId.Value);

        foreach (var lvl in AllLevels)
        {
            var nodes = (visible is null)
                ? lvl.Nodes.ToList()
                : lvl.Nodes.Where(n => visible.Contains(n.Id)).ToList();

            if (nodes.Count == 0)
                continue; // hide empty levels

            var copy = new LevelVm { Level = lvl.Level };
            foreach (var n in nodes)
                copy.Nodes.Add(n);

            FilteredLevels.Add(copy);
        }
    }
}