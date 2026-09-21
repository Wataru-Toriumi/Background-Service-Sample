using System.Text.Json;

namespace BackgroundServiceSample.Workers;

/// <summary>ユーザーごとの設定を JSON で保存する。保存した設定は次回起動時に読み込まれる。</summary>
public sealed class WorkerSettingsStore
{
    private readonly string _filePath;

    public WorkerSettings Current { get; private set; } = new();
    public string? LoadError { get; }

    public WorkerSettingsStore(string directory)
    {
        _filePath = Path.Combine(directory, "settings.json");
        try
        {
            var settings = JsonSerializer.Deserialize<WorkerSettings>(File.ReadAllText(_filePath))
                ?? throw new JsonException("設定が空です。");
            settings.Validate();
            Current = settings;
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentOutOfRangeException)
        {
            LoadError = "設定を読み込めなかったため、既定の 3 秒で起動しました。設定を確認して保存し直してください。";
        }
    }

    public void Save(WorkerSettings settings)
    {
        settings.Validate();
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var temporaryPath = _filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, _filePath, overwrite: true);
            Current = settings;
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
