namespace Assignment.TextGenerator;

public interface IGenerator
{
    Task CreateFile(string outputFilePath = null);
}