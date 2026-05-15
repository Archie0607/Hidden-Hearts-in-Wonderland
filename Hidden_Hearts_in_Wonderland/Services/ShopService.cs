using Hidden_Hearts_in_Wonderland.Models;

namespace Hidden_Hearts_in_Wonderland.Services;

public class ShopService
{
    private readonly List<ShopItem> _items =
    [
        new ShopItem
        {
            Id = "warm_tea",
            Name = "Warm Tea",
            Description = "A gentle drink for quiet moments.",
            Image = "tea.png",
            Price = 20,
            HappinessBonus = 2,
            TrustBonus = 1,
            AffectionBonusByCharacter = new()
            {
                ["Luna"] = 6,
                ["Alice"] = 2,
                ["Ryne"] = 2,
                ["Raven"] = 3
            }
        },
        new ShopItem
        {
            Id = "storybook",
            Name = "Story Book",
            Description = "A small book filled with sweet stories.",
            Image = "book.png",
            Price = 45,
            HappinessBonus = 3,
            TrustBonus = 2,
            AffectionBonusByCharacter = new()
            {
                ["Luna"] = 8,
                ["Alice"] = 4,
                ["Ryne"] = 2,
                ["Raven"] = 5
            }
        },
        new ShopItem
        {
            Id = "adventure_charm",
            Name = "Adventure Charm",
            Description = "A bright charm for someone brave.",
            Image = "charm.png",
            Price = 55,
            HappinessBonus = 4,
            TrustBonus = 2,
            AffectionBonusByCharacter = new()
            {
                ["Luna"] = 2,
                ["Alice"] = 5,
                ["Ryne"] = 8,
                ["Raven"] = 3
            }
        },
        new ShopItem
        {
            Id = "moon_ribbon",
            Name = "Moon Ribbon",
            Description = "A soft ribbon with a moonlit shine.",
            Image = "ribbon.png",
            Price = 60,
            HappinessBonus = 4,
            TrustBonus = 3,
            AffectionBonusByCharacter = new()
            {
                ["Luna"] = 4,
                ["Alice"] = 5,
                ["Ryne"] = 3,
                ["Raven"] = 8
            }
        },
        new ShopItem
        {
            Id = GameService.AttackRuneId,
            Name = "ATK Rune",
            Description = "Use in Stats to increase attack.",
            Image = "atk.png",
            Price = 300,
            IsRune = true,
            RuneStat = "atk"
        },
        new ShopItem
        {
            Id = GameService.DefenseRuneId,
            Name = "DFF Rune",
            Description = "Use in Stats to increase defense.",
            Image = "dff.png",
            Price = 300,
            IsRune = true,
            RuneStat = "dff"
        },
        new ShopItem
        {
            Id = GameService.HealthRuneId,
            Name = "HP Rune",
            Description = "Use in Stats to increase max HP.",
            Image = "hp.png",
            Price = 350,
            IsRune = true,
            RuneStat = "hp"
        }
    ];

    public IReadOnlyList<ShopItem> GetItems()
    {
        return _items;
    }

    public ShopItem? GetItem(string itemId)
    {
        return _items.FirstOrDefault(item => item.Id == itemId);
    }
}
