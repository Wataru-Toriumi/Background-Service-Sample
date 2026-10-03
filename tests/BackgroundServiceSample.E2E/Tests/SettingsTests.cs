using BackgroundServiceSample.E2E.Infrastructure;
using Xunit;

namespace BackgroundServiceSample.E2E.Tests;

public sealed class SettingsTests
{
    [Fact]
    public void Settings_validate_save_and_survive_restart()
    {
        using var scenario = new E2EScenario(nameof(Settings_validate_save_and_survive_restart));
        scenario.RunApp(app =>
        {
            var screen = app.OpenSettings();
            Assert.Equal("3", screen.Interval);
            screen.Interval = "0";
            screen.Save();
            app.WaitUntil(() => screen.Message.Contains("整数"), "Validation error");
            Assert.False(File.Exists(scenario.SettingsPath));
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
        scenario.RunApp(app =>
        {
            var screen = app.OpenSettings();
            Assert.Equal("7", screen.Interval);
            Assert.Equal("現在の実行間隔: 7 秒", screen.RunningInterval);
            app.CaptureScreenshot("settings-after-restart");
            app.CloseWindowAndAssertExit();
        });
    }

    [Fact]
    public void Corrupt_settings_show_warning_and_can_be_repaired()
    {
        using var scenario = new E2EScenario(nameof(Corrupt_settings_show_warning_and_can_be_repaired));
        Directory.CreateDirectory(scenario.SettingsDirectory);
        File.WriteAllText(scenario.SettingsPath, "broken json");
        scenario.RunApp(app =>
        {
            var screen = app.OpenSettings();
            Assert.Equal("3", screen.Interval);
            Assert.Contains("設定を読み込めなかった", screen.Message);
            Assert.Equal("broken json", File.ReadAllText(scenario.SettingsPath));
            screen.Interval = "7";
            screen.Save();
            app.WaitUntil(() => screen.Message.Contains("保存しました"), "Corrupt settings repaired");
            app.CloseWindowAndAssertExit();
        });
        scenario.RunApp(app =>
        {
            var screen = app.OpenSettings();
            Assert.Equal("7", screen.Interval);
            Assert.Equal("現在の実行間隔: 7 秒", screen.RunningInterval);
            Assert.DoesNotContain("設定を読み込めなかった", screen.Message);
            app.CloseWindowAndAssertExit();
        });
    }
}
