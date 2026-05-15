using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;
using Microsoft.Maui.Controls.Shapes;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class MatchGamePage : ContentPage
{
    private const double BoardPadding = 6;
    private const double TileSpacing = 2;
    private const double SideHudWidth = 92;
    private const double MaxTileSize = 76;
    private int _renderedRows;
    private int _renderedColumns;

    public MatchGamePage()
    {
        InitializeComponent();
        BindingContext = new MatchGameViewModel();
        BuildBoardGrid();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        BuildBoardGrid();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();
    }

    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        if (BindingContext is not MatchGameViewModel viewModel || Width <= 0 || Height <= 0)
        {
            return;
        }

        var isLandscape = Width > Height;
        var rows = isLandscape ? 5 : 9;
        var columns = isLandscape ? 9 : 5;

        ApplyHudLayout(isLandscape);
        viewModel.SetBoardShape(rows, columns);
        BuildBoardGrid();
        UpdateTileSize(viewModel, rows, columns, isLandscape);
    }

    private void OnBoardSizeChanged(object? sender, EventArgs e)
    {
        if (BindingContext is not MatchGameViewModel viewModel || Width <= 0 || Height <= 0)
        {
            return;
        }

        UpdateTileSize(viewModel, viewModel.Rows, viewModel.Columns, Width > Height);
    }

    private void UpdateTileSize(MatchGameViewModel viewModel, int rows, int columns, bool isLandscape)
    {
        var availableWidth = GetBoardAvailableWidth(isLandscape, columns);
        var availableHeight = GetBoardAvailableHeight(isLandscape, rows);

        if (availableWidth <= 0 || availableHeight <= 0)
        {
            return;
        }

        var tileSize = Math.Floor(Math.Min(availableWidth / columns, availableHeight / rows));

        viewModel.SetTileSize(Math.Clamp(tileSize, 24, MaxTileSize));
    }

    private double GetBoardAvailableWidth(bool isLandscape, int columns)
    {
        if (BoardBorder.Width > 0)
        {
            return BoardBorder.Width - BoardBorder.Padding.HorizontalThickness - (TileSpacing * (columns - 1));
        }

        var reservedWidth = isLandscape ? (SideHudWidth * 2) + (RootGrid.ColumnSpacing * 2) : 0;
        return Width - RootGrid.Padding.HorizontalThickness - reservedWidth - BoardPadding - (TileSpacing * (columns - 1));
    }

    private double GetBoardAvailableHeight(bool isLandscape, int rows)
    {
        if (BoardBorder.Height > 0)
        {
            return BoardBorder.Height - BoardBorder.Padding.VerticalThickness - (TileSpacing * (rows - 1));
        }

        var reservedHeight = isLandscape ? 0 : TopHud.Height + RootGrid.RowSpacing;
        return Height - RootGrid.Padding.VerticalThickness - reservedHeight - BoardPadding - (TileSpacing * (rows - 1));
    }

    private void ApplyHudLayout(bool isLandscape)
    {
        TopHud.IsVisible = !isLandscape;
        LeftHud.IsVisible = isLandscape;
        RightHud.IsVisible = isLandscape;

        if (isLandscape)
        {
            BoardBorder.SetValue(Grid.ColumnProperty, 1);
            BoardBorder.SetValue(Grid.ColumnSpanProperty, 1);
        }
        else
        {
            BoardBorder.SetValue(Grid.ColumnProperty, 0);
            BoardBorder.SetValue(Grid.ColumnSpanProperty, 3);
        }
    }

    private void BuildBoardGrid()
    {
        if (BindingContext is not MatchGameViewModel viewModel || viewModel.Tiles.Count == 0)
        {
            return;
        }

        if (_renderedRows == viewModel.Rows && _renderedColumns == viewModel.Columns && BoardGrid.Children.Count == viewModel.Tiles.Count)
        {
            return;
        }

        _renderedRows = viewModel.Rows;
        _renderedColumns = viewModel.Columns;

        BoardGrid.Children.Clear();
        BoardGrid.RowDefinitions.Clear();
        BoardGrid.ColumnDefinitions.Clear();
        BoardGrid.RowSpacing = TileSpacing;
        BoardGrid.ColumnSpacing = TileSpacing;

        for (var row = 0; row < viewModel.Rows; row++)
        {
            BoardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        for (var column = 0; column < viewModel.Columns; column++)
        {
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        }

        foreach (var tile in viewModel.Tiles)
        {
            var tileView = CreateTileView(tile, viewModel);
            BoardGrid.SetRow(tileView, tile.Row);
            BoardGrid.SetColumn(tileView, tile.Column);
            BoardGrid.Children.Add(tileView);
        }
    }

    private Border CreateTileView(MatchTile tile, MatchGameViewModel viewModel)
    {
        var slimeImage = new Image
        {
            Aspect = Aspect.AspectFit,
            Margin = 5,
            Clip = new RoundRectangleGeometry
            {
                CornerRadius = 12,
                Rect = new Rect(0, 0, tile.TileSize, tile.TileSize)
            }
        };
        slimeImage.SetBinding(Image.SourceProperty, nameof(MatchTile.Image));

        var tileView = new Border
        {
            BindingContext = tile,
            StrokeThickness = 2,
            Content = slimeImage
        };

        tileView.SetBinding(HeightRequestProperty, nameof(MatchTile.TileSize));
        tileView.SetBinding(WidthRequestProperty, nameof(MatchTile.TileSize));
        tileView.SetBinding(Border.BackgroundColorProperty, nameof(MatchTile.TileBackground));
        tileView.SetBinding(Border.StrokeProperty, nameof(MatchTile.BorderColor));
        tileView.StrokeShape = new RoundRectangle { CornerRadius = 14 };

        var tapGesture = new TapGestureRecognizer
        {
            Command = viewModel.SelectTileCommand,
            CommandParameter = tile
        };
        tapGesture.Tapped += OnTileTapped;
        tileView.GestureRecognizers.Add(tapGesture);

        return tileView;
    }

    private async void OnTileTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not VisualElement tile)
        {
            return;
        }

        _ = AudioService.Instance.PlayClickAsync();
        await tile.ScaleToAsync(0.9, 55, Easing.CubicOut);
        await tile.ScaleToAsync(1, 70, Easing.CubicIn);
    }
}
