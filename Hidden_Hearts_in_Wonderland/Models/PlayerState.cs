namespace Hidden_Hearts_in_Wonderland.Models;

public class PlayerState
{
    public Dictionary<string, CharacterState> CharacterStates { get; set; } = new();

    public Dictionary<string, int> Inventory { get; set; } = new();

    public CharacterState GetCharacterState(string character)
    {
        // ถ้ายังไม่เคยมีข้อมูลของตัวละครนี้ ให้สร้าง state ใหม่ให้ทันที
        if (!CharacterStates.ContainsKey(character))
        {
            CharacterStates[character] = new CharacterState();
        }

        return CharacterStates[character];
    }

    public void AddAffection(string character, int amount)
    {
        // เพิ่มหรือลดค่าความสัมพันธ์ของตัวละคร
        GetCharacterState(character).Affection += amount;
    }

    public int GetAffection(string character)
    {
        // อ่านค่าความสัมพันธ์ของตัวละคร ถ้ายังไม่มีข้อมูลจะสร้างให้ก่อน
        return GetCharacterState(character).Affection;
    }

    public void AddTrust(string character, int amount)
    {
        // เพิ่มค่า trust ของตัวละคร
        GetCharacterState(character).Trust += amount;
    }

    public void AddHappiness(string character, int amount)
    {
        // เพิ่มค่า happiness ของตัวละคร
        GetCharacterState(character).Happiness += amount;
    }

    public void AddItem(string itemId, int amount = 1)
    {
        // เพิ่ม item เข้ากระเป๋า ถ้ายังไม่มี key นี้ก็เริ่มจาก 0 ก่อน
        if (!Inventory.ContainsKey(itemId))
        {
            Inventory[itemId] = 0;
        }

        Inventory[itemId] += amount;
    }

    public bool RemoveItem(string itemId, int amount = 1)
    {
        // ลบ item จากกระเป๋า ถ้าจำนวนไม่พอจะคืน false และไม่เปลี่ยนอะไร
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
