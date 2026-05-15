using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class CardBattlePage : ContentPage
{
    private VisualElement? _selectedPlayerView;

    public CardBattlePage()
    {
        InitializeComponent();
        var viewModel = new CardBattleViewModel
        {
            AnimateAttackAsync = AnimateFighterAttack
        };
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = AudioService.Instance.PlayBattleMusicOnceAsync();
    }

    private async void OnPlayerCardTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not VisualElement cardView || BindingContext is not CardBattleViewModel viewModel)
        {
            return;
        }

        if (cardView.BindingContext is not BattleFighter fighter || !fighter.IsAlive || fighter.HasActed)
        {
            return;
        }

        _ = AudioService.Instance.PlayClickAsync();
        viewModel.SelectPlayerCommand.Execute(fighter);
        _selectedPlayerView = cardView;

        await cardView.ScaleTo(1.08, 90, Easing.CubicOut);
        await cardView.ScaleTo(1, 90, Easing.CubicIn);
    }

    private async void OnEnemyCardTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not VisualElement enemyView || BindingContext is not CardBattleViewModel viewModel)
        {
            return;
        }

        if (enemyView.BindingContext is not BattleFighter enemy || _selectedPlayerView == null)
        {
            return;
        }

        _ = AudioService.Instance.PlayClickAsync();
        await AnimateAttack(_selectedPlayerView, enemyView);
        viewModel.AttackEnemyCommand.Execute(enemy);
        _selectedPlayerView = null;
    }

    private static async Task AnimateAttack(VisualElement attacker, VisualElement target)
    {
        var attackerCenter = GetCenter(attacker);
        var targetCenter = GetCenter(target);
        var deltaX = targetCenter.X - attackerCenter.X;
        var deltaY = targetCenter.Y - attackerCenter.Y;

        await attacker.TranslateTo(deltaX, deltaY, 150, Easing.CubicOut);
        await target.ScaleTo(0.94, 60, Easing.CubicOut);
        await target.ScaleTo(1, 70, Easing.CubicIn);
        await attacker.TranslateTo(0, 0, 150, Easing.CubicIn);
    }

    private async Task AnimateFighterAttack(BattleFighter attacker, BattleFighter target)
    {
        var attackerView = FindBoundElement(BattleRoot, attacker);
        var targetView = FindBoundElement(BattleRoot, target);

        if (attackerView == null || targetView == null)
        {
            return;
        }

        await AnimateAttack(attackerView, targetView);
    }

    private static VisualElement? FindBoundElement(Element element, object bindingContext)
    {
        if (element is VisualElement visualElement && ReferenceEquals(visualElement.BindingContext, bindingContext))
        {
            return visualElement;
        }

        foreach (var child in element.LogicalChildren)
        {
            var match = FindBoundElement(child, bindingContext);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static Point GetCenter(VisualElement element)
    {
        var x = element.X + (element.Width / 2);
        var y = element.Y + (element.Height / 2);
        var parent = element.Parent;

        while (parent is VisualElement parentElement)
        {
            x += parentElement.X;
            y += parentElement.Y;
            parent = parentElement.Parent;
        }

        return new Point(x, y);
    }
}
