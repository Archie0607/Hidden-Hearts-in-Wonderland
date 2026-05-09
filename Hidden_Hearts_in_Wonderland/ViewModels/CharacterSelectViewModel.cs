using System.Collections.ObjectModel;
using System.Windows.Input;
using Hidden_Hearts_in_Wonderland.Models;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.ViewModels;

public class CharacterSelectViewModel
{
    // 🔥 ใช้ instance เดียวทั้งเกม
    private readonly GameService _gameService = GameService.Instance;

    public ObservableCollection<Character> Characters { get; set; }

    public ICommand SelectCharacterCommand { get; }

    public CharacterSelectViewModel()
    {
        Characters = new ObservableCollection<Character>
        {
            new Character { Name = "อายะ", Image = "luna_smile.png", StartNodeId = "start_aya" }
        };

        SelectCharacterCommand = new Command<Character>(OnSelectCharacter);
    }

    async void OnSelectCharacter(Character character)
    {
        // 🎯 บอกระบบว่าเลือกใคร
        _gameService.SelectCharacter(character.Name);

        // 👉 ไปหน้า Dialogue พร้อมส่งค่า
        await Shell.Current.GoToAsync(
            $"{nameof(Views.DialoguePage)}?startId={character.StartNodeId}&character={character.Name}"
        );
    }
}
