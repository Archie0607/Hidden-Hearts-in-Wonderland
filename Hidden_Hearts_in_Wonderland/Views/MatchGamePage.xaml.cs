using Hidden_Hearts_in_Wonderland.ViewModels;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class MatchGamePage : ContentPage
{
    public MatchGamePage()
    {
        InitializeComponent();
        BindingContext = new MatchGameViewModel();
    }

    private async void OnTileTapped(object sender, TappedEventArgs e)
    {
        if (sender is not VisualElement tile)
        {
            return;
        }

        await tile.ScaleTo(0.9, 60, Easing.CubicOut);
        await tile.ScaleTo(1, 120, Easing.SpringOut);
    }
}
