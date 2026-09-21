using System.Collections.Concurrent;
using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

namespace BackgroundServiceSample.E2E;

internal sealed class AppSession : IDisposable
{
    private readonly ConcurrentQueue<string> _output = new();
    private Process? _process;
    private UIA3Automation? _automation;
    private Window? _window;

    public void Start()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("FlaUI E2E requires an interactive Windows desktop.");

        var executable = Environment.GetEnvironmentVariable("E2E_APP_PATH");
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
            throw new InvalidOperationException("Set E2E_APP_PATH to the published app exe, or run scripts/test-e2e.ps1.");

        _process = new Process
        {
            StartInfo = new ProcessStartInfo(Path.GetFullPath(executable))
            {
                WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(executable))!,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
        };
        _process.OutputDataReceived += (_, args) => { if (args.Data is not null) _output.Enqueue(args.Data); };
        _process.ErrorDataReceived += (_, args) => { if (args.Data is not null) _output.Enqueue(args.Data); };
        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        _automation = new UIA3Automation();
        WaitUntil(() =>
        {
            _process.Refresh();
            if (_process.MainWindowHandle == IntPtr.Zero) return false;
            _window = _automation.FromHandle(_process.MainWindowHandle).AsWindow();
            return !_window.IsOffscreen && _window.Title == "Background Service Sample";
        }, "Visible main window");
        _window!.Focus();
    }

    private AutomationElement Element(string id) =>
        _window!.FindFirstDescendant(cf => cf.ByAutomationId(id))
        ?? throw new InvalidOperationException($"UI element not found: {id}");

    public string Status => Element("StatusText").Name;
    public bool StartEnabled => Element("StartButton").IsEnabled;
    public bool StopEnabled => Element("StopButton").IsEnabled;
    public double Progress => Element("ProgressBar").Patterns.RangeValue.Pattern.Value.Value;
    public string[] Logs => _window!.FindAllDescendants(cf => cf.ByAutomationId("LogEntry"))
        .Select(element => element.Name).ToArray();

    public void Click(string id) => Element(id).AsButton().Click();

    public void AssertAlive() => Assert.False(_process!.HasExited, "Application exited unexpectedly.");

    public void WaitUntil(Func<bool> condition, string description)
    {
        var timer = Stopwatch.StartNew();
        Exception? lastError = null;
        while (timer.Elapsed < TimeSpan.FromSeconds(20))
        {
            AssertAlive();
            try
            {
                if (condition()) return;
            }
            catch (Exception error)
            {
                // UIA may briefly observe missing or stale elements during layout updates.
                lastError = error;
            }
            Thread.Sleep(100);
        }
        throw new TimeoutException($"Timed out: {description}", lastError);
    }

    public void CloseWindowAndAssertExit()
    {
        _window!.Close();
        Assert.True(_process!.WaitForExit(10_000), "Closing the window must terminate the host and process.");
        Assert.Equal(0, _process.ExitCode);
    }

    public void SaveFailure(Exception error)
    {
        var root = Environment.GetEnvironmentVariable("E2E_ARTIFACTS_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "TestResults");
        var directory = Path.Combine(root, $"failure-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "error.txt"), error.ToString());
            File.WriteAllLines(Path.Combine(directory, "process.log"), _output);
            if (_window is not null)
            {
                TryCapture(() => _window.CaptureToFile(Path.Combine(directory, "window.png")), directory);
                TryCapture(() => File.WriteAllLines(Path.Combine(directory, "ui-elements.txt"),
                    _window.FindAllDescendants().Select(element =>
                        $"{element.ControlType} | {element.AutomationId} | {element.Name}")), directory);
            }
            Console.WriteLine($"E2E failure artifacts: {directory}");
        }
        catch (Exception captureError)
        {
            Console.WriteLine($"Could not save failure artifacts: {captureError}");
        }
    }

    private static void TryCapture(Action action, string directory)
    {
        try { action(); }
        catch (Exception error) { File.AppendAllText(Path.Combine(directory, "capture-errors.txt"), error + Environment.NewLine); }
    }

    public void Dispose()
    {
        try
        {
            if (_process is not null)
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.CloseMainWindow();
                        if (!_process.WaitForExit(5_000))
                        {
                            _process.Kill(entireProcessTree: true);
                            _process.WaitForExit(5_000);
                        }
                    }
                }
                catch (InvalidOperationException) { /* Process was never started or already exited. */ }
            }
        }
        finally
        {
            _process?.Dispose();
            _automation?.Dispose();
        }
    }
}
