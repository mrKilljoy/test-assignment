using System.Reflection;
using Assignment.Infrastructure.Configuration;
using Assignment.Infrastructure.Services;

namespace Assignment.Tests;

[TestFixture]
public class TextSortServiceTests
{
    private TextSortOptions _defaultOptions;
    private TextSortService _service;
    private string _inputFilePath;
    private string _outputFilePath;

    [SetUp]
    public void SetUp()
    {
        // Default options for successful scenarios
        _defaultOptions = new TextSortOptions
        {
            ChunkSize = 2 // Small chunk size for testing
        };
        _service = new TextSortService(_defaultOptions);

        // Create temporary file paths for each test
        _inputFilePath = Path.Combine(Path.GetTempPath(), $"input_{Guid.NewGuid()}.txt");
        _outputFilePath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid()}.txt");
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up temporary files after each test
        if (File.Exists(_inputFilePath))
        {
            File.Delete(_inputFilePath);
        }
        if (File.Exists(_outputFilePath))
        {
            File.Delete(_outputFilePath);
        }
    }

    private void CreateInputFile(string[] lines)
    {
        File.WriteAllLines(_inputFilePath, lines);
    }

    [Test]
    public async Task Sort_ValidInputFile_SortsAndCreatesOutputFile()
    {
        // Arrange
        var inputLines = new[]
        {
            "3. Three Four",
            "1. One Two",
            "2. Two Three"
        };
        CreateInputFile(inputLines);

        // Act
        await _service.Sort(_inputFilePath, _outputFilePath);

        // Assert
        Assert.IsTrue(File.Exists(_outputFilePath));
        var outputLines = await File.ReadAllLinesAsync(_outputFilePath);
        Assert.That(outputLines.Length, Is.EqualTo(3));
        Assert.That(outputLines[0], Is.EqualTo("1. One Two"));
        Assert.That(outputLines[1], Is.EqualTo("2. Two Three"));
        Assert.That(outputLines[2], Is.EqualTo("3. Three Four"));
    }

    [Test]
    public async Task Sort_NullOutputPath_UsesDefaultOutputPath()
    {
        // Arrange
        var inputLines = new[] { "1. One Two" };
        CreateInputFile(inputLines);
        var defaultOutputPath = (string)typeof(TextSortService)
            .GetMethod("GenerateDefaultFileName", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(_service, new object[] { _inputFilePath });
        if (File.Exists(defaultOutputPath))
        {
            File.Delete(defaultOutputPath);
        }

        // Act
        await _service.Sort(_inputFilePath, null);

        // Assert
        Assert.IsTrue(File.Exists(defaultOutputPath));
        var outputLines = await File.ReadAllLinesAsync(defaultOutputPath);
        Assert.That(outputLines.Length, Is.EqualTo(1));
        Assert.That(outputLines[0], Is.EqualTo("1. One Two"));
    }

    [Test]
    public async Task Sort_OutputFileExists_DoesNotOverwrite()
    {
        // Arrange
        var inputLines = new[] { "1. One Two" };
        CreateInputFile(inputLines);
        await File.WriteAllTextAsync(_outputFilePath, "Existing content");
        var originalContent = await File.ReadAllTextAsync(_outputFilePath);

        // Act
        await _service.Sort(_inputFilePath, _outputFilePath);

        // Assert
        Assert.IsTrue(File.Exists(_outputFilePath));
        var content = await File.ReadAllTextAsync(_outputFilePath);
        Assert.That(content, Is.EqualTo(originalContent));
    }

    [Test]
    public async Task Sort_NullInputPath_DoesNotCreateOutput()
    {
        // Act
        await _service.Sort(null, _outputFilePath);

        // Assert
        Assert.IsFalse(File.Exists(_outputFilePath));
    }

    [Test]
    public void ValidateInput_NullInputPath_ReturnsFalse()
    {
        // Arrange
        var method = typeof(TextSortService).GetMethod("ValidateInput", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method.Invoke(_service, new object[] { null, _outputFilePath });

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public void ValidateInput_OutputFileExists_ReturnsFalse()
    {
        // Arrange
        CreateInputFile(new[] { "1. One Two" });
        File.WriteAllText(_outputFilePath, "Existing content");
        var method = typeof(TextSortService).GetMethod("ValidateInput", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method.Invoke(_service, new object[] { _inputFilePath, _outputFilePath });

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public void ValidateInput_ValidInput_ReturnsTrue()
    {
        // Arrange
        CreateInputFile(new[] { "1. One Two" });
        var method = typeof(TextSortService).GetMethod("ValidateInput", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method.Invoke(_service, new object[] { _inputFilePath, _outputFilePath });

        // Assert
        Assert.IsTrue(result);
    }

    [Test]
    public async Task PartitionInput_ValidInput_CreatesSortedChunks()
    {
        // Arrange
        var inputLines = new[]
        {
            "3. Three Four",
            "1. One Two",
            "2. Two Three"
        };
        CreateInputFile(inputLines);
        var method = typeof(TextSortService).GetMethod("PartitionInput", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var tempFiles = (string[])await (Task<string[]>)method.Invoke(_service, new object[] { _inputFilePath, _defaultOptions.ChunkSize });

        // Assert
        Assert.That(tempFiles.Length, Is.EqualTo(2)); // 3 lines, chunk size 2 -> 2 chunks
        foreach (var tempFile in tempFiles)
        {
            Assert.IsTrue(File.Exists(tempFile));
            var lines = await File.ReadAllLinesAsync(tempFile);
            Assert.IsTrue(lines.Length > 0);
            
            for (int i = 1; i < lines.Length; i++)
            {
                Assert.LessOrEqual(lines[i - 1], lines[i]);
            }
        }

        // Cleanup
        foreach (var tempFile in tempFiles)
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task PartitionInput_EmptyFile_ReturnsEmptyArray()
    {
        // Arrange
        CreateInputFile(new string[] { });
        var method = typeof(TextSortService).GetMethod("PartitionInput", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var tempFiles = (string[])await (Task<string[]>)method.Invoke(_service, new object[] { _inputFilePath, _defaultOptions.ChunkSize });

        // Assert
        Assert.IsEmpty(tempFiles);
    }

    [Test]
    public async Task MergeSortedFragments_MultipleChunks_MergesIntoOutputFile()
    {
        // Arrange
        var chunk1 = Path.GetTempFileName();
        var chunk2 = Path.GetTempFileName();
        File.WriteAllLines(chunk1, new[] { "1. One Two", "2. Two Three" });
        File.WriteAllLines(chunk2, new[] { "3. Three Four", "4. Four Five" });
        var tempFiles = new[] { chunk1, chunk2 };
        var method = typeof(TextSortService).GetMethod("MergeSortedFragments", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var filesToRemove = (string[])await (Task<string[]>)method.Invoke(_service, new object[] { tempFiles, _outputFilePath });

        // Assert
        Assert.IsTrue(File.Exists(_outputFilePath));
        var outputLines = await File.ReadAllLinesAsync(_outputFilePath);
        Assert.That(outputLines.Length, Is.EqualTo(4));
        Assert.That(outputLines[0], Is.EqualTo("1. One Two"));
        Assert.That(outputLines[1], Is.EqualTo("2. Two Three"));
        Assert.That(outputLines[2], Is.EqualTo("3. Three Four"));
        Assert.That(outputLines[3], Is.EqualTo("4. Four Five"));

        // Cleanup
        foreach (var file in filesToRemove)
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    [Test]
    public async Task MergeSortedFragments_SingleChunk_MovesToOutput()
    {
        // Arrange
        var chunk = Path.GetTempFileName();
        File.WriteAllLines(chunk, new[] { "1. One Two" });
        var tempFiles = new[] { chunk };
        var method = typeof(TextSortService).GetMethod("MergeSortedFragments", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var filesToRemove = (string[])await (Task<string[]>)method.Invoke(_service, new object[] { tempFiles, _outputFilePath });

        // Assert
        Assert.IsTrue(File.Exists(_outputFilePath));
        var outputLines = await File.ReadAllLinesAsync(_outputFilePath);
        Assert.That(outputLines.Length, Is.EqualTo(1));
        Assert.That(outputLines[0], Is.EqualTo("1. One Two"));
        Assert.That(filesToRemove.Length, Is.EqualTo(1));

        // Cleanup
        foreach (var file in filesToRemove)
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    [Test]
    public async Task MergeSortedFragments_NullInput_ReturnsEmptyArray()
    {
        // Arrange
        var method = typeof(TextSortService).GetMethod("MergeSortedFragments", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var filesToRemove = (string[])await (Task<string[]>)method.Invoke(_service, new object[] { null, _outputFilePath });

        // Assert
        Assert.IsEmpty(filesToRemove);
        Assert.IsFalse(File.Exists(_outputFilePath));
    }

    [Test]
    public void MergeFiles_TwoSortedFiles_MergesCorrectly()
    {
        // Arrange
        var file1 = Path.GetTempFileName();
        var file2 = Path.GetTempFileName();
        File.WriteAllLines(file1, new[] { "1. One Two", "3. Three Four" });
        File.WriteAllLines(file2, new[] { "2. Two Three", "4. Four Five" });
        var method = typeof(TextSortService).GetMethod("MergeFiles", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var mergedFile = (string)method.Invoke(_service, new object[] { file1, file2 });

        // Assert
        Assert.IsTrue(File.Exists(mergedFile));
        var outputLines = File.ReadAllLines(mergedFile);
        Assert.That(outputLines.Length, Is.EqualTo(4));
        Assert.That(outputLines[0], Is.EqualTo("1. One Two"));
        Assert.That(outputLines[1], Is.EqualTo("2. Two Three"));
        Assert.That(outputLines[2], Is.EqualTo("3. Three Four"));
        Assert.That(outputLines[3], Is.EqualTo("4. Four Five"));

        // Cleanup
        File.Delete(file1);
        File.Delete(file2);
        File.Delete(mergedFile);
    }

    [Test]
    public void MergeFiles_OneEmptyFile_MergesRemainingLines()
    {
        // Arrange
        var file1 = Path.GetTempFileName();
        var file2 = Path.GetTempFileName();
        File.WriteAllLines(file1, new[] { "1. One Two" });
        File.WriteAllLines(file2, new string[] { });
        var method = typeof(TextSortService).GetMethod("MergeFiles", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var mergedFile = (string)method.Invoke(_service, new object[] { file1, file2 });

        // Assert
        Assert.IsTrue(File.Exists(mergedFile));
        var outputLines = File.ReadAllLines(mergedFile);
        Assert.That(outputLines.Length, Is.EqualTo(1));
        Assert.That(outputLines[0], Is.EqualTo("1. One Two"));

        // Cleanup
        File.Delete(file1);
        File.Delete(file2);
        File.Delete(mergedFile);
    }

    [Test]
    public void Cleanup_ValidFiles_RemovesFiles()
    {
        // Arrange
        var file1 = Path.GetTempFileName();
        var file2 = Path.GetTempFileName();
        var filesToRemove = new[] { file1, file2 };
        var method = typeof(TextSortService).GetMethod("Cleanup", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        method.Invoke(_service, new object[] { filesToRemove });

        // Assert
        Assert.IsFalse(File.Exists(file1));
        Assert.IsFalse(File.Exists(file2));
    }

    [Test]
    public void GenerateDefaultFileName_ReturnsCorrectPath()
    {
        // Arrange
        var method = typeof(TextSortService).GetMethod("GenerateDefaultFileName", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (string)method.Invoke(_service, new object[] { _inputFilePath });

        // Assert
        var expectedName = Path.GetFileNameWithoutExtension(_inputFilePath) + "-sorted.txt";
        var expectedPath = Path.Combine(Path.GetDirectoryName(_inputFilePath), expectedName);
        Assert.That(result, Is.EqualTo(expectedPath));
    }
}