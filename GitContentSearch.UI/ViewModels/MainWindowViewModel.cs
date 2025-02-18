using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace GitContentSearch.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string filePath = string.Empty;

    [ObservableProperty]
    private string searchString = string.Empty;

    [ObservableProperty]
    private string earliestCommit = string.Empty;

    [ObservableProperty]
    private string latestCommit = string.Empty;

    [ObservableProperty]
    private string workingDirectory = string.Empty;

    [ObservableProperty]
    private string logDirectory = string.Empty;

    [ObservableProperty]
    private bool disableLinearSearch;

    [ObservableProperty]
    private bool followHistory;

    [ObservableProperty]
    private double searchProgress;

    [ObservableProperty]
    private ObservableCollection<string> logOutput = new();

    [ObservableProperty]
    private bool isSearching;

    [RelayCommand]
    private async Task BrowseFilePathAsync()
    {
        // TODO: Implement file browsing
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task BrowseWorkingDirectoryAsync()
    {
        // TODO: Implement directory browsing
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task BrowseLogDirectoryAsync()
    {
        // TODO: Implement directory browsing
        await Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanStartSearch))]
    private async Task StartSearchAsync()
    {
        IsSearching = true;
        try
        {
            // TODO: Implement search logic
            await Task.Delay(100); // Placeholder
        }
        finally
        {
            IsSearching = false;
        }
    }

    private bool CanStartSearch => 
        !string.IsNullOrWhiteSpace(FilePath) && 
        !string.IsNullOrWhiteSpace(SearchString) &&
        !IsSearching;
} 