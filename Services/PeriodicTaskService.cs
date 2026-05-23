using Microsoft.Extensions.Hosting;

namespace BackgroundServiceSample.Services;

/// <summary>
/// 一定間隔で起動し、アクティブ状態のときだけ擬似タスクを実行する定期実行サービス。
/// </summary>
public sealed class PeriodicTaskService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(3);
    private const int StepCount = 20;

    private readonly WorkerCoordinator _coordinator;
    private int _runCount;

    public PeriodicTaskService(WorkerCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _coordinator.ReportLog("バックグラウンドサービスを起動しました (待機中)");

        using var timer = new PeriodicTimer(Interval);

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
