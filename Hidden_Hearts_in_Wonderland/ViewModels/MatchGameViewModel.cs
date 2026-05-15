using System.Collections.ObjectModel;
using System.Windows.Input;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.ViewModels;

[QueryProperty(nameof(NextNodeId), "nextNodeId")]
[QueryProperty(nameof(CharacterName), "character")]
public class MatchGameViewModel : BaseViewModel
{
    private const int StartingMoves = 12;
    private const int Goal = 30;
    private const int CoinPerSlime = 5;
    private const string RainbowSlimeName = "Rainbow";

    private readonly Random _random = new();
    private readonly GameService _gameService = GameService.Instance;
    private MatchTile? _selectedTile;
    private bool _isBusy;
    private bool _isFinished;
    private int _movesLeft;
    private int _collected;
    private int _score;
    private int _coinsEarned;
    private bool _isResultPopupVisible;
    private string _resultTitle = "";
    private string _resultCoinsText = "";
    private string _resultScoreText = "";
    private string _resultCollectedText = "";
    private string _resultBonusText = "";
    private int _rows = 5;
    private int _columns = 9;
    private string _message = "Match 3 colors";
    private string _nextNodeId = string.Empty;
    private string _characterName = string.Empty;

    private readonly SlimeInfo[] _slimes =
    [
        new("Fire", "fire.png"),
        new("Water", "water.png"),
        new("Wind", "wind.png"),
        new("Earth", "earth.png"),
        new("Light", "light.png"),
        new("Dark", "dark.png")
    ];

    private readonly SlimeInfo _rainbowSlime = new(RainbowSlimeName, "rainbow.png");

    public ObservableCollection<MatchTile> Tiles { get; } = new();

    public ICommand SelectTileCommand { get; }
    public ICommand RestartCommand { get; }
    public ICommand ContinueResultCommand { get; }
    public ICommand BackCommand { get; }

    public string NextNodeId
    {
        get => _nextNodeId;
        set => _nextNodeId = value ?? string.Empty;
    }

    public string CharacterName
    {
        get => _characterName;
        set => _characterName = value ?? string.Empty;
    }

    public int Rows
    {
        get => _rows;
        private set
        {
            _rows = value;
            OnPropertyChanged();
        }
    }

    public int Columns
    {
        get => _columns;
        private set
        {
            _columns = value;
            OnPropertyChanged();
        }
    }

