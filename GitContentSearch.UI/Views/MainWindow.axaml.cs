using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using GitContentSearch.UI.ViewModels;

namespace GitContentSearch.UI.Views;

public partial class MainWindow : Window
{
    private TextBox? _filePathTextBox;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnFilePathTextBoxAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _filePathTextBox = sender as TextBox;
        if (_filePathTextBox != null)
        {
            _filePathTextBox.LostFocus += OnFilePathLostFocus;
        }
    }

    private void OnFilePathLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            // Only process if focus is still within our window
            var focusManager = TopLevel.GetTopLevel(this)?.FocusManager;
            var currentFocus = focusManager?.GetFocusedElement();
            
            // Check if the newly focused element is still within our window
            if (currentFocus != null && currentFocus is Visual visual && visual.GetVisualRoot() == this.GetVisualRoot())
            {
                viewModel.HandleFilePathLostFocusCommand.Execute(null);
            }
        }
    }
} 