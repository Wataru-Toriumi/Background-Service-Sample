using Xunit;

namespace BackgroundServiceSample.E2E;

public sealed class SettingsTests
{
    [Fact]
    public void Settings_validate_save_and_survive_restart()
    {
        var directory = Path.Combine(Path.GetTempPath(), "BackgroundServiceSample.E2E", Guid.NewGuid().ToString("N"));
        try
        {
            WithApp(directory, app =>
            {
                var screen = app.OpenSettings();
                Assert.Equal("3", screen.Interval);
                screen.Interval = "0";
                screen.Save();
                app.WaitUntil(() => screen.Message.Contains("整数"), "Validation error");
                Assert.False(File.Exists(Path.Combine(directory, "settings.json")));
                screen.Interval = "7";
                screen.Save();
                app.WaitUntil(() => screen.Message.Contains("保存しました"), "Settings saved");
                Assert.Contains("再起動", screen.Message);
                Assert.Equal("現在の実行間隔: 3 秒", screen.RunningInterval);
                app.CaptureScreenshot("settings-saved");
                screen.Interval = "9";
                screen.Reset();
                app.WaitUntil(() => screen.Interval == "7", "Unsaved edits discarded");
                app.CloseWindowAndAssertExit();
            });
            WithApp(directory, app =>
            {
                var screen = app.OpenSettings();
                Assert.Equal("7", screen.Interval);
                Assert.Equal("現在の実行間隔: 7 秒", screen.RunningInterval);
                app.CaptureScreenshot("settings-after-restart");
                app.CloseWindowAndAssertExit();
            });
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static void WithApp(string directory, Action<AppSession> scenario)
    {
        using var app = new AppSession(directory);
        try { app.Start(); scenario(app); }
        catch (Exception error) { app.SaveFailure(error); throw; }
    }
}
