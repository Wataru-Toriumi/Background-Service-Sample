using Avalonia;
using BackgroundServiceSample.Workers;
using BackgroundServiceSample.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BackgroundServiceSample;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var settingsDirectory = Environment.GetEnvironmentVariable("BACKGROUND_SERVICE_SAMPLE_SETTINGS_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BackgroundServiceSample");
        var settingsStore = new WorkerSettingsStore(settingsDirectory);
        builder.Services.AddSingleton(settingsStore);
        builder.Services.AddSingleton(settingsStore.Current);
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddSingleton<WorkerCoordinator>();
        builder.Services.AddSingleton<MainWindowViewModel>();
        builder.Services.AddHostedService<PeriodicTaskService>();

        using var host = builder.Build();
        host.Start();

        try
        {
            BuildAvaloniaApp(host.Services)
                .StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            host.StopAsync().GetAwaiter().GetResult();
        }
    }

    // Avalonia デザインプレビュー用のパラメータレス エントリ。
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static AppBuilder BuildAvaloniaApp(IServiceProvider services)
        => AppBuilder.Configure(() => new App(services))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
