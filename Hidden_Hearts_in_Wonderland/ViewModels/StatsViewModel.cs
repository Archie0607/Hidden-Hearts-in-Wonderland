using System.Windows.Input;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.ViewModels;

public class StatsViewModel : BaseViewModel
{
    private readonly GameService _gameService = GameService.Instance;
    private string _message = "Use runes and stat points to power up your cards";

    public ICommand UpgradeAttackCommand { get; }
    public ICommand UpgradeDefenseCommand { get; }
    public ICommand UpgradeHealthCommand { get; }
    public ICommand UseAttackRuneCommand { get; }
    public ICommand UseDefenseRuneCommand { get; }
    public ICommand UseHealthRuneCommand { get; }
    public ICommand BackCommand { get; }

    public StatsViewModel()
    {
        // ผูก command ของปุ่มอัป stat และใช้ rune แต่ละชนิด
        UpgradeAttackCommand = new Command(() => Upgrade("atk"));
        UpgradeDefenseCommand = new Command(() => Upgrade("dff"));
        UpgradeHealthCommand = new Command(() => Upgrade("hp"));
        UseAttackRuneCommand = new Command(() => UseRune("atk"));
        UseDefenseRuneCommand = new Command(() => UseRune("dff"));
        UseHealthRuneCommand = new Command(() => UseRune("hp"));
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        Refresh();
    }

    public string Message
    {
        get => _message;
        set
        {
            _message = value;
            OnPropertyChanged();
        }
    }

    public string LevelText => $"Level {_gameService.Level}/{GameService.MaxLevel}";
    public string ExpText => _gameService.Level >= GameService.MaxLevel
        ? "MAX"
        : $"{_gameService.Experience}/{_gameService.ExperienceToNextLevel}";

    public double ExpProgress => _gameService.Level >= GameService.MaxLevel || _gameService.ExperienceToNextLevel == 0
        ? 1
        : Math.Clamp((double)_gameService.Experience / _gameService.ExperienceToNextLevel, 0, 1);

    public string CoinsText => _gameService.Coins.ToString();
    public string StatPointsText => $"Stat Points: {_gameService.StatPoints}";
    public string AttackText => $"Lv {_gameService.AttackLevel}/{GameService.MaxStatRank}  Main +{_gameService.AttackLevel * 2}  Rune +{_gameService.RuneAttackBonus}  Total +{_gameService.AttackBonus}";
    public string DefenseText => $"Lv {_gameService.DefenseLevel}/{GameService.MaxStatRank}  Main +{_gameService.DefenseLevel}  Rune +{_gameService.RuneDefenseBonus}  Total +{_gameService.DefenseBonus}";
    public string HealthText => $"Lv {_gameService.HealthLevel}/{GameService.MaxStatRank}  Main +{_gameService.HealthLevel * 10}  Rune +{_gameService.RuneHealthBonus}  Total +{_gameService.HealthBonus}";
    public string AttackRuneText => $"ATK Rune x{_gameService.GetRuneCount(GameService.AttackRuneId)}";
    public string DefenseRuneText => $"DFF Rune x{_gameService.GetRuneCount(GameService.DefenseRuneId)}";
    public string HealthRuneText => $"HP Rune x{_gameService.GetRuneCount(GameService.HealthRuneId)}";

    public void Refresh()
    {
        // แจ้ง UI ทุกค่าที่ขึ้นกับ stat/exp/coin ให้แสดงตัวเลขล่าสุด
        OnPropertyChanged(nameof(LevelText));
        OnPropertyChanged(nameof(ExpText));
        OnPropertyChanged(nameof(ExpProgress));
        OnPropertyChanged(nameof(CoinsText));
        OnPropertyChanged(nameof(StatPointsText));
        OnPropertyChanged(nameof(AttackText));
        OnPropertyChanged(nameof(DefenseText));
        OnPropertyChanged(nameof(HealthText));
        OnPropertyChanged(nameof(AttackRuneText));
        OnPropertyChanged(nameof(DefenseRuneText));
        OnPropertyChanged(nameof(HealthRuneText));
    }

    private void Upgrade(string stat)
    {
        // อัป stat ด้วย stat point แล้วเอาข้อความผลลัพธ์ขึ้นหน้าจอ
        _gameService.UpgradeStat(stat, out var message);
        Message = message;
        Refresh();
    }

    private void UseRune(string stat)
    {
        // ใช้ rune จาก inventory แล้วรีเฟรช bonus ที่แสดงในหน้า stats
        _gameService.UseRune(stat, out var message);
        Message = message;
        Refresh();
    }
}
