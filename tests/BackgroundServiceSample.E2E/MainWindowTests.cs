using System.Diagnostics;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace BackgroundServiceSample.E2E;

public sealed class MainWindowTests
{
    [Fact]
    public void Start_progress_stop_and_close_work_through_the_real_window()
    {
        using var app = new AppSession();
        try
        {
            app.Start();
            app.WaitUntil(() => app.Status == "状態: 停止中" && app.StartEnabled && !app.StopEnabled,
                "Initial stopped state");
            Assert.Equal(0d, app.Progress);

            app.Click("StartButton");
            app.WaitUntil(() => app.Status == "状態: 実行中" && !app.StartEnabled && app.StopEnabled,
                "Running state and button availability");
            app.WaitUntil(() => app.Logs.Any(x => x.Contains("タスク開始")) && app.Progress > 0,
                "Task start log and progress update");

            // Stop prevents future runs; an in-flight task is allowed to finish.
            app.Click("StopButton");
            app.WaitUntil(() => app.Status == "状態: 停止中" && app.StartEnabled && !app.StopEnabled,
                "Stopped state and button availability");
            app.WaitUntil(() =>
            {
                var logs = app.Logs;
                var starts = logs.Count(x => x.Contains("タスク開始"));
                var completions = logs.Count(x => x.Contains("タスク完了"));
                return starts > 0 && starts == completions && app.Progress == 100;
            }, "In-flight work completes and progress reaches 100%");

            var completedLogs = app.Logs;
            // Observe for two production timer intervals: a single snapshot cannot prove no new run.
            var observation = Stopwatch.StartNew();
            while (observation.Elapsed < TimeSpan.FromSeconds(6))
            {
                app.AssertAlive();
                Assert.Equal(completedLogs, app.Logs);
                Thread.Sleep(100);
            }

            app.CloseWindowAndAssertExit();
        }
        catch (Exception error)
        {
            app.SaveFailure(error);
            throw;
        }
    }
}
