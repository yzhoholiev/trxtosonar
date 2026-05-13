using System.CommandLine;
using System.Globalization;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using TrxToSonar;

// Configure Serilog. Level is controlled by the --verbosity flag below.
var levelSwitch = new LoggingLevelSwitch();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(levelSwitch)
    .Enrich.FromLogContext()
    .WriteTo.Async(sinks => sinks.Console(formatProvider: CultureInfo.InvariantCulture, applyThemeToRedirectedOutput: false))
    .CreateLogger();

try
{
    // Display logo and version unless --no-logo or --help is specified
    bool noLogo = args.Contains("--no-logo");
    bool isHelp = args.Contains("--help") || args.Contains("-h") || args.Contains("-?");

    if (!noLogo && !isHelp)
    {
        ConsoleOutput.WriteLogo();
    }

    // Create command line options
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

    var noLogoOption = new Option<bool>("--no-logo")
    {
        Description = "Suppress logo and version information"
    };

    var verbosityOption = new Option<Verbosity>("--verbosity")
    {
        Description = "Set log verbosity: Quiet | Minimal | Normal | Detailed | Diagnostic (default: Normal)",
        DefaultValueFactory = _ => Verbosity.Normal
    };

    // Create root command
    var rootCommand = new RootCommand("Converts TRX test result files to SonarQube Generic Test Data format")
    {
        solutionDirectoryOption,
        outputOption,
        absolutePathOption,
        noLogoOption,
        verbosityOption
    };

    // Set command handler
    rootCommand.SetAction(parseResult =>
    {
        try
        {
            DirectoryInfo solutionDir = parseResult.GetRequiredValue(solutionDirectoryOption);
            FileInfo output = parseResult.GetRequiredValue(outputOption);
            bool useAbsolute = parseResult.GetValue(absolutePathOption);
            levelSwitch.MinimumLevel = MapVerbosity(parseResult.GetValue(verbosityOption));

            using var loggerFactory = new SerilogLoggerFactory(Log.Logger);
            var converter = new Converter(loggerFactory.CreateLogger<Converter>());

            ConversionResult result = converter.Parse(solutionDir.FullName, useAbsolute);
            ConsoleOutput.WriteSummary(result);
            if (result.Document is null)
            {
                return 1;
            }

            Converter.Save(result.Document, output.FullName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while processing TRX files");
            return 1;
        }

        return 0;
    });

    // Execute command
    ParseResult parseResult = rootCommand.Parse(args);
    return await parseResult.InvokeAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

static LogEventLevel MapVerbosity(Verbosity verbosity)
{
    return verbosity switch
    {
        Verbosity.Quiet => LogEventLevel.Error,
        Verbosity.Minimal => LogEventLevel.Warning,
        Verbosity.Normal => LogEventLevel.Information,
        Verbosity.Detailed => LogEventLevel.Debug,
        Verbosity.Diagnostic => LogEventLevel.Verbose,
        _ => LogEventLevel.Information
    };
}
