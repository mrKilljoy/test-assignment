using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Channels;
using Assignment.Infrastructure.Configuration;
using Assignment.Infrastructure.Interfaces;

namespace Assignment.Infrastructure.Services;

public class TextGenerator : IGenerator
{
    private readonly TextGeneratorOptions _options;

    public TextGenerator(TextGeneratorOptions options)
    {
        _options = options;
    }
    
    public async Task CreateFile(string outputFilePath = null)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            if (string.IsNullOrEmpty(outputFilePath))
            {
                Console.WriteLine("Output file path is not specified, current directory is selected.");
                outputFilePath = CreateDefaultOutputPath();
            }

            if (File.Exists(outputFilePath))
            {
                Console.WriteLine("File with this name already exists.");
                return;
            }

            Console.WriteLine($"Output file path: {outputFilePath}");

            if (!ValidateOptions(_options))
                return;

            Channel<string> channel = Channel.CreateBounded<string>(CreateChannelOptions());
            string[] words = CreateWordBank();

            var populateTask = PopulateQueue(channel, words);
            var writeTask = WriteLines(channel, outputFilePath);

            await Task.WhenAll(populateTask, writeTask);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An unexpected error has occurred. See the details below.");
            Console.WriteLine(ex);
        }
        finally
        {
            sw.Stop();
            Console.WriteLine($"Total time: {sw.Elapsed.ToString()}");
        }
    }

    private BoundedChannelOptions CreateChannelOptions()
    {
        return new BoundedChannelOptions(_options.MaxItems)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = true
        };
    }

    private string[] CreateWordBank()
    {
        return new[] { "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
    }

    private bool ValidateOptions(TextGeneratorOptions options)
    {
        if (options is null)
        {
            Console.WriteLine("Configuration is not provided.");
            return false;
        }
        
        if (options.LineCount < 1)
        {
            Console.WriteLine("Line count is supposed to be greater than zero.");
            return false;
        }

        if (options.MaxLineNumber < 1)
        {
            Console.WriteLine("Line numbers are supposed to be greater than zero.");
            return false;
        }

        if (options.MaxWordsPerLine < 1)
        {
            Console.WriteLine("Line cannot contain less than single word.");
            return false;
        }
        
        if (options.MaxItems < 1)
        {
            Console.WriteLine("Intermediate storage is not initialized.");
            return false;
        }

        return true;
    }
    
    private async Task PopulateQueue(Channel<string> channel, string[] words)
    {
        try
        {
            Random rnd = new();
            long created = 0;
            StringBuilder sb = new();

            while (created < _options.LineCount)
            {
                int num = rnd.Next(2, _options.MaxWordsPerLine);    // '2' is chosen as starting value since we need at least one word per line
                for (int j = 0; j < num; j++)
                {
                    if (j == 0)
                        sb.Append($"{rnd.Next(0, _options.MaxLineNumber)}.");
                    else
                        sb.Append($" {words[rnd.Next(0, words.Length - 1)]}");

                    if (j == num - 1)
                        sb.AppendLine();
                }

                await channel.Writer.WriteAsync(sb.ToString());
                sb.Clear();

                if (created == _options.LineCount - 1)
                    break;

                created++;
            }

            channel.Writer.Complete();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }
    
    private async Task WriteLines(Channel<string> channel, string filePath)
    {
        try
        {
            using FileStream fs = File.Create(filePath);
            using BufferedStream bs = new BufferedStream(fs);
            using var swr = new StreamWriter(bs);

            while (await channel.Reader.WaitToReadAsync())
            {
                var item = await channel.Reader.ReadAsync();
                if (item is null)
                    break;

                await swr.WriteAsync(item);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    private string CreateDefaultOutputPath()
    {
        var folderPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
        return Path.Combine(folderPath, "generated-file.txt");
    }
}