using System.Collections.ObjectModel;
using System.Windows.Input;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.ViewModels;

public class MatchGameViewModel : BaseViewModel
{
    private const int BoardRows = 6;
    private const int BoardColumns = 6;
    private const string SpecialImage = "slime_2_2.png";

    private readonly Random _random = new();
    private readonly GameService _gameService = GameService.Instance;
    private MatchTile? _selectedTile;
    private bool _isResolving;
    private int _movesLeft = 20;
    private int _coinsEarned;

    private readonly string[] _slimeImages =
    {
        "slime_0_0.png",
        "slime_0_1.png",
        "slime_0_2.png",
        "slime_1_0.png",
        "slime_1_1.png",
        "slime_1_2.png",
        "slime_2_0.png"
    };

    public ObservableCollection<MatchTile> Tiles { get; } = new();

    public ICommand SelectTileCommand { get; }
    public ICommand RestartCommand { get; }
    public ICommand BackCommand { get; }

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

    public int CoinsEarned
    {
        get => _coinsEarned;
        set
        {
            _coinsEarned = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public string StatusText => $"Moves: {MovesLeft}   Earned: {CoinsEarned} coins   Total: {_gameService.Coins}";

    public MatchGameViewModel()
    {
        SelectTileCommand = new Command<MatchTile>(async tile => await SelectTile(tile));
        RestartCommand = new Command(BuildBoard);
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        BuildBoard();
    }

    private void BuildBoard()
    {
        Tiles.Clear();
        _selectedTile = null;
        _isResolving = false;
        MovesLeft = 20;
        CoinsEarned = 0;

        for (var row = 0; row < BoardRows; row++)
        {
            for (var column = 0; column < BoardColumns; column++)
            {
                Tiles.Add(new MatchTile(row, column, PickImageAvoidingMatch(row, column)));
            }
        }
    }

    private string PickImageAvoidingMatch(int row, int column)
    {
        string image;

        do
        {
            image = RandomImage();
        }
        while (WouldCreateMatch(row, column, image));

        return image;
    }

    private bool WouldCreateMatch(int row, int column, string image)
    {
        if (column >= 2 &&
            GetTile(row, column - 1)?.Image == image &&
            GetTile(row, column - 2)?.Image == image)
        {
            return true;
        }

        if (row >= 2 &&
            GetTile(row - 1, column)?.Image == image &&
            GetTile(row - 2, column)?.Image == image)
        {
            return true;
        }

        return false;
    }

    private string RandomImage()
    {
        return _slimeImages[_random.Next(_slimeImages.Length)];
    }

    private async Task SelectTile(MatchTile? tile)
    {
        if (tile == null || _isResolving || MovesLeft <= 0)
        {
            return;
        }

        if (_selectedTile == null)
        {
            SelectOnly(tile);
            return;
        }

        if (_selectedTile == tile)
        {
            tile.IsSelected = false;
            _selectedTile = null;
            return;
        }

        if (!AreAdjacent(_selectedTile, tile))
        {
            SelectOnly(tile);
            return;
        }

        var first = _selectedTile;
        first.IsSelected = false;
        _selectedTile = null;
        MovesLeft--;

        if (first.IsSpecial || tile.IsSpecial)
        {
            await ActivateSpecial(first, tile);
            await CheckForGameOver();
            return;
        }

        SwapImages(first, tile);

        var groups = FindMatchGroups();
        if (groups.Count == 0)
        {
            await Task.Delay(220);
            SwapImages(first, tile);
            await CheckForGameOver();
            return;
        }

        await ResolveMatches(groups, first, tile);
        await CheckForGameOver();
    }

    private void SelectOnly(MatchTile tile)
    {
        if (_selectedTile != null)
        {
            _selectedTile.IsSelected = false;
        }

        tile.IsSelected = true;
        _selectedTile = tile;
    }

    private static bool AreAdjacent(MatchTile first, MatchTile second)
    {
        return Math.Abs(first.Row - second.Row) + Math.Abs(first.Column - second.Column) == 1;
    }

    private static void SwapImages(MatchTile first, MatchTile second)
    {
        (first.Image, second.Image) = (second.Image, first.Image);
    }

    private async Task ActivateSpecial(MatchTile first, MatchTile second)
    {
        _isResolving = true;

        var targetImage = first.IsSpecial ? second.Image : first.Image;
        var targets = first.IsSpecial && second.IsSpecial
            ? Tiles.Where(tile => !tile.IsSpecial).ToHashSet()
            : Tiles.Where(tile => tile.Image == targetImage).ToHashSet();

        targets.Add(first);
        targets.Add(second);

        await ClearTiles(targets, keepSpecial: null);
        await ResolveMatches(FindMatchGroups(), null, null);

        _isResolving = false;
    }

    private async Task ResolveMatches(List<List<MatchTile>> groups, MatchTile? firstMoveTile, MatchTile? secondMoveTile)
    {
        _isResolving = true;

        while (groups.Count > 0)
        {
            var allMatches = groups.SelectMany(group => group).ToHashSet();
            var specialTiles = groups
                .Where(group => group.Count >= 4)
                .Select(group => PickSpecialTile(group, firstMoveTile, secondMoveTile))
                .ToHashSet();

            await ClearTiles(allMatches, specialTiles);

            foreach (var tile in specialTiles)
            {
                tile.Image = SpecialImage;
                tile.IsMatched = false;
            }

            await Task.Delay(120);
            groups = FindMatchGroups();
            firstMoveTile = null;
            secondMoveTile = null;
        }

        _isResolving = false;
    }

    private async Task ClearTiles(HashSet<MatchTile> tiles, HashSet<MatchTile>? keepSpecial)
    {
        var clearingTiles = keepSpecial == null
            ? tiles
            : tiles.Where(tile => !keepSpecial.Contains(tile)).ToHashSet();

        if (clearingTiles.Count == 0)
        {
            return;
        }

        var reward = clearingTiles.Count * 5;
        CoinsEarned += reward;
        _gameService.AddCoins(reward);
        OnPropertyChanged(nameof(StatusText));

        foreach (var tile in clearingTiles)
        {
            tile.IsMatched = true;
        }

        await Task.Delay(240);

        foreach (var tile in clearingTiles)
        {
            tile.IsMatched = false;
            tile.Image = RandomImage();
        }
    }

    private static MatchTile PickSpecialTile(List<MatchTile> group, MatchTile? firstMoveTile, MatchTile? secondMoveTile)
    {
        if (firstMoveTile != null && group.Contains(firstMoveTile))
        {
            return firstMoveTile;
        }

        if (secondMoveTile != null && group.Contains(secondMoveTile))
        {
            return secondMoveTile;
        }

        return group[group.Count / 2];
    }

    private List<List<MatchTile>> FindMatchGroups()
    {
        var groups = new List<List<MatchTile>>();

        for (var row = 0; row < BoardRows; row++)
        {
            var runStart = 0;

            for (var column = 1; column <= BoardColumns; column++)
            {
                var startTile = GetTile(row, runStart);
                var currentTile = column < BoardColumns ? GetTile(row, column) : null;
                var isSame = currentTile != null &&
                             startTile != null &&
                             !currentTile.IsSpecial &&
                             !startTile.IsSpecial &&
                             currentTile.Image == startTile.Image;

                if (isSame)
                {
                    continue;
                }

                if (column - runStart >= 3)
                {
                    groups.Add(Enumerable.Range(runStart, column - runStart)
                        .Select(matchColumn => GetTile(row, matchColumn)!)
                        .ToList());
                }

                runStart = column;
            }
        }

        for (var column = 0; column < BoardColumns; column++)
        {
            var runStart = 0;

            for (var row = 1; row <= BoardRows; row++)
            {
                var startTile = GetTile(runStart, column);
                var currentTile = row < BoardRows ? GetTile(row, column) : null;
                var isSame = currentTile != null &&
                             startTile != null &&
                             !currentTile.IsSpecial &&
                             !startTile.IsSpecial &&
                             currentTile.Image == startTile.Image;

                if (isSame)
                {
                    continue;
                }

                if (row - runStart >= 3)
                {
                    groups.Add(Enumerable.Range(runStart, row - runStart)
                        .Select(matchRow => GetTile(matchRow, column)!)
                        .ToList());
                }

                runStart = row;
            }
        }

        return groups;
    }

    private MatchTile? GetTile(int row, int column)
    {
        return Tiles.FirstOrDefault(tile => tile.Row == row && tile.Column == column);
    }

    private async Task CheckForGameOver()
    {
        if (MovesLeft > 0)
        {
            return;
        }

        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page != null)
        {
            await page.DisplayAlertAsync("จบมินิเกม", $"ได้เงิน {CoinsEarned} coins", "OK");
        }
    }
}

public class MatchTile : BaseViewModel
{
    private string _image;
    private bool _isSelected;
    private bool _isMatched;

