namespace Assignment.TextSort;

public interface ISortService
{
    Task Sort(string inputFilePath, string outputFilePath = null);
}