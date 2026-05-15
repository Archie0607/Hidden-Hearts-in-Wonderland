using System.Collections.ObjectModel;
using System.Windows.Input;
using Hidden_Hearts_in_Wonderland.Models;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.ViewModels;

[QueryProperty(nameof(StageNumber), "stage")]
public class CardBattleViewModel : BaseViewModel
{
    private readonly CardBattleService _cardService = CardBattleService.Instance;
    private readonly GameService _gameService = GameService.Instance;
    private readonly Random _random = new();
    private int _stageIndex;
    private int _stageNumber = 1;
    private BattleFighter? _selectedPlayer;
    private string _message = "Select your card, then select a monster";
    private bool _isRewardPopupVisible;
    private string _rewardTitle = "";
    private string _rewardCoinsText = "";
    private string _rewardExpText = "";
    private string _rewardRuneText = "";
    private bool _isDefeatPopupVisible;
    private bool _isBattleEnded;
    private bool _isLoaded;

    public ObservableCollection<BattleFighter> PlayerTeam { get; } = [];
    public ObservableCollection<BattleFighter> EnemyTeam { get; } = [];
    public Func<BattleFighter, BattleFighter, Task>? AnimateAttackAsync { get; set; }

    public ICommand SelectPlayerCommand { get; }
    public ICommand AttackEnemyCommand { get; }
    public ICommand NextStageCommand { get; }
    public ICommand StageSelectCommand { get; }
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
    public string StageBackgroundImage => $"bgstate_{CurrentStage.Number}.png";

    public bool CanGoNext => _isBattleEnded
        && EnemyTeam.All(enemy => !enemy.IsAlive)
        && _stageIndex < _cardService.Stages.Count - 1
        && _cardService.IsStageUnlocked(_stageIndex + 2);

    public bool IsRewardPopupVisible
    {
        get => _isRewardPopupVisible;
        set
        {
            _isRewardPopupVisible = value;
            OnPropertyChanged();
        }
    }

    public bool IsDefeatPopupVisible
    {
        get => _isDefeatPopupVisible;
        set
        {
            _isDefeatPopupVisible = value;
            OnPropertyChanged();
        }
    }

    public string RewardTitle
    {
        get => _rewardTitle;
        set
        {
            _rewardTitle = value;
            OnPropertyChanged();
        }
    }

    public string RewardCoinsText
    {
        get => _rewardCoinsText;
        set
        {
            _rewardCoinsText = value;
            OnPropertyChanged();
        }
    }

    public string RewardExpText
    {
        get => _rewardExpText;
        set
        {
            _rewardExpText = value;
            OnPropertyChanged();
        }
    }

    public string RewardRuneText
    {
        get => _rewardRuneText;
        set
        {
            _rewardRuneText = value;
            OnPropertyChanged();
        }
    }

    public string NextButtonText => _stageIndex >= _cardService.Stages.Count - 1 ? "Finished" : "Next stage";

    private BattleStage CurrentStage => _cardService.Stages[_stageIndex];

