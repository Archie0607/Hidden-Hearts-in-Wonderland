using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Hidden_Hearts_in_Wonderland.ViewModels;

[QueryProperty(nameof(NextNodeId), "nextNodeId")]
[QueryProperty(nameof(CharacterName), "character")]
public class MiniGameViewModel : BaseViewModel
{
    private readonly Random _random = new();
    private CardItem? _firstCard;
    private CardItem? _secondCard;
    private bool _isChecking;
    private int _matchedPairs;
    private int _moves;

    public ObservableCollection<CardItem> Cards { get; } = new();

    public ICommand SelectCardCommand { get; }
    public ICommand RestartCommand { get; }

    public string NextNodeId { get; set; } = "lana_memory";
    public string CharacterName { get; set; } = "Lana";

    public int Moves
    {
        get => _moves;
        set
        {
            _moves = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public string StatusText => $"Moves: {Moves}";

    public MiniGameViewModel()
    {
        // เตรียม command สำหรับพลิกการ์ดและเริ่มเกมจับคู่ใหม่
        SelectCardCommand = new Command<CardItem>(async card => await SelectCard(card));
        RestartCommand = new Command(BuildDeck);
        BuildDeck();
    }

    private void BuildDeck()
    {
        // สร้างสำรับคู่การ์ดใหม่ สุ่มลำดับ แล้วรีเซ็ตสถานะเกมทั้งหมด
        Cards.Clear();
        _firstCard = null;
        _secondCard = null;
        _isChecking = false;
        _matchedPairs = 0;
        Moves = 0;

        var pairs = new[]
        {
            ("Tea", "ชา"),
            ("Book", "หนังสือ"),
            ("Flower", "ดอกไม้"),
            ("Heart", "หัวใจ")
        };

        foreach (var pair in pairs.OrderBy(_ => _random.Next()))
        {
            Cards.Add(new CardItem(pair.Item1, pair.Item2));
            Cards.Add(new CardItem(pair.Item1, pair.Item2));
        }

        var shuffled = Cards.OrderBy(_ => _random.Next()).ToList();
        Cards.Clear();

        foreach (var card in shuffled)
        {
            Cards.Add(card);
        }
    }

    private async Task SelectCard(CardItem? card)
    {
        // พลิกการ์ดทีละใบ พอครบสองใบจะเช็กว่าเป็นคู่เดียวกันไหม
        if (card == null || _isChecking || card.IsMatched || card.IsFaceUp)
        {
            return;
        }

        card.IsFaceUp = true;

        if (_firstCard == null)
        {
            _firstCard = card;
            return;
        }

        _secondCard = card;
        Moves++;

        if (_firstCard.MatchKey == _secondCard.MatchKey)
        {
            _firstCard.IsMatched = true;
            _secondCard.IsMatched = true;
            _matchedPairs++;
            ClearSelection();

            if (_matchedPairs == Cards.Count / 2)
            {
                await FinishGame();
            }

            return;
        }

        _isChecking = true;
        await Task.Delay(700);

        _firstCard.IsFaceUp = false;
        _secondCard.IsFaceUp = false;
        ClearSelection();
        _isChecking = false;
    }

    private void ClearSelection()
    {
        // ล้างการ์ดสองใบที่กำลังถือไว้ เพื่อรอเลือกคู่ถัดไป
        _firstCard = null;
        _secondCard = null;
    }

    private async Task FinishGame()
    {
        // จบเกมจับคู่ แล้วส่งผู้เล่นกลับไปยังบทสนทนาตาม next node
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;

        if (page != null)
        {
            await page.DisplayAlertAsync("สำเร็จ", $"จับคู่ครบแล้ว ใช้ {Moves} moves", "ไปต่อ");
        }

        await Shell.Current.GoToAsync($"../{nameof(Views.DialoguePage)}?startId={NextNodeId}&character={CharacterName}");
    }
}

public class CardItem : BaseViewModel
{
    private bool _isFaceUp;
    private bool _isMatched;

    public CardItem(string matchKey, string value)
    {
        // เก็บ key สำหรับเทียบคู่ และข้อความที่จะแสดงตอนการ์ดหงาย
        MatchKey = matchKey;
        Value = value;
    }

    public string MatchKey { get; }
    public string Value { get; }

    public bool IsFaceUp
    {
        get => _isFaceUp;
        set
        {
            _isFaceUp = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
            OnPropertyChanged(nameof(CardColor));
        }
    }

    public bool IsMatched
    {
        get => _isMatched;
        set
        {
            _isMatched = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CardColor));
        }
    }

    public string DisplayText => IsFaceUp || IsMatched ? Value : "?";
    public Color CardColor => IsMatched ? Color.FromArgb("#C8E6C9") : IsFaceUp ? Color.FromArgb("#FFF8E1") : Color.FromArgb("#90CAF9");
}
