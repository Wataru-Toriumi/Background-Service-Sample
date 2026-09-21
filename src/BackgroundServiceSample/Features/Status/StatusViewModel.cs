using System.Collections.ObjectModel;
using Avalonia.Threading;
using BackgroundServiceSample.Workers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BackgroundServiceSample.Features.Status;

public sealed partial class StatusViewModel : ObservableObject
{
    private const int MaxLogCount = 200;

    private readonly WorkerCoordinator _coordinator;
    private bool _isActive;
    private double _progress;
    private string _statusText = "停止中";

    public StatusViewModel(WorkerCoordinator coordinator, WorkerSettings runningSettings)
    {
        _coordinator = coordinator;
        RunningIntervalText = $"現在の実行間隔: {runningSettings.IntervalSeconds} 秒";

        _coordinator.ActiveChanged += OnActiveChanged;
        _coordinator.LogProduced += OnLogProduced;
        _coordinator.ProgressChanged += OnProgressChanged;
    }

    public ObservableCollection<string> Logs { get; } = [];

    public string RunningIntervalText
    {
        get;
    }

    private bool CanStart() => !IsActive;
    private bool CanStop() => IsActive;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start() => _coordinator.Start();

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop() => _coordinator.Stop();

    public bool IsActive
    {
        get => _isActive;
        private set
        {
            if (SetProperty(ref _isActive, value))
            {
                StatusText = value ? "実行中" : "停止中";
                StartCommand.NotifyCanExecuteChanged();
                StopCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    private void OnActiveChanged(object? sender, bool active)
        => Dispatcher.UIThread.Post(() => IsActive = active);

    private void OnProgressChanged(object? sender, double ratio)
        => Dispatcher.UIThread.Post(() => Progress = ratio * 100d);

    private void OnLogProduced(object? sender, string message)
        => Dispatcher.UIThread.Post(() =>
        {
            Logs.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
            while (Logs.Count > MaxLogCount)
            {
                Logs.RemoveAt(Logs.Count - 1);
            }
        });
}
