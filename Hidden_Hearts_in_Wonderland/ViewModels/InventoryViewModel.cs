using System.Collections.ObjectModel;
using System.Windows.Input;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.ViewModels;

public class InventoryViewModel : BaseViewModel
{
    private readonly GameService _gameService = GameService.Instance;
    private string _selectedCharacter = "";
    private InventoryDisplayItem? _pendingGiftItem;
    private bool _isTargetPopupVisible;
    private string _giftPopupMessage = "Choose a character";

    public ObservableCollection<InventoryDisplayItem> Items { get; } = [];
    public ObservableCollection<string> Characters { get; } = [];
    public ObservableCollection<GiftTargetOption> GiftTargets { get; } = [];

    public ICommand GiveGiftCommand { get; }
    public ICommand SelectGiftTargetCommand { get; }
    public ICommand CloseGiftPopupCommand { get; }
    public ICommand BackCommand { get; }

    public InventoryViewModel()
    {
        GiveGiftCommand = new Command<InventoryDisplayItem>(GiveGift);
        SelectGiftTargetCommand = new Command<GiftTargetOption>(SelectGiftTarget);
        CloseGiftPopupCommand = new Command(() => IsTargetPopupVisible = false);
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        Refresh();
    }

    public string SelectedCharacter
    {
        get => _selectedCharacter;
        set
        {
            if (_selectedCharacter == value)
            {
                return;
            }

            _selectedCharacter = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TargetStateText));
            OnPropertyChanged(nameof(SelectedCharacterImage));
        }
    }

    public bool IsTargetPopupVisible
    {
        get => _isTargetPopupVisible;
        set
        {
            _isTargetPopupVisible = value;
            OnPropertyChanged();
        }
    }

    public string GiftPopupMessage
    {
        get => _giftPopupMessage;
        set
        {
            _giftPopupMessage = value;
            OnPropertyChanged();
        }
    }

    public string SelectedCharacterImage => string.IsNullOrWhiteSpace(SelectedCharacter)
        ? "luna.png"
        : CharacterProfileService.GetByName(SelectedCharacter).Image;

    public string PendingGiftImage => _pendingGiftItem?.Image ?? "";

    public string PendingGiftText => _pendingGiftItem == null
        ? "Select Gift"
        : $"{_pendingGiftItem.Name} x{_pendingGiftItem.Count}";

    public string TargetStateText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SelectedCharacter))
            {
                return "Choose a character";
            }

            var state = _gameService.Player.GetCharacterState(SelectedCharacter);
            return $"Affection {state.Affection}  Trust {state.Trust}  Happiness {state.Happiness}";
        }
    }

    public void Refresh()
    {
        Characters.Clear();
        GiftTargets.Clear();

        foreach (var character in CharacterProfileService.GetCharacters())
        {
            Characters.Add(character.Name);
            GiftTargets.Add(new GiftTargetOption
            {
                Name = character.Name,
                Image = character.Image,
                Affection = _gameService.Player.GetAffection(character.Name)
            });
        }

        if (string.IsNullOrWhiteSpace(SelectedCharacter))
        {
            SelectedCharacter = !string.IsNullOrWhiteSpace(_gameService.CurrentCharacter)
                ? _gameService.CurrentCharacter
                : Characters.FirstOrDefault() ?? "";
        }

        Items.Clear();

        foreach (var item in _gameService.Player.Inventory)
        {
            var shopItem = _gameService.GetShopItem(item.Key);

            Items.Add(new InventoryDisplayItem
            {
                ItemId = item.Key,
                Name = shopItem?.Name ?? item.Key,
                Description = shopItem?.Description ?? "",
                Image = shopItem?.Image ?? "",
                Count = item.Value,
                CanGiveGift = shopItem?.IsRune != true
            });
        }

        OnPropertyChanged(nameof(TargetStateText));
        OnPropertyChanged(nameof(SelectedCharacterImage));
        OnPropertyChanged(nameof(PendingGiftText));
    }

    private void GiveGift(InventoryDisplayItem? item)
    {
        if (item == null)
        {
            return;
        }

        _pendingGiftItem = item;
        GiftPopupMessage = "Tap a character to give";
        IsTargetPopupVisible = true;
        OnPropertyChanged(nameof(PendingGiftImage));
        OnPropertyChanged(nameof(PendingGiftText));
    }

    private void SelectGiftTarget(GiftTargetOption? target)
    {
        if (target == null)
        {
            return;
        }

        SelectedCharacter = target.Name;
        ConfirmGift();
    }

    private void ConfirmGift()
    {
        if (_pendingGiftItem == null)
        {
            GiftPopupMessage = "No gift selected";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedCharacter))
        {
            GiftPopupMessage = "Choose a character first";
            return;
        }

        var giftName = _pendingGiftItem.Name;
        var affectionBefore = _gameService.Player.GetCharacterState(SelectedCharacter).Affection;
        var wasGiven = _gameService.GiveGift(SelectedCharacter, _pendingGiftItem.ItemId, out var message);
        var affectionAfter = _gameService.Player.GetCharacterState(SelectedCharacter).Affection;
        var affectionGain = Math.Max(0, affectionAfter - affectionBefore);

        GiftPopupMessage = wasGiven
            ? $"{SelectedCharacter} received {giftName}: \u2764\uFE0F +{affectionGain}  Total \u2764\uFE0F {affectionAfter}"
            : message;
        Refresh();

        if (wasGiven)
        {
            _pendingGiftItem = null;
            OnPropertyChanged(nameof(PendingGiftImage));
            OnPropertyChanged(nameof(PendingGiftText));
        }
    }
}

public class InventoryDisplayItem
{
    public string ItemId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Image { get; set; } = "";
    public int Count { get; set; }
    public bool CanGiveGift { get; set; } = true;
    public string CountText => $"x{Count}";
    public string UseText => CanGiveGift ? "" : "Use in Stats";
}

public class GiftTargetOption
{
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public int Affection { get; set; }
    public string AffectionText => $"\u2764\uFE0F {Affection}";
}
