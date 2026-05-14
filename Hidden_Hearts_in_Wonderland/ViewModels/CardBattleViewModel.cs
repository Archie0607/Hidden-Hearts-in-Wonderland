using System.Collections.ObjectModel;
using System.Windows.Input;
using Hidden_Hearts_in_Wonderland.Models;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.ViewModels;

[QueryProperty(nameof(StageNumber), "stage")]
public class CardBattleViewModel : BaseViewModel
{
    private readonly CardBattleService _cardService = CardBattleService.Instance;
    private readonly Random _random = new();
    private int _stageIndex;
    private int _stageNumber = 1;
    private BattleFighter? _selectedPlayer;
    private string _message = "Select your card, then select a monster";
    private bool _isBattleEnded;
    private bool _isLoaded;

    public ObservableCollection<BattleFighter> PlayerTeam { get; } = [];
    public ObservableCollection<BattleFighter> EnemyTeam { get; } = [];

    public ICommand SelectPlayerCommand { get; }
    public ICommand AttackEnemyCommand { get; }
    public ICommand NextStageCommand { get; }
    public ICommand RestartCommand { get; }
    public ICommand BackCommand { get; }

    public string Message
    {
        get => _message;
        set
        {
            _message = value;
            OnPropertyChanged();
        }
    }

    public int StageNumber
    {
        get => _stageNumber;
        set
        {
            _stageNumber = Math.Clamp(value, 1, _cardService.Stages.Count);

            if (_isLoaded)
            {
                BuildPlayerTeam();
                LoadStage(_stageNumber - 1);
            }
        }
    }

    public string StageText => $"{CurrentStage.Name} / 4";

    public bool CanGoNext => _isBattleEnded
        && EnemyTeam.All(enemy => !enemy.IsAlive)
        && _stageIndex < _cardService.Stages.Count - 1
        && _cardService.IsStageUnlocked(_stageIndex + 2);

    public string NextButtonText => _stageIndex >= _cardService.Stages.Count - 1 ? "Finished" : "Next stage";

    private BattleStage CurrentStage => _cardService.Stages[_stageIndex];

