using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Felweed.Models;

namespace Felweed.ViewModels;

public partial class LevelVm : ObservableObject
{
    [ObservableProperty] private int _level;
    [ObservableProperty] private ObservableCollection<Solution> _nodes = [];
    
    public string Header => $"Уровень {Level} ({Nodes.Count} шт.)";
}