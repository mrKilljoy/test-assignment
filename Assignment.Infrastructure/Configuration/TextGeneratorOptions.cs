namespace Assignment.Infrastructure.Configuration;

public class TextGeneratorOptions
{
    /// <summary>
    /// The maximum number of lines in the output file.
    /// </summary>
    public long LineCount { get; set; }

    /// <summary>
    /// The maximum number of words that a line can contain (except the starting number).
    /// </summary>
    public int MaxWordsPerLine { get; set; }

    /// <summary>
    /// The upper value of the range in which numbers for every line can be generated.
    /// </summary>
    public int MaxLineNumber { get; set; }

    /// <summary>
    /// The maximum number of read lines that a line buffer can contain.
    /// </summary>
    public int MaxItems { get; set; } = 1000;

    public static readonly TextGeneratorOptions Default = new TextGeneratorOptions()
    {
        LineCount = 500_000,
        MaxWordsPerLine = 1000,
        MaxLineNumber = 1000
    };
}