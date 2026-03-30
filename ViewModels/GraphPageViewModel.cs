using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
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
    [ObservableProperty] private ObservableCollection<LevelNodeVm> _filterListLibraries = [];

    public void Load()
    {
        _graph = DependencyGraphBuilder.Build(MainViewModel.GraphPageSelector == SolutionKind.CSharp
            ? SolutionScanner.CsharpSolutions
            : SolutionScanner.AngularSolutions);
        var layers = GraphLayering.BuildLayers(_graph);

        AllLevels.Clear();
        FilterListLibraries.Clear();

        for (int i = 0; i < layers.Count; i++)
        {
            var level = new LevelVm { Level = i };

            foreach (var id in layers[i])
            {
                var s = _graph.Nodes[id].Solution;

                var node = new LevelNodeVm
                {
                    Id = s.Id,
                    Title = s.Name,
                    SolutionType = s.Type.Value,
                    Path = s.Path
                };

                level.Nodes.Add(node);

                if (node.SolutionType == SolutionType.Library)
                    FilterListLibraries.Add(node);
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

        HashSet<Guid>? visible = libraryId is null
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