namespace BackgroundServiceSample.Workers;

/// <summary>
/// UI とバックグラウンドサービスの間で実行状態の制御と進捗・ログの通知を仲介する。
/// </summary>
public sealed class WorkerCoordinator
{
    private volatile bool _isActive;

    public bool IsActive => _isActive;

    public event EventHandler<bool>? ActiveChanged;
    public event EventHandler<string>? LogProduced;
    public event EventHandler<double>? ProgressChanged;

    public void Start()
    {
        if (_isActive)
        {
            return;
        }

        _isActive = true;
        ActiveChanged?.Invoke(this, true);
    }

    public void Stop()
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;
        ActiveChanged?.Invoke(this, false);
    }

    public void ReportLog(string message) => LogProduced?.Invoke(this, message);

    public void ReportProgress(double ratio) => ProgressChanged?.Invoke(this, ratio);
}
