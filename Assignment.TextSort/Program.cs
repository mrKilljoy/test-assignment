using Assignment.Infrastructure.Configuration;
using Assignment.Infrastructure.Interfaces;
using Assignment.Infrastructure.Services;

namespace Assignment.TextSort;

class Program
{
    static async Task Main(string[] args)
    {
        var (inputPath, outputPath, options) = ParseInput(args);
        if (options is null)
            return;
        
        ISortService sortService = new TextSortService(options);
        await sortService.Sort(inputPath, outputPath);
    }
    
    private static (string, string, TextSortOptions) ParseInput(string[] args)
    {
        try
        {
            if (args is null || args.Length == 0)
            {
                Console.WriteLine("Input file path is not specified.");
                return (null, null, null);
            }

            string inputPath = default;
            string outputPath = default;
            var options = TextSortOptions.Default;
        
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-i":
                        if (i + 1 < args.Length)
                            inputPath = args[i + 1];
                        break;
                    
                    case "-o":
                        if (i + 1 < args.Length)
                            outputPath = args[i + 1];
                        break;
                
                    case "-s":
                        if (i + 1 < args.Length && long.TryParse(args[i + 1], out var chunkSize))
                            options.ChunkSize = chunkSize;
                        break;
                }
            }

            return (inputPath, outputPath, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Input parsing error");
            Console.WriteLine(ex.Message);
            return (null, null, null);
        }
    }
}