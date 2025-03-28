namespace Assignment.Tests;

using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Reflection;
using Assignment.Infrastructure.Configuration;
using Assignment.Infrastructure.Services;

[TestFixture]
public class TextGeneratorTests
{
    private TextGeneratorOptions _defaultOptions;
    private TextGenerator _generator;
    private string _tempFilePath;

    [SetUp]
    public void SetUp()
    {
        // Default options for successful scenarios
        _defaultOptions = new TextGeneratorOptions
        {
            LineCount = 10,
            MaxLineNumber = 100,
            MaxWordsPerLine = 5,
            MaxItems = 100
        };
        _generator = new TextGenerator(_defaultOptions);

        // Create a temporary file path for each test
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up temporary files after each test
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Test]
    public async Task CreateFile_ValidOptions_CreatesFileWithContent()
    {
        // Act
        await _generator.CreateFile(_tempFilePath);

        // Assert
        Assert.IsTrue(File.Exists(_tempFilePath));
        var content = await File.ReadAllTextAsync(_tempFilePath);
        Assert.IsFalse(string.IsNullOrEmpty(content));

        var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.That(_defaultOptions.LineCount, Is.EqualTo(lines.Length));

        foreach (var line in lines)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.IsTrue(int.TryParse(parts[0].TrimEnd('.'), out _)); // Starts with a number
            Assert.GreaterOrEqual(parts.Length, 2); // At least one word after the number
        }
    }

    [Test]
    public async Task CreateFile_NullOutputPath_UsesDefaultPath()
    {
        // Arrange
        var defaultPath = (string)typeof(TextGenerator)
            .GetMethod("CreateDefaultOutputPath", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(_generator, null);

        if (File.Exists(defaultPath))
        {
            File.Delete(defaultPath);
        }

        // Act
        await _generator.CreateFile(null);

        // Assert
        Assert.IsTrue(File.Exists(defaultPath));
        var content = await File.ReadAllTextAsync(defaultPath);
        Assert.IsFalse(string.IsNullOrEmpty(content));
    }

    [Test]
    public async Task CreateFile_FileAlreadyExists_DoesNotOverwrite()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempFilePath, "Existing content");
        var originalContent = await File.ReadAllTextAsync(_tempFilePath);

        // Act
        await _generator.CreateFile(_tempFilePath);

        // Assert
        Assert.IsTrue(File.Exists(_tempFilePath));
        var content = await File.ReadAllTextAsync(_tempFilePath);
        Assert.That(originalContent, Is.EqualTo(content));
    }

    [Test]
    public async Task CreateFile_InvalidOptions_DoesNotCreateFile()
    {
        // Arrange
        var invalidOptions = new TextGeneratorOptions
        {
            LineCount = 0, // Invalid
            MaxLineNumber = 100,
            MaxWordsPerLine = 5,
            MaxItems = 100
        };
        var generator = new TextGenerator(invalidOptions);

        // Act
        await generator.CreateFile(_tempFilePath);

        // Assert
        Assert.IsFalse(File.Exists(_tempFilePath));
    }

    [Test]
    public void ValidateOptions_NullOptions_ReturnsFalse()
    {
        // Arrange
        var generator = new TextGenerator(null);
        var method = typeof(TextGenerator).GetMethod("ValidateOptions", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method.Invoke(generator, new object[] { null });

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public void ValidateOptions_InvalidLineCount_ReturnsFalse()
    {
        // Arrange
        var options = new TextGeneratorOptions
        {
            LineCount = 0,
            MaxLineNumber = 100,
            MaxWordsPerLine = 5,
            MaxItems = 100
        };
        var generator = new TextGenerator(options);
        var method = typeof(TextGenerator).GetMethod("ValidateOptions", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method.Invoke(generator, new object[] { options });

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public void ValidateOptions_InvalidMaxLineNumber_ReturnsFalse()
    {
        // Arrange
        var options = new TextGeneratorOptions
        {
            LineCount = 10,
            MaxLineNumber = 0,
            MaxWordsPerLine = 5,
            MaxItems = 100
        };
        var generator = new TextGenerator(options);
        var method = typeof(TextGenerator).GetMethod("ValidateOptions", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method.Invoke(generator, new object[] { options });

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public void ValidateOptions_InvalidMaxWordsPerLine_ReturnsFalse()
    {
        // Arrange
        var options = new TextGeneratorOptions
        {
            LineCount = 10,
            MaxLineNumber = 100,
            MaxWordsPerLine = 0,
            MaxItems = 100
        };
        var generator = new TextGenerator(options);
        var method = typeof(TextGenerator).GetMethod("ValidateOptions", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method.Invoke(generator, new object[] { options });

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public void ValidateOptions_InvalidMaxItems_ReturnsFalse()
    {
        // Arrange
        var options = new TextGeneratorOptions
        {
            LineCount = 10,
            MaxLineNumber = 100,
            MaxWordsPerLine = 5,
            MaxItems = 0
        };
        var generator = new TextGenerator(options);
        var method = typeof(TextGenerator).GetMethod("ValidateOptions", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method.Invoke(generator, new object[] { options });

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public void ValidateOptions_ValidOptions_ReturnsTrue()
    {
        // Arrange
        var method = typeof(TextGenerator).GetMethod("ValidateOptions", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method.Invoke(_generator, new object[] { _defaultOptions });

        // Assert
        Assert.IsTrue(result);
    }

    [Test]
    public void CreateChannelOptions_ReturnsCorrectOptions()
    {
        // Arrange
        var method = typeof(TextGenerator).GetMethod("CreateChannelOptions", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (BoundedChannelOptions)method.Invoke(_generator, null);

        // Assert
        Assert.That(_defaultOptions.MaxItems, Is.EqualTo(result.Capacity));
        Assert.That(BoundedChannelFullMode.Wait, Is.EqualTo(result.FullMode));
        Assert.IsTrue(result.SingleWriter);
        Assert.IsTrue(result.SingleReader);
    }

    [Test]
    public void CreateWordBank_ReturnsExpectedWords()
    {
        // Arrange
        var method = typeof(TextGenerator).GetMethod("CreateWordBank", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (string[])method.Invoke(_generator, null);

        // Assert
        var expectedWords = new[] { "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
        Assert.That(expectedWords.Length, Is.EqualTo(result.Length));
        CollectionAssert.AreEqual(expectedWords, result);
    }

    [Test]
    public void CreateDefaultOutputPath_ReturnsValidPath()
    {
        // Arrange
        var method = typeof(TextGenerator).GetMethod("CreateDefaultOutputPath", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (string)method.Invoke(_generator, null);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(result));
        Assert.IsTrue(result.EndsWith("generated-file.txt"));
    }

    [Test]
    public async Task PopulateQueue_GeneratesCorrectNumberOfLines()
    {
        // Act
        await _generator.CreateFile(_tempFilePath);

        // Assert
        var content = await File.ReadAllTextAsync(_tempFilePath);
        var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.That(_defaultOptions.LineCount, Is.EqualTo(lines.Length));
    }

    [Test]
    public async Task WriteLines_WritesAllLinesToFile()
    {
        // Act
        await _generator.CreateFile(_tempFilePath);

        // Assert
        var content = await File.ReadAllTextAsync(_tempFilePath);
        var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.That(_defaultOptions.LineCount, Is.EqualTo(lines.Length));
    }
}