    public CardBattleViewModel()
    {
        // เตรียม command ทั้งหมดของหน้าต่อสู้ แล้วโหลดทีมกับด่านแรกเข้ามา
        SelectPlayerCommand = new Command<BattleFighter>(SelectPlayer);
        AttackEnemyCommand = new Command<BattleFighter>(async enemy => await AttackEnemy(enemy));
        NextStageCommand = new Command(NextStage);
        StageSelectCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        RestartCommand = new Command(Restart);
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));

        BuildPlayerTeam();
        LoadStage(StageNumber - 1);
        _isLoaded = true;
    }

    private void BuildPlayerTeam()
    {
        // สร้างทีมผู้เล่นจาก hero หลักและ support ที่เลือกไว้ พร้อมบวก stat bonus จาก GameService
        PlayerTeam.Clear();

        var teamCards = _cardService.GetBattleTeamCards().ToList();

        if (teamCards.Count == 0)
        {
            return;
        }

        var mainCard = teamCards[0];
        var supportCards = teamCards.Skip(1).ToList();

        if (supportCards.Count > 0)
        {
            PlayerTeam.Add(new BattleFighter(supportCards[0], false, _gameService.AttackBonus, _gameService.DefenseBonus, _gameService.HealthBonus));
        }

        PlayerTeam.Add(new BattleFighter(mainCard, false, _gameService.AttackBonus, _gameService.DefenseBonus, _gameService.HealthBonus, true));

        if (supportCards.Count > 1)
        {
            PlayerTeam.Add(new BattleFighter(supportCards[1], false, _gameService.AttackBonus, _gameService.DefenseBonus, _gameService.HealthBonus));
        }
    }

    private void LoadStage(int stageIndex)
    {
        // โหลดศัตรูของด่าน รีเซ็ตสถานะเทิร์น และอัปเดตข้อความบนจอ
        _stageIndex = Math.Clamp(stageIndex, 0, _cardService.Stages.Count - 1);
        _selectedPlayer = null;
        _isBattleEnded = false;
        IsRewardPopupVisible = false;
        IsDefeatPopupVisible = false;
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
        OnPropertyChanged(nameof(StageBackgroundImage));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(NextButtonText));
    }

    private void SelectPlayer(BattleFighter? fighter)
    {
        // เลือกการ์ดฝ่ายเราเพื่อเตรียมโจมตี ถ้าตายหรือใช้เทิร์นแล้วจะกดไม่ได้
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
        // ใช้ตัวที่เลือกไว้โจมตีศัตรู แล้วให้ระบบเช็กว่าจะจบด่านหรือถึงเทิร์นศัตรูไหม
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
        // แยกผลของแต่ละ role เช่น support ฮีล mage โจมตีหมู่ assassin ใส่พิษ
        switch (attacker.Role)
        {
            case CardRole.Support:
                var ally = PlayerTeam.Where(player => player.IsAlive).OrderBy(player => player.HpPercent).First();
                ally.Heal(30);
                ally.ShowFloatingText("+30", Color.FromArgb("#62E68A"));
                Message = $"{attacker.Name} heals {ally.Name} +30";
                break;
            case CardRole.Mage:
                foreach (var enemy in EnemyTeam.Where(enemy => enemy.IsAlive))
                {
                    var mageDamage = CalculateDamage(attacker, enemy, 0.75);
                    enemy.TakeDamage(mageDamage);
                    enemy.ShowFloatingText($"-{mageDamage}", Color.FromArgb("#FF6B6B"));
                }
                Message = $"{attacker.Name} casts area magic";
                break;
            case CardRole.Assassin:
                var poisonDamage = CalculateDamage(attacker, target);
                target.TakeDamage(poisonDamage);
                target.ShowFloatingText($"-{poisonDamage}", Color.FromArgb("#FF6B6B"));
                target.PoisonTurns = 3;
                Message = $"{attacker.Name} poisons {target.Name} for {poisonDamage}";
                break;
            case CardRole.Archer:
                var isCrit = _random.NextDouble() < 0.35;
                var arrowDamage = CalculateDamage(attacker, target, isCrit ? 2 : 1);
                target.TakeDamage(arrowDamage);
                target.ShowFloatingText($"-{arrowDamage}", isCrit ? Color.FromArgb("#FFD45A") : Color.FromArgb("#FF6B6B"));
                Message = isCrit ? $"{attacker.Name} crits {target.Name} for {arrowDamage}" : $"{attacker.Name} shoots {target.Name} for {arrowDamage}";
                break;
            case CardRole.Tank:
                var tankDamage = CalculateDamage(attacker, target);
                target.TakeDamage(tankDamage);
                target.ShowFloatingText($"-{tankDamage}", Color.FromArgb("#FF6B6B"));
                attacker.TauntTurns = 1;
                Message = $"{attacker.Name} hits {target.Name} for {tankDamage} and taunts";
                break;
            default:
                var damage = CalculateDamage(attacker, target);
                target.TakeDamage(damage);
                target.ShowFloatingText($"-{damage}", Color.FromArgb("#FF6B6B"));
                Message = $"{attacker.Name} attacks {target.Name} for {damage}";
                break;
        }
    }

    private async Task CheckBattleProgress()
    {
        // หลังผู้เล่นลงมือ ถ้าศัตรูหมดคือชนะ ไม่งั้นรอเล็กน้อยแล้วให้ศัตรูสวน
        if (EnemyTeam.All(enemy => !enemy.IsAlive))
        {
            WinStage();
            return;
        }

        await Task.Delay(350);
        await EnemyTurn();
    }

    private async Task EnemyTurn()
    {
        // เทิร์นศัตรูเริ่มจากคิดพิษก่อน แล้วค่อยสุ่มตัวโจมตีกับเป้าหมาย
        foreach (var enemy in EnemyTeam.Where(enemy => enemy.IsAlive).ToList())
        {
            if (enemy.PoisonTurns <= 0)
            {
                continue;
            }

            enemy.TakeDamage(8);
            enemy.ShowFloatingText("-8", Color.FromArgb("#A86BFF"));
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
            if (AnimateAttackAsync != null)
            {
                await AnimateAttackAsync(attacker, target);
            }

            target.TakeDamage(damage);
            target.ShowFloatingText($"-{damage}", Color.FromArgb("#FF6B6B"));
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
            RewardTitle = "Defeated";
            RewardCoinsText = "0";
            RewardExpText = "0";
            RewardRuneText = "Try again";
            IsDefeatPopupVisible = true;
            IsRewardPopupVisible = true;
            _ = AudioService.Instance.PlayGameOverAsync();
            Message = "Defeated. Restart the stage or choose another stage.";
            OnPropertyChanged(nameof(CanGoNext));
            return;
        }
    }

    private void WinStage()
    {
        // เคลียร์ด่าน แจกของรางวัล ปลดล็อกด่านถัดไป และโชว์ popup สรุปผล
        _isBattleEnded = true;
        _cardService.MarkStageCleared(CurrentStage.Number);
        var reward = _gameService.GrantStageRewards(CurrentStage.Number);
        var dropText = string.IsNullOrWhiteSpace(reward.RuneName) ? "no rune drop" : $"{reward.RuneName} dropped";
        RewardTitle = _stageIndex == _cardService.Stages.Count - 1 ? "Final boss defeated" : "Stage cleared";
        RewardCoinsText = $"+{reward.Coins}";
        RewardExpText = $"+{reward.Experience}";
        RewardRuneText = string.IsNullOrWhiteSpace(reward.RuneName) ? "Rune: none" : $"Rune: {reward.RuneName}";
        IsDefeatPopupVisible = false;
        IsRewardPopupVisible = true;
        _ = AudioService.Instance.PlayWinAsync();
        Message = _stageIndex == _cardService.Stages.Count - 1
            ? $"Final boss defeated +{reward.Coins} coins +{reward.Experience} EXP, {dropText}"
            : $"Stage cleared +{reward.Coins} coins +{reward.Experience} EXP, {dropText}";
        OnPropertyChanged(nameof(CanGoNext));
    }

    private BattleFighter? PickEnemyTarget()
    {
        // ถ้ามีตัวที่ taunt อยู่ ศัตรูต้องตีตัวนั้นก่อน ไม่งั้นสุ่มจากตัวที่ยังรอด
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
        // เลือกศัตรูที่ยังมีชีวิตขึ้นมาเป็นคนโจมตี
        var attackers = EnemyTeam.Where(enemy => enemy.IsAlive).ToList();
        return attackers.Count == 0 ? null : attackers[_random.Next(attackers.Count)];
    }

    private static int CalculateDamage(BattleFighter attacker, BattleFighter target, double multiplier = 1)
    {
        // สูตร damage อย่างง่าย: atk ลบ def และอย่างน้อยต้องเข้า 1
        return Math.Max(1, (int)Math.Round((attacker.Atk * multiplier) - target.Def));
    }

    private void NextStage()
    {
        // ไปด่านถัดไปได้เฉพาะตอนชนะและด่านนั้นปลดล็อกแล้ว
        if (!CanGoNext)
        {
            return;
        }

        StageNumber = _stageIndex + 2;
        _ = AudioService.Instance.PlayBattleMusicOnceAsync();
    }

    private void Restart()
    {
        // เริ่มด่านเดิมใหม่พร้อม rebuild ทีม เผื่อ stat หรือทีมเปลี่ยน
        BuildPlayerTeam();
        LoadStage(StageNumber - 1);
        _ = AudioService.Instance.PlayBattleMusicOnceAsync();
    }
}

