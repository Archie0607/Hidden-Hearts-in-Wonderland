using System.Windows.Input;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views.Controls;

public partial class FantasyButton : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(FantasyButton), "");

    public static readonly BindableProperty ButtonImageProperty = BindableProperty.Create(
        nameof(ButtonImage), typeof(ImageSource), typeof(FantasyButton), ImageSource.FromFile("ui_button_blue_normal.png"));

    public static readonly BindableProperty LabelColorProperty = BindableProperty.Create(
        nameof(LabelColor), typeof(Color), typeof(FantasyButton), Color.FromArgb("#FFF4D6"));

    public static readonly BindableProperty LabelFontSizeProperty = BindableProperty.Create(
        nameof(LabelFontSize), typeof(double), typeof(FantasyButton), 18d);

    public static readonly BindableProperty ButtonWidthProperty = BindableProperty.Create(
        nameof(ButtonWidth), typeof(double), typeof(FantasyButton), 260d);

    public static readonly BindableProperty ButtonHeightProperty = BindableProperty.Create(
        nameof(ButtonHeight), typeof(double), typeof(FantasyButton), 72d);

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(FantasyButton));

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(FantasyButton));

    public event EventHandler? Clicked;

    public FantasyButton()
    {
        // โหลด XAML ของปุ่ม custom fantasy
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ImageSource ButtonImage
    {
        get => (ImageSource)GetValue(ButtonImageProperty);
        set => SetValue(ButtonImageProperty, value);
    }

    public Color LabelColor
    {
        get => (Color)GetValue(LabelColorProperty);
        set => SetValue(LabelColorProperty, value);
    }

    public double LabelFontSize
    {
        get => (double)GetValue(LabelFontSizeProperty);
        set => SetValue(LabelFontSizeProperty, value);
    }

    public double ButtonWidth
    {
        get => (double)GetValue(ButtonWidthProperty);
        set => SetValue(ButtonWidthProperty, value);
    }

    public double ButtonHeight
    {
        get => (double)GetValue(ButtonHeightProperty);
        set => SetValue(ButtonHeightProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        // แตะปุ่มแล้วเล่นเสียง กด command และยิง event เผื่อหน้าไหนฟัง Clicked อยู่
        if (!IsEnabled)
        {
            return;
        }

        _ = AudioService.Instance.PlayClickAsync();

        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }

        Clicked?.Invoke(this, EventArgs.Empty);
    }
}
