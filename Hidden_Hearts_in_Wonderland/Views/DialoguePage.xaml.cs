using Hidden_Hearts_in_Wonderland.ViewModels;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class DialoguePage : ContentPage
{
    public DialoguePage()
    {
        InitializeComponent();
        BindingContext = new DialogueViewModel();
    }
}