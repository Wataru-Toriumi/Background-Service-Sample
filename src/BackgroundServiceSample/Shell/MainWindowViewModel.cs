using BackgroundServiceSample.Features.Settings;
using BackgroundServiceSample.Features.Status;

namespace BackgroundServiceSample.Shell;

/// <summary>各機能の ViewModel をメインウィンドウに渡す。</summary>
public sealed class MainWindowViewModel(StatusViewModel status, SettingsViewModel settings)
{
    public StatusViewModel Status { get; } = status;
    public SettingsViewModel Settings { get; } = settings;
}