    public CardBattleViewModel()
    {
        SelectPlayerCommand = new Command<BattleFighter>(SelectPlayer);
        AttackEnemyCommand = new Command<BattleFighter>(async enemy => await AttackEnemy(enemy));
        NextStageCommand = new Command(NextStage);
        RestartCommand = new Command(Restart);
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));

        BuildPlayerTeam();
        LoadStage(StageNumber - 1);
        _isLoaded = true;
    }

    private void BuildPlayerTeam()
    {
        PlayerTeam.Clear();

        foreach (var card in _cardService.GetBattleTeamCards())
        {
            PlayerTeam.Add(new BattleFighter(card, false));
        }
    }

    private void LoadStage(int stageIndex)
    {
        _stageIndex = Math.Clamp(stageIndex, 0, _cardService.Stages.Count - 1);
        _selectedPlayer = null;
        _isBattleEnded = false;
        EnemyTeam.Clear();

        foreach (var enemyId in CurrentStage.EnemyIds)
        {
            EnemyTeam.Add(new BattleFighter(_cardService.GetEnemyCard(enemyId), true));
        }

        foreach (var fighter in PlayerTeam)
        {
            fighter.ResetTurnState();
            fighter.IsSelected = false;
        }

        Message = $"{CurrentStage.Name}: your turn";
        OnPropertyChanged(nameof(StageText));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(NextButtonText));
    }

    private void SelectPlayer(BattleFighter? fighter)
    {
        if (fighter == null || !fighter.IsAlive || fighter.HasActed || _isBattleEnded)
        {
            return;
        }

        foreach (var player in PlayerTeam)
        {
            player.IsSelected = false;
        }

        _selectedPlayer = fighter;
        fighter.IsSelected = true;
        Message = fighter.Role == CardRole.Support
            ? $"{fighter.Name} is ready to heal. Tap a monster to spend the turn."
            : $"{fighter.Name} is ready. Choose a target.";
    }

    private async Task AttackEnemy(BattleFighter? enemy)
    {
        if (_selectedPlayer == null || enemy == null || !enemy.IsAlive || _selectedPlayer.HasActed || _isBattleEnded)
        {
            return;
        }

        ExecutePlayerAction(_selectedPlayer, enemy);
        _selectedPlayer.HasActed = true;
        _selectedPlayer.IsSelected = false;
        _selectedPlayer = null;

        await CheckBattleProgress();
    }

    private void ExecutePlayerAction(BattleFighter attacker, BattleFighter target)
    {
        switch (attacker.Role)
        {
            case CardRole.Support:
                var ally = PlayerTeam.Where(player => player.IsAlive).OrderBy(player => player.HpPercent).First();
                ally.Heal(30);
                Message = $"{attacker.Name} heals {ally.Name} +30";
                break;
            case CardRole.Mage:
                foreach (var enemy in EnemyTeam.Where(enemy => enemy.IsAlive))
                {
                    enemy.TakeDamage(CalculateDamage(attacker, enemy, 0.75));
                }
                Message = $"{attacker.Name} casts area magic";
                break;
            case CardRole.Assassin:
                target.TakeDamage(CalculateDamage(attacker, target));
                target.PoisonTurns = 3;
                Message = $"{attacker.Name} poisons {target.Name}";
                break;
            case CardRole.Archer:
                var isCrit = _random.NextDouble() < 0.35;
                target.TakeDamage(CalculateDamage(attacker, target, isCrit ? 2 : 1));
                Message = isCrit ? $"{attacker.Name} lands a critical hit" : $"{attacker.Name} shoots {target.Name}";
                break;
            case CardRole.Tank:
                target.TakeDamage(CalculateDamage(attacker, target));
                attacker.TauntTurns = 1;
                Message = $"{attacker.Name} taunts the monsters";
                break;
            default:
                target.TakeDamage(CalculateDamage(attacker, target));
                Message = $"{attacker.Name} attacks {target.Name}";
                break;
        }
    }

    private async Task CheckBattleProgress()
    {
        if (EnemyTeam.All(enemy => !enemy.IsAlive))
        {
            WinStage();
            return;
        }

        await Task.Delay(350);
        EnemyTurn();
    }

    private void EnemyTurn()
    {
        foreach (var enemy in EnemyTeam.Where(enemy => enemy.IsAlive).ToList())
        {
            if (enemy.PoisonTurns <= 0)
            {
                continue;
            }

            enemy.TakeDamage(8);
            enemy.PoisonTurns--;
        }

        if (EnemyTeam.All(enemy => !enemy.IsAlive))
        {
            WinStage();
            return;
        }

        var attacker = PickEnemyAttacker();
        var target = PickEnemyTarget();

        if (attacker != null && target != null)
        {
            var damage = CalculateDamage(attacker, target);
            target.TakeDamage(damage);
            Message = $"{attacker.Name} hits {target.Name} for {damage}";
        }

        foreach (var player in PlayerTeam)
        {
            player.HasActed = false;

            if (player.TauntTurns > 0)
            {
                player.TauntTurns--;
            }
        }

        if (PlayerTeam.All(player => !player.IsAlive))
        {
            _isBattleEnded = true;
            Message = "Defeated. Restart the stage or choose another stage.";
            OnPropertyChanged(nameof(CanGoNext));
            return;
        }
    }

    private void WinStage()
    {
        _isBattleEnded = true;
        _cardService.MarkStageCleared(CurrentStage.Number);
        Message = _stageIndex == _cardService.Stages.Count - 1 ? "Final boss defeated" : "Stage cleared";
        OnPropertyChanged(nameof(CanGoNext));
    }

    private BattleFighter? PickEnemyTarget()
    {
        var tauntTarget = PlayerTeam.FirstOrDefault(player => player.IsAlive && player.TauntTurns > 0);
        if (tauntTarget != null)
        {
            return tauntTarget;
        }

        var targets = PlayerTeam.Where(player => player.IsAlive).ToList();
        return targets.Count == 0 ? null : targets[_random.Next(targets.Count)];
    }

    private BattleFighter? PickEnemyAttacker()
    {
        var attackers = EnemyTeam.Where(enemy => enemy.IsAlive).ToList();
        return attackers.Count == 0 ? null : attackers[_random.Next(attackers.Count)];
    }

    private static int CalculateDamage(BattleFighter attacker, BattleFighter target, double multiplier = 1)
    {
        return Math.Max(1, (int)Math.Round((attacker.Atk * multiplier) - target.Def));
    }

    private void NextStage()
    {
        if (!CanGoNext)
        {
            return;
        }

        StageNumber = _stageIndex + 2;
    }

    private void Restart()
    {
        BuildPlayerTeam();
        LoadStage(StageNumber - 1);
    }
}