    public int MovesLeft
    {
        get => _movesLeft;
        set
        {
            _movesLeft = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public int Collected
    {
        get => _collected;
        set
        {
            _collected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(GoalText));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public int Score
    {
        get => _score;
        set
        {
            _score = value;
            OnPropertyChanged();
        }
    }

    public int CoinsEarned
    {
        get => _coinsEarned;
        set
        {
            _coinsEarned = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CoinText));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public string Message
    {
        get => _message;
        set
        {
            _message = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public string GoalText => $"{Collected}/{Goal}";
    public string CoinText => $"+{CoinsEarned}";
    public string StatusText => Message;

    public bool IsResultPopupVisible
    {
        get => _isResultPopupVisible;
        set
        {
            _isResultPopupVisible = value;
            OnPropertyChanged();
        }
    }

    public string ResultTitle
    {
        get => _resultTitle;
        set
        {
            _resultTitle = value;
            OnPropertyChanged();
        }
    }

    public string ResultCoinsText
    {
        get => _resultCoinsText;
        set
        {
            _resultCoinsText = value;
            OnPropertyChanged();
        }
    }

    public string ResultScoreText
    {
        get => _resultScoreText;
        set
        {
            _resultScoreText = value;
            OnPropertyChanged();
        }
    }

    public string ResultCollectedText
    {
        get => _resultCollectedText;
        set
        {
            _resultCollectedText = value;
            OnPropertyChanged();
        }
    }

    public string ResultBonusText
    {
        get => _resultBonusText;
        set
        {
            _resultBonusText = value;
            OnPropertyChanged();
        }
    }

    public MatchGameViewModel()
    {
        // เตรียม command ของเกมจับคู่ slime แล้วสร้างกระดานเริ่มต้น
        SelectTileCommand = new Command<MatchTile>(async tile => await SelectTile(tile));
        RestartCommand = new Command(BuildBoard);
        ContinueResultCommand = new Command(async () => await ContinueResult());
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        BuildBoard();
    }

    public void SetBoardShape(int rows, int columns)
    {
        // เปลี่ยนทรงกระดานตามแนวหน้าจอ ถ้าขนาดเดิมอยู่แล้วไม่ต้องสร้างใหม่
        if (Rows == rows && Columns == columns)
        {
            return;
        }

        Rows = rows;
        Columns = columns;
        BuildBoard();
    }

    public void SetTileSize(double tileSize)
    {
        // อัปเดตขนาด tile ทุกช่องให้พอดีกับพื้นที่จริงของหน้าจอ
        foreach (var tile in Tiles)
        {
            tile.TileSize = tileSize;
        }
    }

    private void BuildBoard()
    {
        // รีเซ็ตเกมใหม่ทั้งหมด แล้วสุ่ม slime ลงกระดานโดยไม่ให้มี match ตั้งแต่เริ่ม
        Tiles.Clear();
        _selectedTile = null;
        _isBusy = false;
        _isFinished = false;
        IsResultPopupVisible = false;
        MovesLeft = StartingMoves;
        Collected = 0;
        Score = 0;
        CoinsEarned = 0;
        Message = "Match 3 colors";

        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                Tiles.Add(new MatchTile(row, column, PickSlime(row, column)));
            }
        }
    }

    private SlimeInfo PickSlime(int row, int column)
    {
        // สุ่ม slime สำหรับตำแหน่งนี้จนกว่าจะไม่สร้าง match อัตโนมัติ
        SlimeInfo slime;

        do
        {
            slime = RandomSlime();
        }
        while (WouldCreateStartingMatch(row, column, slime));

        return slime;
    }

    private SlimeInfo RandomSlime()
    {
        // มีโอกาสเล็กน้อยได้ rainbow slime ที่ใช้ล้างสีทั้งกระดาน
        if (_random.NextDouble() < 0.06)
        {
            return _rainbowSlime;
        }

        return _slimes[_random.Next(_slimes.Length)];
    }

    private bool WouldCreateStartingMatch(int row, int column, SlimeInfo slime)
    {
        // กันการวาง slime ที่จะทำให้เกิดสามตัวติดกันตั้งแต่เริ่มเกม
        var horizontalMatch = column >= 2
            && GetTile(row, column - 1).Slime?.Name == slime.Name
            && GetTile(row, column - 2).Slime?.Name == slime.Name;

        var verticalMatch = row >= 2
            && GetTile(row - 1, column).Slime?.Name == slime.Name
            && GetTile(row - 2, column).Slime?.Name == slime.Name;

        return horizontalMatch || verticalMatch;
    }

    private async Task SelectTile(MatchTile? tile)
    {
        // คุม flow การเลือกสองช่อง สลับ slime แล้วเช็กว่าเกิด match หรือไม่
        if (tile == null || _isBusy || _isFinished || MovesLeft <= 0)
        {
            return;
        }

        if (_selectedTile == null)
        {
            Select(tile);
            return;
        }

        if (_selectedTile == tile)
        {
            ClearSelection();
            return;
        }

        if (!AreAdjacent(_selectedTile, tile))
        {
            ClearSelection();
            Select(tile);
            return;
        }

        _isBusy = true;
        MovesLeft--;
        SwapSlimes(_selectedTile, tile);

        var rainbowTiles = FindRainbowClearTiles(_selectedTile, tile);
        if (rainbowTiles.Count > 0)
        {
            var clearedColor = GetRainbowTargetName(_selectedTile, tile);
            ClearSelection();
            Message = clearedColor == null ? "Rainbow burst" : $"Rainbow cleared {clearedColor}";
            await ResolveMatches(rainbowTiles);
            _isBusy = false;
            await CheckGameEnd();
            return;
        }

        var matchedTiles = FindMatches();

        if (matchedTiles.Count == 0)
        {
            await Task.Delay(220);
            SwapSlimes(_selectedTile, tile);
            Message = "No match. Try another swap.";
            ClearSelection();
            _isBusy = false;
            await CheckGameEnd();
            return;
        }

        ClearSelection();
        await ResolveMatches(matchedTiles);
        _isBusy = false;
        await CheckGameEnd();
    }

    private void Select(MatchTile tile)
    {
        // mark tile ที่ผู้เล่นเลือกไว้เป็นช่องแรก
        tile.IsSelected = true;
        _selectedTile = tile;
        Message = "Pick a nearby slime";
    }

    private void ClearSelection()
    {
        // เคลียร์กรอบเลือกออกจาก tile เดิม
        if (_selectedTile != null)
        {
            _selectedTile.IsSelected = false;
        }

        _selectedTile = null;
    }

    private static bool AreAdjacent(MatchTile first, MatchTile second)
    {
        // เช็กว่าอยู่ติดกันแบบบนล่างซ้ายขวา ไม่รับแนวทแยง
        return Math.Abs(first.Row - second.Row) + Math.Abs(first.Column - second.Column) == 1;
    }

    private static void SwapSlimes(MatchTile first, MatchTile second)
    {
        // สลับ slime สองช่องแบบ tuple สั้น ๆ
        (first.Slime, second.Slime) = (second.Slime, first.Slime);
    }

    private List<MatchTile> FindRainbowClearTiles(MatchTile first, MatchTile second)
    {
        // ถ้ามี rainbow เกี่ยวข้อง ให้หาทุกช่องที่ต้องถูกล้างออกพร้อมกัน
        var targetName = GetRainbowTargetName(first, second);

        if (targetName == null)
        {
            return [];
        }

        return Tiles
            .Where(tile => tile.Slime?.Name == targetName || tile.Slime?.Name == RainbowSlimeName)
            .ToList();
    }

    private static string? GetRainbowTargetName(MatchTile first, MatchTile second)
    {
        // rainbow + สีปกติ จะล้างสีนั้นทั้งหมด ส่วน rainbow + rainbow จะล้าง rainbow ทั้งหมด
        if (first.Slime?.Name == RainbowSlimeName && second.Slime?.Name != RainbowSlimeName)
        {
            return second.Slime?.Name;
        }

        if (second.Slime?.Name == RainbowSlimeName && first.Slime?.Name != RainbowSlimeName)
        {
            return first.Slime?.Name;
        }

        if (first.Slime?.Name == RainbowSlimeName && second.Slime?.Name == RainbowSlimeName)
        {
            return RainbowSlimeName;
        }

        return null;
    }

    private async Task ResolveMatches(List<MatchTile> matchedTiles)
    {
        // ล้าง match เป็นรอบ ๆ พร้อมให้ slime ตกลงมาและเช็ก chain ต่อ
        while (matchedTiles.Count > 0)
        {
            foreach (var tile in matchedTiles)
            {
                tile.IsMatched = true;
            }

            await Task.Delay(240);

            var coins = matchedTiles.Count * CoinPerSlime;
            Collected += matchedTiles.Count;
            Score += matchedTiles.Count * 50;
            CoinsEarned += coins;
            _gameService.AddCoins(coins);
            Message = $"Cleared {matchedTiles.Count}";

            ClearMatchedTiles(matchedTiles);
            await Task.Delay(120);

            DropSlimesIntoEmptySpaces();
            Message = "Slimes dropped in";
            await Task.Delay(170);

            matchedTiles = FindMatches();
        }
    }

    private static void ClearMatchedTiles(List<MatchTile> matchedTiles)
    {
        // ทำช่องที่ match เป็นช่องว่างก่อนให้ระบบ drop เติมลงมา
        foreach (var tile in matchedTiles)
        {
            tile.Slime = null;
            tile.IsMatched = false;
        }
    }

    private void DropSlimesIntoEmptySpaces()
    {
        // ดึง slime ในแต่ละคอลัมน์ลงล่าง แล้วสุ่มตัวใหม่เติมด้านบน
        for (var column = 0; column < Columns; column++)
        {
            var slimesInColumn = new List<SlimeInfo>();

            for (var row = Rows - 1; row >= 0; row--)
            {
                var slime = GetTile(row, column).Slime;

                if (slime != null)
                {
                    slimesInColumn.Add(slime);
                }
            }

            var slimeIndex = 0;

            for (var row = Rows - 1; row >= 0; row--)
            {
                var tile = GetTile(row, column);

                tile.Slime = slimeIndex < slimesInColumn.Count
                    ? slimesInColumn[slimeIndex++]
                    : RandomSlime();
            }
        }
    }

    private List<MatchTile> FindMatches()
    {
        // หา match แนวนอนและแนวตั้งทั้งหมด แล้วรวมเป็น set กันช่องซ้ำ
        var matched = new HashSet<MatchTile>();

        for (var row = 0; row < Rows; row++)
        {
            var run = new List<MatchTile> { GetTile(row, 0) };

            for (var column = 1; column < Columns; column++)
            {
                var tile = GetTile(row, column);

                if (HasSameSlime(tile, run[^1]))
                {
                    run.Add(tile);
                }
                else
                {
                    AddRunIfMatch(run, matched);
                    run = [tile];
                }
            }

            AddRunIfMatch(run, matched);
        }

        for (var column = 0; column < Columns; column++)
        {
            var run = new List<MatchTile> { GetTile(0, column) };

            for (var row = 1; row < Rows; row++)
            {
                var tile = GetTile(row, column);

                if (HasSameSlime(tile, run[^1]))
                {
                    run.Add(tile);
                }
                else
                {
                    AddRunIfMatch(run, matched);
                    run = [tile];
                }
            }

            AddRunIfMatch(run, matched);
        }

        return matched.ToList();
    }

    private static bool HasSameSlime(MatchTile first, MatchTile second)
    {
        // เทียบ slime สีเดียวกัน โดย rainbow ไม่นับเป็น match ปกติ
        return first.Slime is { } firstSlime
            && second.Slime is { } secondSlime
            && firstSlime.Name != RainbowSlimeName
            && secondSlime.Name != RainbowSlimeName
            && firstSlime.Name == secondSlime.Name;
    }

    private static void AddRunIfMatch(List<MatchTile> run, HashSet<MatchTile> matched)
    {
        // run ที่ยาวตั้งแต่ 3 ช่องขึ้นไปถือว่า match
        if (run.Count < 3)
        {
            return;
        }

        foreach (var tile in run)
        {
            matched.Add(tile);
        }
    }

    private MatchTile GetTile(int row, int column)
    {
        // แปลง row/column เป็น index ใน ObservableCollection
        return Tiles[row * Columns + column];
    }

    private async Task CheckGameEnd()
    {
        // เช็กเงื่อนไขจบเกมหลังทุกการสลับหรือ chain
        if (Collected >= Goal)
        {
            await FinishGame(true);
            return;
        }

        if (MovesLeft <= 0)
        {
            await FinishGame(false);
        }
    }

    private async Task FinishGame(bool isWin)
    {
        // สรุปผลเกม จ่ายโบนัส affection ถ้าเข้ามาจาก dialogue
        _isFinished = true;

        if (isWin)
        {
            _ = AudioService.Instance.PlayWinAsync();
        }
        else
        {
            _ = AudioService.Instance.PlayGameOverAsync();
        }

        ResultTitle = isWin ? "Mini game cleared" : "Mini game finished";
        ResultCoinsText = $"+{CoinsEarned}";
        ResultScoreText = $"Score: {Score}";
        ResultCollectedText = $"Collected: {Collected}/{Goal}";

        if (IsDialogueGame)
        {
            var bonus = isWin ? 20 : 5;

            if (!string.IsNullOrWhiteSpace(CharacterName))
            {
                _gameService.Player.AddAffection(CharacterName, bonus);
            }

            ResultBonusText = $"+{bonus} affection";
            IsResultPopupVisible = true;
            return;
        }

        ResultBonusText = $"Total coins: {_gameService.Coins}";
        IsResultPopupVisible = true;
        await Task.CompletedTask;
    }

    private async Task ContinueResult()
    {
        // ปิด popup แล้วกลับเข้า dialogue ถ้าเกมนี้ถูกเปิดจากบทสนทนา
        IsResultPopupVisible = false;

        if (IsDialogueGame)
        {
            await Shell.Current.GoToAsync($"../{nameof(Views.DialoguePage)}?startId={NextNodeId}&character={CharacterName}");
        }
    }

    private bool IsDialogueGame => !string.IsNullOrWhiteSpace(NextNodeId);
}

public class MatchTile : BaseViewModel
{
    private SlimeInfo? _slime;
    private bool _isSelected;
    private bool _isMatched;
    private double _tileSize = 64;

    public MatchTile(int row, int column, SlimeInfo slime)
    {
        // เก็บตำแหน่งถาวรของ tile และ slime ที่อยู่ในช่องตอนสร้างกระดาน
        Row = row;
        Column = column;
        _slime = slime;
    }

    public int Row { get; }
    public int Column { get; }

    public SlimeInfo? Slime
    {
        get => _slime;
        set
        {
            _slime = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Image));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TileBackground));
            OnPropertyChanged(nameof(BorderColor));
        }
    }

    public bool IsMatched
    {
        get => _isMatched;
        set
        {
            _isMatched = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TileBackground));
            OnPropertyChanged(nameof(BorderColor));
        }
    }

    public double TileSize
    {
        get => _tileSize;
        set
        {
            if (Math.Abs(_tileSize - value) < 0.1)
            {
                return;
            }

            _tileSize = value;
            OnPropertyChanged();
        }
    }

    public string? Image => Slime?.Image;
    public Color TileBackground => IsMatched ? Color.FromArgb("#DFF6DD") : IsSelected ? Color.FromArgb("#FFF2A8") : Color.FromArgb("#E7F1FB");
    public Color BorderColor => IsMatched ? Color.FromArgb("#65B76F") : IsSelected ? Color.FromArgb("#F2C94C") : Color.FromArgb("#B9D5EE");
}

public record SlimeInfo(string Name, string Image);
