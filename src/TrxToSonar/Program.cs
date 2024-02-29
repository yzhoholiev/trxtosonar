using System.Globalization;
using McMaster.Extensions.CommandLineUtils;
using Serilog;
using TrxToSonar;
using TrxToSonar.Model.Sonar;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Async(config => config.Console(formatProvider: CultureInfo.InvariantCulture, applyThemeToRedirectedOutput: true))
    .CreateLogger();

using var app = new CommandLineApplication();

app.Name = "TRX To Sonar";
app.Description = "";

app.HelpOption("-?|-h|--help");

CommandOption solutionDirectoryOption = app.Option("-d", "Solution Directory to parse.", CommandOptionType.SingleValue);
CommandOption outputOption = app.Option("-o", "Output filename.", CommandOptionType.SingleValue);
CommandOption absolutePathOption = app.Option("-a|--absolute", "Use Absolute Path", CommandOptionType.NoValue);

app.OnExecute(
    () =>
    {
        if (!solutionDirectoryOption.HasValue() || !outputOption.HasValue())
        {
            app.ShowHint();
            return 0;
        }

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        using ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IConverter converter = serviceProvider.GetRequiredService<IConverter>();
        SonarDocument? sonarDocument = converter.Parse(solutionDirectoryOption.Value(), absolutePathOption.HasValue());
        if (sonarDocument is null)
        {
            return 1;
        }

        converter.Save(sonarDocument, outputOption.Value()!);

        return 0;
    });

return app.Execute(args);

static void ConfigureServices(IServiceCollection serviceCollection)
{
    serviceCollection.AddLogging(builder => builder.AddSerilog(dispose: true));
    serviceCollection.AddSingleton<IConverter, Converter>();
}
