using System.Collections.ObjectModel;
using System.Windows.Input;
using Hidden_Hearts_in_Wonderland.Models;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.ViewModels;

public class CardTeamViewModel : BaseViewModel
{
    private readonly CardBattleService _cardService = CardBattleService.Instance;
    private string _message = $"Affection {CardBattleService.RelationshipUnlockRequirement}+ unlocks battle cards";

    public ObservableCollection<CardTeamItem> Cards { get; } = [];

    public ICommand ToggleCardCommand { get; }
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

    public CardTeamViewModel()
    {
        ToggleCardCommand = new Command<CardTeamItem>(ToggleCard);
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        Refresh();
    }

    public void Refresh()
    {
        Cards.Clear();

        foreach (var card in _cardService.GetUnlockableCards())
        {
            Cards.Add(new CardTeamItem(
                card,
                _cardService.GetUnlockAffection(card),
                _cardService.IsUnlocked(card),
                _cardService.IsSelectedSupport(card)));
        }
    }

    private void ToggleCard(CardTeamItem? item)
    {
        if (item == null)
        {
            return;
        }

        _cardService.TryToggleSupportCard(item.Id, out var message);
        Message = message;
        Refresh();
    }
}

public class CardTeamItem
{
    public CardTeamItem(CardUnit card, int affection, bool isUnlocked, bool isSelected)
    {
        Id = card.Id;
        Name = card.Name;
        Description = card.Description;
        Image = card.Image;
        Affection = affection;
        IsUnlocked = isUnlocked;
        IsSelected = isSelected;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string Image { get; }
    public int Affection { get; }
    public bool IsUnlocked { get; }
    public bool IsSelected { get; }
    public double Opacity => IsUnlocked ? 1 : 0.38;
    public Color StrokeColor => IsSelected ? Color.FromArgb("#6AA4FF") : Color.FromArgb("#D6DCE8");
    public string StatusText => IsUnlocked ? (IsSelected ? "In team" : "Unlocked") : $"Need {CardBattleService.RelationshipUnlockRequirement}";
    public string AffectionText => $"Affection {Affection}/{CardBattleService.RelationshipUnlockRequirement}";
}
