using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using GitContentSearch.UI.ViewModels;
using GitContentSearch.UI.Views;
using GitContentSearch.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace GitContentSearch.UI;

public partial class App : Application
{
    public new static App? Current => Application.Current as App;
    
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        var services = new ServiceCollection();
        
        // Register services
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            services.AddSingleton(mainWindow.StorageProvider);
            services.AddSingleton<SettingsService>();
            services.AddTransient<MainWindowViewModel>();
            
            Services = services.BuildServiceProvider();
            
            var viewModel = Services.GetRequiredService<MainWindowViewModel>();
            mainWindow.DataContext = viewModel;

            // Handle window closing to save settings
            mainWindow.Closing += async (s, e) =>
            {
                if (mainWindow.DataContext is MainWindowViewModel vm)
                {
                    try 
                    {
                        // Only save settings and cancel if this is the first close attempt
                        if (!e.IsProgrammatic)
                        {
                            e.Cancel = true;
                            await vm.OnClosingAsync();
                            // Now trigger the close programmatically
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => mainWindow.Close());
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log any errors but still allow the window to close
                        Console.WriteLine($"Error saving settings: {ex}");
                    }
                }
            };
            
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
} 