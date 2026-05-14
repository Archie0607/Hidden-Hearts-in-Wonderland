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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CoinsLabel.Text = $"Coins: {GameService.Instance.Coins}";

        if (BindingContext is CharacterSelectViewModel viewModel)
        {
            viewModel.RefreshCharacters();
        }
    }

    private async void OnMatchGameClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MatchGamePage));
    }
}
