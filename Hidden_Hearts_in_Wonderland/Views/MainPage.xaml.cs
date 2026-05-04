using Hidden_Hearts_in_Wonderland.Views;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }


    private async void OnStartClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CharacterSelectPage));
    }
}