public class BattleFighter : BaseViewModel
{
    private int _hp;
    private bool _isSelected;
    private bool _hasActed;
    private int _poisonTurns;
    private int _tauntTurns;
    private string _floatingText = "";
    private bool _isFloatingTextVisible;
    private Color _floatingTextColor = Color.FromArgb("#FF6B6B");

    public BattleFighter(CardUnit card, bool isEnemy, int attackBonus = 0, int defenseBonus = 0, int healthBonus = 0, bool isPrimary = false)
    {
        // แปลงข้อมูลการ์ดเป็นตัวละครที่ลงสนามจริง พร้อม bonus ฝั่งผู้เล่น
        Id = card.Id;
        Name = card.Name;
        Description = card.Description;
        Image = card.Image;
        Role = card.Role;
        MaxHp = card.MaxHp + (isEnemy ? 0 : healthBonus);
        Atk = card.Atk + (isEnemy ? 0 : attackBonus);
        Def = card.Def + (isEnemy ? 0 : defenseBonus);
        IsEnemy = isEnemy;
        IsPrimary = isPrimary;
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
    public bool IsPrimary { get; }
    public bool IsAlive => Hp > 0;
    public double HpPercent => MaxHp == 0 ? 0 : (double)Hp / MaxHp;
    public double HpBarWidth => Math.Max(0, HpPercent * 92);
    public double AtkBarWidth => Math.Clamp(Atk * 2.2, 8, 92);
    public double DefBarWidth => Math.Clamp(Def * 5.2, 8, 92);
    public double HpMiniBarWidth => Math.Max(0, HpPercent * 22);
    public double AtkMiniBarWidth => Math.Clamp(Atk * 0.55, 4, 22);
    public double DefMiniBarWidth => Math.Clamp(Def * 1.25, 4, 22);
    public double CardWidth => IsEnemy ? 112 : IsPrimary ? 142 : 96;
    public double CardHeight => IsEnemy ? 136 : IsPrimary ? 176 : 132;
    public double StatColumnWidth => IsEnemy ? 24 : IsPrimary ? 28 : 22;
    public double ImageColumnWidth => Math.Max(80, CardWidth - StatColumnWidth - 6);
    public double ImageHeight => IsEnemy ? 116 : IsPrimary ? 154 : 110;
    public double ImageWidth => IsEnemy ? 86 : IsPrimary ? 110 : 74;
    public double StatusFontSize => IsPrimary ? 10 : 9;
    public string StatText => $"HP {Hp}/{MaxHp}  ATK {Atk}  DEF {Def}";
    public string StatusText => PoisonTurns > 0 ? $"Poison {PoisonTurns}" : TauntTurns > 0 ? "Taunt" : HasActed ? "Acted" : string.Empty;
    public string FloatingText
    {
        get => _floatingText;
        private set
        {
            _floatingText = value;
            OnPropertyChanged();
        }
    }

    public bool IsFloatingTextVisible
    {
        get => _isFloatingTextVisible;
        private set
        {
            _isFloatingTextVisible = value;
            OnPropertyChanged();
        }
    }

    public Color FloatingTextColor
    {
        get => _floatingTextColor;
        private set
        {
            _floatingTextColor = value;
            OnPropertyChanged();
        }
    }

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
        // ลด HP และเล่นเสียงโดนโจมตีถ้าดาเมจมากกว่า 0
        if (amount > 0)
        {
            _ = AudioService.Instance.PlayAttackHitAsync();
        }

        Hp -= amount;
    }

