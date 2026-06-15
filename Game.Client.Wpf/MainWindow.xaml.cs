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
    private const double GridStep = 1000;       // world units between grid lines
    private const double ClickRadiusPx = 24;    // hit-test radius for targeting

    private readonly NetworkChannel _net = new();
    private readonly Dictionary<Guid, EntityVisual> _visuals = new();
    private readonly List<(Line Visual, bool Vertical, double WorldCoord)> _gridLines = new();
    private readonly List<FloatingText> _floatingTexts = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    private Guid _myId;
    private string _myName = "";
    private EntityDto? _myDto;
    private Guid? _targetId;
    private double _camX = GameConstants.ZoneWidth / 2;
    private double _camY = GameConstants.ZoneHeight / 2;
    private double _lastFrameTime;
    private bool _inGame;

    private int _level = 1;
    private long _exp;
    private long _expToNext = StatCalculator.ExpToNext(1);

    public MainWindow()
    {
        InitializeComponent();

        RaceCombo.ItemsSource = Enum.GetValues<Race>();
        RaceCombo.SelectedIndex = 0;
        ClassCombo.ItemsSource = Enum.GetValues<BaseClass>();
        ClassCombo.SelectedIndex = 0;

        _net.SnapshotReceived += s => Dispatcher.BeginInvoke(() => ApplySnapshot(s));
        _net.ChatReceived += m => Dispatcher.BeginInvoke(() => AppendChat(m));
        _net.CombatReceived += c => Dispatcher.BeginInvoke(() => OnCombatEvent(c));
        _net.ProgressReceived += p => Dispatcher.BeginInvoke(() => OnProgress(p));
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
                "Welcome! Left-click ground to move, left-click a mob to attack. " +
                "Enter sends local chat, '/all text' sends world chat.",
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
            visual.Latest = dto;
            UpdateVisualState(visual, dto);

            if (dto.Id == _myId)
            {
                _myDto = dto;
                DeathOverlay.Visibility = dto.Dead ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // Anything not in the snapshot has left our view range (or despawned).
        foreach (var id in _visuals.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            WorldCanvas.Children.Remove(_visuals[id].Root);
            _visuals.Remove(id);
            if (_targetId == id)
                _targetId = null;
        }

        UpdateTargetFrame();
    }

    private void UpdateVisualState(EntityVisual visual, EntityDto dto)
    {
        double ratio = dto.MaxHp > 0 ? Math.Clamp((double)dto.Hp / dto.MaxHp, 0, 1) : 0;
        visual.HpFill.Width = 40 * ratio;
        visual.Root.Opacity = dto.Dead ? 0.45 : 1.0;
        visual.Label.Text = dto.Dead
            ? $"{dto.Name}  Lv{dto.Level} (dead)"
            : $"{dto.Name}  Lv{dto.Level}";
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
            Foreground = dto.Kind == EntityKind.Mob ? Brushes.LightCoral : Brushes.White,
            FontSize = 11,
            TextAlignment = TextAlignment.Center
        };

        var hpFill = new Rectangle
        {
            Fill = Brushes.LimeGreen,
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 40
        };

        var hpBar = new Border
        {
            Width = 40,
            Height = 5,
            Margin = new Thickness(0, 2, 0, 0),
            Background = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = hpFill
        };

        var stack = new StackPanel { Width = 90 };
        stack.Children.Add(label);
        stack.Children.Add(dot);
        stack.Children.Add(hpBar);

        return new EntityVisual { Root = stack, Dot = dot, Label = label, HpFill = hpFill };
    }

    // -----------------------------------------------------------------------
    // Combat feedback
    // -----------------------------------------------------------------------

    private void OnCombatEvent(CombatEvent evt)
    {
        // Floating text anchors to the target (or the attacker as fallback).
        EntityVisual? anchor =
            _visuals.TryGetValue(evt.TargetId, out var tv) ? tv :
            _visuals.TryGetValue(evt.AttackerId, out var av) ? av : null;

        if (evt.Outcome == CombatOutcome.Death)
        {
            AppendChat(new ChatMessage("COMBAT",
                $"{evt.AttackerName} slew {evt.TargetName}.", ChatChannel.System));
            if (_targetId == evt.TargetId)
            {
                _targetId = null;
                UpdateTargetFrame();
            }
            return;
        }

        if (anchor is null)
            return;

        (string text, Brush brush) = evt.Outcome switch
        {
            CombatOutcome.Miss => ("miss", Brushes.Gray),
            CombatOutcome.Crit => ($"{evt.Damage}!", Brushes.Orange),
            _ => (evt.Damage.ToString(),
                  evt.TargetId == _myId ? Brushes.OrangeRed : Brushes.White)
        };

        var tb = new TextBlock
        {
            Text = text,
            Foreground = brush,
            FontSize = evt.Outcome == CombatOutcome.Crit ? 17 : 14,
            FontWeight = FontWeights.Bold
        };
        WorldCanvas.Children.Add(tb);

        _floatingTexts.Add(new FloatingText
        {
            Visual = tb,
            WorldX = anchor.CurX,
            WorldY = anchor.CurY,
            Born = _clock.Elapsed.TotalSeconds
        });
    }

    private void OnProgress(ProgressUpdate progress)
    {
        _level = progress.Level;
        _exp = progress.Exp;
        _expToNext = progress.ExpToNext;

        if (progress.LeveledUp)
            AppendChat(new ChatMessage("SYSTEM",
                $"You reached level {progress.Level}!", ChatChannel.System));
    }

    private void UpdateTargetFrame()
    {
        if (_targetId is Guid id &&
            _visuals.TryGetValue(id, out var visual) &&
            visual.Latest is { Dead: false } dto)
        {
            TargetFrame.Visibility = Visibility.Visible;
            TargetNameText.Text = $"{dto.Name}  Lv{dto.Level}   {dto.Hp}/{dto.MaxHp}";
            double ratio = dto.MaxHp > 0 ? Math.Clamp((double)dto.Hp / dto.MaxHp, 0, 1) : 0;
            TargetHpFill.Width = 208 * ratio;
        }
        else
        {
            TargetFrame.Visibility = Visibility.Collapsed;
        }
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

            var hp = _myDto is null ? "" : $"  HP {_myDto.Hp}/{_myDto.MaxHp}";
            StatusText.Text =
                $"{_myName}  Lv{_level}{hp}  EXP {_exp}/{_expToNext}  " +
                $"({(int)_camX},{(int)_camY})  visible: {_visuals.Count}";
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

        UpdateFloatingTexts(now, cw, ch);
        UpdateGridLines(cw, ch);
    }

    private void UpdateFloatingTexts(double now, double cw, double ch)
    {
        for (int i = _floatingTexts.Count - 1; i >= 0; i--)
        {
            var ft = _floatingTexts[i];
            double age = now - ft.Born;

            if (age > 1.2)
            {
                WorldCanvas.Children.Remove(ft.Visual);
                _floatingTexts.RemoveAt(i);
                continue;
            }

            ft.Visual.Opacity = 1.0 - age / 1.2;
            Canvas.SetLeft(ft.Visual, (ft.WorldX - _camX) * Scale + cw / 2 - 8);
            Canvas.SetTop(ft.Visual, (ft.WorldY - _camY) * Scale + ch / 2 - 34 - age * 38);
        }
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
        if (!_inGame || _myDto is { Dead: true })
            return;

        Point click = e.GetPosition(WorldCanvas);
        double cw = WorldCanvas.ActualWidth;
        double ch = WorldCanvas.ActualHeight;

        // Hit-test entities first: clicking a living entity targets+attacks it.
        Guid? hit = null;
        double best = ClickRadiusPx * ClickRadiusPx;

        foreach (var (id, visual) in _visuals)
        {
            if (id == _myId || visual.Latest is null or { Dead: true })
                continue;

            double sx = (visual.CurX - _camX) * Scale + cw / 2;
            double sy = (visual.CurY - _camY) * Scale + ch / 2;
            double dx = click.X - sx;
            double dy = click.Y - sy;
            double distSq = dx * dx + dy * dy;

            if (distSq < best)
            {
                best = distSq;
                hit = id;
            }
        }

        if (hit is Guid targetId)
        {
            _targetId = targetId;
            UpdateTargetFrame();
            await _net.AttackAsync(targetId);
            return;
        }

        // Otherwise: ground click = move (server cancels any engagement).
        double worldX = _camX + (click.X - cw / 2) / Scale;
        double worldY = _camY + (click.Y - ch / 2) / Scale;
        await _net.MoveAsync((float)worldX, (float)worldY);
    }

    private async void RespawnButton_Click(object sender, RoutedEventArgs e)
    {
        await _net.RespawnAsync();
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

    // -----------------------------------------------------------------------
    // Visual state holders
    // -----------------------------------------------------------------------

    /// <summary>Visual + interpolation state for one entity on screen.</summary>
    private class EntityVisual
    {
        public required StackPanel Root { get; init; }
        public required Ellipse Dot { get; init; }
        public required TextBlock Label { get; init; }
        public required Rectangle HpFill { get; init; }
        public EntityDto? Latest { get; set; }
        public double CurX { get; set; }
        public double CurY { get; set; }
        public double TargetX { get; set; }
        public double TargetY { get; set; }
    }

    private class FloatingText
    {
        public required TextBlock Visual { get; init; }
        public double WorldX { get; init; }
        public double WorldY { get; init; }
        public double Born { get; init; }
    }
}
