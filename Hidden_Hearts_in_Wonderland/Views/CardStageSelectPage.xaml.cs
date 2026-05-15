using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class CardStageSelectPage : ContentPage
{
    public CardStageSelectPage()
    {
        InitializeComponent();
        BindingContext = new CardStageSelectViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();

        if (BindingContext is CardStageSelectViewModel viewModel)
        {
            viewModel.Refresh();
        }
    }
}
