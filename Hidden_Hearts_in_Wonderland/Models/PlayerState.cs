namespace Hidden_Hearts_in_Wonderland.Models;

public class PlayerState
{
    public Dictionary<string, CharacterState> CharacterStates { get; set; } = new();

    public Dictionary<string, int> Inventory { get; set; } = new();

    public CharacterState GetCharacterState(string character)
    {
        if (!CharacterStates.ContainsKey(character))
        {
            CharacterStates[character] = new CharacterState();
        }

        return CharacterStates[character];
    }

    public void AddAffection(string character, int amount)
    {
        GetCharacterState(character).Affection += amount;
    }

    public int GetAffection(string character)
    {
        return GetCharacterState(character).Affection;
    }

    public void AddTrust(string character, int amount)
    {
        GetCharacterState(character).Trust += amount;
    }

    public void AddHappiness(string character, int amount)
    {
        GetCharacterState(character).Happiness += amount;
    }

    public void AddItem(string itemId, int amount = 1)
    {
        if (!Inventory.ContainsKey(itemId))
        {
            Inventory[itemId] = 0;
        }

        Inventory[itemId] += amount;
    }

    public bool RemoveItem(string itemId, int amount = 1)
    {
        if (!Inventory.ContainsKey(itemId) || Inventory[itemId] < amount)
        {
            return false;
        }

        Inventory[itemId] -= amount;

        if (Inventory[itemId] <= 0)
        {
            Inventory.Remove(itemId);
        }

        return true;
    }
}

public class CharacterState
{
    public int Affection { get; set; }
    public int Trust { get; set; }
    public int Happiness { get; set; }
    public int GiftCount { get; set; }
}
