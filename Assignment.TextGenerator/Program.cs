namespace Assignment.TextGenerator;

class Program
{
    static async Task Main(string[] args)
    {
        var (outputPath, options) = ParseInput(args);
        if (options is null)
            return;
        
        IGenerator generator = new TextGenerator(options);
        await generator.CreateFile(outputPath);
    }

    private static (string, TextGeneratorOptions) ParseInput(string[] args)
    {
        try
        {
            if (args is null || args.Length == 0)
                return (null, TextGeneratorOptions.Default);

            string outputPath = default;
            var options = TextGeneratorOptions.Default;
        
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-o":
                        if (i + 1 < args.Length)
                            outputPath = args[i + 1];
                        break;
                
                    case "-c":
                        if (i + 1 < args.Length && long.TryParse(args[i + 1], out var lineCount))
                            options.LineCount = lineCount;
                        break;
                
                    case "-i":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out var maxItems))
                            options.MaxItems = maxItems;
                        break;
                
                    case "-n":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out var maxLineNumber))
                            options.MaxLineNumber = maxLineNumber;
                        break;
                
                    case "-w":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out var wordsPerLine))
                            options.MaxWordsPerLine = wordsPerLine;
                        break;
                }
            }

            return (outputPath, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Input parsing error");
            Console.WriteLine(ex.Message);
            return (null, null);
        }
    }
}