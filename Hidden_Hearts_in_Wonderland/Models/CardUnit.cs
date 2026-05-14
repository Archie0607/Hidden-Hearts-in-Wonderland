namespace Hidden_Hearts_in_Wonderland.Models;

public enum CardRole
{
    Balanced,
    Assassin,
    Archer,
    Support,
    Gunner,
    Mage,
    Tank,
    Monster,
    Boss
}

public class CardUnit
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ThaiName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public CardRole Role { get; set; }
    public int MaxHp { get; set; }
    public int Atk { get; set; }
    public int Def { get; set; }
    public bool IsMainHero { get; set; }
    public string? UnlockCharacter { get; set; }
}
