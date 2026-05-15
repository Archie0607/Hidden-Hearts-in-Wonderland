using Hidden_Hearts_in_Wonderland.Models;

namespace Hidden_Hearts_in_Wonderland.Services;

public class CardBattleService
{
    public static CardBattleService Instance { get; } = new();
    public const int RelationshipUnlockRequirement = 50;

    private readonly GameService _gameService = GameService.Instance;
    private readonly List<string> _selectedSupportCardIds = [];

    private readonly List<CardUnit> _playerCards =
    [
        new()
        {
            Id = "arthur",
            Name = "Arthur",
            ThaiName = "Arthur",
            Description = "Balanced swordsman",
            Image = "arthur.png",
            Role = CardRole.Balanced,
            MaxHp = 120,
            Atk = 24,
            Def = 10,
            IsMainHero = true
        },
        new()
        {
            Id = "lucas",
            Name = "Lucas",
            ThaiName = "Lucas",
            Description = "Assassin, applies poison",
            Image = "lucas.png",
            Role = CardRole.Assassin,
            MaxHp = 92,
            Atk = 28,
            Def = 6,
            IsMainHero = true
        },
        new()
        {
            Id = "elrond",
            Name = "Elrond",
            ThaiName = "Elrond",
            Description = "Archer, high damage and crit",
            Image = "elrond.png",
            Role = CardRole.Archer,
            MaxHp = 96,
            Atk = 31,
            Def = 5,
            IsMainHero = true
        },
        new()
        {
            Id = "ryne",
            Name = "Ryne",
            ThaiName = "Ryne",
            Description = "Support, heals allies",
            Image = "ryne_card.png",
            Role = CardRole.Support,
            MaxHp = 104,
            Atk = 18,
            Def = 8,
            UnlockCharacter = "Ryne"
        },
        new()
        {
            Id = "alice",
            Name = "Alice",
            ThaiName = "Alice",
            Description = "Gunner, heavy single shot",
            Image = "alice_card.png",
            Role = CardRole.Gunner,
            MaxHp = 88,
            Atk = 36,
            Def = 4,
            UnlockCharacter = "Alice"
        },
        new()
        {
            Id = "lana",
            Name = "Lana",
            ThaiName = "Lana",
            Description = "Mage, area attack",
            Image = "lana.png",
            Role = CardRole.Mage,
            MaxHp = 86,
            Atk = 22,
            Def = 5,
            UnlockCharacter = "Luna"
        },
        new()
        {
            Id = "raven",
            Name = "Raven",
            ThaiName = "Raven",
            Description = "Tank, taunts monsters",
            Image = "raven_card.png",
            Role = CardRole.Tank,
            MaxHp = 150,
            Atk = 17,
            Def = 15,
            UnlockCharacter = "Raven"
        }
    ];

    private readonly List<CardUnit> _enemyCards =
    [
        new() { Id = "goblin", Name = "Goblin", ThaiName = "Goblin", Description = "Small monster", Image = "goblin.png", Role = CardRole.Monster, MaxHp = 62, Atk = 15, Def = 4 },
        new() { Id = "lizard", Name = "Lizard", ThaiName = "Lizard", Description = "Stage 1 leader", Image = "lizard.png", Role = CardRole.Monster, MaxHp = 95, Atk = 19, Def = 8 },
        new() { Id = "oorc", Name = "Oorc", ThaiName = "Oorc", Description = "Orc minion", Image = "oorc.png", Role = CardRole.Monster, MaxHp = 78, Atk = 19, Def = 7 },
        new() { Id = "orc", Name = "Orc", ThaiName = "Orc", Description = "Stage 2 leader", Image = "orc.png", Role = CardRole.Monster, MaxHp = 130, Atk = 25, Def = 11 },
        new() { Id = "skeleton", Name = "Skeleton", ThaiName = "Skeleton", Description = "Bone soldier", Image = "skeleton.png", Role = CardRole.Monster, MaxHp = 86, Atk = 23, Def = 6 },
        new() { Id = "demon_minion", Name = "Demon Minion", ThaiName = "Demon Minion", Description = "Demon servant", Image = "demon_minion.png", Role = CardRole.Monster, MaxHp = 150, Atk = 29, Def = 12 },
        new() { Id = "loadmutatu", Name = "Loadmutatu", ThaiName = "Loadmutatu", Description = "Final boss", Image = "loadmutatu.png", Role = CardRole.Boss, MaxHp = 260, Atk = 35, Def = 16 }
    ];

    public IReadOnlyList<BattleStage> Stages { get; } =
    [
        new() { Number = 1, Name = "Stage 1", EnemyIds = ["goblin", "lizard", "goblin"] },
        new() { Number = 2, Name = "Stage 2", EnemyIds = ["oorc", "orc", "oorc"] },
        new() { Number = 3, Name = "Stage 3", EnemyIds = ["skeleton", "demon_minion", "skeleton"] },
        new() { Number = 4, Name = "Stage 4 Final Boss", EnemyIds = ["loadmutatu"] }
    ];

