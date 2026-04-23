using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Felweed.Models;

namespace Felweed.ViewModels.MainMenu.Graph;

public partial class LevelVm : ObservableObject
{
    [ObservableProperty]
    public partial int Level { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Solution> Nodes { get; set; } = [];

    public string Header => $"Уровень {Level} ({Nodes.Count} шт.)";
}