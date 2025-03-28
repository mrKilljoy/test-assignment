namespace Assignment.Infrastructure.Interfaces;

public interface ISortService
{
    Task Sort(string inputFilePath, string outputFilePath = null);
}