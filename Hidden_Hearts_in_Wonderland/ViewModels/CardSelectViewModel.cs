using System.Collections.ObjectModel;
using System.Windows.Input;
using Hidden_Hearts_in_Wonderland.Models;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.ViewModels;

public class CardSelectViewModel : BaseViewModel
{
    private readonly CardBattleService _cardService = CardBattleService.Instance;
    private string _message = "Choose one main character";

    public ObservableCollection<CardSelectItem> MainCards { get; } = [];

    public ICommand SelectHeroCommand { get; }

    public string Message
    {
        get => _message;
        set
        {
            _message = value;
            OnPropertyChanged();
        }
    }

    public CardSelectViewModel()
    {
        SelectHeroCommand = new Command<CardSelectItem>(async item => await SelectHero(item));
        Refresh();
    }

    public void Refresh()
    {
        MainCards.Clear();

        foreach (var card in _cardService.GetMainCards())
        {
            MainCards.Add(new CardSelectItem(card));
        }
    }

    private async Task SelectHero(CardSelectItem? item)
    {
        if (item == null)
        {
            return;
        }

        _cardService.SelectMainHero(item.Id);
        Message = $"{item.Name} selected";
        await Shell.Current.GoToAsync(nameof(Views.CharacterSelectPage));
    }
}

public class CardSelectItem
{
    public CardSelectItem(CardUnit card)
    {
        Id = card.Id;
        Name = card.Name;
        Description = card.Description;
        Image = card.Image;
        Hp = card.MaxHp;
        Atk = card.Atk;
        Def = card.Def;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string Image { get; }
    public int Hp { get; }
    public int Atk { get; }
    public int Def { get; }
}
