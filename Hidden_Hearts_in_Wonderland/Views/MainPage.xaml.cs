using Hidden_Hearts_in_Wonderland.Views;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class MainPage : ContentPage
{
    private bool _isSyncingSettings;

    public MainPage()
    {
        // หน้าแรกของเกม พร้อม sync ค่าเสียงให้ slider ตรงกับค่าที่เซฟไว้
        InitializeComponent();
        SyncSettingsControls();
    }

    protected override async void OnAppearing()
    {
        // กลับมาหน้าแรกแล้วเปิดเพลงหลัก
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        // เริ่มเกมโดยพาไปเลือก hero หลักก่อน
        _ = AudioService.Instance.PlayClickAsync();
        await Shell.Current.GoToAsync(nameof(CardSelectPage));
    }

    private void OnSettingsClicked(object sender, EventArgs e)
    {
        // เปิดหน้าตั้งค่าเสียง และ sync ค่า slider ก่อนโชว์
        SyncSettingsControls();
        SettingsOverlay.IsVisible = true;
    }

    private void OnSettingsBackdropTapped(object sender, TappedEventArgs e)
    {
        // แตะพื้นหลังนอกกล่องเพื่่อปิด settings
        SettingsOverlay.IsVisible = false;
    }

    private void OnCloseSettingsClicked(object sender, EventArgs e)
    {
        // ปิด settings จากปุ่มปิด
        SettingsOverlay.IsVisible = false;
    }

    private void OnMainVolumeChanged(object sender, ValueChangedEventArgs e)
    {
        // ปรับเสียงเพลงหลักจาก slider
        if (_isSyncingSettings)
        {
            return;
        }

        AudioService.Instance.MainVolume = e.NewValue;
        UpdateVolumeLabels();
    }

    private void OnBattleVolumeChanged(object sender, ValueChangedEventArgs e)
    {
        // ปรับเสียงเพลง/เอฟเฟกต์ฝั่ง battle
        if (_isSyncingSettings)
        {
            return;
        }

        AudioService.Instance.BattleVolume = e.NewValue;
        UpdateVolumeLabels();
    }

    private void OnClickVolumeChanged(object sender, ValueChangedEventArgs e)
    {
        // ปรับเสียงคลิก UI
        if (_isSyncingSettings)
        {
            return;
        }

        AudioService.Instance.ClickVolume = e.NewValue;
        UpdateVolumeLabels();
    }

    private void SyncSettingsControls()
    {
        // อัปเดต slider จาก service โดยกันไม่ให้ event changed ยิงกลับไปเซฟซ้ำ
        _isSyncingSettings = true;
        MainVolumeSlider.Value = AudioService.Instance.MainVolume;
        BattleVolumeSlider.Value = AudioService.Instance.BattleVolume;
        ClickVolumeSlider.Value = AudioService.Instance.ClickVolume;
        _isSyncingSettings = false;
        UpdateVolumeLabels();
    }

    private void UpdateVolumeLabels()
    {
        // แสดงเลขเปอร์เซ็นต์ข้าง slider ให้ตรงกับ volume ตอนนี้
        MainVolumeLabel.Text = $"{AudioService.Instance.MainVolume:P0}";
        BattleVolumeLabel.Text = $"{AudioService.Instance.BattleVolume:P0}";
        ClickVolumeLabel.Text = $"{AudioService.Instance.ClickVolume:P0}";
    }
}
