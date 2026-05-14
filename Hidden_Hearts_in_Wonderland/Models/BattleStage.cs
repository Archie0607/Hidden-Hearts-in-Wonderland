namespace Hidden_Hearts_in_Wonderland.Models;

public class BattleStage
{
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> EnemyIds { get; set; } = [];
}
