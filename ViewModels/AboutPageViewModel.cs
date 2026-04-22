using System.Net.Http;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Felweed.Models;
using Felweed.Services;
using Serilog;

namespace Felweed.ViewModels;

public partial class AboutPageViewModel : ObservableObject
{
    [ObservableProperty] private string? _anecdote;
    [ObservableProperty] private string? _anecdoteSponsorText;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasAnecdoteSponsor;

    private const string BadAnecdotePlaceholder = "А, нет, не рассказали анекдот :(";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HttpClient Client = new();
    
    public async Task GetAnecdoteAsync()
    {
        IsLoading = true;

        try
        {
            var config = ConfigurationService.LoadConfig();
            var anecdoteUrl = UrlHelper.GetSafeUrl(config.AnecdoteUrl);
            if (anecdoteUrl is null)
            {
                Anecdote = BadAnecdotePlaceholder;
                HasAnecdoteSponsor = false;
                return;
            }

            HasAnecdoteSponsor = true;
            AnecdoteSponsorText = $"Спонсор анекдотов: {anecdoteUrl.GetLeftPart(UriPartial.Authority)}";
            
            var json = await Client.GetStringAsync(anecdoteUrl);
            var anecdotes = JsonSerializer.Deserialize<Anecdote[]>(json, Options);

            Anecdote = anecdotes?[0].Content ?? BadAnecdotePlaceholder;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get anecdote");
            Anecdote = BadAnecdotePlaceholder;
        }
        finally
        {
            IsLoading = false;
        }
    }
}