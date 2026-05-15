using Plugin.Maui.Audio;

namespace Hidden_Hearts_in_Wonderland.Services;

public class AudioService
{
    public static AudioService Instance { get; } = new();

    private const string MainVolumeKey = "audio_main_volume";
    private const string BattleVolumeKey = "audio_battle_volume";
    private const string ClickVolumeKey = "audio_click_volume";

    private IAudioPlayer? _musicPlayer;
    private string _currentMusic = "";
    private bool _isMusicLoading;
    private double _mainVolume;
    private double _battleVolume;
    private double _clickVolume;

    private AudioService()
    {
        _mainVolume = Preferences.Get(MainVolumeKey, 0.45);
        _battleVolume = Preferences.Get(BattleVolumeKey, 0.55);
        _clickVolume = Preferences.Get(ClickVolumeKey, 0.75);
    }

    public double MainVolume
    {
        get => _mainVolume;
        set
        {
            _mainVolume = ClampVolume(value);
            Preferences.Set(MainVolumeKey, _mainVolume);
            ApplyCurrentMusicVolume();
        }
    }

    public double BattleVolume
    {
        get => _battleVolume;
        set
        {
            _battleVolume = ClampVolume(value);
            Preferences.Set(BattleVolumeKey, _battleVolume);
            ApplyCurrentMusicVolume();
        }
    }

    public double ClickVolume
    {
        get => _clickVolume;
        set
        {
            _clickVolume = ClampVolume(value);
            Preferences.Set(ClickVolumeKey, _clickVolume);
        }
    }

    public async Task PlayComedyMusicAsync()
    {
        await PlayMusicAsync("commady.mp3", MainVolume);
    }

    public async Task PlayBattleMusicAsync()
    {
        await PlayMusicAsync("battle.mp3", BattleVolume);
    }

    public async Task PlayBattleMusicOnceAsync()
    {
        await PlayMusicAsync("battle.mp3", BattleVolume, false, true);
    }

    public async Task PlayClickAsync()
    {
        await PlayOneShotAsync("click.mp3", ClickVolume);
    }

    public async Task PlayWinAsync()
    {
        await PlayOneShotAsync("gamewin.mp3");
    }

    public async Task PlayGameOverAsync()
    {
        await PlayOneShotAsync("game_over.mp3");
    }

    public async Task PlayAttackHitAsync()
    {
        await PlayOneShotAsync("hit.mp3", BattleVolume);
    }

    private async Task PlayMusicAsync(string fileName, double volume, bool loop = true, bool forceRestart = false)
    {
        if (!forceRestart && _currentMusic == fileName && _musicPlayer?.IsPlaying == true)
        {
            _musicPlayer.Volume = volume;
            return;
        }

        if (_isMusicLoading)
        {
            return;
        }

        try
        {
            _isMusicLoading = true;
            _musicPlayer?.Stop();
            _musicPlayer?.Dispose();

            var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            _musicPlayer = AudioManager.Current.CreatePlayer(stream);
            _musicPlayer.Loop = loop;
            _musicPlayer.Volume = volume;
            _currentMusic = fileName;
            _musicPlayer.Play();
        }
        catch
        {
            _currentMusic = "";
        }
        finally
        {
            _isMusicLoading = false;
        }
    }

    private static async Task PlayOneShotAsync(string fileName, double volume = 1)
    {
        try
        {
            var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            var player = AudioManager.Current.CreatePlayer(stream);
            player.Volume = volume;
            player.Play();

            _ = Task.Run(async () =>
            {
                await Task.Delay(2500);
                player.Dispose();
            });
        }
        catch
        {
        }
    }

    private void ApplyCurrentMusicVolume()
    {
        if (_musicPlayer == null)
        {
            return;
        }

        _musicPlayer.Volume = _currentMusic == "battle.mp3"
            ? BattleVolume
            : MainVolume;
    }

    private static double ClampVolume(double value)
    {
        return Math.Clamp(value, 0, 1);
    }
}
