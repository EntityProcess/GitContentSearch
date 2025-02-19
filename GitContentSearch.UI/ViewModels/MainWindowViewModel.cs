using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitContentSearch.Helpers;
using GitContentSearch.UI.Helpers;
using GitContentSearch.UI.Models;
using GitContentSearch.UI.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace GitContentSearch.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IStorageProvider _storageProvider;
    private readonly SettingsService _settingsService;
    private IGitHelper? _gitHelper;
    private IFileSearcher? _fileSearcher;
    private IFileManager? _fileManager;

    public MainWindowViewModel(IStorageProvider storageProvider, SettingsService settingsService)
    {
        _storageProvider = storageProvider;
        _settingsService = settingsService;
        LoadSettingsAsync().ConfigureAwait(false);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSearchCommand))]
    private string filePath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSearchCommand))]
    private string searchString = string.Empty;

    [ObservableProperty]
    private string earliestCommit = string.Empty;

    [ObservableProperty]
    private string latestCommit = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSearchCommand))]
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
    [NotifyCanExecuteChangedFor(nameof(StartSearchCommand))]
    private bool isSearching;

    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync();
        if (settings != null)
        {
            FilePath = settings.FilePath;
            SearchString = settings.SearchString;
            EarliestCommit = settings.EarliestCommit;
            LatestCommit = settings.LatestCommit;
            WorkingDirectory = settings.WorkingDirectory;
            LogDirectory = settings.LogDirectory;
            DisableLinearSearch = settings.DisableLinearSearch;
            FollowHistory = settings.FollowHistory;
        }
        // If settings is null, we'll keep the default empty string values initialized in the properties
    }

    public async Task SaveSettingsAsync()
    {
        var settings = new ApplicationSettings
        {
            FilePath = FilePath,
            SearchString = SearchString,
            EarliestCommit = EarliestCommit,
            LatestCommit = LatestCommit,
            WorkingDirectory = WorkingDirectory,
            LogDirectory = LogDirectory,
            DisableLinearSearch = DisableLinearSearch,
            FollowHistory = FollowHistory
        };

        await _settingsService.SaveSettingsAsync(settings);
    }

    // Add this method to save settings when the window is closing
    public void OnClosing()
    {
        SaveSettingsAsync().Wait();
    }

    [RelayCommand]
    private async Task BrowseFilePathAsync()
    {
        var file = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select File to Search",
            AllowMultiple = false
        });

        if (file.Count > 0)
        {
            var selectedFile = file[0];
            var processWrapper = new ProcessWrapper();
            var result = processWrapper.Start("rev-parse --show-toplevel", Path.GetDirectoryName(selectedFile.Path.LocalPath), null);
            
            if (result.ExitCode == 0)
            {
                var gitRoot = result.StandardOutput.Trim().Replace('/', '\\'); // Normalize to Windows path
                var filePath = selectedFile.Path.LocalPath;
                
                LogOutput.Add($"Git Root: {gitRoot}");
                LogOutput.Add($"File Path: {filePath}");
                
                WorkingDirectory = gitRoot;
                
                // Convert paths to the same format and case for comparison
                if (filePath.StartsWith(gitRoot, StringComparison.OrdinalIgnoreCase))
                {
                    var relativePath = filePath[gitRoot.Length..].TrimStart('\\', '/');
                    FilePath = relativePath.Replace('\\', '/');
                    LogOutput.Add($"Relative Path: {FilePath}");
                }
                else
                {
                    LogOutput.Add("Warning: File path does not start with git root path.");
                    FilePath = string.Empty;
                }
            }
            else
            {
                LogOutput.Add("Warning: Selected file is not in a Git repository. Please select a file within a Git repository.");
                FilePath = string.Empty;
                WorkingDirectory = string.Empty;
            }
        }
    }

    [RelayCommand]
    private async Task BrowseWorkingDirectoryAsync()
    {
        var folder = await _storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Git Repository Directory"
        });

        if (folder.Count > 0)
        {
            WorkingDirectory = folder[0].Path.LocalPath;
            // Verify it's a git repository
            if (!Directory.Exists(Path.Combine(WorkingDirectory, ".git")))
            {
                LogOutput.Add("Warning: Selected directory is not a Git repository.");
                WorkingDirectory = string.Empty;
            }
        }
    }

    [RelayCommand]
    private async Task BrowseLogDirectoryAsync()
    {
        var folder = await _storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Log Directory"
        });

        if (folder.Count > 0)
        {
            LogDirectory = folder[0].Path.LocalPath;
        }
    }

    private bool CanStartSearch => 
        !string.IsNullOrEmpty(FilePath) && 
        !string.IsNullOrEmpty(SearchString) && 
        !string.IsNullOrEmpty(WorkingDirectory) &&
        !IsSearching;

    [RelayCommand(CanExecute = nameof(CanStartSearch))]
    private async Task StartSearchAsync()
    {
        IsSearching = true;
        LogOutput.Clear();
        try
        {
            var processWrapper = new ProcessWrapper();
            _gitHelper = new GitHelper(processWrapper, WorkingDirectory, FollowHistory);
            _fileSearcher = new FileSearcher();
            
            string logAndTempFileDirectory = LogDirectory;
            if (string.IsNullOrEmpty(logAndTempFileDirectory))
            {
                logAndTempFileDirectory = Path.Combine(Path.GetTempPath(), "GitContentSearch");
                Directory.CreateDirectory(logAndTempFileDirectory);
            }

            _fileManager = new FileManager(logAndTempFileDirectory);

            var uiTextWriter = new UiTextWriter(LogOutput);
            var logFile = Path.Combine(logAndTempFileDirectory, "search_log.txt");
            var fileWriter = new StreamWriter(logFile, append: true);

            using var writer = new CompositeTextWriter(uiTextWriter, fileWriter);
            
            writer.WriteLine(new string('=', 50));
            writer.WriteLine($"GitContentSearch started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Working Directory (Git Repo): {WorkingDirectory}");
            writer.WriteLine($"Logs and temporary files will be created in: {logAndTempFileDirectory}");
            writer.WriteLine(new string('=', 50));

            var gitContentSearcher = new GitContentSearcher(_gitHelper, _fileSearcher, _fileManager, DisableLinearSearch, writer);
            
            // Since we've already validated in CanStartSearch that FilePath and SearchString are non-empty,
            // and EarliestCommit and LatestCommit have default empty string values, we can safely pass them
            await Task.Run(() => gitContentSearcher.SearchContent(
                FilePath,
                SearchString,
                EarliestCommit,
                LatestCommit));

            writer.WriteLine($"GitContentSearch completed at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine(new string('=', 50));
        }
        catch (Exception ex)
        {
            LogOutput.Add($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                LogOutput.Add($"Inner Error: {ex.InnerException.Message}");
            }
        }
        finally
        {
            IsSearching = false;
        }
    }
} 