using Hidden_Hearts_in_Wonderland.ViewModels;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class MiniGamePage : ContentPage
{
    public MiniGamePage()
    {
        InitializeComponent();
        BindingContext = new MiniGameViewModel();
    }
}
