using System.CommandLine;
using TrxToSonar;
using TrxToSonar.Logging;

try
{
    var solutionDirectoryOption = new Option<DirectoryInfo>("--directory", "-d")
    {
        Description = "Solution directory to parse",
        Required = true
    };

    var outputOption = new Option<FileInfo>("--output", "-o")
    {
        Description = "Output filename",
        Required = true
    };

    var absolutePathOption = new Option<bool>("--absolute", "-a")
    {
        Description = "Use absolute path"
    };

    var verbosityOption = new Option<Verbosity>("--verbosity")
    {
        Description = "Set log verbosity: Quiet | Minimal | Normal | Detailed | Diagnostic (default: Normal)",
        DefaultValueFactory = _ => Verbosity.Normal
    };

    var rootCommand = new RootCommand("Converts TRX test result files to SonarQube Generic Test Data format")
    {
        solutionDirectoryOption,
        outputOption,
        absolutePathOption,
        verbosityOption
    };

    rootCommand.SetAction(parseResult =>
    {
        var logLevel = parseResult.GetValue(verbosityOption).ToLogLevel();
        var logger = new ConsoleLogger<Converter>(logLevel);

        DirectoryInfo solutionDir = parseResult.GetRequiredValue(solutionDirectoryOption);
        FileInfo output = parseResult.GetRequiredValue(outputOption);
        bool useAbsolute = parseResult.GetValue(absolutePathOption);

        var converter = new Converter(logger);

        ConversionResult result = converter.Parse(solutionDir, useAbsolute);
        logger.Summary(result.TrxFileCount, result.Total, result.Passed, result.Skipped, result.Failed, result.Errored, result.Unresolved);
        if (result.Document is null)
        {
            return 1;
        }

        Result save = Converter.Save(result.Document, output.FullName);
        if (save.IsSuccess)
        {
            return 0;
        }

        logger.SaveFailed(save.Error!);
        return 1;
    });

    ParseResult parseResult = rootCommand.Parse(args);
    return await parseResult.InvokeAsync();
}
catch (Exception ex)
{
    new ConsoleLogger<Program>(LogLevel.Information).TerminatedUnexpectedly(ex.Message);
    return 1;
}
