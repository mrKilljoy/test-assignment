namespace Assignment.Infrastructure.Shared;

public class CustomStringComparer : IComparer<(string, string)>
{
    public int Compare((string, string) x, (string, string) y)
    {
        // compare parts with words
        var item1Comparison = string.Compare(x.Item2, y.Item2, StringComparison.Ordinal);
        if (item1Comparison != 0) return item1Comparison;
        
        // compare starting numbers
        return string.Compare(x.Item1, y.Item1, StringComparison.Ordinal);
    }
}