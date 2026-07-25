namespace BatteryTracker;

using System.Media;
using Microsoft.VisualBasic;

public class MyContext : ApplicationContext
{
    private int _lastWarnLevel = -1;

    private readonly NotifyIcon _notifyIcon;
    private Font _globalFont;
    private readonly SoundPlayer _lowBatterySound = new("low_power_1.wav");
    private readonly Control _ui = new();

    public MyContext()
    {
        // LOAD JSON
        SettingsManager.Load();
        _globalFont = SettingsManager.Settings.GlobalFont;
        _ui.CreateControl();

        // SET UP NOTIFY ICON
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Change Font", null, OnChangeFont);
        contextMenu.Items.Add("Change Canvas Size", null, OnChangeCanvasSize);
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, OnExit);
        _notifyIcon = new NotifyIcon
        {
            Icon = Helper.CreateTextIcon("-1", _globalFont, SettingsManager.Settings.CanvasSize),
            ContextMenuStrip = contextMenu,
            Visible = true,
        };

        Task.Run(ReadLoop);
    }

    private void OnChangeFont(object? sender, EventArgs e)
    {
        FontDialog fontDialog = new() { Font = _globalFont };
        if (fontDialog.ShowDialog() == DialogResult.OK)
        {
            _globalFont.Dispose();
            _globalFont = fontDialog.Font;
            SettingsManager.Settings.GlobalFont.Dispose();
            SettingsManager.Settings.GlobalFont = _globalFont;
            SettingsManager.Save();
        }
    }

    private void OnChangeCanvasSize(object? sender, EventArgs e)
    {
        string input;
        int canvasSize;
        do
        {
            input = Interaction.InputBox(
                "Nhập Canvas Size: ",
                "Canvas Size",
                SettingsManager.Settings.CanvasSize.ToString()
            );
            if (string.IsNullOrWhiteSpace(input))
                return;
        } while (!int.TryParse(input, out canvasSize));

        SettingsManager.Settings.CanvasSize = canvasSize;
        SettingsManager.Save();
    }

    private void ReadLoop()
    {
        HIDReportReader mouseReader = new(DeviceID.M900P_VID, DeviceID.M900P_PID, "col03");

        while (true)
        {
            var data = mouseReader.ReadReportData();
            if (data == null)
                _ui.BeginInvoke(() => SetBatteryPercent(-1));
            else if (data[0] != 0)
                _ui.BeginInvoke(() => SetBatteryPercent(data[^1]));
            Thread.Sleep(5000);
        }
    }

    private void SetBatteryPercent(int percent)
    {
        string text = percent >= 100 ? "F" : percent.ToString();
        _notifyIcon.Icon = Helper.CreateTextIcon(
            text,
            _globalFont,
            SettingsManager.Settings.CanvasSize
        );
        CheckBatteryWarning(percent);
    }

    private void CheckBatteryWarning(int percent)
    {
        int warnLevel = -1;

        if (percent < 0)
            return;
        if (percent <= 5)
            warnLevel = 5;
        else if (percent <= 10)
            warnLevel = 10;
        else if (percent <= 15)
            warnLevel = 15;
        else if (percent <= 20)
            warnLevel = 20;

        if (warnLevel != -1 && warnLevel != _lastWarnLevel)
        {
            _lowBatterySound.Play();
            _lastWarnLevel = warnLevel;
        }

        if (percent > 20)
            _lastWarnLevel = -1;
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _notifyIcon.Visible = false;
        _lowBatterySound.Dispose();
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
