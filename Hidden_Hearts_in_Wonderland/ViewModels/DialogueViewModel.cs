using System.Text.Json;
using System.Windows.Input;
using Hidden_Hearts_in_Wonderland.Models;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.ViewModels;

public class DialogueViewModel : BaseViewModel, IQueryAttributable
{
    private const string RoundEndNodeId = "__round_end__";
    private const int MaxTurnsPerRound = 6;

    private readonly GameService _gameService = GameService.Instance;
    private readonly AiDialogueService _aiDialogueService = new();
    private readonly List<string> _roundHistory = [];

    private List<DialogueNode> _allNodes;
    private Character _character = CharacterProfileService.GetCharacters()[0];
    private int _turnCount;
    private int _sceneTurnIndex;
    private int _loadVersion;
    private string _roundId = Guid.NewGuid().ToString("N");
    private AiDialogueService.ScenePack? _scenePack;

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

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
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

    public ICommand SelectChoiceCommand { get; }

    public DialogueViewModel()
    {
        SelectChoiceCommand = new Command<Choice>(async choice => await SelectChoice(choice));

        Init(); //  ใช้ async init
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _startId = GetQueryValue(query, "startId");
        _characterName = GetQueryValue(query, "character");
        _roundId = GetQueryValue(query, "roundId");

        if (string.IsNullOrWhiteSpace(_roundId))
        {
            _roundId = Guid.NewGuid().ToString("N");
        }

        if (!string.IsNullOrWhiteSpace(_characterName))
        {
            _character = CharacterProfileService.GetByName(_characterName);
            _gameService.SelectCharacter(_character.Name);
        }

        _ = LoadConfiguredStartNode();
    }

    async void Init()
    {
        await LoadDialogueFromJson();

        if (!string.IsNullOrEmpty(_startId))
            await LoadConfiguredStartNode();
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
        if (choice == null || IsBusy)
            return;

        if (choice.NextNodeId == RoundEndNodeId)
        {
            await EndRound();
            return;
        }

        if (choice.NextNodeId == AiDialogueService.GeneratedNextNodeId)
        {
            await SelectPregeneratedChoice(choice);
            return;
        }

        if (choice.IsMiniGame && !string.IsNullOrEmpty(choice.NextNodeId))
        {
            _gameService.ApplyChoice(choice);
            Affection = _gameService.GetCurrentAffection();

            await Shell.Current.GoToAsync(
                $"{nameof(Views.MatchGamePage)}?nextNodeId={choice.NextNodeId}&character={_characterName}"
            );
            return;
        }

        if (!string.IsNullOrEmpty(choice.NextNodeId))
        {
            _gameService.ApplyChoice(choice);
            Affection = _gameService.GetCurrentAffection();

            await LoadNode(choice.NextNodeId, ++_loadVersion);
        }
    }

    async Task SelectPregeneratedChoice(Choice choice)
    {
        if (_scenePack == null || _sceneTurnIndex < 0 || _sceneTurnIndex >= _scenePack.Turns.Count)
        {
            await ShowAiError(new InvalidOperationException("ไม่พบชุดบทสนทนาที่ AI เจนไว้ กรุณากลับไปเลือกตัวละครใหม่"));
            return;
        }

        var sceneTurn = _scenePack.Turns[_sceneTurnIndex];
        var sceneChoice = FindSceneChoice(sceneTurn, choice);
        if (sceneChoice == null)
        {
            await ShowAiError(new InvalidOperationException("ช้อยนี้ไม่ตรงกับชุดบทสนทนาที่ AI เจนไว้ กรุณากลับไปเลือกตัวละครใหม่"));
            return;
        }

        _turnCount++;
        _gameService.ApplyChoice(choice);
        Affection = _gameService.GetCurrentAffection();

        AddRoundHistory($"Player: {choice.Text}");

        var nextChoices = CreateEndRoundChoices();
        if (_turnCount < MaxTurnsPerRound && _sceneTurnIndex + 1 < _scenePack.Turns.Count)
        {
            _sceneTurnIndex++;
            nextChoices = CreateSceneChoices(_scenePack.Turns[_sceneTurnIndex]);
        }

        CurrentNode = new DialogueNode
        {
            Id = $"scene_reply_{_roundId}_{_turnCount}_{Guid.NewGuid():N}",
            CharacterName = _character.Name,
            Image = CharacterProfileService.GetImageForMood(_character.Name, sceneChoice.MoodTag, sceneChoice.AffectionChange),
            Text = sceneChoice.Reply.Trim(),
            Choices = nextChoices
        };

        AddRoundHistory($"{_character.Name}: {CurrentNode.Text}");
    }

    async Task LoadConfiguredStartNode()
    {
        if (_allNodes == null || string.IsNullOrWhiteSpace(_startId))
            return;

        await LoadNode(_startId, ++_loadVersion);
    }

