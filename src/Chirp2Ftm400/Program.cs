using Chirp2Ftm400;
using Chirp2Ftm400.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

var console = AnsiConsole.Console;
var registrar = new ConsoleTypeRegistrar(console);

var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("chirp2ftm400");
    config.SetApplicationVersion("1.0.0");

    config.AddCommand<ConvertCommand>("convert")
        .WithDescription("Convert a CHIRP CSV file to Yaesu FTM-400D ADMS-4 CSV format")
        .WithExample("convert", "my_channels.csv")
        .WithExample("convert", "my_channels.csv", "-o", "ftm400.csv", "--overwrite");

    config.AddCommand<ValidateCommand>("validate")
        .WithDescription("Validate a CHIRP CSV file without converting")
        .WithExample("validate", "my_channels.csv");

    config.AddCommand<InspectCommand>("inspect")
        .WithDescription("Inspect and display CHIRP CSV channels in a table")
        .WithExample("inspect", "my_channels.csv", "--page-size", "25");
});

return app.Run(args);
