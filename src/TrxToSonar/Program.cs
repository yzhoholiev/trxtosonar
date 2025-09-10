using System.CommandLine;
using System.Globalization;
using Serilog;
using TrxToSonar;
using TrxToSonar.Model.Sonar;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Async(config => config.Console(formatProvider: CultureInfo.InvariantCulture, applyThemeToRedirectedOutput: true))
    .CreateLogger();

try
{
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

    // Create root command
    var rootCommand = new RootCommand("TRX To Sonar")
    {
        solutionDirectoryOption,
        outputOption,
        absolutePathOption
    };

    // Set command handler
    rootCommand.SetAction(parseResult =>
    {
        try
        {
            // Create and configure host
            HostApplicationBuilder builder = Host.CreateApplicationBuilder();
            builder.Services.AddSerilog(dispose: true);
            builder.Services.AddSingleton<IConverter, Converter>();

            using IHost host = builder.Build();

            // Get options values
            DirectoryInfo solutionDir = parseResult.GetRequiredValue(solutionDirectoryOption);
            FileInfo output = parseResult.GetRequiredValue(outputOption);
            bool useAbsolute = parseResult.GetValue(absolutePathOption);

            // Get converter service
            IConverter converter = host.Services.GetRequiredService<IConverter>();

            // Parse TRX files
            SonarDocument? sonarDocument = converter.Parse(solutionDir.FullName, useAbsolute);

            if (sonarDocument is null)
            {
                return 1;
            }

            // Save output
            converter.Save(sonarDocument, output.FullName);
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