    public MatchTile(int row, int column, string image)
    {
        Row = row;
        Column = column;
        _image = image;
    }

    public int Row { get; }
    public int Column { get; }
    public bool IsSpecial => Image == "slime_2_2.png";

    public string Image
    {
        get => _image;
        set
        {
            _image = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSpecial));
            OnPropertyChanged(nameof(TileStrokeColor));
            OnPropertyChanged(nameof(TileStrokeThickness));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TileColor));
            OnPropertyChanged(nameof(TileStrokeColor));
            OnPropertyChanged(nameof(TileStrokeThickness));
            OnPropertyChanged(nameof(TileScale));
        }
    }

    public bool IsMatched
    {
        get => _isMatched;
        set
        {
            _isMatched = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TileColor));
            OnPropertyChanged(nameof(TileStrokeColor));
            OnPropertyChanged(nameof(TileStrokeThickness));
            OnPropertyChanged(nameof(TileScale));
        }
    }

    public Color TileStrokeColor
    {
        get
        {
            if (IsSelected)
            {
                return Color.FromArgb("#FF2F7D");
            }

            return IsSpecial ? Color.FromArgb("#8E24AA") : Color.FromArgb("#E6CFA4");
        }
    }

    public double TileStrokeThickness => IsSelected || IsSpecial ? 4 : 1;

    public double TileScale => IsSelected ? 1.06 : 1;

    public Color TileColor
    {
        get
        {
            if (IsMatched)
            {
                return Color.FromArgb("#C8E6C9");
            }

            if (IsSpecial)
            {
                return Color.FromArgb("#F3D7FF");
            }

            return IsSelected ? Color.FromArgb("#FFD1E0") : Color.FromArgb("#FFF8E1");
        }
    }
}
