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
        // เตรียม command เลือกตัวละครหลัก แล้วโหลดรายการการ์ดให้หน้าแสดง
        SelectHeroCommand = new Command<CardSelectItem>(async item => await SelectHero(item));
        Refresh();
    }

    public void Refresh()
    {
        // โหลดการ์ด main hero ใหม่ เผื่อข้อมูลมีการเปลี่ยนระหว่างกลับมาหน้านี้
        MainCards.Clear();

        foreach (var card in _cardService.GetMainCards())
        {
            MainCards.Add(new CardSelectItem(card));
        }
    }

    private async Task SelectHero(CardSelectItem? item)
    {
        // เลือก hero หลักแล้วกลับไปหน้าเลือกตัวละครเพื่อเริ่ม flow เกมต่อ
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
        // แปลง CardUnit เป็น model เบา ๆ สำหรับ bind ในหน้าเลือก hero
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
