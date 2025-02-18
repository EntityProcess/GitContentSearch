using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitContentSearch.Helpers;
using GitContentSearch.UI.Helpers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace GitContentSearch.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IStorageProvider _storageProvider;
    private IGitHelper? _gitHelper;
    private IFileSearcher? _fileSearcher;
    private IFileManager? _fileManager;

    public MainWindowViewModel(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
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
            // Convert local path to git path by removing working directory prefix
            if (!string.IsNullOrEmpty(WorkingDirectory) && selectedFile.Path.LocalPath.StartsWith(WorkingDirectory))
            {
                var relativePath = selectedFile.Path.LocalPath[WorkingDirectory.Length..].TrimStart('\\', '/');
                FilePath = relativePath.Replace('\\', '/');
            }
            else
            {
                LogOutput.Add("Warning: Selected file is not in the working directory. Please select a file within the Git repository.");
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
            await Task.Run(() => gitContentSearcher.SearchContent(FilePath, SearchString, EarliestCommit, LatestCommit));

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