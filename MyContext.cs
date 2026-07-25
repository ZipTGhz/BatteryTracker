namespace BatteryTracker;

using System.Media;
using HidLibrary;
using Microsoft.VisualBasic;

public class MyContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;

    private Font _globalFont;

    private readonly string _defaultFontName = "Consolas Bold";
    private readonly SoundPlayer _lowBatterySound = new("low_power_1.wav");
    private int _lastWarnLevel = -1;
    private HidDevice? _mouse = null;
    private readonly SynchronizationContext _ui;

    public MyContext()
    {
        // LOAD JSON
        SettingsManager.Load();
        _globalFont = new Font(
            SettingsManager.Settings.FontName,
            SettingsManager.Settings.FontSize,
            SettingsManager.Settings.FontStyle
        );

        // SET UP NOTIFY ICON
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Exit", null, Exit_Click);
        contextMenu.Items.Add("Change Font", null, OnChangeFont);
        contextMenu.Items.Add("Change Canvas Size", null, OnChangeCanvasSize);
        _notifyIcon = new NotifyIcon
        {
            Icon = Helper.CreateTextIcon(
                "-1",
                _globalFont,
                canvasSize: SettingsManager.Settings.CanvasSize
            ),
            ContextMenuStrip = contextMenu,
            Visible = true,
        };

        _ui = SynchronizationContext.Current!;
        Task.Run(ReadLoop);
    }

    private void OnChangeFont(object? sender, EventArgs e)
    {
        string input;
        int fontSize;
        do
        {
            input = Interaction.InputBox(
                "Nhập Font Size: ",
                "Font Size",
                SettingsManager.Settings.FontSize.ToString()
            );
            if (string.IsNullOrWhiteSpace(input))
                return;
        } while (!int.TryParse(input, out fontSize));

        SettingsManager.Settings.FontSize = fontSize;
        SettingsManager.Save();
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
        while (true)
        {
            try
            {
                if (_mouse == null || !_mouse.IsConnected)
                    GetHidDevice();

                var report = _mouse?.Read();
                if (report?.Status != HidDeviceData.ReadStatus.Success)
                    continue;

                byte[] data = report.Data;
                int reportId = data[0];
                if (reportId != 0)
                {
                    // Console.WriteLine($"Report Data: [{string.Join(", ", report.Data)}]");
                    int percent = report.Data[^1];
                    _ui.Post(
                        _ =>
                        {
                            SetBatteryPercent(percent);
                            CheckBatteryWarning(percent);
                        },
                        null
                    );
                }
            }
            catch (Exception)
            {
                _mouse = null;
                Thread.Sleep(2000);
            }
        }
    }

    private void GetHidDevice()
    {
        while (true)
        {
            try
            {
                var device =
                    HidDevices
                        .Enumerate(DeviceID.DELUX_M900_PRO_VID, DeviceID.DELUX_M900_PRO_PID)
                        .FirstOrDefault(d =>
                            d.DevicePath.Contains("col03", StringComparison.OrdinalIgnoreCase)
                        )
                    ?? throw new Exception("Device not found");
                device.OpenDevice();

                if (!device.IsConnected)
                    throw new Exception("Device not connected");

                _mouse = device;

                Console.WriteLine("Connected!");
                return;
            }
            catch
            {
                _mouse = null;
                _ui.Post(
                    _ =>
                    {
                        SetBatteryPercent(-1);
                    },
                    null
                );
                Thread.Sleep(2000);
            }
        }
    }

    private void SetBatteryPercent(int percent)
    {
        string text = percent >= 100 ? "F" : percent.ToString();

        using Font font = new(_defaultFontName, SettingsManager.Settings.FontSize, FontStyle.Bold);
        _notifyIcon.Icon = Helper.CreateTextIcon(
            text,
            font,
            canvasSize: SettingsManager.Settings.CanvasSize
        );
    }

    private void CheckBatteryWarning(int percent)
    {
        int warnLevel = -1;

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

    private void Exit_Click(object? sender, EventArgs e)
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
