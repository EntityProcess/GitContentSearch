using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace GitContentSearch.UI.Helpers;

public class UiTextWriter : TextWriter
{
    private readonly ObservableCollection<string> _logOutput;
    private readonly StringBuilder _currentLine = new();

    public UiTextWriter(ObservableCollection<string> logOutput)
    {
        _logOutput = logOutput;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        if (value == '\n')
        {
            if (_currentLine.Length > 0)
            {
                // Dispatch to UI thread since we're modifying an ObservableCollection
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    _logOutput.Add(_currentLine.ToString());
                    _currentLine.Clear();
                });
            }
        }
        else if (value != '\r') // Skip carriage returns
        {
            _currentLine.Append(value);
        }
    }

    public override void WriteLine(string? value)
    {
        if (value != null)
        {
            // Dispatch to UI thread since we're modifying an ObservableCollection
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _logOutput.Add(value);
            });
        }
    }
} 