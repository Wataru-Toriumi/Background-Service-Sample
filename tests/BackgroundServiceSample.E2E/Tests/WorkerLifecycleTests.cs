using System.Diagnostics;
using BackgroundServiceSample.E2E.Infrastructure;
using Xunit;

namespace BackgroundServiceSample.E2E.Tests;

public sealed class WorkerLifecycleTests
{
    [Fact]
    public void Start_progress_stop_and_close_work_through_the_real_window()
    {
        using var app = new AppSession();
        try
        {
            app.Start();
            var screen = app.OpenStatus();
            app.WaitUntil(() => screen.Status == "状態: 停止中" && screen.StartEnabled && !screen.StopEnabled,
                "Initial stopped state");
            Assert.Equal(0d, screen.Progress);

            screen.Start();
            app.WaitUntil(() => screen.Status == "状態: 実行中" && !screen.StartEnabled && screen.StopEnabled,
                "Running state and button availability");
            app.WaitUntil(() => screen.Logs.Any(x => x.Contains("タスク開始")) && screen.Progress > 0,
                "Task start log and progress update");

            // Stop prevents future runs; an in-flight task is allowed to finish.
            screen.Stop();
            app.WaitUntil(() => screen.Status == "状態: 停止中" && screen.StartEnabled && !screen.StopEnabled,
                "Stopped state and button availability");
            app.WaitUntil(() =>
            {
                var logs = screen.Logs;
                var starts = logs.Count(x => x.Contains("タスク開始"));
                var completions = logs.Count(x => x.Contains("タスク完了"));
                return starts > 0 && starts == completions && screen.Progress == 100;
            }, "In-flight work completes and progress reaches 100%");

            var completedLogs = screen.Logs;
            // Observe for two production timer intervals: a single snapshot cannot prove no new run.
            var observation = Stopwatch.StartNew();
            while (observation.Elapsed < TimeSpan.FromSeconds(6))
            {
                app.AssertAlive();
                Assert.Equal(completedLogs, screen.Logs);
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
