using Microsoft.Extensions.Hosting;

namespace BackgroundServiceSample.Workers;

/// <summary>
/// 一定間隔で起動し、アクティブ状態のときだけ擬似タスクを実行する定期実行サービス。
/// </summary>
public sealed class PeriodicTaskService : BackgroundService
{
    private readonly TimeSpan _interval;
    private const int StepCount = 20;

    private readonly WorkerCoordinator _coordinator;
    private int _runCount;

    public PeriodicTaskService(WorkerCoordinator coordinator, WorkerSettings settings)
    {
        _coordinator = coordinator;
        settings.Validate();
        _interval = TimeSpan.FromSeconds(settings.IntervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _coordinator.ReportLog("バックグラウンドサービスを起動しました (待機中)");

        using var timer = new PeriodicTimer(_interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (!_coordinator.IsActive)
                {
                    continue;
                }

                await RunOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // アプリ終了時の正常なキャンセル。
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        var run = Interlocked.Increment(ref _runCount);
        _coordinator.ReportLog($"#{run} タスク開始");
        _coordinator.ReportProgress(0d);

        for (var step = 1; step <= StepCount; step++)
        {
            await Task.Delay(100, stoppingToken);
            _coordinator.ReportProgress((double)step / StepCount);
        }

        _coordinator.ReportLog($"#{run} タスク完了");
    }
}
