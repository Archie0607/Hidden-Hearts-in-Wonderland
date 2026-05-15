using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class LoadingPage : ContentPage
{
    public LoadingPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        
        var loadDataTask = LoadGameAsync();
        var animationTask = LoadingProgressBar.ProgressTo(1.0, 5000, Easing.Linear);

        
        await Task.WhenAll(loadDataTask, animationTask);

        
        Application.Current.MainPage = new AppShell();
    }

    private async Task LoadGameAsync()
    {
        var data = await new SaveService().LoadAsync();

        if (data != null)
        {
            GameService.Instance.LoadFromSave(data);
        }
    }
}