using System.Text.Json;
using Hidden_Hearts_in_Wonderland.Models;

namespace Hidden_Hearts_in_Wonderland.Services;

public class SaveService
{
    private static string SavePath => Path.Combine(FileSystem.AppDataDirectory, "save.json");

    public async Task SaveAsync(GameService game)
    {
        var data = new SaveData
        {
            Player = game.Player,
            CurrentCharacter = game.CurrentCharacter,
            Coins = game.Coins,
            Experience = game.Experience,
            Level = game.Level,
            StatPoints = game.StatPoints,
            AttackLevel = game.AttackLevel,
            DefenseLevel = game.DefenseLevel,
            HealthLevel = game.HealthLevel,
            RuneAttackBonus = game.RuneAttackBonus,
            RuneDefenseBonus = game.RuneDefenseBonus,
            RuneHealthBonus = game.RuneHealthBonus,
            AttackBonus = game.AttackBonus,
            DefenseBonus = game.DefenseBonus,
            HealthBonus = game.HealthBonus
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(SavePath, json);
    }

    public async Task<SaveData?> LoadAsync()
    {
        if (!File.Exists(SavePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(SavePath);
        return JsonSerializer.Deserialize<SaveData>(json);
    }
}

public class SaveData
{
    public PlayerState Player { get; set; } = new();
    public string CurrentCharacter { get; set; } = "";
    public int Coins { get; set; }
    public int Experience { get; set; }
    public int Level { get; set; } = 1;
    public int StatPoints { get; set; }
    public int AttackLevel { get; set; }
    public int DefenseLevel { get; set; }
    public int HealthLevel { get; set; }
    public int RuneAttackBonus { get; set; }
    public int RuneDefenseBonus { get; set; }
    public int RuneHealthBonus { get; set; }
    public int AttackBonus { get; set; }
    public int DefenseBonus { get; set; }
    public int HealthBonus { get; set; }
}
