namespace BackgroundServiceSample.E2E.Infrastructure;

internal sealed class E2EScenario : IDisposable
{
    private readonly string _artifactsDirectory;
    private int _launchCount;

    public E2EScenario(string name)
    {
        var id = $"{name}-{Guid.NewGuid():N}";
        SettingsDirectory = Path.Combine(Path.GetTempPath(), "BackgroundServiceSample.E2E", id);
        var root = Environment.GetEnvironmentVariable("E2E_ARTIFACTS_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "TestResults");
        _artifactsDirectory = Path.Combine(root, id);
    }

    public string SettingsDirectory
    {
        get;
    }
    public string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public void RunApp(Action<AppSession> scenario)
    {
        var artifactsDirectory = Path.Combine(_artifactsDirectory, $"launch-{++_launchCount}");
        using var app = new AppSession(SettingsDirectory, artifactsDirectory);
        try
        {
            app.Start();
            scenario(app);
        }
        catch (Exception error)
        {
            app.SaveFailure(error);
            throw;
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(SettingsDirectory))
        {
            Directory.Delete(SettingsDirectory, recursive: true);
        }
    }
}
