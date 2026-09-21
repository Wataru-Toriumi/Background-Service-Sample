using FlaUI.Core.AutomationElements;

namespace BackgroundServiceSample.E2E;

internal sealed class SettingsScreen(Window window)
{
    private AutomationElement Element(string id) => window.FindFirstDescendant(cf => cf.ByAutomationId(id))
        ?? throw new InvalidOperationException($"Settings element not found: {id}");

    public string Interval
    {
        get => Element("IntervalInput").AsTextBox().Text;
        set => Element("IntervalInput").AsTextBox().Text = value;
    }

    public string Message => Element("SettingsMessage").Name;
    public string RunningInterval => Element("RunningInterval").Name;
    public void Save() => Element("SaveSettingsButton").AsButton().Click();
    public void Reset() => Element("ResetSettingsButton").AsButton().Click();
}
