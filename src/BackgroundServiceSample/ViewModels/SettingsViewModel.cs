using System.Globalization;
using BackgroundServiceSample.Common;
using BackgroundServiceSample.Workers;

namespace BackgroundServiceSample.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly WorkerSettingsStore _store;
    private readonly int _runningIntervalSeconds;
    private string _intervalSeconds;
    private string _message;

    public SettingsViewModel(WorkerSettingsStore store, WorkerSettings runningSettings)
    {
        _store = store;
        _runningIntervalSeconds = runningSettings.IntervalSeconds;
        _intervalSeconds = store.Current.IntervalSeconds.ToString(CultureInfo.InvariantCulture);
        _message = store.LoadError ?? "";
        SaveCommand = new RelayCommand(Save);
        ResetCommand = new RelayCommand(Reset);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand ResetCommand { get; }
    public string RunningIntervalText => $"現在の実行間隔: {_runningIntervalSeconds} 秒";

    public string IntervalSeconds
    {
        get => _intervalSeconds;
        set
        {
            if (SetField(ref _intervalSeconds, value)) Message = "未保存の変更があります。";
        }
    }

    public string Message
    {
        get => _message;
        private set => SetField(ref _message, value);
    }

    private void Save()
    {
        if (!int.TryParse(IntervalSeconds, out var seconds) || seconds is < 1 or > WorkerSettings.MaxIntervalSeconds)
        {
            Message = "実行間隔は 1〜3600 の整数で入力してください。";
            return;
        }

        try
        {
            _store.Save(new WorkerSettings { IntervalSeconds = seconds });
            Message = seconds == _runningIntervalSeconds
                ? "設定を保存しました。"
                : "設定を保存しました。アプリを再起動すると反映されます。";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Message = "設定を保存できませんでした。保存先へのアクセス権や空き容量を確認してください。";
        }
    }

    private void Reset()
    {
        IntervalSeconds = _store.Current.IntervalSeconds.ToString(CultureInfo.InvariantCulture);
        Message = _store.Current.IntervalSeconds == _runningIntervalSeconds
            ? "保存済みの設定に戻しました。"
            : "保存済みの設定に戻しました。アプリを再起動すると反映されます。";
    }
}
