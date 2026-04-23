using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Felweed.Models;
using Felweed.Models.Enumerators;
using Felweed.Models.Graph;
using Felweed.Services;
using Felweed.Services.Graph;

namespace Felweed.ViewModels.MainMenu.Graph;

public partial class GraphPageViewModel : ObservableObject
{
    private DependencyGraph? _graph;

    [ObservableProperty]
    public partial ObservableCollection<LevelVm> AllLevels { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<LevelVm> FilteredLevels { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Solution> FilterListLibraries { get; set; } = [];

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
                ? Enumerable.ToList<Solution>(lvl.Nodes)
                : Enumerable.Where<Solution>(lvl.Nodes, n => visible.Contains(n.Id)).ToList();

            if (nodes.Count == 0)
                continue; // hide empty levels

            var copy = new LevelVm { Level = lvl.Level };
            foreach (var n in nodes)
                copy.Nodes.Add(n);

            FilteredLevels.Add(copy);
        }
    }
}