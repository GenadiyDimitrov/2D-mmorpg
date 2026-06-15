using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Game.Shared;

namespace Game.Client.Wpf;

public partial class MainWindow : Window
{
    /// <summary>World units -> screen pixels. 0.18 means the 3000-unit view
    /// range becomes a 540 px radius, which fits a 1280x800 window.</summary>
    private const double Scale = 0.18;

    private const string ServerUrl = "http://localhost:5238/game";
    private const double GridStep = 1000; // world units between background grid lines

    private readonly NetworkChannel _net = new();
    private readonly Dictionary<Guid, EntityVisual> _visuals = new();
    private readonly List<(Line Visual, bool Vertical, double WorldCoord)> _gridLines = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    private Guid _myId;
    private string _myName = "";
    private double _camX = GameConstants.ZoneWidth / 2;
    private double _camY = GameConstants.ZoneHeight / 2;
    private double _lastFrameTime;
    private bool _inGame;

    public MainWindow()
    {
        InitializeComponent();

        RaceCombo.ItemsSource = Enum.GetValues<Race>();
        RaceCombo.SelectedIndex = 0;
        ClassCombo.ItemsSource = Enum.GetValues<BaseClass>();
        ClassCombo.SelectedIndex = 0;

        _net.SnapshotReceived += snapshot => Dispatcher.BeginInvoke(() => ApplySnapshot(snapshot));
        _net.ChatReceived += message => Dispatcher.BeginInvoke(() => AppendChat(message));
        _net.Disconnected += reason => Dispatcher.BeginInvoke(() =>
        {
            _inGame = false;
            StatusText.Text = $"Disconnected: {reason}";
            LoginPanel.Visibility = Visibility.Visible;
        });

        Loaded += (_, _) => BuildGridLines();
        CompositionTarget.Rendering += OnRenderFrame;
    }

    // -----------------------------------------------------------------------
    // Login
    // -----------------------------------------------------------------------

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        ConnectButton.IsEnabled = false;
        LoginError.Visibility = Visibility.Collapsed;

