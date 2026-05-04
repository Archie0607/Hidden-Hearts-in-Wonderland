using Hidden_Hearts_in_Wonderland.Models;

namespace Hidden_Hearts_in_Wonderland.Services;

public class GameService
{
    public static GameService Instance { get; } = new GameService();

    public PlayerState Player { get; set; } = new();

    public string CurrentCharacter { get; set; }

    public void SelectCharacter(string characterName)
    {
        CurrentCharacter = characterName;
    }

    public void ApplyChoice(Choice choice)
    {
        Player.AddAffection(CurrentCharacter, choice.AffectionChange);
    }

    public int GetCurrentAffection()
    {
        return Player.GetAffection(CurrentCharacter);
    }
}