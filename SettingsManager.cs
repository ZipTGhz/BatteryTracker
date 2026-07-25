using System.Text.Json;

public static class SettingsManager
{
    public static AppSettings Settings { get; private set; } = new();
    private static readonly string FILE_PATH = "settings.json";
    private static readonly JsonSerializerOptions SAVE_OPTION = new()
    {
        WriteIndented = true,
    };

    public static void Load()
    {
        if (File.Exists(FILE_PATH))
        {
            Settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FILE_PATH))!;
        }
    }

    public static void Save()
    {
        File.WriteAllText(FILE_PATH, JsonSerializer.Serialize(Settings, SAVE_OPTION));
    }
}
