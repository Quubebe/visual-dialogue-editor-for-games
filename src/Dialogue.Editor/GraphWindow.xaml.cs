using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace Dialogue.Editor;

public partial class GraphWindow : Window
{
    private const double MinGraphWidth = 1400;
    private const double MinGraphHeight = 900;
    private readonly HashSet<NodeViewModel> _observedNodes = new();
    private readonly List<Rect> _captionBounds = new();
    public GraphWindow(EditorViewModel editor)
    {
        InitializeComponent();
        DataContext = editor;
        foreach (var node in editor.Nodes)
            SubscribeNode(node);

        editor.Nodes.CollectionChanged += Nodes_CollectionChanged;
        Closed += Graph_Closed;
        UpdateGraphExtent();
        DrawConnections();
    }
    private void GraphSurface_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 ||
            !ReferenceEquals(e.OriginalSource, GraphSurface) ||
            DataContext is not EditorViewModel editor)
            return;

        Point point = e.GetPosition(GraphSurface);
        editor.AddNodeAt(point.X, point.Y);
        e.Handled = true;
    }
    private void Card_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb { DataContext: NodeViewModel node })
            return;

        node.X = Math.Max(0, node.X + e.HorizontalChange);
        node.Y = Math.Max(0, node.Y + e.VerticalChange);
    }

    private void Card_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Thumb { DataContext: NodeViewModel node } ||
            DataContext is not EditorViewModel editor)
            return;

        if (Keyboard.Modifiers == ModifierKeys.Control &&
            editor.SelectedNode is { } source &&
            source != node)
        {
            if (source.Choices.Count > 0)
            {
                MessageBox.Show(
                    this,
                    "У исходной реплики есть ответы. Для неё нельзя одновременно задать прямой переход.",
                    "Прямой переход");
                e.Handled = true;
                return;
            }

            source.IsEnding = false;
            source.NextNodeId = node.Id;
            e.Handled = true;
            return;
        }
        if (Keyboard.Modifiers == ModifierKeys.Shift && editor.SelectedNode is { } answerSource && answerSource != node)
        {
            if (answerSource.NextNodeId is not null)
            {
                MessageBox.Show(
                    this,
                    "У исходной реплики задан прямой переход. Сначала уберите его.",
                    "Новый ответ");
                e.Handled = true;
                return;
            }

            editor.AddChoiceTo(answerSource, node.Id);
            e.Handled = true;
            return;
        }

        editor.SelectedNode = node;
    }
    private void Node_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(NodeViewModel.X) or nameof(NodeViewModel.Y))
            UpdateGraphExtent();

        if (e.PropertyName is nameof(NodeViewModel.NextNodeId) or
            nameof(NodeViewModel.X) or nameof(NodeViewModel.Y))
            DrawConnections();
    }

    private void UpdateGraphExtent()
    {
        if (DataContext is not EditorViewModel editor)
            return;

        GraphSurface.Width = Math.Max(MinGraphWidth,
            editor.Nodes.Select(node => node.X + 350).DefaultIfEmpty(0).Max());
        GraphSurface.Height = Math.Max(MinGraphHeight,
            editor.Nodes.Select(node => node.Y + 220).DefaultIfEmpty(0).Max());
    }

    private void Choice_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ChoiceViewModel.TargetNodeId) or nameof(ChoiceViewModel.Text))
            DrawConnections();
    }

    private void SubscribeNode(NodeViewModel node)
    {
        if (!_observedNodes.Add(node))
            return;

        node.PropertyChanged += Node_PropertyChanged;
        node.Choices.CollectionChanged += Choices_CollectionChanged;

        foreach (var choice in node.Choices)
            choice.PropertyChanged += Choice_PropertyChanged;
    }

    private void UnsubscribeNode(NodeViewModel node)
    {
        if (!_observedNodes.Remove(node))
            return;

        node.PropertyChanged -= Node_PropertyChanged;
        node.Choices.CollectionChanged -= Choices_CollectionChanged;

        foreach (var choice in node.Choices)
            choice.PropertyChanged -= Choice_PropertyChanged;
    }

    private void Nodes_CollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        if (DataContext is not EditorViewModel editor)
            return;

        foreach (var node in _observedNodes.ToArray())
        {
            if (!editor.Nodes.Contains(node))
                UnsubscribeNode(node);
        }

        foreach (var node in editor.Nodes)
            SubscribeNode(node);

        UpdateGraphExtent();
        DrawConnections();
    }

    private void Graph_Closed(object? sender, EventArgs e)
    {
        if (DataContext is EditorViewModel editor)
            editor.Nodes.CollectionChanged -= Nodes_CollectionChanged;

        foreach (var node in _observedNodes.ToArray())
            UnsubscribeNode(node);
    }
    private void DrawConnections()
    {
        if (DataContext is not EditorViewModel editor)
            return;

        ConnectionsCanvas.Children.Clear();
        _captionBounds.Clear();

        foreach (var source in editor.Nodes)
        {
            if (source.Model.NextNodeId is Guid nextId)
                DrawConnection(editor, source, nextId, Brushes.SteelBlue);

            foreach (var choice in source.Model.Choices)
            {
                if (choice.TargetNodeId is Guid targetId)
                    DrawConnection(editor, source, targetId, Brushes.DarkOrange, choice.Text);
            }
        }
    }

    private void DrawConnection(EditorViewModel editor, NodeViewModel source, Guid targetId, Brush color, string? label = null)
    {
        var target = editor.Nodes.FirstOrDefault(node => node.Id == targetId);
        if (target is null) return;

        bool goesRight = target.X >= source.X;

        double startX = goesRight ? source.X + 190 : source.X;
        double startY = source.Y + 24;
        double endX = goesRight ? target.X - 4 : target.X + 194;
        double endY = target.Y + 24;

        ConnectionsCanvas.Children.Add(new Line
        {
            X1 = startX,
            Y1 = startY,
            X2 = endX,
            Y2 = endY,
            Stroke = color,
            StrokeThickness = 2
        });

        double angle = Math.Atan2(endY - startY, endX - startX);
        const double arrowSize = 10;

        ConnectionsCanvas.Children.Add(new Polygon
        {
            Fill = color,
            Points = new PointCollection
            {
                new(endX, endY),
                new(endX - arrowSize * Math.Cos(angle - 0.45), endY - arrowSize * Math.Sin(angle - 0.45)),
                new(endX - arrowSize * Math.Cos(angle + 0.45), endY - arrowSize * Math.Sin(angle + 0.45))
            }
        });

        if (!string.IsNullOrWhiteSpace(label))
        {
            var caption = new Border
            {
                Background = Brushes.White,
                BorderBrush = color,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4, 2, 4, 2),
                Child = new TextBlock
                {
                    Text = label,
                    FontSize = 12,
                    MaxWidth = 160,
                    TextTrimming = TextTrimming.CharacterEllipsis
                }
            };

            Rect bounds = FindCaptionBounds(editor, caption,
                (startX + endX) / 2, (startY + endY) / 2);
            Canvas.SetLeft(caption, bounds.X);
            Canvas.SetTop(caption, bounds.Y);
            Panel.SetZIndex(caption, 1);
            _captionBounds.Add(bounds);
            ConnectionsCanvas.Children.Add(caption);
        }
    }

    private Rect FindCaptionBounds(EditorViewModel editor, Border caption,
        double centerX, double centerY)
    {
        caption.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double width = caption.DesiredSize.Width;
        double height = caption.DesiredSize.Height;
        double baseLeft = centerX - width / 2;
        double baseTop = centerY - height / 2;
        double horizontalStep = width + 8;
        double verticalStep = height + 8;

        var offsets = from dx in Enumerable.Range(-12, 25)
                      from dy in Enumerable.Range(-12, 25)
                      orderby dx * dx * horizontalStep * horizontalStep +
                              dy * dy * verticalStep * verticalStep
                      select (dx, dy);

        foreach (var (dx, dy) in offsets)
        {
            double left = baseLeft + dx * horizontalStep;
            double top = baseTop + dy * verticalStep;
            var bounds = new Rect(left, top, width, height);

            if (left < 4 || bounds.Right > GraphSurface.Width - 4 ||
                top < 4 || bounds.Bottom > GraphSurface.Height - 4)
                continue;
            if (_captionBounds.Any(existing => existing.IntersectsWith(bounds)))
                continue;
            if (editor.Nodes.Any(node =>
                new Rect(node.X - 6, node.Y - 6, 202, 160).IntersectsWith(bounds)))
                continue;

            return bounds;
        }

        return new Rect(
            Math.Clamp(baseLeft, 4, Math.Max(4, GraphSurface.Width - width - 4)),
            Math.Clamp(baseTop, 4, Math.Max(4, GraphSurface.Height - height - 4)),
            width, height);
    }

    private void Choices_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (ChoiceViewModel choice in e.OldItems)
                choice.PropertyChanged -= Choice_PropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (ChoiceViewModel choice in e.NewItems)
                choice.PropertyChanged += Choice_PropertyChanged;
        }

        DrawConnections();
    }
}
