using Hidden_Hearts_in_Wonderland.ViewModels;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class CardTeamPage : ContentPage
{
    public CardTeamPage()
    {
        InitializeComponent();
        BindingContext = new CardTeamViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is CardTeamViewModel viewModel)
        {
            viewModel.Refresh();
        }
    }
}
