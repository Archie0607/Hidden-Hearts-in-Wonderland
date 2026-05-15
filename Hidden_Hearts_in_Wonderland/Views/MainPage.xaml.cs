using Hidden_Hearts_in_Wonderland.Views;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class MainPage : ContentPage
{
    private bool _isSyncingSettings;

    public MainPage()
    {
        InitializeComponent();
        SyncSettingsControls();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        _ = AudioService.Instance.PlayClickAsync();
        await Shell.Current.GoToAsync(nameof(CardSelectPage));
    }

    private void OnSettingsClicked(object sender, EventArgs e)
    {
        SyncSettingsControls();
        SettingsOverlay.IsVisible = true;
    }

    private void OnSettingsBackdropTapped(object sender, TappedEventArgs e)
    {
        SettingsOverlay.IsVisible = false;
    }

    private void OnCloseSettingsClicked(object sender, EventArgs e)
    {
        SettingsOverlay.IsVisible = false;
    }

    private void OnMainVolumeChanged(object sender, ValueChangedEventArgs e)
    {
        if (_isSyncingSettings)
        {
            return;
        }

        AudioService.Instance.MainVolume = e.NewValue;
        UpdateVolumeLabels();
    }

    private void OnBattleVolumeChanged(object sender, ValueChangedEventArgs e)
    {
        if (_isSyncingSettings)
        {
            return;
        }

        AudioService.Instance.BattleVolume = e.NewValue;
        UpdateVolumeLabels();
    }

    private void OnClickVolumeChanged(object sender, ValueChangedEventArgs e)
    {
        if (_isSyncingSettings)
        {
            return;
        }

        AudioService.Instance.ClickVolume = e.NewValue;
        UpdateVolumeLabels();
    }

    private void SyncSettingsControls()
    {
        _isSyncingSettings = true;
        MainVolumeSlider.Value = AudioService.Instance.MainVolume;
        BattleVolumeSlider.Value = AudioService.Instance.BattleVolume;
        ClickVolumeSlider.Value = AudioService.Instance.ClickVolume;
        _isSyncingSettings = false;
        UpdateVolumeLabels();
    }

    private void UpdateVolumeLabels()
    {
        MainVolumeLabel.Text = $"{AudioService.Instance.MainVolume:P0}";
        BattleVolumeLabel.Text = $"{AudioService.Instance.BattleVolume:P0}";
        ClickVolumeLabel.Text = $"{AudioService.Instance.ClickVolume:P0}";
    }
}
