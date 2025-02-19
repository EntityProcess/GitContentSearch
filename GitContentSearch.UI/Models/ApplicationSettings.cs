using System;

namespace GitContentSearch.UI.Models;

public record ApplicationSettings
{
    public string FilePath { get; init; } = string.Empty;
    public string SearchString { get; init; } = string.Empty;
    public string EarliestCommit { get; init; } = string.Empty;
    public string LatestCommit { get; init; } = string.Empty;
    public string WorkingDirectory { get; init; } = string.Empty;
    public string LogDirectory { get; init; } = string.Empty;
    public bool DisableLinearSearch { get; init; }
    public bool FollowHistory { get; init; }
} 