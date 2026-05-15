using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class DialoguePage : ContentPage
{
    public DialoguePage()
    {
        InitializeComponent();
        BindingContext = new DialogueViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        _ = AudioService.Instance.PlayClickAsync();
        await Shell.Current.GoToAsync("..");
    }
}
