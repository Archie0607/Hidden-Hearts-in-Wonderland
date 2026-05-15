namespace Hidden_Hearts_in_Wonderland.Models;

public class ShopItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Image { get; set; } = "";
    public int Price { get; set; }
    public bool IsRune { get; set; }
    public string RuneStat { get; set; } = "";
    public int HappinessBonus { get; set; }
    public int TrustBonus { get; set; }
    public Dictionary<string, int> AffectionBonusByCharacter { get; set; } = new();
}
