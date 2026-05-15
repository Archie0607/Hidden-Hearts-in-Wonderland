using Hidden_Hearts_in_Wonderland.Models;

namespace Hidden_Hearts_in_Wonderland.Services;

public class GameService
{
    public static GameService Instance { get; } = new GameService();
    public const string AttackRuneId = "rune_atk";
    public const string DefenseRuneId = "rune_dff";
    public const string HealthRuneId = "rune_hp";
    public const int MaxLevel = 30;
    public const int MaxStatRank = 30;

    private readonly ShopService _shopService = new();
    private readonly Random _random = new();
    private readonly string[] _runeDropIds = [AttackRuneId, DefenseRuneId, HealthRuneId];

    public PlayerState Player { get; set; } = new();

    public string CurrentCharacter { get; set; } = "";

    public int Coins { get; private set; } = 100;

    public int Experience { get; private set; }
    public int Level { get; private set; } = 1;
    public int StatPoints { get; private set; }
    public int AttackLevel { get; private set; }
    public int DefenseLevel { get; private set; }
    public int HealthLevel { get; private set; }
    public int RuneAttackBonus { get; private set; }
    public int RuneDefenseBonus { get; private set; }
    public int RuneHealthBonus { get; private set; }
    public int AttackBonus => (AttackLevel * 2) + RuneAttackBonus;
    public int DefenseBonus => DefenseLevel + RuneDefenseBonus;
    public int HealthBonus => (HealthLevel * 10) + RuneHealthBonus;
    public int ExperienceToNextLevel => Level >= MaxLevel ? 0 : Level * 100;

    public void SelectCharacter(string characterName)
    {
        // จำตัวละครที่กำลังคุยอยู่ เพื่อให้ choice ต่อไปเพิ่ม affection ถูกคน
        CurrentCharacter = characterName;
        SaveSoon();
    }

    public void ApplyChoice(Choice choice)
    {
        // ใช้ผลคะแนนจากช้อยที่ผู้เล่นเลือกไปเพิ่ม/ลดค่าความสัมพันธ์
        Player.AddAffection(CurrentCharacter, choice.AffectionChange);
        SaveSoon();
    }

    public void AddCoins(int amount)
    {
        // เพิ่มเหรียญจากรางวัลหรือมินิเกม แล้วเซฟตามทันที
        Coins += amount;
        SaveSoon();
    }

    public bool SpendCoins(int amount)
    {
        // หักเหรียญแบบเช็กก่อน ถ้าไม่พอจะไม่แตะยอดเงิน
        if (Coins < amount)
        {
            return false;
        }

        Coins -= amount;
        SaveSoon();
        return true;
    }

    public void AddExperience(int amount)
    {
        // เพิ่ม exp และวนเลเวลอัปจนกว่า exp จะไม่พอขึ้นเลเวลถัดไป
        if (Level >= MaxLevel)
        {
            Experience = 0;
            SaveSoon();
            return;
        }

        Experience += amount;

        while (Level < MaxLevel && Experience >= ExperienceToNextLevel)
        {
            Experience -= ExperienceToNextLevel;
            Level++;
            StatPoints += 3;
        }

        if (Level >= MaxLevel)
        {
            Experience = 0;
        }

        SaveSoon();
    }

    public int GetRuneCount(string runeId)
    {
        // อ่านจำนวน rune จาก inventory ถ้าไม่มีให้ถือว่า 0
        return Player.Inventory.TryGetValue(runeId, out var count) ? count : 0;
    }

    public bool UpgradeStat(string stat, out string message)
    {
        // ใช้ stat point อัปค่าสเตตัสหลักทีละขั้น พร้อมส่งข้อความให้ UI
        if (StatPoints <= 0)
        {
            message = "Stat Points ไม่พอ";
            return false;
        }

        switch (stat.ToLowerInvariant())
        {
            case "atk":
                if (AttackLevel >= MaxStatRank)
                {
                    message = "ATK เต็มแล้ว";
                    return false;
                }

                AttackLevel++;
                StatPoints--;
                message = "ATK Level +1";
                break;
            case "dff":
            case "def":
                if (DefenseLevel >= MaxStatRank)
                {
                    message = "DFF เต็มแล้ว";
                    return false;
                }

                DefenseLevel++;
                StatPoints--;
                message = "DFF Level +1";
                break;
            case "hp":
                if (HealthLevel >= MaxStatRank)
                {
                    message = "HP เต็มแล้ว";
                    return false;
                }

                HealthLevel++;
                StatPoints--;
                message = "HP Level +1";
                break;
            default:
                message = "ไม่พบค่าสเตตัสนี้";
                return false;
        }

        SaveSoon();
        return true;
    }

