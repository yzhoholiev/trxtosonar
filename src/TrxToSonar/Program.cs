using System.CommandLine;
using System.Globalization;
using System.Reflection;
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
    // Display logo and version unless --no-logo or --help is specified
    bool noLogo = args.Contains("--no-logo");
    bool isHelp = args.Contains("--help") || args.Contains("-h") || args.Contains("-?");

    if (!noLogo && !isHelp)
    {
        PrintLogo();
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

    // Create root command
    var rootCommand = new RootCommand("Converts TRX test result files to SonarQube Generic Test Data format")
    {
        solutionDirectoryOption,
        outputOption,
        absolutePathOption,
        noLogoOption
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

static void PrintLogo()
{
    var assembly = Assembly.GetExecutingAssembly();
    string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                     ?? assembly.GetName().Version?.ToString()
                     ?? "Unknown";

    const int maxVersionLength = 43;
    if (version.Length > maxVersionLength)
    {
        version = version[..(maxVersionLength - 3)] + "...";
    }

    Console.WriteLine();
    Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                                                               ║");
    Console.WriteLine("║                   TRX to Sonar Converter                      ║");
    Console.WriteLine("║                                                               ║");
    Console.WriteLine($"║          Version: {version,-maxVersionLength} ║");
    Console.WriteLine("║          Copyright (c) 2022-2025 Yurii Zhoholiev              ║");
    Console.WriteLine("║                                                               ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
}
