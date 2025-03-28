using System.Collections.Concurrent;
using System.Diagnostics;
using Assignment.Infrastructure.Configuration;
using Assignment.Infrastructure.Interfaces;
using Assignment.Infrastructure.Shared;

namespace Assignment.Infrastructure.Services;

public class TextSortService : ISortService
{
    private readonly TextSortOptions _options;

    public TextSortService(TextSortOptions options)
    {
        _options = options;
    }
    
    private static readonly CustomStringComparer ItemComparer = new();
    
    public async Task Sort(string inputFilePath, string outputFilePath = null)
    {
        if (!ValidateInput(inputFilePath, outputFilePath))
            return;

        if (string.IsNullOrEmpty(outputFilePath))
        {
            Console.WriteLine("Output file path not specified, default one is selected.");
            outputFilePath = GenerateDefaultFileName(inputFilePath);
        }
        
        try
        {
            var sortedChunks = await PartitionInput(inputFilePath, _options.ChunkSize);
            var filesToRemove = await MergeSortedFragments(sortedChunks, outputFilePath);
            Cleanup(filesToRemove);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An unexpected error has occurred. See the details below.");
            Console.WriteLine(ex);
        }
    }

    private bool ValidateInput(string inputFilePath, string outputFilePath = null)
    {
        if (string.IsNullOrEmpty(inputFilePath))
        {
            Console.WriteLine("Invalid input file path.");
            return false;
        }

        if (!File.Exists(inputFilePath))
        {
            Console.WriteLine("Input file not found.");
            return false;
        }
        
        if (File.Exists(outputFilePath))
        {
            Console.WriteLine("Output file with this name already exists.");
            return false;
        }

        return true;
    }
    
    private async Task<string[]> PartitionInput(string inputFile, long chunkSize)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            List<Task<string>> sortingTasks = new List<Task<string>>();

            if (!File.Exists(inputFile))
                return [];

            using (var reader = new StreamReader(inputFile))
            {
                while (!reader.EndOfStream)
                {
                    List<(string, string)> lines = new List<(string, string)>();
                    for (int i = 0; i < chunkSize && !reader.EndOfStream; i++)
                    {
                        var item = reader.ReadLine();   // for some reason, async version may produce weird results, so I chose this one
                        if (string.IsNullOrEmpty(item))
                            break;
                        var parts = item.Split(' ', 2);
                        lines.Add((parts[0], parts[1]));
                    }
                    
                    Task<string> createSortedChunk = Task.Run(async () =>
                    {
                        string tempFile = default;
                        try
                        {
                            lines.Sort(ItemComparer);
                            tempFile = Path.GetTempFileName();
                            await File.WriteAllLinesAsync(tempFile, lines.Select(x => $"{x.Item1} {x.Item2}"));
                            return tempFile;
                        }
                        catch
                        {
                            if (!string.IsNullOrEmpty(tempFile) && File.Exists(tempFile))
                                File.Delete(tempFile);
                            throw;
                        }
                    });

                    sortingTasks.Add(createSortedChunk);
                }
            }
            
            var items = await Task.WhenAll(sortingTasks);
            return items;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
        finally
        {
            sw.Stop();
            Console.WriteLine($"Input partitioning: {sw.Elapsed.ToString()}");
        }
    }
    
    private async Task<string[]> MergeSortedFragments(string[] tempFiles, string outputFile)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            if (tempFiles is null || tempFiles.Length == 0)
                return [];

            ConcurrentQueue<string> queue = new(tempFiles);
            ConcurrentBag<Task<string>> bag = new();
            var itemsToRemove = new List<string>();

            while (!queue.IsEmpty)
            {
                while (queue.Count > 0)
                {
                    if (queue.TryDequeue(out var item1) && queue.TryDequeue(out var item2))
                    {
                        bag.Add(Task.Run(() => MergeFiles(item1, item2)));
                        itemsToRemove.Add(item1);
                        itemsToRemove.Add(item2);
                    }
                    else if (!string.IsNullOrEmpty(item1))
                    {
                        if (bag.Count > 0)
                            bag.Add(Task.FromResult(item1));
                        else
                            File.Move(item1, outputFile);

                        itemsToRemove.Add(item1);
                    }
                }

                var items = await Task.WhenAll(bag);
                bag.Clear();

                queue = new ConcurrentQueue<string>(items);
            }

            return itemsToRemove.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
        finally
        {
            sw.Stop();
            Console.WriteLine($"Merging fragments: {sw.Elapsed.ToString()}");
        }
    }
    
    private string MergeFiles(string fileOne, string fileTwo)
    {
        try
        {
            string finalFilePath = Path.GetTempFileName();
        
            using var srdr1 = new StreamReader(fileOne);
            using var srdr2 = new StreamReader(fileTwo);
            using var swr = new StreamWriter(finalFilePath);
            {
                string l1 = srdr1.ReadLine();
                string l2 = srdr2.ReadLine();

                while (l1 is not null && l2 is not null)
                {
                    if (string.CompareOrdinal(l1, l2) <= 0)
                    {
                        swr.WriteLine(l1);
                        l1 = srdr1.ReadLine();
                    }
                    else
                    {
                        swr.WriteLine(l2);
                        l2 = srdr2.ReadLine();
                    }
                }

                while (l1 is not null)
                {
                    swr.WriteLine(l1);
                    l1 = srdr1.ReadLine();
                }

                while (l2 is not null)
                {
                    swr.WriteLine(l2);
                    l2 = srdr2.ReadLine();
                }
            }

            return finalFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    private void Cleanup(string[] filesToRemove)
    {
        if (filesToRemove is null || filesToRemove.Length < 1)
            return;

        var sw = Stopwatch.StartNew();
        try
        {
            foreach (var file in filesToRemove)
            {
                if (!File.Exists(file))
                    continue;
                
                File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
        finally
        {
            sw.Stop();
            Console.WriteLine($"Cleanup: {sw.Elapsed.ToString()}");
        }
    }

    private string GenerateDefaultFileName(string targetFilePath)
    {
        var name = Path.GetFileNameWithoutExtension(targetFilePath);
        var ext = Path.GetExtension(targetFilePath);
        var newName = $"{name}-sorted{ext}";

        return Path.Combine(Path.GetDirectoryName(targetFilePath), newName);
    }
}