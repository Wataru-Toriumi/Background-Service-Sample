namespace BackgroundServiceSample.Workers;

public sealed record WorkerSettings
{
    public const int DefaultIntervalSeconds = 3;
    public const int MaxIntervalSeconds = 3600;

    public int IntervalSeconds { get; init; } = DefaultIntervalSeconds;

    public void Validate()
    {
        if (IntervalSeconds is < 1 or > MaxIntervalSeconds)
            throw new ArgumentOutOfRangeException(nameof(IntervalSeconds), "実行間隔は 1〜3600 秒で指定してください。");
    }
}
