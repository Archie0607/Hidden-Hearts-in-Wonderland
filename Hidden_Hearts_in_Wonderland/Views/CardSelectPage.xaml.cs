using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class CardSelectPage : ContentPage
{
    public CardSelectPage()
    {
        InitializeComponent();
        BindingContext = new CardSelectViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();

        if (BindingContext is CardSelectViewModel viewModel)
        {
            viewModel.Refresh();
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
