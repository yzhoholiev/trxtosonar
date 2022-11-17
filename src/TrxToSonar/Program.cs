using McMaster.Extensions.CommandLineUtils;
using TrxToSonar;
using TrxToSonar.Model.Sonar;

using var app = new CommandLineApplication
{
    Name = "Trx To Sonar",
    Description = ""
};

app.HelpOption("-?|-h|--help");

CommandOption solutionDirectoryOption = app.Option("-d", "Solution Directory to parse.", CommandOptionType.SingleValue);
CommandOption outputOption = app.Option("-o", "Output filename.", CommandOptionType.SingleValue);
CommandOption absolutePathOption = app.Option("-a|--absolute", "Use Absolute Path", CommandOptionType.NoValue);

app.OnExecute(
    () =>
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        using ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        if (solutionDirectoryOption.HasValue() && outputOption.HasValue())
        {
            IConverter converter = serviceProvider.GetRequiredService<IConverter>();
            SonarDocument? sonarDocument = converter.Parse(solutionDirectoryOption.Value()!, absolutePathOption.HasValue());
            if (sonarDocument is null)
            {
                return 1;
            }

            converter.Save(sonarDocument, outputOption.Value()!);
        }
        else
        {
            app.ShowHint();
        }

        return 0;
    });

return app.Execute(args);

static void ConfigureServices(IServiceCollection serviceCollection)
{
    serviceCollection.AddLogging(builder => builder.AddConsole());
    serviceCollection.AddSingleton<IConverter, Converter>();
}
