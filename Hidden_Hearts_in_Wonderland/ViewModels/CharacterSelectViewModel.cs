using System.Collections.ObjectModel;
using System.Windows.Input;
using Hidden_Hearts_in_Wonderland.Models;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.ViewModels;

public class CharacterSelectViewModel
{
    // instance เดียวทั้งเกม
    private readonly GameService _gameService = GameService.Instance;

    public ObservableCollection<Character> Characters { get; set; }

    public ICommand SelectCharacterCommand { get; }

    public CharacterSelectViewModel()
    {
        // สร้างรายการตัวละครตอนเข้า viewmodel และผูก command ตอนเลือกตัวละคร
        Characters = [];
        RefreshCharacters();

        SelectCharacterCommand = new Command<Character>(OnSelectCharacter);
    }

    public void RefreshCharacters()
    {
        // เติม affection ล่าสุดเข้าไปในโปรไฟล์ที่หน้าเลือกตัวละครใช้แสดง
        Characters.Clear();

        foreach (var character in CharacterProfileService.GetCharacters())
        {
            Characters.Add(new Character
            {
                Name = character.Name,
                Image = character.Image,
                StartNodeId = character.StartNodeId,
                PersonalityPrompt = character.PersonalityPrompt,
                AffectionScore = _gameService.Player.GetAffection(character.Name)
            });
        }
    }

    async void OnSelectCharacter(Character character)
    {
        // บอกระบบว่าเลือกใคร เพื่อให้ affection ต่อจากนี้เข้าตัวละครถูกคน
        _gameService.SelectCharacter(character.Name);

        // ไปหน้า Dialogue พร้อมส่ง start node และ round id ใหม่ให้ AI สร้างรอบสนทนา
        await Shell.Current.GoToAsync(
            $"{nameof(Views.DialoguePage)}?startId={character.StartNodeId}&character={character.Name}&roundId={Guid.NewGuid():N}"
        );
    }
}
