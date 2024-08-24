using System.Text;

public class CompositeTextWriter : TextWriter
{
    private readonly List<TextWriter> _writers = new List<TextWriter>();

    public CompositeTextWriter(params TextWriter[] writers)
    {
        _writers.AddRange(writers);
    }

    public override Encoding Encoding => Encoding.Default;

    public override void Write(char value)
    {
        foreach (var writer in _writers)
        {
            writer.Write(value);
        }
    }

    public override void WriteLine(string value)
    {
        foreach (var writer in _writers)
        {
            writer.WriteLine(value);
        }
    }

    public override void Flush()
    {
        foreach (var writer in _writers)
        {
            writer.Flush();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var writer in _writers)
            {
                if (writer != Console.Out) // Do not dispose of Console.Out
                {
                    writer.Dispose();
                }
            }
        }

        base.Dispose(disposing);
    }
}