    public void Heal(int amount)
    {
        // ฟื้น HP โดย property Hp จะ clamp ไม่ให้เกิน MaxHp เอง
        Hp += amount;
    }

    public void ShowFloatingText(string text, Color color)
    {
        // โชว์ตัวเลขลอยบนการ์ด เช่น damage หรือ heal แล้วซ่อนเองภายหลัง
        FloatingText = text;
        FloatingTextColor = color;
        IsFloatingTextVisible = true;
        _ = HideFloatingTextSoon();
    }

    public void ResetTurnState()
    {
        // รีเซ็ตสถานะที่ควรกลับใหม่ตอนเริ่มด่าน
        HasActed = false;
        PoisonTurns = 0;
        TauntTurns = 0;
    }

    private void OnVitalsChanged()
    {
        // HP เปลี่ยนแล้วต้องแจ้งทุกค่าที่คำนวณจาก HP ให้ UI วาดใหม่
        OnPropertyChanged(nameof(Hp));
        OnPropertyChanged(nameof(IsAlive));
        OnPropertyChanged(nameof(HpPercent));
        OnPropertyChanged(nameof(HpBarWidth));
        OnPropertyChanged(nameof(HpMiniBarWidth));
        OnPropertyChanged(nameof(StatText));
        OnPropertyChanged(nameof(Opacity));
    }

    private async Task HideFloatingTextSoon()
    {
        // ปล่อย floating text ค้างนิดหนึ่งให้เห็น ก่อนซ่อนออกจากจอ
        await Task.Delay(900);
        IsFloatingTextVisible = false;
    }
}