    public string SelectedHeroId { get; private set; } = "arthur";
    public int HighestUnlockedStageNumber { get; private set; } = 1;
    public IReadOnlyList<string> SelectedSupportCardIds => _selectedSupportCardIds;

    // คืนเฉพาะตัวหลักที่เลือกเป็นหัวหน้าทีมได้
    public IReadOnlyList<CardUnit> GetMainCards() => _playerCards.Where(card => card.IsMainHero).ToList();

    // คืนการ์ดซัพพอร์ตที่ต้องปลดล็อกด้วยค่าความสัมพันธ์
    public IReadOnlyList<CardUnit> GetUnlockableCards() => _playerCards.Where(card => !card.IsMainHero).ToList();

    public bool IsUnlocked(CardUnit card)
    {
        // การ์ดหลักเปิดทันที ส่วนซัพพอร์ตดูจาก affection ของตัวละครนั้น
        return card.UnlockCharacter == null || GetUnlockAffection(card) >= RelationshipUnlockRequirement;
    }

    public int GetUnlockAffection(CardUnit card)
    {
        // ถ้าไม่ต้องปลดล็อกก็ถือว่าไม่ต้องโชว์ affection เฉพาะตัว
        return card.UnlockCharacter == null ? 0 : _gameService.Player.GetAffection(card.UnlockCharacter);
    }

    public void SelectMainHero(string cardId)
    {
        // เปลี่ยนได้เฉพาะการ์ด main hero กัน id แปลก ๆ หลุดเข้าทีม
        if (GetMainCards().Any(card => string.Equals(card.Id, cardId, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedHeroId = cardId;
        }
    }

    public bool IsSelectedSupport(CardUnit card)
    {
        // เช็กว่า card นี้อยู่ในช่องซัพพอร์ตตอนนี้ไหม
        return _selectedSupportCardIds.Any(id => string.Equals(id, card.Id, StringComparison.OrdinalIgnoreCase));
    }

    public bool TryToggleSupportCard(string cardId, out string message)
    {
        // เพิ่ม/ถอดซัพพอร์ตในทีม พร้อมส่งข้อความกลับไปให้หน้า UI แสดงผล
        var card = GetPlayerCard(cardId);

        if (card.IsMainHero)
        {
            message = "Main hero is already in team";
            return false;
        }

        if (!IsUnlocked(card))
        {
            message = $"Need {RelationshipUnlockRequirement} affection with {card.Name}";
            return false;
        }

        var selectedId = _selectedSupportCardIds.FirstOrDefault(id => string.Equals(id, card.Id, StringComparison.OrdinalIgnoreCase));

        if (selectedId != null)
        {
            _selectedSupportCardIds.Remove(selectedId);
            message = $"{card.Name} removed";
            return true;
        }

        if (_selectedSupportCardIds.Count >= 2)
        {
            message = "Team uses 1 main hero + 2 support cards";
            return false;
        }

        _selectedSupportCardIds.Add(card.Id);
        message = $"{card.Name} joined";
        return true;
    }

    public IReadOnlyList<CardUnit> GetBattleTeamCards()
    {
        // ทีมจริงตอนเข้าฉากต่อสู้คือ main hero 1 ใบ และ support สูงสุด 2 ใบ
        var team = new List<CardUnit> { GetPlayerCard(SelectedHeroId) };

        foreach (var cardId in _selectedSupportCardIds.ToList())
        {
            if (team.Count >= 3)
            {
                break;
            }

            var card = GetPlayerCard(cardId);

            if (IsUnlocked(card))
            {
                team.Add(card);
            }
        }

        return team;
    }

    public bool IsStageUnlocked(int stageNumber)
    {
        // ด่านที่ยังไม่ถึงจะยังเลือกไม่ได้
        return stageNumber <= HighestUnlockedStageNumber;
    }

    public void MarkStageCleared(int stageNumber)
    {
        // ผ่านด่านแล้วปลดล็อกด่านถัดไป แต่ไม่ให้เลขเกินจำนวนด่านจริง
        HighestUnlockedStageNumber = Math.Max(HighestUnlockedStageNumber, Math.Min(stageNumber + 1, Stages.Count));
    }

    public CardUnit GetPlayerCard(string id)
    {
        // หา card ฝั่งผู้เล่นจาก id แบบไม่สนตัวพิมพ์เล็กใหญ่
        return _playerCards.First(card => string.Equals(card.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public CardUnit GetEnemyCard(string id)
    {
        // หา monster card จาก id ของด่าน
        return _enemyCards.First(card => string.Equals(card.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