        try
        {
            if (!_net.IsConnected)
            {
                StatusText.Text = "Connecting...";
                await _net.ConnectAsync(ServerUrl);
            }

            var request = new LoginRequest(
                NameInput.Text,
                (Race)RaceCombo.SelectedItem!,
                (BaseClass)ClassCombo.SelectedItem!);

            var result = await _net.LoginAsync(request);

            if (!result.Success)
            {
                ShowLoginError(result.Error ?? "Login failed.");
                return;
            }

            _myId = result.EntityId;
            _myName = NameInput.Text.Trim();
            _camX = result.X;
            _camY = result.Y;
            _inGame = true;

            LoginPanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Visible;
            AppendChat(new ChatMessage("SYSTEM",
                "Welcome! Left-click to move. Enter sends local chat, '/all text' sends world chat.",
                ChatChannel.System));
        }
        catch (Exception ex)
        {
            ShowLoginError($"Could not reach the server at {ServerUrl}.\n" +
                           $"Is Game.Server running?\n({ex.Message})");
        }
        finally
        {
            ConnectButton.IsEnabled = true;
        }
    }

    private void ShowLoginError(string text)
    {
        LoginError.Text = text;
        LoginError.Visibility = Visibility.Visible;
        StatusText.Text = "Not connected";
    }

    // -----------------------------------------------------------------------
    // Snapshots -> visuals
    // -----------------------------------------------------------------------

    private void ApplySnapshot(WorldSnapshot snapshot)
    {
        var seen = new HashSet<Guid>();

        foreach (var dto in snapshot.Entities)
        {
            seen.Add(dto.Id);

            if (!_visuals.TryGetValue(dto.Id, out var visual))
            {
                visual = CreateVisual(dto);
                visual.CurX = dto.X;
                visual.CurY = dto.Y;
                _visuals[dto.Id] = visual;
                WorldCanvas.Children.Add(visual.Root);
            }

            visual.TargetX = dto.X;
            visual.TargetY = dto.Y;
        }

        // Anything we knew about that is no longer in the snapshot has left
        // our view range (or logged off) — remove its visual.
        foreach (var id in _visuals.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            WorldCanvas.Children.Remove(_visuals[id].Root);
            _visuals.Remove(id);
        }
    }

    private EntityVisual CreateVisual(EntityDto dto)
    {
        Color color = dto.Kind == EntityKind.Mob
            ? Colors.IndianRed
            : dto.BaseClass == BaseClass.Mage ? Colors.CornflowerBlue : Colors.Orange;

        var dot = new Ellipse
        {
            Width = 16,
            Height = 16,
            Fill = new SolidColorBrush(color),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        if (dto.Id == _myId)
        {
            dot.Stroke = Brushes.White;
            dot.StrokeThickness = 2.5;
        }

        var label = new TextBlock
        {
            Text = $"{dto.Name}  Lv{dto.Level}",
            Foreground = dto.Kind == EntityKind.Mob ? Brushes.LightCoral : Brushes.White,
            FontSize = 11,
            TextAlignment = TextAlignment.Center
        };

        var stack = new StackPanel { Width = 90 };
        stack.Children.Add(label);
        stack.Children.Add(dot);

        return new EntityVisual { Root = stack };
    }

    // -----------------------------------------------------------------------
    // Render loop — interpolates between 10 t/s snapshots for smooth motion
    // -----------------------------------------------------------------------

    private void OnRenderFrame(object? sender, EventArgs e)
    {
        double now = _clock.Elapsed.TotalSeconds;
        double dt = Math.Min(0.1, now - _lastFrameTime);
        _lastFrameTime = now;

        if (!_inGame && _visuals.Count == 0)
            return;

        double lerp = Math.Min(1.0, dt * 12);

        foreach (var visual in _visuals.Values)
        {
            visual.CurX += (visual.TargetX - visual.CurX) * lerp;
            visual.CurY += (visual.TargetY - visual.CurY) * lerp;
        }

        // Camera follows my own (interpolated) entity.
        if (_visuals.TryGetValue(_myId, out var me))
        {
            _camX = me.CurX;
            _camY = me.CurY;
            StatusText.Text = $"{_myName}   ({(int)_camX}, {(int)_camY})   visible: {_visuals.Count}";
        }

        double cw = WorldCanvas.ActualWidth;
        double ch = WorldCanvas.ActualHeight;
        if (cw <= 0 || ch <= 0)
            return;

        foreach (var visual in _visuals.Values)
        {
            Canvas.SetLeft(visual.Root, (visual.CurX - _camX) * Scale + cw / 2 - 45);
            Canvas.SetTop(visual.Root, (visual.CurY - _camY) * Scale + ch / 2 - 18);
        }

        UpdateGridLines(cw, ch);
    }

    // -----------------------------------------------------------------------
    // World-anchored background grid (gives the feeling of movement)
    // -----------------------------------------------------------------------

    private void BuildGridLines()
    {
        var brush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
        brush.Freeze();

        for (double x = 0; x <= GameConstants.ZoneWidth; x += GridStep)
        {
            var line = new Line { Stroke = brush, StrokeThickness = 1 };
            _gridLines.Add((line, true, x));
            WorldCanvas.Children.Insert(0, line);
        }

        for (double y = 0; y <= GameConstants.ZoneHeight; y += GridStep)
        {
            var line = new Line { Stroke = brush, StrokeThickness = 1 };
            _gridLines.Add((line, false, y));
            WorldCanvas.Children.Insert(0, line);
        }
    }

    private void UpdateGridLines(double cw, double ch)
    {
        foreach (var (line, vertical, world) in _gridLines)
        {
            if (vertical)
            {
                double sx = (world - _camX) * Scale + cw / 2;
                line.Visibility = sx < -2 || sx > cw + 2 ? Visibility.Collapsed : Visibility.Visible;
                line.X1 = sx; line.X2 = sx;
                line.Y1 = 0; line.Y2 = ch;
            }
            else
            {
                double sy = (world - _camY) * Scale + ch / 2;
                line.Visibility = sy < -2 || sy > ch + 2 ? Visibility.Collapsed : Visibility.Visible;
                line.Y1 = sy; line.Y2 = sy;
                line.X1 = 0; line.X2 = cw;
            }
        }
    }

    // -----------------------------------------------------------------------
    // Input
    // -----------------------------------------------------------------------

    private async void WorldCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_inGame)
            return;

        Point click = e.GetPosition(WorldCanvas);
        double worldX = _camX + (click.X - WorldCanvas.ActualWidth / 2) / Scale;
        double worldY = _camY + (click.Y - WorldCanvas.ActualHeight / 2) / Scale;

        await _net.MoveAsync((float)worldX, (float)worldY);
    }

    private async void ChatInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || !_inGame)
            return;

        string text = ChatInput.Text.Trim();
        ChatInput.Clear();

        if (text.Length == 0)
            return;

        var channel = ChatChannel.Local;
        if (text.StartsWith("/all ", StringComparison.OrdinalIgnoreCase))
        {
            channel = ChatChannel.World;
            text = text[5..].Trim();
            if (text.Length == 0)
                return;
        }

        await _net.ChatAsync(text, channel);
    }

    private void AppendChat(ChatMessage message)
    {
        Brush brush = message.Channel switch
        {
            ChatChannel.World => Brushes.Gold,
            ChatChannel.System => Brushes.LightGreen,
            _ => Brushes.White
        };

        ChatList.Items.Add(new TextBlock
        {
            Text = $"[{message.Channel}] {message.From}: {message.Text}",
            Foreground = brush,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        });

        if (ChatList.Items.Count > 100)
            ChatList.Items.RemoveAt(0);

        ChatScroll.ScrollToEnd();
    }

    protected override async void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        await _net.DisposeAsync();
    }

    /// <summary>Visual + interpolation state for one entity on screen.</summary>
    private class EntityVisual
    {
        public required StackPanel Root { get; init; }
        public double CurX { get; set; }
        public double CurY { get; set; }
        public double TargetX { get; set; }
        public double TargetY { get; set; }
    }
}
