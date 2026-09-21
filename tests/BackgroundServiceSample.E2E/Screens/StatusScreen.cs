using FlaUI.Core.AutomationElements;

namespace BackgroundServiceSample.E2E.Screens;

internal sealed class StatusScreen(Window window)
{
    private AutomationElement Element(string id) => window.FindFirstDescendant(cf => cf.ByAutomationId(id))
        ?? throw new InvalidOperationException($"Status element not found: {id}");

    public void Open() => Element("StatusTab").Click();
    public bool IsVisible => !Element("StatusText").IsOffscreen;
    public string Status => Element("StatusText").Name;
    public bool StartEnabled => Element("StartButton").IsEnabled;
    public bool StopEnabled => Element("StopButton").IsEnabled;
    public double Progress => Element("ProgressBar").Patterns.RangeValue.Pattern.Value.Value;
    public string[] Logs => window.FindAllDescendants(cf => cf.ByAutomationId("LogEntry"))
        .Select(element => element.Name).ToArray();

    public void Start() => Element("StartButton").AsButton().Click();
    public void Stop() => Element("StopButton").AsButton().Click();
}
