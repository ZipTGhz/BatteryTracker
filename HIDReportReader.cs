using HidLibrary;

namespace BatteryTracker;

public class HIDReportReader
{
    private readonly int _vendorId;
    private readonly int _productId;
    private readonly string _devicePathKeyword;

    private HidDevice? _hidDevice;

    public HIDReportReader(int vendorId, int productId, string devicePathKeyword)
    {
        _vendorId = vendorId;
        _productId = productId;
        _devicePathKeyword = devicePathKeyword;
    }

    public bool IsReadable() => _hidDevice != null && _hidDevice.IsConnected;

    public byte[]? ReadReportData()
    {
        if (!IsReadable())
            LoadDevice(_vendorId, _productId, _devicePathKeyword);

        if (!IsReadable())
            return null;

        var report = _hidDevice?.Read();
        if (report?.Status != HidDeviceData.ReadStatus.Success)
            return null;

        int reportId = report.Data[0];
        if (reportId == 0)
            return null;

        return report.Data;
    }

    private void LoadDevice(int vendorId, int productId, string devicePathKeyword)
    {
        if (!IsReadable())
            _hidDevice = HidDevices
                .Enumerate(vendorId, productId)
                .FirstOrDefault(d =>
                    d.DevicePath.Contains(devicePathKeyword, StringComparison.OrdinalIgnoreCase)
                );
    }
}
