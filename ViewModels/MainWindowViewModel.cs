using System.Collections.ObjectModel;
using Avalonia.Threading;
using BackgroundServiceSample.Common;
using BackgroundServiceSample.Services;

namespace BackgroundServiceSample.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private const int MaxLogCount = 200;

    private readonly WorkerCoordinator _coordinator;
    private bool _isActive;
    private double _progress;
    private string _statusText = "停止中";

    public MainWindowViewModel(WorkerCoordinator coordinator)
    {
        _coordinator = coordinator;

        StartCommand = new RelayCommand(_coordinator.Start, () => !IsActive);
        StopCommand = new RelayCommand(_coordinator.Stop, () => IsActive);

        _coordinator.ActiveChanged += OnActiveChanged;
        _coordinator.LogProduced += OnLogProduced;
        _coordinator.ProgressChanged += OnProgressChanged;
    }

    public ObservableCollection<string> Logs { get; } = new();

    public RelayCommand StartCommand { get; }

    public RelayCommand StopCommand { get; }

    public bool IsActive
    {
        get => _isActive;
        private set
        {
            if (SetField(ref _isActive, value))
            {
                StatusText = value ? "実行中" : "停止中";
                StartCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public double Progress
    {
        get => _progress;
        private set => SetField(ref _progress, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
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
