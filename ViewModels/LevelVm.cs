using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Felweed.ViewModels;

public partial class LevelVm : ObservableObject
{
    [ObservableProperty] private int _level;
    [ObservableProperty] private ObservableCollection<LevelNodeVm> _nodes = [];
    
    public string Header => $"Level {Level} ({Nodes.Count})";
}