using System.Collections.ObjectModel;
using System.Windows.Input;
using Hidden_Hearts_in_Wonderland.Models;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.ViewModels;

public class CardStageSelectViewModel : BaseViewModel
{
    private readonly CardBattleService _cardService = CardBattleService.Instance;
    private string _message = "Choose an unlocked stage";

    public ObservableCollection<CardStageSelectItem> Stages { get; } = [];

    public ICommand SelectStageCommand { get; }
    public ICommand TeamCommand { get; }
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

    public CardStageSelectViewModel()
    {
        SelectStageCommand = new Command<CardStageSelectItem>(async item => await SelectStage(item));
        TeamCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(Views.CardTeamPage)));
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        Refresh();
    }

    public void Refresh()
    {
        Stages.Clear();

        foreach (var stage in _cardService.Stages)
        {
            Stages.Add(new CardStageSelectItem(stage, _cardService.IsStageUnlocked(stage.Number)));
        }
    }

    private async Task SelectStage(CardStageSelectItem? item)
    {
        if (item == null)
        {
            return;
        }

        if (!item.IsUnlocked)
        {
            Message = "Clear the previous stage first";
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(Views.CardBattlePage)}?stage={item.Number}");
    }
}

public class CardStageSelectItem
{
    public CardStageSelectItem(BattleStage stage, bool isUnlocked)
    {
        Number = stage.Number;
        Name = stage.Name;
        EnemyText = string.Join(" / ", stage.EnemyIds);
        IsUnlocked = isUnlocked;
    }

    public int Number { get; }
    public string Name { get; }
    public string EnemyText { get; }
    public bool IsUnlocked { get; }
    public string LockText => IsUnlocked ? "Playable" : "Locked";
    public double Opacity => IsUnlocked ? 1 : 0.42;
}