    async Task LoadNode(string nodeId, int loadVersion)
    {
        var node = _allNodes?.FirstOrDefault(n => n.Id == nodeId);

        if (node != null)
        {
            _turnCount = 0;
            _sceneTurnIndex = 0;
            _scenePack = null;
            _roundHistory.Clear();
            _character = CharacterProfileService.GetByStartNodeId(node.Id);
            _gameService.SelectCharacter(_character.Name);
            Affection = _gameService.GetCurrentAffection();

            IsBusy = true;
            CurrentNode = CreateLoadingNode();

            try
            {
                _scenePack = await _aiDialogueService.GenerateScenePackAsync(_character, node, Affection, _roundId);
                if (loadVersion != _loadVersion)
                    return;

                CurrentNode = CreateSceneTurnNode(_scenePack.Turns[0], 0);
                AddRoundHistory($"{_character.Name}: {CurrentNode.Text}");

                if (CurrentNode.Choices == null || CurrentNode.Choices.Count == 0)
                {
                    CheckEnding();
                }
            }
            catch (Exception ex)
            {
                CurrentNode = CreateAiErrorNode(ex);
                await ShowAiError(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    async Task EndRound()
    {
        _turnCount = 0;
        _sceneTurnIndex = 0;
        _scenePack = null;
        _roundHistory.Clear();
        IsBusy = false;

        await Shell.Current.Navigation.PopToRootAsync();
        await Shell.Current.GoToAsync(nameof(Views.CharacterSelectPage));
    }

    DialogueNode CreateSceneTurnNode(AiDialogueService.SceneTurn turn, int turnIndex)
    {
        _sceneTurnIndex = turnIndex;
        return new DialogueNode
        {
            Id = $"scene_{_roundId}_{turnIndex + 1}",
            CharacterName = _character.Name,
            Text = turn.Reply.Trim(),
            Image = CharacterProfileService.GetImageForMood(_character.Name, turn.MoodTag, 0),
            Choices = CreateSceneChoices(turn)
        };
    }

    List<Choice> CreateSceneChoices(AiDialogueService.SceneTurn turn)
    {
        return turn.Choices
            .Select((choice, index) => new Choice
            {
                Text = choice.Text.Trim(),
                NextNodeId = AiDialogueService.GeneratedNextNodeId,
                AffectionChange = choice.AffectionChange,
                IsMiniGame = false,
                ExpectedMood = string.IsNullOrWhiteSpace(choice.ExpectedMood) ? "neutral" : choice.ExpectedMood.Trim(),
                SceneChoiceIndex = index
            })
            .ToList();
    }

    List<Choice> CreateEndRoundChoices()
    {
        return
        [
            new Choice
            {
                Text = "จบรอบนี้",
                NextNodeId = RoundEndNodeId,
                AffectionChange = 0,
                IsMiniGame = false,
                ExpectedMood = "neutral"
            }
        ];
    }

    static AiDialogueService.SceneChoice? FindSceneChoice(AiDialogueService.SceneTurn turn, Choice choice)
    {
        if (choice.SceneChoiceIndex >= 0 && choice.SceneChoiceIndex < turn.Choices.Count)
            return turn.Choices[choice.SceneChoiceIndex];

        return turn.Choices.FirstOrDefault(sceneChoice => sceneChoice.Text == choice.Text);
    }

    DialogueNode CreateLoadingNode()
    {
        return new DialogueNode
        {
            Id = $"loading_{_roundId}",
            CharacterName = _character.Name,
            Text = "กำลังเริ่มบทสนทนา...",
            Image = CharacterProfileService.GetImageForMood(_character.Name, "neutral", 0),
            Choices = []
        };
    }

    DialogueNode CreateAiErrorNode(Exception exception)
    {
        return new DialogueNode
        {
            Id = $"ai_error_{Guid.NewGuid():N}",
            CharacterName = _character.Name,
            Text = $"AI สร้างบทสนทนาไม่สำเร็จ\n{FormatAiError(exception.Message)}",
            Image = CharacterProfileService.GetImageForMood(_character.Name, "neutral", 0),
            Choices = []
        };
    }

    async Task ShowAiError(Exception exception)
    {
        if (Application.Current?.MainPage == null)
            return;

        await Application.Current.MainPage.DisplayAlert(
            "AI Error",
            $"Groq สร้างบทสนทนาไม่สำเร็จ\n\n{FormatAiError(exception.Message)}",
            "OK");
    }

    static string FormatAiError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "ไม่พบรายละเอียดจาก API";

        var safeMessage = message;

        const int maxLength = 360;
        return safeMessage.Length <= maxLength
            ? safeMessage
            : $"{safeMessage[..maxLength]}...";
    }

    void AddRoundHistory(string entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
            return;

        _roundHistory.Add(entry.Trim());

        const int maxHistoryEntries = MaxTurnsPerRound * 2 + 1;
        if (_roundHistory.Count > maxHistoryEntries)
        {
            _roundHistory.RemoveRange(0, _roundHistory.Count - maxHistoryEntries);
        }
    }

    static string GetQueryValue(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var value))
            return string.Empty;

        return Uri.UnescapeDataString(value?.ToString() ?? string.Empty);
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