public class BattleFighter : BaseViewModel
{
    private int _hp;
    private bool _isSelected;
    private bool _hasActed;
    private int _poisonTurns;
    private int _tauntTurns;

    public BattleFighter(CardUnit card, bool isEnemy)
    {
        Id = card.Id;
        Name = card.Name;
        Description = card.Description;
        Image = card.Image;
        Role = card.Role;
        MaxHp = card.MaxHp;
        Atk = card.Atk;
        Def = card.Def;
        IsEnemy = isEnemy;
        _hp = MaxHp;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string Image { get; }
    public CardRole Role { get; }
    public int MaxHp { get; }
    public int Atk { get; }
    public int Def { get; }
    public bool IsEnemy { get; }
    public bool IsAlive => Hp > 0;
    public double HpPercent => MaxHp == 0 ? 0 : (double)Hp / MaxHp;
    public double HpBarWidth => Math.Max(0, HpPercent * 92);
    public double AtkBarWidth => Math.Clamp(Atk * 2.2, 8, 92);
    public double DefBarWidth => Math.Clamp(Def * 5.2, 8, 92);
    public double HpMiniBarWidth => Math.Max(0, HpPercent * 22);
    public double AtkMiniBarWidth => Math.Clamp(Atk * 0.55, 4, 22);
    public double DefMiniBarWidth => Math.Clamp(Def * 1.25, 4, 22);
    public string StatText => $"HP {Hp}/{MaxHp}  ATK {Atk}  DEF {Def}";
    public string StatusText => PoisonTurns > 0 ? $"Poison {PoisonTurns}" : TauntTurns > 0 ? "Taunt" : HasActed ? "Acted" : string.Empty;
    public double Opacity => IsAlive ? HasActed ? 0.62 : 1 : 0.35;
    public Color BorderColor => IsSelected ? Color.FromArgb("#FFB84D") : IsEnemy ? Color.FromArgb("#E07A7A") : Color.FromArgb("#6E9EEB");
    public Color BackgroundColor => IsEnemy ? Color.FromArgb("#FFF0F0") : Color.FromArgb("#F1F6FF");

    public int Hp
    {
        get => _hp;
        private set
        {
            _hp = Math.Clamp(value, 0, MaxHp);
            OnVitalsChanged();
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BorderColor));
        }
    }

    public bool HasActed
    {
        get => _hasActed;
        set
        {
            _hasActed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(Opacity));
        }
    }

    public int PoisonTurns
    {
        get => _poisonTurns;
        set
        {
            _poisonTurns = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public int TauntTurns
    {
        get => _tauntTurns;
        set
        {
            _tauntTurns = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public void TakeDamage(int amount)
    {
        Hp -= amount;
    }

    public void Heal(int amount)
    {
        Hp += amount;
    }

    public void ResetTurnState()
    {
        HasActed = false;
        PoisonTurns = 0;
        TauntTurns = 0;
    }

    private void OnVitalsChanged()
    {
        OnPropertyChanged(nameof(Hp));
        OnPropertyChanged(nameof(IsAlive));
        OnPropertyChanged(nameof(HpPercent));
        OnPropertyChanged(nameof(HpBarWidth));
        OnPropertyChanged(nameof(HpMiniBarWidth));
        OnPropertyChanged(nameof(StatText));
        OnPropertyChanged(nameof(Opacity));
    }
}
