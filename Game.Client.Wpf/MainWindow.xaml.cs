using System.Collections.ObjectModel;
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
    private const double Scale = 0.18;
    private const string ServerUrl = "http://localhost:5238/game";
    private const double GridStep = 1000;
    private const double ClickRadiusPx = 24;

    private readonly NetworkChannel _net = new();
    private readonly Dictionary<Guid, EntityVisual> _visuals = new();
    private readonly List<(Line Visual, bool Vertical, double WorldCoord)> _gridLines = new();
    private readonly List<FloatingText> _floatingTexts = new();
    private readonly List<SkillSlot> _skillSlots = new();
    private readonly ObservableCollection<string> _whisperNames = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    private Ellipse? _safeZoneVisual;

    private Guid _myId;
    private string _myName = "";
    private Race _myRace;
    private BaseClass _myBaseClass;
    private int _mySecondClass;
    private EntityDto? _myDto;
    private Guid? _targetId;
    private double _camX = GameConstants.ZoneWidth / 2;
    private double _camY = GameConstants.ZoneHeight / 2;
    private double _lastFrameTime;
    private bool _inGame;

    private int _level = 1;
    private long _exp;
    private long _expToNext = StatCalculator.ExpToNext(1);

    private double _castStart;
    private double _castDuration;

    // Phase 4 state (see MainWindow.Phase4.cs)
    private readonly List<InventoryItemDto> _inventory = new();
    private readonly HashSet<Guid> _myTradeOffer = new();
    private bool _tradeActive;
    private Guid? _pendingTradeFrom;

    public MainWindow()
    {
        InitializeComponent();

        RaceCombo.ItemsSource = Enum.GetValues<Race>();
        RaceCombo.SelectedIndex = 0;
        ClassCombo.ItemsSource = Enum.GetValues<BaseClass>();
        ClassCombo.SelectedIndex = 0;
        WhisperNames.ItemsSource = _whisperNames;

        _net.SnapshotReceived += s => Dispatcher.BeginInvoke(() => ApplySnapshot(s));
        _net.ChatReceived += m => Dispatcher.BeginInvoke(() => AppendChat(m));
        _net.CombatReceived += c => Dispatcher.BeginInvoke(() => OnCombatEvent(c));
        _net.ProgressReceived += p => Dispatcher.BeginInvoke(() => OnProgress(p));
        _net.CastReceived += c => Dispatcher.BeginInvoke(() => OnCast(c));
        _net.InventoryReceived += i => Dispatcher.BeginInvoke(() => OnInventory(i));
        _net.TradeRequestReceived += t => Dispatcher.BeginInvoke(() => OnTradeRequest(t));
        _net.TradeStateReceived += t => Dispatcher.BeginInvoke(() => OnTradeState(t));
        _net.Disconnected += reason => Dispatcher.BeginInvoke(() =>
        {
            _inGame = false;
            StatusText.Text = $"Disconnected: {reason}";
            LoginPanel.Visibility = Visibility.Visible;
        });

        Loaded += (_, _) => BuildWorldDecor();
        PreviewKeyDown += OnPreviewKeyDown;
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

            _myRace = (Race)RaceCombo.SelectedItem!;
            _myBaseClass = (BaseClass)ClassCombo.SelectedItem!;

            var result = await _net.LoginAsync(new LoginRequest(NameInput.Text, _myRace, _myBaseClass));

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

            RebuildSkillBar();

            LoginPanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Visible;
            InventoryButton.Visibility = Visibility.Visible;
            ClassButton.Visibility = Visibility.Visible;

            AppendChat(new ChatMessage("SYSTEM",
                "Click ground = move, click target = attack, 1-5 = skills, I = inventory. " +
                "Chat: plain = local, '!text' = world, '/w Name text' = whisper.",
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
    // Skill bar (rebuilt on class change to include the signature skill)
    // -----------------------------------------------------------------------

    private void RebuildSkillBar()
    {
        SkillBar.Children.Clear();
        _skillSlots.Clear();

        Archetype? archetype = _mySecondClass > 0
            ? ClassCatalog.Get(_mySecondClass)?.Archetype : null;

        int key = 1;
        foreach (var def in SkillCatalog.ForCharacter(_myBaseClass, archetype))
        {
            var button = new Button
            {
                Width = 118, Height = 36, Margin = new Thickness(3, 0, 3, 0),
                FontSize = 11, Content = $"{key}. {def.Name}"
            };
            var slot = new SkillSlot { Def = def, Button = button, Key = key };
            button.Click += (_, _) => UseSkill(slot);
            _skillSlots.Add(slot);
            SkillBar.Children.Add(button);
            key++;
        }

        SkillBar.Visibility = Visibility.Visible;
    }

    private async void UseSkill(SkillSlot slot)
    {
        if (!_inGame || _myDto is { Dead: true })
            return;

        double now = _clock.Elapsed.TotalSeconds;
        if (slot.ReadyAt > now)
            return;

        slot.ReadyAt = now + slot.Def.CooldownTicks * GameConstants.TickSeconds;
        await _net.UseSkillAsync(slot.Def.Id, _targetId);
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_inGame || ChatInput.IsKeyboardFocusWithin)
            return;

        if (e.Key is Key.I)
        {
            ToggleInventory();
            e.Handled = true;
            return;
        }

        int index = e.Key switch
        {
            Key.D1 or Key.NumPad1 => 0,
            Key.D2 or Key.NumPad2 => 1,
            Key.D3 or Key.NumPad3 => 2,
            Key.D4 or Key.NumPad4 => 3,
            Key.D5 or Key.NumPad5 => 4,
            Key.D6 or Key.NumPad6 => 5,
            _ => -1
        };

        if (index >= 0 && index < _skillSlots.Count)
        {
            UseSkill(_skillSlots[index]);
            e.Handled = true;
        }
    }

    private void OnCast(CastInfo cast)
    {
        if (cast.Seconds <= 0)
        {
            CastBar.Visibility = Visibility.Collapsed;
            _castDuration = 0;
            return;
        }

        _castStart = _clock.Elapsed.TotalSeconds;
        _castDuration = cast.Seconds;
        CastText.Text = cast.SkillName;
        CastFill.Width = 0;
        CastBar.Visibility = Visibility.Visible;
    }

    // -----------------------------------------------------------------------
    // Snapshots
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
                if (dto.SecondClass != _mySecondClass)
                {
                    _mySecondClass = dto.SecondClass;
                    RebuildSkillBar();
                }
                DeathOverlay.Visibility = dto.Dead ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        foreach (var id in _visuals.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            WorldCanvas.Children.Remove(_visuals[id].Root);
            _visuals.Remove(id);
            if (_targetId == id)
                _targetId = null;
        }

        UpdateTargetFrame();
    }

    private Brush MobNameBrush(int mobLevel)
    {
        int diff = mobLevel - _level;
        return diff switch
        {
            <= -6 => Brushes.Gray,
            <= -2 => Brushes.LightGreen,
            <= 1 => Brushes.White,
            <= 5 => Brushes.Yellow,
            _ => Brushes.Red
        };
    }

    private void UpdateVisualState(EntityVisual visual, EntityDto dto)
    {
        double ratio = dto.MaxHp > 0 ? Math.Clamp((double)dto.Hp / dto.MaxHp, 0, 1) : 0;
        visual.HpFill.Width = 40 * ratio;
        visual.Root.Opacity = dto.Dead ? 0.45 : 1.0;

        string classTag = dto.Kind == EntityKind.Player && dto.SecondClass > 0
            ? $" {ClassCatalog.Get(dto.SecondClass)?.Name}" : "";
        visual.Label.Text = dto.Dead
            ? $"{dto.Name} Lv{dto.Level} (dead)"
            : $"{dto.Name}{classTag} Lv{dto.Level}";

        if (dto.Kind == EntityKind.Mob)
            visual.Label.Foreground = MobNameBrush(dto.Level);
    }

    private EntityVisual CreateVisual(EntityDto dto)
    {
        Color color = dto.Kind == EntityKind.Mob
            ? Colors.IndianRed
            : dto.BaseClass == BaseClass.Mage ? Colors.CornflowerBlue : Colors.Orange;

        var dot = new Ellipse
        {
            Width = 16, Height = 16,
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
            Foreground = dto.Kind == EntityKind.Mob ? MobNameBrush(dto.Level) : Brushes.White,
            FontSize = 11, TextAlignment = TextAlignment.Center
        };

        var hpFill = new Rectangle
        {
            Fill = Brushes.LimeGreen, HorizontalAlignment = HorizontalAlignment.Left, Width = 40
        };
        var hpBar = new Border
        {
            Width = 40, Height = 5, Margin = new Thickness(0, 2, 0, 0),
            Background = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)),
            HorizontalAlignment = HorizontalAlignment.Center, Child = hpFill
        };

        var stack = new StackPanel { Width = 110 };
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
        EntityVisual? anchor =
            _visuals.TryGetValue(evt.TargetId, out var tv) ? tv :
            _visuals.TryGetValue(evt.AttackerId, out var av) ? av : null;

        if (evt.Outcome == CombatOutcome.Death)
        {
            AppendChat(new ChatMessage("SYSTEM",
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
            CombatOutcome.Fail => ("fail", Brushes.MediumPurple),
            CombatOutcome.Crit => ($"{evt.Damage}!", Brushes.Orange),
            CombatOutcome.Heal => ($"+{evt.Damage}", Brushes.LightGreen),
            CombatOutcome.Buff => (evt.Skill ?? "buff", Brushes.LightSkyBlue),
            _ => (evt.Damage.ToString(),
                  evt.TargetId == _myId ? Brushes.OrangeRed : Brushes.White)
        };

        var tb = new TextBlock
        {
            Text = text, Foreground = brush,
            FontSize = evt.Outcome == CombatOutcome.Crit ? 17 : 14,
            FontWeight = FontWeights.Bold
        };
        WorldCanvas.Children.Add(tb);

        _floatingTexts.Add(new FloatingText
        {
            Visual = tb, WorldX = anchor.CurX, WorldY = anchor.CurY,
            Born = _clock.Elapsed.TotalSeconds
        });
    }

    private void OnProgress(ProgressUpdate progress)
    {
        _level = progress.Level;
        _exp = progress.Exp;
        _expToNext = progress.ExpToNext;
    }

    private void UpdateTargetFrame()
    {
        if (_targetId is Guid id &&
            _visuals.TryGetValue(id, out var visual) &&
            visual.Latest is { Dead: false } dto)
        {
            TargetFrame.Visibility = Visibility.Visible;
            string classTag = dto.SecondClass > 0
                ? $" {ClassCatalog.Get(dto.SecondClass)?.Name}" : "";
            TargetNameText.Text = $"{dto.Name}{classTag} Lv{dto.Level}  {dto.Hp}/{dto.MaxHp}";
            double ratio = dto.MaxHp > 0 ? Math.Clamp((double)dto.Hp / dto.MaxHp, 0, 1) : 0;
            TargetHpFill.Width = 218 * ratio;

            // Trade button only for other living players, nearby, not mid-trade.
            bool canTrade = dto.Kind == EntityKind.Player && !_tradeActive &&
                _myDto is not null &&
                Dist(dto.X, dto.Y, _myDto.X, _myDto.Y) <= GameConstants.TradeRange;
            TradeButton.Visibility = canTrade ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            TargetFrame.Visibility = Visibility.Collapsed;
            TradeButton.Visibility = Visibility.Collapsed;
        }
    }

    private static double Dist(double ax, double ay, double bx, double by)
    {
        double dx = ax - bx, dy = ay - by;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    // -----------------------------------------------------------------------
    // Render loop
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

        if (_visuals.TryGetValue(_myId, out var me))
        {
            _camX = me.CurX;
            _camY = me.CurY;

            var vitals = _myDto is null ? "" :
                $"  HP {_myDto.Hp}/{_myDto.MaxHp}  MP {_myDto.Mp}/{_myDto.MaxMp}";
            var cls = _mySecondClass > 0 ? $" {ClassCatalog.Get(_mySecondClass)?.Name}" : "";
            var zone = _myDto is not null && GameConstants.InSafeZone(_myDto.X, _myDto.Y) ? "  [SAFE]" : "";
            StatusText.Text = $"{_myName}{cls}  Lv{_level}{vitals}  EXP {_exp}/{_expToNext}{zone}";
        }

        double cw = WorldCanvas.ActualWidth;
        double ch = WorldCanvas.ActualHeight;
        if (cw <= 0 || ch <= 0)
            return;

        foreach (var visual in _visuals.Values)
        {
            Canvas.SetLeft(visual.Root, (visual.CurX - _camX) * Scale + cw / 2 - 55);
            Canvas.SetTop(visual.Root, (visual.CurY - _camY) * Scale + ch / 2 - 18);
        }

        UpdateSafeZoneVisual(cw, ch);
        UpdateFloatingTexts(now, cw, ch);
        UpdateGridLines(cw, ch);
        UpdateSkillCooldowns(now);
        UpdateCastBar(now);
    }

    private void UpdateSkillCooldowns(double now)
    {
        foreach (var slot in _skillSlots)
        {
            double remaining = slot.ReadyAt - now;
            if (remaining > 0)
            {
                slot.Button.IsEnabled = false;
                slot.Button.Content = $"{slot.Key}. {slot.Def.Name} ({remaining:0}s)";
            }
            else
            {
                slot.Button.IsEnabled = _myDto is not { Dead: true };
                slot.Button.Content = $"{slot.Key}. {slot.Def.Name}";
            }
        }
    }

    private void UpdateCastBar(double now)
    {
        if (_castDuration <= 0 || CastBar.Visibility != Visibility.Visible)
            return;

        double progress = (now - _castStart) / _castDuration;
        if (progress >= 1)
        {
            CastBar.Visibility = Visibility.Collapsed;
            _castDuration = 0;
            return;
        }
        CastFill.Width = 226 * Math.Clamp(progress, 0, 1);
    }

    // -----------------------------------------------------------------------
    // World decor
    // -----------------------------------------------------------------------

    private void BuildWorldDecor()
    {
        _safeZoneVisual = new Ellipse
        {
            Width = GameConstants.SafeZoneRadius * 2 * Scale,
            Height = GameConstants.SafeZoneRadius * 2 * Scale,
            Fill = new SolidColorBrush(Color.FromArgb(55, 70, 200, 90)),
            Stroke = new SolidColorBrush(Color.FromArgb(120, 90, 220, 110)),
            StrokeThickness = 2
        };
        WorldCanvas.Children.Insert(0, _safeZoneVisual);

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

    private void UpdateSafeZoneVisual(double cw, double ch)
    {
        if (_safeZoneVisual is null)
            return;
        double r = GameConstants.SafeZoneRadius * Scale;
        Canvas.SetLeft(_safeZoneVisual, (GameConstants.ZoneWidth / 2 - _camX) * Scale + cw / 2 - r);
        Canvas.SetTop(_safeZoneVisual, (GameConstants.ZoneHeight / 2 - _camY) * Scale + ch / 2 - r);
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

    private void UpdateGridLines(double cw, double ch)
    {
        foreach (var (line, vertical, world) in _gridLines)
        {
            if (vertical)
            {
                double sx = (world - _camX) * Scale + cw / 2;
                line.Visibility = sx < -2 || sx > cw + 2 ? Visibility.Collapsed : Visibility.Visible;
                line.X1 = sx; line.X2 = sx; line.Y1 = 0; line.Y2 = ch;
            }
            else
            {
                double sy = (world - _camY) * Scale + ch / 2;
                line.Visibility = sy < -2 || sy > ch + 2 ? Visibility.Collapsed : Visibility.Visible;
                line.Y1 = sy; line.Y2 = sy; line.X1 = 0; line.X2 = cw;
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

        Guid? hit = null;
        double best = ClickRadiusPx * ClickRadiusPx;

        foreach (var (id, visual) in _visuals)
        {
            if (id == _myId || visual.Latest is null or { Dead: true })
                continue;

            double sx = (visual.CurX - _camX) * Scale + cw / 2;
            double sy = (visual.CurY - _camY) * Scale + ch / 2;
            double dx = click.X - sx, dy = click.Y - sy;
            double distSq = dx * dx + dy * dy;
            if (distSq < best) { best = distSq; hit = id; }
        }

        if (hit is Guid targetId)
        {
            _targetId = targetId;
            UpdateTargetFrame();

            // Clicking another player just targets; clicking a mob attacks.
            if (_visuals[targetId].Latest is { Kind: EntityKind.Mob })
                await _net.AttackAsync(targetId);
            return;
        }

        double worldX = _camX + (click.X - cw / 2) / Scale;
        double worldY = _camY + (click.Y - ch / 2) / Scale;
        await _net.MoveAsync((float)worldX, (float)worldY);
    }

    private async void RespawnButton_Click(object sender, RoutedEventArgs e) =>
        await _net.RespawnAsync();

    // -----------------------------------------------------------------------
    // Chat
    // -----------------------------------------------------------------------

    private async void ChatInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || !_inGame)
            return;

        string raw = ChatInput.Text.Trim();
        ChatInput.Clear();
        if (raw.Length == 0)
            return;

        if (raw.StartsWith('!'))
        {
            string text = raw[1..].Trim();
            if (text.Length > 0)
                await _net.ChatAsync(text, ChatChannel.World);
            return;
        }

        if (raw.StartsWith("/w ", StringComparison.OrdinalIgnoreCase))
        {
            var rest = raw[3..].Trim();
            int space = rest.IndexOf(' ');
            if (space <= 0 || space == rest.Length - 1)
            {
                AppendChat(new ChatMessage("SYSTEM", "Usage: /w CharName message", ChatChannel.System));
                return;
            }
            string name = rest[..space];
            string text = rest[(space + 1)..].Trim();
            RememberWhisperName(name);
            await _net.ChatAsync(text, ChatChannel.Whisper, name);
            return;
        }

        await _net.ChatAsync(raw, ChatChannel.Local);
    }

    private void RememberWhisperName(string name)
    {
        if (!_whisperNames.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
            _whisperNames.Add(name);
    }

    private bool _whisperSelectionGuard;

    private void WhisperNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_whisperSelectionGuard || WhisperNames.SelectedItem is not string name)
            return;
        ChatInput.Text = $"/w {name} ";
        ChatInput.Focus();
        ChatInput.CaretIndex = ChatInput.Text.Length;
        _whisperSelectionGuard = true;
        WhisperNames.SelectedIndex = -1;
        _whisperSelectionGuard = false;
    }

    private void AppendChat(ChatMessage message)
    {
        switch (message.Channel)
        {
            case ChatChannel.System:
                AddChatLine(SystemList, SystemScroll, $"{message.From}: {message.Text}", Brushes.LightGreen);
                break;
            case ChatChannel.World:
                AddChatLine(AllList, AllScroll, $"[W] {message.From}: {message.Text}", Brushes.Gold);
                AddChatLine(WorldList, WorldScroll, $"{message.From}: {message.Text}", Brushes.Gold);
                break;
            case ChatChannel.Whisper:
                RememberWhisperName(message.From == _myName ? message.To ?? "" : message.From);
                string line = $"{message.From} -> {message.To}: {message.Text}";
                AddChatLine(AllList, AllScroll, $"[PM] {line}", Brushes.Violet);
                AddChatLine(WhisperList, WhisperScroll, line, Brushes.Violet);
                break;
            default:
                AddChatLine(AllList, AllScroll, $"{message.From}: {message.Text}", Brushes.White);
                AddChatLine(LocalList, LocalScroll, $"{message.From}: {message.Text}", Brushes.White);
                break;
        }
    }

    private static void AddChatLine(ItemsControl list, ScrollViewer scroll, string text, Brush brush)
    {
        list.Items.Add(new TextBlock
        {
            Text = text, Foreground = brush, FontSize = 12, TextWrapping = TextWrapping.Wrap
        });
        while (list.Items.Count > GameConstants.ChatHistoryLimit)
            list.Items.RemoveAt(0);
        scroll.ScrollToEnd();
    }

    protected override async void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        await _net.DisposeAsync();
    }

    // -----------------------------------------------------------------------
    // State holders
    // -----------------------------------------------------------------------

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

    private class SkillSlot
    {
        public required SkillDef Def { get; init; }
        public required Button Button { get; init; }
        public required int Key { get; init; }
        public double ReadyAt { get; set; }
    }
}
