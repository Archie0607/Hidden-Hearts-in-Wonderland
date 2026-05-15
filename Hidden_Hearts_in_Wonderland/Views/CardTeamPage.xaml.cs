using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class CardTeamPage : ContentPage
{
    public CardTeamPage()
    {
        InitializeComponent();
        BindingContext = new CardTeamViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();

        if (BindingContext is CardTeamViewModel viewModel)
        {
            viewModel.Refresh();
        }
    }
}
