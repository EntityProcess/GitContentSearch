using System;

namespace GitContentSearch.UI.Models;

public record ApplicationSettings
{
    public string FilePath { get; init; } = string.Empty;
    public string SearchString { get; init; } = string.Empty;
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public string WorkingDirectory { get; init; } = string.Empty;
    public string LogDirectory { get; init; } = string.Empty;
    public bool FollowHistory { get; init; }
} 