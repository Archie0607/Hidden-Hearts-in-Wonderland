using Hidden_Hearts_in_Wonderland.ViewModels;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class CardStageSelectPage : ContentPage
{
    public CardStageSelectPage()
    {
        InitializeComponent();
        BindingContext = new CardStageSelectViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is CardStageSelectViewModel viewModel)
        {
            viewModel.Refresh();
        }
    }
}
