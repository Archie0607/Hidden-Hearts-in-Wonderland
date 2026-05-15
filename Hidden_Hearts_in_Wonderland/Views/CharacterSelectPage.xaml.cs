using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class CharacterSelectPage : ContentPage
{
    public CharacterSelectPage()
    {
        InitializeComponent();
        BindingContext = new CharacterSelectViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();

        if (BindingContext is CharacterSelectViewModel viewModel)
        {
            viewModel.RefreshCharacters();
        }
    }

    private async void OnMatchGameClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MatchGamePage));
    }

    private async void OnBattleClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CardStageSelectPage));
    }

    private async void OnShopClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ShopPage));
    }

    private async void OnBagClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(InventoryPage));
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
