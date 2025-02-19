using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reflection;
using Avalonia.Platform.Storage;
using GitContentSearch.UI.Models;

namespace GitContentSearch.UI.Services;

public class SettingsService
{
    private const string SETTINGS_FILE = "settings.json";
    private readonly IStorageProvider _storageProvider;
    private readonly string _settingsPath;

    public SettingsService(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
        
        // Get the directory where the application executable is located
        var appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        _settingsPath = Path.Combine(appDirectory, SETTINGS_FILE);
    }

    public async Task SaveSettingsAsync(ApplicationSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch (Exception ex)
        {
            // Log error but don't crash the application
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    public async Task<ApplicationSettings?> LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return null;

            var json = await File.ReadAllTextAsync(_settingsPath);
            return JsonSerializer.Deserialize<ApplicationSettings>(json);
        }
        catch (Exception ex)
        {
            // Log error but don't crash the application
            Console.WriteLine($"Error loading settings: {ex.Message}");
            return null;
        }
    }
} 