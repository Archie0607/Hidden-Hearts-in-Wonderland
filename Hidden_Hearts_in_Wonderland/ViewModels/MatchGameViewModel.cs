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

    private readonly Random _random = new();
    private readonly GameService _gameService = GameService.Instance;
    private MatchTile? _selectedTile;
    private bool _isBusy;
    private bool _isFinished;
    private int _movesLeft;
    private int _collected;
    private int _score;
    private int _coinsEarned;
    private int _rows = 5;
    private int _columns = 9;
    private string _message = "Match 3 colors";
    private string _nextNodeId = string.Empty;
    private string _characterName = string.Empty;

    private readonly SlimeInfo[] _slimes =
    [
        new("Blue", "slime_0_0.png"),
        new("Green", "slime_0_1.png"),
        new("Yellow", "slime_0_2.png"),
        new("Red", "slime_1_0.png"),
        new("Purple", "slime_1_1.png"),
        new("Pink", "slime_1_2.png"),
        new("Orange", "slime_2_0.png")
    ];

    public ObservableCollection<MatchTile> Tiles { get; } = new();

    public ICommand SelectTileCommand { get; }
    public ICommand RestartCommand { get; }
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

    public MatchGameViewModel()
    {
        SelectTileCommand = new Command<MatchTile>(async tile => await SelectTile(tile));
        RestartCommand = new Command(BuildBoard);
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        BuildBoard();
    }

    public void SetBoardShape(int rows, int columns)
    {
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
        foreach (var tile in Tiles)
        {
            tile.TileSize = tileSize;
        }
    }

    private void BuildBoard()
    {
        Tiles.Clear();
        _selectedTile = null;
        _isBusy = false;
        _isFinished = false;
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
        return _slimes[_random.Next(_slimes.Length)];
    }

    private bool WouldCreateStartingMatch(int row, int column, SlimeInfo slime)
    {
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
        tile.IsSelected = true;
        _selectedTile = tile;
        Message = "Pick a nearby slime";
    }

    private void ClearSelection()
    {
        if (_selectedTile != null)
        {
            _selectedTile.IsSelected = false;
        }

        _selectedTile = null;
    }

    private static bool AreAdjacent(MatchTile first, MatchTile second)
    {
        return Math.Abs(first.Row - second.Row) + Math.Abs(first.Column - second.Column) == 1;
    }

    private static void SwapSlimes(MatchTile first, MatchTile second)
    {
        (first.Slime, second.Slime) = (second.Slime, first.Slime);
    }

    private async Task ResolveMatches(List<MatchTile> matchedTiles)
    {
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
            Message = $"Matched {matchedTiles.Count}";

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
        foreach (var tile in matchedTiles)
        {
            tile.Slime = null;
            tile.IsMatched = false;
        }
    }

    private void DropSlimesIntoEmptySpaces()
    {
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
        return first.Slime is { } firstSlime
            && second.Slime is { } secondSlime
            && firstSlime.Name == secondSlime.Name;
    }

    private static void AddRunIfMatch(List<MatchTile> run, HashSet<MatchTile> matched)
    {
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
        return Tiles[row * Columns + column];
    }

    private async Task CheckGameEnd()
    {
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
        _isFinished = true;

        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        var title = isWin ? "Mini game cleared" : "Mini game finished";
        var result = $"Coins earned: {CoinsEarned}\nScore: {Score}\nCollected: {Collected}/{Goal}";

        if (IsDialogueGame)
        {
            var bonus = isWin ? 20 : 5;

            if (!string.IsNullOrWhiteSpace(CharacterName))
            {
                _gameService.Player.AddAffection(CharacterName, bonus);
            }

            if (page != null)
            {
                await page.DisplayAlertAsync(title, $"{result}\n+{bonus} affection", "Continue");
            }

            await Shell.Current.GoToAsync($"../{nameof(Views.DialoguePage)}?startId={NextNodeId}&character={CharacterName}");
            return;
        }

        if (page != null)
        {
            await page.DisplayAlertAsync(title, $"{result}\nTotal coins: {_gameService.Coins}", "OK");
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
