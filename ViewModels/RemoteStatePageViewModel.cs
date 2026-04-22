using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Felweed.Extensions;
using Felweed.Models;
using Felweed.Models.Digestion;
using Felweed.Services;

namespace Felweed.ViewModels;

public partial class RemoteStatePageViewModel : ObservableObject, IAsyncDisposable
{
    [ObservableProperty] private bool _isConnecting = true;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isError;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private CobwebState? _state;
    
    [ObservableProperty]
    private ObservableCollection<Solution> _solutions = [];

    public async Task ConnectAsync()
    {
        Error = await HubConnector.InitAsync(OnFullState);

        IsConnecting = false;
        
        if (Error == null)
        {
            IsConnected = true;
        }
        else
        {
            IsError = true;
        }
    }

    private void OnFullState(byte[] data)
    {
        State = data.CobwebDecompress<CobwebState>();

        foreach (var solution in SolutionScanner.AngularSolutions)
        {
            solution.BindToCobwebProject(State.Projects.SingleOrDefault(x => x.HttpUrl == solution.GitOriginUrl));
        }
        
        foreach (var solution in SolutionScanner.CsharpSolutions)
        {
            solution.BindToCobwebProject(State.Projects.SingleOrDefault(x => x.HttpUrl == solution.GitOriginUrl));
        }
        
        List<Solution> solutions = [..SolutionScanner.CsharpSolutions, ..SolutionScanner.AngularSolutions];

        Solutions = new ObservableCollection<Solution>(solutions.OrderByDescending(x => x.Type));
    }
    
    public async ValueTask DisposeAsync()
    {
        await HubConnector.CleanupConnectionAsync();
    }
}