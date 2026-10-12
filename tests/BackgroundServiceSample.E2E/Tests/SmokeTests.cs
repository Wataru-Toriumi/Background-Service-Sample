using BackgroundServiceSample.E2E.Infrastructure;
using Xunit;

namespace BackgroundServiceSample.E2E.Tests;

public sealed class SmokeTests
{
    [Fact]
    [Trait("Category", "Smoke")]
    public void App_opens_both_screens_and_exits_cleanly()
    {
        using var scenario = new E2EScenario(nameof(App_opens_both_screens_and_exits_cleanly));
        scenario.RunApp(app =>
        {
            var status = app.OpenStatus();
            app.WaitUntil(() => status.Status == "状態: 停止中" && status.StartEnabled && !status.StopEnabled,
                "Initial stopped state");
            Assert.Equal(0d, status.Progress);
            Assert.DoesNotContain(status.Logs, log => log.Contains("タスク開始"));

            var settings = app.OpenSettings();
            Assert.Equal("3", settings.Interval);
            Assert.Equal("現在の実行間隔: 3 秒", settings.RunningInterval);

            status = app.OpenStatus();
            Assert.Equal("状態: 停止中", status.Status);
            app.CloseWindowAndAssertExit();
        });
    }
}
