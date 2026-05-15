using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class StatsPage : ContentPage
{
    public StatsPage()
    {
        InitializeComponent();
        BindingContext = new StatsViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();

        if (BindingContext is StatsViewModel viewModel)
        {
            viewModel.Refresh();
        }
    }
}
