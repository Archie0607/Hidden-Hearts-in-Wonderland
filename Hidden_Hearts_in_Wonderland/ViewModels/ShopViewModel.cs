using System.Collections.ObjectModel;
using System.Windows.Input;
using Hidden_Hearts_in_Wonderland.Models;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.ViewModels;

public class ShopViewModel : BaseViewModel
{
    private readonly GameService _gameService = GameService.Instance;

    public ObservableCollection<ShopItem> Items { get; } = [];

    private bool _isShowPopup;
    public bool IsShowPopup
    {
        get => _isShowPopup;
        set
        {
            _isShowPopup = value;
            OnPropertyChanged(nameof(IsShowPopup));
        }
    }

    private string _popupMessage;
    public string PopupMessage
    {
        get => _popupMessage;
        set
        {
            _popupMessage = value;
            OnPropertyChanged(nameof(PopupMessage));
        }
    }

    
    private string _popupImage;
    public string PopupImage
    {
        get => _popupImage;
        set
        {
            _popupImage = value;
            OnPropertyChanged(nameof(PopupImage));
        }
    }
    

    public ICommand BuyCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand ClosePopupCommand { get; }

    public string CoinsText => _gameService.Coins.ToString();

    public ShopViewModel()
    {
        BuyCommand = new Command<ShopItem>(BuyItem);
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));

        ClosePopupCommand = new Command(() => IsShowPopup = false);

        Refresh();
    }

    public void Refresh()
    {
        Items.Clear();

        foreach (var item in _gameService.GetShopItems())
        {
            Items.Add(item);
        }

        OnPropertyChanged(nameof(CoinsText));
    }

    private void BuyItem(ShopItem? item)
    {
        if (item == null)
        {
            return;
        }

        
        bool isSuccess = _gameService.BuyItem(item.Id, out var message);
        OnPropertyChanged(nameof(CoinsText));

        PopupMessage = message;

        
        if (isSuccess)
        {
            PopupImage = "ss.png"; 
        }
        else
        {
            PopupImage = "ff.png"; 
        }
       

        IsShowPopup = true;
    }
}
