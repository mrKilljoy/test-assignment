namespace Assignment.Infrastructure.Interfaces;

public interface IGenerator
{
    Task CreateFile(string outputFilePath = null);
}