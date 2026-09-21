using BackgroundServiceSample.Features.Settings;
using BackgroundServiceSample.Workers;
using Xunit;

namespace BackgroundServiceSample.Tests;

public sealed class SettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "BackgroundServiceSample.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Missing_settings_use_defaults_without_creating_a_file()
    {
        var store = new WorkerSettingsStore(_directory);
        Assert.Equal(3, store.Current.IntervalSeconds);
        Assert.Null(store.LoadError);
        Assert.False(Directory.Exists(_directory));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(3600)]
    public void Saved_settings_survive_reload_and_leave_running_settings_unchanged(int seconds)
    {
        var store = new WorkerSettingsStore(_directory);
        var runningSettings = store.Current;
        store.Save(new WorkerSettings { IntervalSeconds = seconds });
        Assert.Equal(seconds, new WorkerSettingsStore(_directory).Current.IntervalSeconds);
        Assert.Equal(3, runningSettings.IntervalSeconds);
        Assert.Single(Directory.GetFiles(_directory));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("3601")]
    [InlineData("1.5")]
    [InlineData("abc")]
    [InlineData("")]
    public void Invalid_input_does_not_overwrite_saved_settings(string input)
    {
        var store = new WorkerSettingsStore(_directory);
        store.Save(new WorkerSettings { IntervalSeconds = 7 });
        var viewModel = new SettingsViewModel(store, store.Current) { IntervalSeconds = input };
        viewModel.SaveCommand.Execute(null);
        Assert.Contains("整数", viewModel.Message);
        Assert.Equal(7, new WorkerSettingsStore(_directory).Current.IntervalSeconds);
    }

    [Theory]
    [InlineData("broken json")]
    [InlineData("null")]
    [InlineData("{\"IntervalSeconds\":0}")]
    public void Corrupt_settings_show_warning_and_are_not_silently_overwritten(string content)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        File.WriteAllText(path, content);
        var store = new WorkerSettingsStore(_directory);
        Assert.Equal(3, store.Current.IntervalSeconds);
        Assert.NotNull(store.LoadError);
        Assert.Equal(content, File.ReadAllText(path));
        Assert.Equal(store.LoadError, new SettingsViewModel(store, store.Current).Message);
    }

    [Fact]
    public void Reset_discards_edits_and_save_explains_restart()
    {
        var store = new WorkerSettingsStore(_directory);
        var viewModel = new SettingsViewModel(store, store.Current) { IntervalSeconds = "7" };
        viewModel.SaveCommand.Execute(null);
        Assert.Contains("再起動", viewModel.Message);
        Assert.Equal("現在の実行間隔: 3 秒", viewModel.RunningIntervalText);
        viewModel.IntervalSeconds = "9";
        viewModel.ResetCommand.Execute(null);
        Assert.Equal("7", viewModel.IntervalSeconds);
    }

    [Fact]
    public void Save_failure_is_shown_without_changing_current_settings()
    {
        Directory.CreateDirectory(_directory);
        var blockedDirectory = Path.Combine(_directory, "file-instead-of-directory");
        File.WriteAllText(blockedDirectory, "blocked");
        var store = new WorkerSettingsStore(blockedDirectory);
        var viewModel = new SettingsViewModel(store, store.Current) { IntervalSeconds = "7" };
        viewModel.SaveCommand.Execute(null);
        Assert.Contains("保存できませんでした", viewModel.Message);
        Assert.Equal(3, store.Current.IntervalSeconds);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