    public bool UseRune(string stat, out string message)
    {
        // ใช้ rune จากกระเป๋าเพื่อเพิ่ม bonus ถาวรของ stat นั้น
        var runeId = stat.ToLowerInvariant() switch
        {
            "atk" => AttackRuneId,
            "dff" or "def" => DefenseRuneId,
            "hp" => HealthRuneId,
            _ => ""
        };

        if (string.IsNullOrWhiteSpace(runeId))
        {
            message = "ไม่พบ Rune นี้";
            return false;
        }

        if (!Player.RemoveItem(runeId))
        {
            message = "ไม่มี Rune ชนิดนี้ในกระเป๋า";
            return false;
        }

        switch (runeId)
        {
            case AttackRuneId:
                RuneAttackBonus += 10;
                message = "ATK Rune bonus +10";
                break;
            case DefenseRuneId:
                RuneDefenseBonus += 5;
                message = "DFF Rune bonus +5";
                break;
            default:
                RuneHealthBonus += 50;
                message = "HP Rune bonus +50";
                break;
        }

        SaveSoon();
        return true;
    }

    public StageReward GrantStageRewards(int stageNumber)
    {
        // แจกเหรียญ exp และมีโอกาสดรอป rune หลังเคลียร์ด่าน battle
        var coins = 25 + (stageNumber * 15);
        var exp = 40 + (stageNumber * 20);
        string? runeId = null;

        Coins += coins;
        AddExperience(exp);

        if (_random.NextDouble() < 0.30)
        {
            runeId = _runeDropIds[_random.Next(_runeDropIds.Length)];
            Player.AddItem(runeId);
        }

        SaveSoon();

        return new StageReward(coins, exp, runeId, runeId == null ? "" : GetShopItem(runeId)?.Name ?? runeId);
    }

    public int GetCurrentAffection()
    {
        // คืน affection ของตัวละครที่กำลังเลือกอยู่ตอนนี้
        return Player.GetAffection(CurrentCharacter);
    }

    public IReadOnlyList<ShopItem> GetShopItems()
    {
        // ให้หน้า shop อ่านรายการสินค้าโดยไม่ต้องรู้รายละเอียดของ ShopService
        return _shopService.GetItems();
    }

    public ShopItem? GetShopItem(string itemId)
    {
        // หา item รายตัว ใช้ทั้งตอนซื้อและตอนแสดง inventory
        return _shopService.GetItem(itemId);
    }

    public bool BuyItem(string itemId, out string message)
    {
        // ซื้อของจากร้าน ถ้าเงินพอจะเพิ่มเข้ากระเป๋าและส่งข้อความสำเร็จกลับไป
        var item = _shopService.GetItem(itemId);

        if (item == null)
        {
            message = "ไม่พบไอเทมนี้";
            return false;
        }

        if (!SpendCoins(item.Price))
        {
            message = "Coins ไม่พอ";
            return false;
        }

        Player.AddItem(item.Id);
        message = $"ซื้อ {item.Name} สำเร็จ";
        return true;
    }

    public bool GiveGift(string characterName, string itemId, out string message)
    {
        // มอบของขวัญให้ตัวละคร แล้วคำนวณ affection/trust/happiness ตาม item
        var item = _shopService.GetItem(itemId);

        if (item == null)
        {
            message = "ไม่พบของขวัญนี้";
            return false;
        }

        if (item.IsRune)
        {
            message = "Rune ใช้สำหรับอัปค่าสเตตัสในหน้า Stats";
            return false;
        }

        if (!Player.RemoveItem(itemId))
        {
            message = "ไม่มีไอเทมนี้ในกระเป๋า";
            return false;
        }

        var characterState = Player.GetCharacterState(characterName);
        var affectionBonus = item.AffectionBonusByCharacter.TryGetValue(characterName, out var bonus)
            ? bonus
            : 1;

        characterState.Affection += affectionBonus;
        characterState.Happiness += item.HappinessBonus;
        characterState.Trust += item.TrustBonus;
        characterState.GiftCount++;

        message = $"{characterName} ได้รับ {item.Name}: \u2764\uFE0F +{affectionBonus}";
        SaveSoon();
        return true;
    }

    public void LoadFromSave(SaveData data)
    {
        // โหลดข้อมูลเก่ากลับเข้า service พร้อม clamp ค่าที่ไม่ควรติดลบหรือเกินเพดาน
        Player = data.Player ?? new PlayerState();
        CurrentCharacter = data.CurrentCharacter ?? "";
        Coins = data.Coins;
        Experience = data.Experience;
        Level = Math.Clamp(data.Level <= 0 ? 1 : data.Level, 1, MaxLevel);
        StatPoints = Math.Max(0, data.StatPoints);
        AttackLevel = Math.Clamp(data.AttackLevel, 0, MaxStatRank);
        DefenseLevel = Math.Clamp(data.DefenseLevel, 0, MaxStatRank);
        HealthLevel = Math.Clamp(data.HealthLevel, 0, MaxStatRank);
        RuneAttackBonus = Math.Max(0, data.RuneAttackBonus == 0 ? data.AttackBonus : data.RuneAttackBonus);
        RuneDefenseBonus = Math.Max(0, data.RuneDefenseBonus == 0 ? data.DefenseBonus : data.RuneDefenseBonus);
        RuneHealthBonus = Math.Max(0, data.RuneHealthBonus == 0 ? data.HealthBonus : data.RuneHealthBonus);
    }

    private void SaveSoon()
    {
        // fire-and-forget เพื่อไม่ให้ UI หน่วงทุกครั้งที่ค่าเล็ก ๆ เปลี่ยน
        _ = new SaveService().SaveAsync(this);
    }
}

public record StageReward(int Coins, int Experience, string? RuneId, string RuneName);
