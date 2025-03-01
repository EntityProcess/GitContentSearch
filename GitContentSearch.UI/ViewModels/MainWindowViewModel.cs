using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitContentSearch.Helpers;
using GitContentSearch.Interfaces;
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
        _logOutput.CollectionChanged += LogOutput_CollectionChanged;
        LoadSettingsAsync().ConfigureAwait(false);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSearchCommand))]
	[NotifyCanExecuteChangedFor(nameof(LocateFileCommand))]
	private string filePath = string.Empty;

    [RelayCommand]
    private async Task HandleFilePathLostFocusAsync()
    {
        if (string.IsNullOrWhiteSpace(FilePath) || string.IsNullOrWhiteSpace(WorkingDirectory))
            return;

        try
        {
            var processWrapper = new ProcessWrapper();
            // Trim leading slash if present, as it's meant to be relative to the git repository
            var normalizedPath = FilePath.TrimStart('/');
            var absolutePath = Path.IsPathRooted(normalizedPath) 
                ? normalizedPath 
                : Path.GetFullPath(Path.Combine(WorkingDirectory, normalizedPath));

            var directoryPath = Path.GetDirectoryName(absolutePath);
            if (string.IsNullOrEmpty(directoryPath))
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LogOutput.Add($"Error: Invalid file path '{absolutePath}'.");
                });
                return;
            }

            // First check if we already have a valid git root in WorkingDirectory
            var gitRootResult = await Task.Run(() => 
                processWrapper.Start("rev-parse --show-toplevel", WorkingDirectory, null));
            
            if (gitRootResult.ExitCode == 0)
            {
                var gitRoot = gitRootResult.StandardOutput.Trim().Replace('/', '\\'); // Normalize to Windows path
                
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LogOutput.Add($"Git Root: {gitRoot}");
                    LogOutput.Add($"File Path: {absolutePath}");
                    
                    // Keep the existing working directory since it's a valid git root
                    
                    // Convert paths to the same format and case for comparison
                    if (absolutePath.StartsWith(gitRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        var relativePath = absolutePath[gitRoot.Length..].TrimStart('\\', '/');
                        FilePath = relativePath.Replace('\\', '/');
                        LogOutput.Add($"Relative Path: {FilePath}");
                    }
                    else
                    {
                        // If the path doesn't start with git root, it might be a remote path
                        // Just keep the normalized path without clearing anything
                        FilePath = normalizedPath.Replace('\\', '/');
                        LogOutput.Add($"Using path as provided: {FilePath}");
                    }
                });
            }
            else
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LogOutput.Add("Warning: Current working directory is not a Git repository. Please select a valid Git repository directory.");
                    WorkingDirectory = string.Empty;
                });
            }
        }
        catch (Exception ex)
        {
            LogOutput.Add($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                LogOutput.Add($"Inner Error: {ex.InnerException.Message}");
            }
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSearchCommand))]
    private string searchString = string.Empty;

    [ObservableProperty]
    private string earliestCommit = string.Empty;

    [ObservableProperty]
    private string latestCommit = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSearchCommand))]
	[NotifyCanExecuteChangedFor(nameof(LocateFileCommand))]
	private string workingDirectory = string.Empty;

    [ObservableProperty]
    private string logDirectory = string.Empty;

    [ObservableProperty]
    private bool followHistory;

    [ObservableProperty]
    private double searchProgress;

    [ObservableProperty]
    private bool showProgress;

    private ObservableCollection<string> _logOutput = new();
    public ObservableCollection<string> LogOutput
    {
        get => _logOutput;
        set
        {
            if (_logOutput != null)
            {
                _logOutput.CollectionChanged -= LogOutput_CollectionChanged;
            }
            _logOutput = value;
            if (_logOutput != null)
            {
                _logOutput.CollectionChanged += LogOutput_CollectionChanged;
            }
            OnPropertyChanged();
            UpdateJoinedLogOutput();
        }
    }

    private void LogOutput_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateJoinedLogOutput();
    }

    private void UpdateJoinedLogOutput()
    {
        JoinedLogOutput = string.Join(Environment.NewLine, LogOutput);
    }

    [ObservableProperty]
    private string joinedLogOutput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSearchCommand))]
    private bool isSearching;

    [ObservableProperty]
    private bool isLocateOperation;

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
            FollowHistory = FollowHistory
        };

        await _settingsService.SaveSettingsAsync(settings);
    }

    // Add this method to save settings when the window is closing
    public async Task OnClosingAsync()
    {
        await SaveSettingsAsync();
    }

    // Keep a sync version that uses a different dispatcher to avoid deadlocks
    public void OnClosing()
    {
        // Use Task.Run to avoid deadlocks by running on a different thread
        Task.Run(async () => await SaveSettingsAsync()).Wait();
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

    private bool CanLocateFile =>
        !string.IsNullOrEmpty(FilePath) &&
        !string.IsNullOrEmpty(WorkingDirectory) &&
        !IsSearching;

    [RelayCommand(CanExecute = nameof(CanLocateFile))]
    private async Task LocateFileAsync()
    {
        IsSearching = true;
        IsLocateOperation = true;
        ShowProgress = true;
        SearchProgress = 0;
        LogOutput.Clear();
        StreamWriter? fileWriter = null;
        try
        {
            if (!Directory.Exists(WorkingDirectory))
            {
                LogOutput.Add($"Error: Working directory '{WorkingDirectory}' does not exist or is invalid.");
                ShowProgress = false;
                return;
            }

            var processWrapper = new ProcessWrapper();
            string logAndTempFileDirectory = LogDirectory;
            if (string.IsNullOrEmpty(logAndTempFileDirectory))
            {
                logAndTempFileDirectory = Path.Combine(Path.GetTempPath(), "GitContentSearch");
                Directory.CreateDirectory(logAndTempFileDirectory);
            }

            var uiTextWriter = new UiTextWriter(LogOutput);
            var logFile = Path.Combine(logAndTempFileDirectory, "search_log.txt");
            fileWriter = new StreamWriter(logFile, append: true);
            var writer = new CompositeTextWriter(uiTextWriter, fileWriter);
            var searchLogger = new SearchLogger(writer, progressMessage => 
            {
                // Ensure UI updates happen on the UI thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    // Replace the last line if it was a progress message
                    if (LogOutput.Count > 0 && LogOutput[LogOutput.Count - 1].StartsWith("Processing commits:"))
                    {
                        LogOutput[LogOutput.Count - 1] = progressMessage;
                    }
                    else
                    {
                        LogOutput.Add(progressMessage);
                    }
                });
            });

            _gitHelper = new GitHelper(processWrapper, WorkingDirectory, FollowHistory, searchLogger);
            var gitLocator = new GitFileLocator(_gitHelper, searchLogger, processWrapper);

            writer.WriteLine(new string('=', 50));
            writer.WriteLine($"GitContentSearch locate started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Working Directory (Git Repo): {WorkingDirectory}");
            writer.WriteLine($"File to locate: {FilePath}");
            writer.WriteLine(new string('=', 50));

            var (commitHash, foundPath) = await Task.Run(() => 
            {
                var progress = new Progress<double>(value =>
                {
                    // Ensure UI updates happen on the UI thread
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        SearchProgress = value * 100;
                    });
                });

                return gitLocator.LocateFile(FilePath, progress);
            });

            // Update the FilePath with the found path if one was found
            if (!string.IsNullOrEmpty(foundPath))
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    FilePath = foundPath;
                });
            }
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
            if (fileWriter != null)
            {
                fileWriter.Flush();
                fileWriter.Dispose();
            }
            IsSearching = false;
            // Don't reset IsLocateOperation here - let it persist until next operation
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartSearch))]
    private async Task StartSearchAsync()
    {
        IsSearching = true;
        IsLocateOperation = false; // Reset the locate operation state when starting a search
        ShowProgress = true;
        SearchProgress = 0;
        LogOutput.Clear();
        StreamWriter? fileWriter = null;
        try
        {
            if (!Directory.Exists(WorkingDirectory))
            {
                LogOutput.Add($"Error: Working directory '{WorkingDirectory}' does not exist or is invalid.");
                ShowProgress = false;
                return;
            }

            var processWrapper = new ProcessWrapper();
            string logAndTempFileDirectory = LogDirectory;
            if (string.IsNullOrEmpty(logAndTempFileDirectory))
            {
                logAndTempFileDirectory = Path.Combine(Path.GetTempPath(), "GitContentSearch");
                Directory.CreateDirectory(logAndTempFileDirectory);
            }

            var uiTextWriter = new UiTextWriter(LogOutput);
            var logFile = Path.Combine(logAndTempFileDirectory, "search_log.txt");
            fileWriter = new StreamWriter(logFile, append: true);
            var writer = new CompositeTextWriter(uiTextWriter, fileWriter);
            var searchLogger = new SearchLogger(writer, progressMessage => 
            {
                // Ensure UI updates happen on the UI thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    // Replace the last line if it was a progress message
                    if (LogOutput.Count > 0 && LogOutput[LogOutput.Count - 1].StartsWith("Processing commits:"))
                    {
                        LogOutput[LogOutput.Count - 1] = progressMessage;
                    }
                    else
                    {
                        LogOutput.Add(progressMessage);
                    }
                });
            });

            _gitHelper = new GitHelper(processWrapper, WorkingDirectory, FollowHistory, searchLogger);
            _fileSearcher = new FileSearcher();
            _fileManager = new FileManager(logAndTempFileDirectory);
            
            writer.WriteLine(new string('=', 50));
            writer.WriteLine($"GitContentSearch started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Working Directory (Git Repo): {WorkingDirectory}");
            writer.WriteLine($"Logs and temporary files will be created in: {logAndTempFileDirectory}");
            writer.WriteLine(new string('=', 50));

            var gitContentSearcher = new GitContentSearcher(_gitHelper, _fileSearcher, _fileManager, searchLogger);
            
            var progress = new Progress<double>(value =>
            {
                // Ensure UI updates happen on the UI thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    SearchProgress = value * 100;
                });
            });

            // Since we've already validated in CanStartSearch that FilePath and SearchString are non-empty,
            // and EarliestCommit and LatestCommit have default empty string values, we can safely pass them
            await Task.Run(() => gitContentSearcher.SearchContent(
                FilePath,
                SearchString,
                EarliestCommit,
                LatestCommit,
                progress));

            writer.WriteLine($"GitContentSearch completed at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine(new string('=', 50));
            
            // Ensure we flush and dispose the writer
            writer.Flush();
        }
        catch (Exception ex)
        {
            LogOutput.Add($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                LogOutput.Add($"Inner Error: {ex.InnerException.Message}");
            }
            SearchProgress = 0;
            ShowProgress = false;
        }
        finally
        {
            if (fileWriter != null)
            {
                fileWriter.Flush();
                fileWriter.Dispose();
            }
            IsSearching = false;
            // Don't reset IsLocateOperation here
        }
    }
} 