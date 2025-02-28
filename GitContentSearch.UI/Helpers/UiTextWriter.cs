using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace GitContentSearch.UI.Helpers;

public class UiTextWriter : TextWriter
{
    private readonly ObservableCollection<string> _logOutput;
    private readonly StringBuilder _currentLine = new();
    private int _lastLineIndex = -1;

    public UiTextWriter(ObservableCollection<string> logOutput)
    {
        _logOutput = logOutput;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        if (value == '\r')
        {
            // Carriage return means we're going to update the current line
            // Don't clear the buffer yet, wait for the actual content
        }
        else if (value == '\n')
        {
            if (_currentLine.Length > 0)
            {
                // Dispatch to UI thread since we're modifying an ObservableCollection
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    _logOutput.Add(_currentLine.ToString());
                    _lastLineIndex = _logOutput.Count - 1;
                    _currentLine.Clear();
                });
            }
        }
        else
        {
            // If the buffer starts with \r, we're updating the last line
            if (_currentLine.Length == 0 && _lastLineIndex >= 0 && value != '\r')
            {
                // Update the last line in the collection
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (_lastLineIndex >= 0 && _lastLineIndex < _logOutput.Count)
                    {
                        _logOutput[_lastLineIndex] = value.ToString();
                        _currentLine.Clear();
                    }
                });
            }
            else
            {
                _currentLine.Append(value);
            }
        }
    }

    public override void Write(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        // Check if the string starts with a carriage return
        if (value.StartsWith('\r') && _lastLineIndex >= 0)
        {
            // Update the last line in the collection
            string newText = value.TrimStart('\r');
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_lastLineIndex >= 0 && _lastLineIndex < _logOutput.Count)
                {
                    _logOutput[_lastLineIndex] = newText;
                }
            });
        }
        else
        {
            // Normal write, character by character
            foreach (char c in value)
            {
                Write(c);
            }
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
                _lastLineIndex = _logOutput.Count - 1;
            });
        }
    }
} 