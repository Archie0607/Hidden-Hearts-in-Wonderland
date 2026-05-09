using System.Text.Json;
using System.Windows.Input;
using Hidden_Hearts_in_Wonderland.Models;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.ViewModels;

[QueryProperty(nameof(StartId), "startId")]
[QueryProperty(nameof(CharacterName), "character")]
public class DialogueViewModel : BaseViewModel
{
    private readonly GameService _gameService = GameService.Instance;

    private List<DialogueNode> _allNodes;

    private DialogueNode _currentNode;
    public DialogueNode CurrentNode
    {
        get => _currentNode;
        set
        {
            _currentNode = value;
            OnPropertyChanged();
        }
    }

    private int _affection;
    public int Affection
    {
        get => _affection;
        set
        {
            _affection = value;
            OnPropertyChanged();
        }
    }

    private string _characterName;
    private string _startId;

    public string CharacterName
    {
        set
        {
            _characterName = value;
        }
    }

    public string StartId
    {
        set
        {
            _startId = value;

            // ถ้าโหลด JSON แล้ว ค่อยโหลด node
            if (_allNodes != null)
            {
                LoadNode(_startId);
            }
        }
    }

    public ICommand SelectChoiceCommand { get; }

    public DialogueViewModel()
    {
        SelectChoiceCommand = new Command<Choice>(async choice => await SelectChoice(choice));

        Init(); //  ใช้ async init
    }

    async void Init()
    {
        await LoadDialogueFromJson();

        // โหลด node หลัง JSON พร้อมแล้ว
        if (!string.IsNullOrEmpty(_startId))
            LoadNode(_startId);
        else
            LoadNode("start_aya");
    }

    async Task LoadDialogueFromJson()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("dialogue.json");
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        _allNodes = JsonSerializer.Deserialize<List<DialogueNode>>(json);
    }

    async Task SelectChoice(Choice choice)
    {
        _gameService.ApplyChoice(choice);

        Affection = _gameService.GetCurrentAffection();

        if (choice.IsMiniGame && !string.IsNullOrEmpty(choice.NextNodeId))
        {
            await Shell.Current.GoToAsync(
                $"{nameof(Views.MiniGamePage)}?nextNodeId={choice.NextNodeId}&character={_characterName}"
            );
            return;
        }

        if (!string.IsNullOrEmpty(choice.NextNodeId))
        {
            LoadNode(choice.NextNodeId);
        }
    }

    void LoadNode(string nodeId)
    {
        var node = _allNodes?.FirstOrDefault(n => n.Id == nodeId);

        if (node != null)
        {
            CurrentNode = node;

            Affection = _gameService.GetCurrentAffection();

            if (node.Choices == null || node.Choices.Count == 0)
            {
                CheckEnding();
            }
        }
    }

    void CheckEnding()
    {
        string ending;

        if (Affection >= 40)
            ending = "💖 Good Ending";
        else if (Affection >= 10)
            ending = "🙂 Normal Ending";
        else
            ending = "💔 Bad Ending";

        Application.Current.MainPage.DisplayAlert("จบเกม", ending, "OK");
    }
}
