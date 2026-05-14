using Hidden_Hearts_in_Wonderland.ViewModels;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class CardSelectPage : ContentPage
{
    public CardSelectPage()
    {
        InitializeComponent();
        BindingContext = new CardSelectViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is CardSelectViewModel viewModel)
        {
            viewModel.Refresh();
        }
    }
}
