namespace Assignment.TextSort;

public class TextSortOptions
{
    /// <summary>
    /// The maximum number of lines per chunk that can be created during text sorting.
    /// </summary>
    public long ChunkSize { get; set; } = 1000;

    public static readonly TextSortOptions Default = new();
}