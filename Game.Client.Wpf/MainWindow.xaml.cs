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

    private readonly List<(Ellipse Visual, TextBlock Label, float X, float Y, float Radius)> _safeZoneVisuals = new();
    // World-map decor (positioned each frame like the safe zone).
    private readonly List<(Ellipse Visual, TextBlock Label, float X, float Y, float Radius)> _spawnZoneVisuals = new();
    private readonly List<(Polyline Visual, MapPoint[] Points)> _roadVisuals = new();
    private System.Windows.Shapes.Rectangle? _borderVisual;

    private Guid _myId;
    private string _myName = "";
    private Race _myRace;
    private BaseClass _myBaseClass;
    private int _mySecondClass;
    private int _myThirdClass;
    private bool _isAdmin;
    private DateTime _serverEpoch = DateTime.UtcNow;
    private EntityDto? _myDto;
    private Guid? _targetId;
    // Whether the expanded inspect panel on the target frame is open (persists across
    // targets; we re-request inspect data each time the panel is open and target changes).
    private bool _targetExpanded;
    // Throttle/track the live inspect refresh while the expand panel is open.
    private DateTime _lastInspectSent = DateTime.MinValue;
    private Guid? _inspectedTarget;
    // When you click a far NPC we walk you to it and talk on arrival; this holds the
    // NPC we're heading to (cleared when we talk, or when you click somewhere else).
    private Guid? _pendingTalkNpcId;
    private double _camX = GameConstants.ZoneWidth / 2;
    private double _camY = GameConstants.ZoneHeight / 2;
    private double _lastFrameTime;
    private bool _inGame;
    private bool _classQuestNoticeShown;
    private bool _thirdClassNoticeShown;

    private int _level = 1;
    private long _exp;
    private long _expToNext = StatCalculator.ExpToNext(1);
    private long _gold;

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

        WhisperNames.ItemsSource = _whisperNames;
        BuildCreationTree();
        _ = ConnectToServerAsync();

        _net.SnapshotReceived += s => Dispatcher.BeginInvoke(() => ApplySnapshot(s));
        _net.ChatReceived += m => Dispatcher.BeginInvoke(() => AppendChat(m));
        _net.CombatReceived += c => Dispatcher.BeginInvoke(() => OnCombatEvent(c));
        _net.ProgressReceived += p => Dispatcher.BeginInvoke(() => OnProgress(p));
        _net.GoldReceived += g => Dispatcher.BeginInvoke(() => OnGold(g));
        _net.CastReceived += c => Dispatcher.BeginInvoke(() => OnCast(c));
        _net.InventoryReceived += i => Dispatcher.BeginInvoke(() => OnInventory(i));
        _net.TradeRequestReceived += t => Dispatcher.BeginInvoke(() => OnTradeRequest(t));
        _net.TradeStateReceived += t => Dispatcher.BeginInvoke(() => OnTradeState(t));
        _net.StatsReceived += st => Dispatcher.BeginInvoke(() => OnStats(st));
        _net.LearnedReceived += l => Dispatcher.BeginInvoke(() => OnLearned(l));
        _net.DialogReceived += d => Dispatcher.BeginInvoke(() => OnDialog(d));
        _net.QuestLogReceived += q => Dispatcher.BeginInvoke(() => OnQuestLog(q));
        _net.PotionReceived += pt => Dispatcher.BeginInvoke(() => OnPotion(pt));
        _net.BuffsReceived += b => Dispatcher.BeginInvoke(() => OnBuffs(b));
        _net.SelectionReceived += o => Dispatcher.BeginInvoke(() => OnSelection(o));
        _net.TargetDetailsReceived += d => Dispatcher.BeginInvoke(() => OnTargetDetails(d));
        _net.EnchantReceived += en => Dispatcher.BeginInvoke(() => OnEnchant(en));
        _net.RerollReceived += r => Dispatcher.BeginInvoke(() => OnReroll(r));
        _net.ForceDisconnected += reason => Dispatcher.BeginInvoke(() =>
        {
            _inGame = false;
            MessageBox.Show(reason, "Disconnected");
            ShowAccountPanel();
        });
        _net.Disconnected += reason => Dispatcher.BeginInvoke(() =>
        {
            _inGame = false;
            StatusText.Text = $"Disconnected: {reason}";
            ShowAccountPanel();
        });

        Loaded += (_, _) => BuildWorldDecor();
        PreviewKeyDown += OnPreviewKeyDown;
        CompositionTarget.Rendering += OnRenderFrame;
    }

    // -----------------------------------------------------------------------
    // Login
    // -----------------------------------------------------------------------

    private async Task ConnectToServerAsync()
    {
        try
        {
            StatusText.Text = "Connecting...";
            await _net.ConnectAsync(ServerUrl);
            StatusText.Text = "Connected.";
        }
        catch (Exception ex)
        {
            ShowAccountError($"Could not reach the server at {ServerUrl}.\n" +
                             $"Is Game.Server running?\n({ex.Message})");
        }
    }

    private bool _registerMode;

    private void ToggleAuth_Click(object sender, RoutedEventArgs e)
    {
        _registerMode = !_registerMode;
        AccountModeText.Text = _registerMode ? "Create a new account" : "Log in to your account";
        LoginActionButton.Content = _registerMode ? "Register" : "Log In";
        ToggleAuthButton.Content = _registerMode
            ? "Have an account? Log in" : "Need an account? Register";
        AccountError.Visibility = Visibility.Collapsed;
    }

    private async void LoginAction_Click(object sender, RoutedEventArgs e)
    {
        AccountError.Visibility = Visibility.Collapsed;
        LoginActionButton.IsEnabled = false;

        try
        {
            if (!_net.IsConnected)
                await _net.ConnectAsync(ServerUrl);

            string user = UsernameInput.Text;
            string pass = PasswordInput.Password;

            var result = _registerMode
                ? await _net.RegisterAsync(user, pass)
                : await _net.LoginAsync(user, pass);

            if (!result.Success)
            {
                ShowAccountError(result.Error ?? "Authentication failed.");
                return;
            }

            _isAdmin = result.IsAdmin;
            await ShowCharacterSelectAsync();
        }
        catch (Exception ex)
        {
            ShowAccountError($"Connection error: {ex.Message}");
        }
        finally
        {
            LoginActionButton.IsEnabled = true;
        }
    }

    private async Task ShowCharacterSelectAsync()
    {
        AccountPanel.Visibility = Visibility.Collapsed;
        CreatePanel.Visibility = Visibility.Collapsed;
        CharacterSelectPanel.Visibility = Visibility.Visible;

        var list = await _net.ListCharactersAsync();
        CharacterSlots.Children.Clear();

        CharacterSlots.Children.Add(new TextBlock
        {
            Text = list.Characters.Length == 0
                ? "No characters yet. Create one below."
                : $"Characters: {list.Characters.Length} / {GameConstants.MaxCharactersPerAccount}",
            Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 8)
        });

        foreach (var c in list.Characters)
        {
            string cls = c.SecondClass > 0
                ? ClassCatalog.Get(c.SecondClass)?.Name ?? c.BaseClass.ToString()
                : c.BaseClass.ToString();

            int id = c.Id;
            string name = c.Name;
            int level = c.Level;
            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };

            if (c.PendingDeleteAt is DateTime when)
            {
                // Scheduled for deletion: can't play; offer a Cancel (restore).
                var cancel = new Button
                {
                    Content = "Cancel", Width = 96, Height = 40, FontSize = 12,
                    Background = new SolidColorBrush(Color.FromRgb(70, 100, 60)), Foreground = Brushes.White
                };
                cancel.Click += async (_, _) =>
                {
                    await _net.CancelDeleteCharacterAsync(id);
                    await ShowCharacterSelectAsync();
                };
                DockPanel.SetDock(cancel, Dock.Right);
                row.Children.Add(cancel);

                row.Children.Add(new Button
                {
                    Content = $"{name}   Lv{level}   — deleting in {FormatRemaining(when)}",
                    Height = 40, FontSize = 13, IsEnabled = false,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(10, 0, 0, 0)
                });
            }
            else
            {
                var del = new Button
                {
                    Content = "Delete", Width = 96, Height = 40, FontSize = 12,
                    Background = new SolidColorBrush(Color.FromRgb(120, 60, 60)), Foreground = Brushes.White
                };
                del.Click += async (_, _) => await ConfirmDeleteAsync(id, name, level);
                DockPanel.SetDock(del, Dock.Right);
                row.Children.Add(del);

                var play = new Button
                {
                    Content = $"{name}   Lv{level}  {c.Race} {cls}",
                    Height = 40, FontSize = 13,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(10, 0, 0, 0)
                };
                play.Click += async (_, _) => await EnterWorldAsync(id);
                row.Children.Add(play);
            }

            CharacterSlots.Children.Add(row);
        }
    }

    /// <summary>Confirm + request a character deletion, surfacing the level-based delay.</summary>
    private async Task ConfirmDeleteAsync(int id, string name, int level)
    {
        var delay = GameConstants.CharacterDeleteDelay(level);
        string detail = delay <= TimeSpan.Zero
            ? "It will be deleted immediately."
            : $"As a level {level} character, deletion takes {FormatDelay(delay)} to complete. "
              + "You can cancel any time before then.";
        var res = MessageBox.Show(
            $"Delete \"{name}\"?\n\n{detail}", "Confirm Delete",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (res != MessageBoxResult.Yes)
            return;

        var err = await _net.DeleteCharacterAsync(id);
        if (err is not null)
            MessageBox.Show(err, "Error");
        await ShowCharacterSelectAsync();
    }

    private static string FormatDelay(TimeSpan t) =>
        t.TotalDays >= 1 ? $"{t.TotalDays:0} day(s)" : $"{t.TotalHours:0} hour(s)";

    private static string FormatRemaining(DateTime utcWhen)
    {
        var r = utcWhen - DateTime.UtcNow;
        if (r <= TimeSpan.Zero) return "moments";
        if (r.TotalDays >= 1) return $"{(int)r.TotalDays}d {r.Hours}h";
        if (r.TotalHours >= 1) return $"{(int)r.TotalHours}h {r.Minutes}m";
        return $"{(int)r.TotalMinutes}m";
    }

    private async Task EnterWorldAsync(int characterId)
    {
        try
        {
            var result = await _net.EnterWorldAsync(characterId);
            if (!result.Success)
            {
                MessageBox.Show(result.Error ?? "Could not enter world.", "Error");
                return;
            }

            _myId = result.EntityId;
            _camX = result.X;
            _camY = result.Y;
            _inGame = true;
            _serverEpoch = result.ServerEpochUtc == default ? DateTime.UtcNow : result.ServerEpochUtc;
            GameClock.Epoch = _serverEpoch;
            ClockPanel.Visibility = Visibility.Visible;

            EnsureSkillBarSlots();

            CharacterSelectPanel.Visibility = Visibility.Collapsed;
            CreatePanel.Visibility = Visibility.Collapsed;
            AccountPanel.Visibility = Visibility.Collapsed;

            ChatPanel.Visibility = Visibility.Visible;
            ChatToggle.Visibility = Visibility.Visible;
            SkillsButton.Visibility = Visibility.Visible;
            StatsButton.Visibility = Visibility.Visible;
            InventoryButton.Visibility = Visibility.Visible;
            SettingsButton.Visibility = Visibility.Visible;
#if DEBUG
            DebugButton.Visibility = Visibility.Visible;
#endif

            AppendChat(new ChatMessage("SYSTEM",
                "Click ground = move, click target = attack, 1-8 = skills, I = inventory." +
                (_isAdmin ? " Admin: type /help in chat." : ""),
                ChatChannel.System));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Enter world failed: {ex.Message}", "Error");
        }
    }

    private void NewCharacter_Click(object sender, RoutedEventArgs e)
    {
        CharacterSelectPanel.Visibility = Visibility.Collapsed;
        CreatePanel.Visibility = Visibility.Visible;
    }

    private void BackToSelect_Click(object sender, RoutedEventArgs e)
    {
        CreatePanel.Visibility = Visibility.Collapsed;
        _ = ShowCharacterSelectAsync();
    }

    /// <summary>"Create Character" button on the creation tree.</summary>
    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_creationReady)
            return;

        ConnectButton.IsEnabled = false;
        LoginError.Visibility = Visibility.Collapsed;

        try
        {
            string name = NameInput.Text.Trim();
            var error = await _net.CreateCharacterAsync(name, _myRace, _myBaseClass);
            if (error is not null)
            {
                LoginError.Text = error;
                LoginError.Visibility = Visibility.Visible;
                return;
            }

            // Created — go back to selection (the new character is listed).
            CreatePanel.Visibility = Visibility.Collapsed;
            await ShowCharacterSelectAsync();
        }
        catch (Exception ex)
        {
            LoginError.Text = $"Create failed: {ex.Message}";
            LoginError.Visibility = Visibility.Visible;
        }
        finally
        {
            ConnectButton.IsEnabled = true;
        }
    }

    private void ShowAccountPanel()
    {
        AccountPanel.Visibility = Visibility.Visible;
        CharacterSelectPanel.Visibility = Visibility.Collapsed;
        CreatePanel.Visibility = Visibility.Collapsed;
        ChatPanel.Visibility = Visibility.Collapsed;
        SkillsButton.Visibility = Visibility.Collapsed;
        StatsButton.Visibility = Visibility.Collapsed;
        InventoryButton.Visibility = Visibility.Collapsed;
        SettingsButton.Visibility = Visibility.Collapsed;
        DebugButton.Visibility = Visibility.Collapsed;
        SkillBar.Visibility = Visibility.Collapsed;
        PotionBar.Visibility = Visibility.Collapsed;
    }

    private void ShowAccountError(string text)
    {
        AccountError.Text = text;
        AccountError.Visibility = Visibility.Visible;
    }

    /// <summary>Leave the world (saving the character) and return to character
    /// selection, keeping the connection alive so another character can be entered.</summary>
    private async Task ReturnToCharacterSelectAsync()
    {
        _inGame = false;

        try { await _net.LeaveWorldAsync(); }
        catch { /* connection may be down; we still fall back to selection */ }

        // Tear down the in-world view.
        foreach (var visual in _visuals.Values)
            WorldCanvas.Children.Remove(visual.Root);
        _visuals.Clear();
        _myId = Guid.Empty;
        _myDto = null;
        _targetId = null;
        _mySecondClass = 0;
        _myThirdClass = 0;
        _level = 1;
        _classQuestNoticeShown = false;
        _thirdClassNoticeShown = false;

        // Hide every in-game button / panel / overlay.
        ChatPanel.Visibility = Visibility.Collapsed;
        SkillsButton.Visibility = Visibility.Collapsed;
        StatsButton.Visibility = Visibility.Collapsed;
        InventoryButton.Visibility = Visibility.Collapsed;
        SettingsButton.Visibility = Visibility.Collapsed;
        DebugButton.Visibility = Visibility.Collapsed;
        SkillBar.Visibility = Visibility.Collapsed;
        PotionBar.Visibility = Visibility.Collapsed;
        ClockPanel.Visibility = Visibility.Collapsed;
        TargetFrame.Visibility = Visibility.Collapsed;
        CastBar.Visibility = Visibility.Collapsed;
        DeathOverlay.Visibility = Visibility.Collapsed;
        InventoryPanel.Visibility = Visibility.Collapsed;
        StatsPanel.Visibility = Visibility.Collapsed;
        SkillsPanel.Visibility = Visibility.Collapsed;
        DebugPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Collapsed;
        ClassPanel.Visibility = Visibility.Collapsed;
        DialogPanel.Visibility = Visibility.Collapsed;
        ShopPanel.Visibility = Visibility.Collapsed;

        await ShowCharacterSelectAsync();
    }


    // -----------------------------------------------------------------------
    // Skill bar (rebuilt on class change to include the signature skill)
    // -----------------------------------------------------------------------

    private const int SkillBarSlots = 24;   // 2 rows of 12 square slots

    /// <summary>The skill (string id) assigned to each bar slot (null = empty).</summary>
    private readonly string?[] _skillBar = new string?[SkillBarSlots];

    /// <summary>Learned skill ids + current SP (from the server). _learnedLevels holds
    /// the per-skill level (for the learn window's "next level" logic); _learnedSkills
    /// is the id set the skill bar / availability checks use.</summary>
    private readonly HashSet<string> _learnedSkills = new();
    private readonly Dictionary<string, int> _learnedLevels = new();
    private int _skillPoints;
    private MoveState _moveState = MoveState.Running;

    private void EnsureSkillBarSlots()
    {
        // First time: auto-place currently available skills into free slots.
        AutoPlaceNewSkills();
        RenderSkillBar();
    }

    /// <summary>Drop assignments the character can no longer use, then fill any
    /// free slots with newly-available skills (auto-place on acquire).</summary>
    private void AutoPlaceNewSkills()
    {
        // Availability = learned skills.
        var available = _learnedSkills;

        // Remove assignments no longer learned.
        for (int i = 0; i < _skillBar.Length; i++)
            if (_skillBar[i] is string id && !available.Contains(id))
                _skillBar[i] = null;

        // Auto-place any learned skill not already on the bar (passives never go on it).
        var onBar = _skillBar.Where(x => x is not null).Select(x => x!).ToHashSet();
        foreach (var id in available)
        {
            if (onBar.Contains(id)) continue;
            if (SkillCatalog.Get(id) is { Category: SkillCategory.Passive }) continue;
            int free = Array.IndexOf(_skillBar, null);
            if (free < 0) break;
            _skillBar[free] = id;
            onBar.Add(id);
        }
    }

    /// <summary>Assign a skill to the first free slot (from the Skills window).</summary>
    private void AssignSkillToBar(string skillId)
    {
        if (SkillCatalog.Get(skillId) is { Category: SkillCategory.Passive })
            return; // passives are always-on; never on the action bar
        if (_skillBar.Any(x => x == skillId))
            return; // already on the bar
        int free = Array.IndexOf(_skillBar, null);
        if (free < 0)
        {
            AppendChat(new ChatMessage("SYSTEM", "Skill bar is full.", ChatChannel.System));
            return;
        }
        _skillBar[free] = skillId;
        RenderSkillBar();
        if (SkillsPanel.Visibility == Visibility.Visible)
            RefreshSkillsWindow();
    }

    private void RemoveSkillFromBar(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < _skillBar.Length)
        {
            _skillBar[slotIndex] = null;
            RenderSkillBar();
            if (SkillsPanel.Visibility == Visibility.Visible)
                RefreshSkillsWindow();
        }
    }

    /// <summary>The per-class display name for a skill (cleric's wind_walk =
    /// "Holy Speed"); falls back to the canonical name.</summary>
    private string SkillDisplayName(string skillId, string fallback)
    {
        Archetype? arch = _mySecondClass > 0 ? ClassCatalog.Get(_mySecondClass)?.Archetype : null;
        Discipline? disc = _myThirdClass > 0 ? ThirdClassCatalog.Get(_myThirdClass)?.Discipline : null;
        return ClassSkills.DisplayName(skillId, _myRace, _myBaseClass, arch, disc);
    }

    /// <summary>Hotkey label for a 1-based bar slot: 1-9, then 0 for slot 10,
    /// blank for 11/12 (mouse/drag only).</summary>
    private static string HotkeyLabel(int slot1Based) => slot1Based switch
    {
        >= 1 and <= 9 => slot1Based.ToString(),
        10 => "0",
        _ => ""
    };

    /// <summary>Short label for a skill square. Uses the skill's authored Abbrev
    /// when set, else auto-derives from the (per-class) display name: initials of
    /// multi-word names (Magic Bolt → MB), first 3 letters of a single word.</summary>
    private string SkillAbbrev(SkillDef def)
    {
        if (!string.IsNullOrWhiteSpace(def.Abbrev)) return def.Abbrev;
        string name = SkillDisplayName(def.Id, def.Name);
        var words = name.Split(new[] { ' ', '-', '\'' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2)
            return string.Concat(words.Take(3).Select(w => char.ToUpperInvariant(w[0])));
        string w = words.Length == 1 ? words[0] : name;
        return w.Length <= 3 ? w : w.Substring(0, 3);
    }

    private void RenderSkillBar()
    {
        SkillBar.Children.Clear();
        _skillSlots.Clear();

        for (int i = 0; i < _skillBar.Length; i++)
        {
            int slotIndex = i;
            int hotkey = i + 1;

            var button = new Button
            {
                Width = 46, Height = 46, Margin = new Thickness(3),
                Padding = new Thickness(0), AllowDrop = true
            };

            // Each square is a drag SOURCE and a drop TARGET so skills can be
            // rearranged between slots (including onto empty slots).
            button.PreviewMouseLeftButtonDown += (_, e) => _dragStart = e.GetPosition(this);
            button.MouseMove += (s, e) => SkillSlot_MouseMove(slotIndex, s, e);
            button.DragOver += SkillSlot_DragOver;
            button.Drop += (_, e) => SkillSlot_Drop(slotIndex, e);

            var hk = new TextBlock
            {
                Text = HotkeyLabel(hotkey),
                Foreground = Brushes.LightGray, FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(3, 1, 0, 0), IsHitTestVisible = false
            };

            if (_skillBar[i] is string id && SkillCatalog.Get(id) is SkillDef def)
            {
                var abbrev = new TextBlock
                {
                    Text = SkillAbbrev(def),
                    Foreground = Brushes.White, FontSize = 15, FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false
                };
                var cd = new TextBlock
                {
                    Foreground = Brushes.Gold, FontSize = 16, FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = Visibility.Collapsed, IsHitTestVisible = false
                };

                var grid = new Grid();
                grid.Children.Add(abbrev);
                grid.Children.Add(cd);
                grid.Children.Add(hk);
                button.Content = grid;

                var slot = new SkillSlot { Def = def, Button = button, Key = hotkey, CooldownText = cd };
                // Left-click = cast; right-click = remove from bar.
                button.Click += (_, _) => UseSkill(slot);
                button.MouseRightButtonUp += (_, _) => RemoveSkillFromBar(slotIndex);
                // Bar tooltip = name + description only (full timings in the Skills window).
                button.ToolTip = $"{SkillDisplayName(def.Id, def.Name)}\n{def.Description}".TrimEnd();
                _skillSlots.Add(slot);
            }
            else
            {
                var grid = new Grid { Background = Brushes.Transparent }; // keep hit-testable for drop
                grid.Children.Add(hk);
                button.Content = grid;
                button.Opacity = 0.4;
            }

            SkillBar.Children.Add(button);
        }

        SkillBar.Visibility = Visibility.Visible;
    }

    private void ChatToggle_Click(object sender, RoutedEventArgs e)
    {
        ChatPanel.Visibility = ChatPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
    }

    // ---- Skill-bar drag & drop (rearrange slots) --------------------------
    private Point _dragStart;

    private void SkillSlot_MouseMove(int fromIndex, object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (_skillBar[fromIndex] is null) return;
        var pos = e.GetPosition(this);
        if (Math.Abs(pos.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;
        DragDrop.DoDragDrop((DependencyObject)sender, fromIndex, DragDropEffects.Move);
    }

    private void SkillSlot_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(int)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void SkillSlot_Drop(int toIndex, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(int))) return;
        int fromIndex = (int)e.Data.GetData(typeof(int));
        if (fromIndex == toIndex) return;
        // Swap the two slots (move into an empty slot leaves the source empty).
        (_skillBar[toIndex], _skillBar[fromIndex]) = (_skillBar[fromIndex], _skillBar[toIndex]);
        RenderSkillBar();
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

        if (e.Key is Key.C)
        {
            ToggleStats();
            e.Handled = true;
            return;
        }

        if (e.Key is Key.K)
        {
            ToggleSkills();
            e.Handled = true;
            return;
        }

        if (e.Key is Key.J)
        {
            ToggleQuestLog();
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Escape)   // cancel current cast AND clear the current target
        {
            _ = _net.CancelCastAsync();
            if (_targetId is not null)
            {
                _targetId = null;
                UpdateTargetFrame();
            }
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Z)   // sit / stand toggle
        {
            var next = _moveState == MoveState.Sitting ? MoveState.Running : MoveState.Sitting;
            _ = _net.SetMoveStateAsync(next);
            e.Handled = true;
            return;
        }

        if (e.Key is Key.X)   // walk / run toggle
        {
            var next = _moveState == MoveState.Walking ? MoveState.Running : MoveState.Walking;
            _ = _net.SetMoveStateAsync(next);
            e.Handled = true;
            return;
        }

        // Potion hotkeys: Q (first), E (second potion stack).
        if (e.Key is Key.Q or Key.E)
        {
            UsePotionHotkey(e.Key == Key.Q ? 0 : 1);
            e.Handled = true;
            return;
        }

        int hotkey = e.Key switch
        {
            Key.D1 or Key.NumPad1 => 1,
            Key.D2 or Key.NumPad2 => 2,
            Key.D3 or Key.NumPad3 => 3,
            Key.D4 or Key.NumPad4 => 4,
            Key.D5 or Key.NumPad5 => 5,
            Key.D6 or Key.NumPad6 => 6,
            Key.D7 or Key.NumPad7 => 7,
            Key.D8 or Key.NumPad8 => 8,
            Key.D9 or Key.NumPad9 => 9,
            Key.D0 or Key.NumPad0 => 10,
            _ => -1
        };

        if (hotkey > 0)
        {
            var slot = _skillSlots.FirstOrDefault(sl => sl.Key == hotkey);
            if (slot is not null)
            {
                UseSkill(slot);
                e.Handled = true;
            }
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

        // Show the effective cast-speed bonus next to the skill name.
        string castMod = "";
        if (_stats is StatsUpdate st)
        {
            // CastSpeedMult already folds in WIT, gear, masteries and buffs.
            // (Do NOT add CastModifier again here — that double-counts WIT.)
            float faster = (1f - st.CastSpeedMult) * 100f;
            if (Math.Abs(faster) >= 0.5f)
                castMod = faster > 0 ? $"  (-{faster:0}% cast)" : $"  (+{-faster:0}% cast)";
        }
        CastText.Text = cast.SkillName + castMod;
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

            // Snap (don't slide) on a large jump — teleport/respawn covers more
            // ground than a normal tick of movement ever could.
            double jumpDx = dto.X - visual.CurX, jumpDy = dto.Y - visual.CurY;
            if (jumpDx * jumpDx + jumpDy * jumpDy > 600 * 600)
            {
                visual.CurX = dto.X;
                visual.CurY = dto.Y;
            }

            visual.TargetX = dto.X;
            visual.TargetY = dto.Y;
            visual.Latest = dto;
            UpdateVisualState(visual, dto);

            if (dto.Id == _myId)
            {
                _myDto = dto;
                // Keep our level synced from the snapshot so mob con-colors + level-gated
                // UI are right ON ENTER (Progress events only fire on level-up afterward).
                _level = dto.Level;
                // Race/base class can change via a DEBUG character reset — keep ours in sync.
                if (dto.Race != _myRace || dto.BaseClass != _myBaseClass)
                {
                    _myRace = dto.Race;
                    _myBaseClass = dto.BaseClass;
                    if (SkillsPanel.Visibility == Visibility.Visible)
                        RefreshSkillsWindow();
                }
                if (dto.SecondClass != _mySecondClass || dto.ThirdClass != _myThirdClass)
                {
                    _mySecondClass = dto.SecondClass;
                    _myThirdClass = dto.ThirdClass;
                    EnsureSkillBarSlots();
                    // The class just changed: refresh the learn list so newly-available
                    // skills (e.g. lvl-20 masteries) enable immediately, not after +1 level.
                    if (SkillsPanel.Visibility == Visibility.Visible)
                        RefreshSkillsWindow();
                }
                MaybeShowClassChangeNotice(dto.Level, dto.SecondClass);
                MaybeShowThirdClassNotice(dto.Level, dto.SecondClass, dto.ThirdClass);
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

        if (dto.Kind == EntityKind.Npc)
        {
            visual.Label.Text = $"{dto.Name}  [Talk]";
            visual.Label.Foreground = Brushes.Gold;
        }
        else
        {
            string classTag = dto.Kind == EntityKind.Player && dto.SecondClass > 0
                ? $" {ClassCatalog.Get(dto.SecondClass)?.Name}" : "";
            visual.Label.Text = dto.Dead
                ? $"{dto.Name} Lv{dto.Level} (dead)"
                : $"{dto.Name}{classTag} Lv{dto.Level}";
            if (dto.Kind == EntityKind.Mob)
                visual.Label.Foreground = MobNameBrush(dto.Level);
        }
    }

    private EntityVisual CreateVisual(EntityDto dto)
    {
        Color color = dto.Kind switch
        {
            EntityKind.Mob => Colors.IndianRed,
            EntityKind.Npc => Colors.Gold,
            _ => dto.BaseClass == BaseClass.Mage ? Colors.CornflowerBlue : Colors.Orange
        };

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
            Foreground = dto.Kind == EntityKind.Mob ? MobNameBrush(dto.Level)
                : dto.Kind == EntityKind.Npc ? Brushes.Gold : Brushes.White,
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
            CombatOutcome.ManaHeal => ($"+{evt.Damage} MP", Brushes.DeepSkyBlue),
            CombatOutcome.Buff => (evt.Skill ?? "buff", Brushes.LightSkyBlue),
            CombatOutcome.Block => ($"{evt.Damage} (block)", Brushes.LightSteelBlue),
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

    private void OnGold(GoldUpdate update)
    {
        _gold = update.Gold;   // shown in the status line on the next HUD refresh
        if (ShopPanel.Visibility == Visibility.Visible)
            RenderShop();      // keep buy affordability + gold line current
    }

    private void OnProgress(ProgressUpdate progress)
    {
        bool leveled = progress.Level != _level;
        _level = progress.Level;
        _exp = progress.Exp;
        _expToNext = progress.ExpToNext;

        if (leveled)
        {
            // New level may unlock skills (e.g. level-25 flavour skills later).
            AutoPlaceNewSkills();
            RenderSkillBar();
            if (SkillsPanel.Visibility == Visibility.Visible)
                RefreshSkillsWindow();
        }
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

            // Plain NPCs (vendors/gatekeepers) have nothing to inspect — hide the toggle.
            TargetExpandButton.Visibility =
                dto.Kind == EntityKind.Npc ? Visibility.Collapsed : Visibility.Visible;

            // While the inspect panel is open, refresh it (target changed, or ~1s tick).
            if (_targetExpanded && dto.Kind != EntityKind.Npc)
            {
                if (_inspectedTarget != id ||
                    (DateTime.UtcNow - _lastInspectSent).TotalSeconds >= 1.0)
                {
                    _inspectedTarget = id;
                    _lastInspectSent = DateTime.UtcNow;
                    _ = _net.InspectTargetAsync(id);
                }
            }
        }
        else
        {
            TargetFrame.Visibility = Visibility.Collapsed;
            TradeButton.Visibility = Visibility.Collapsed;
            TargetDetailsPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void TargetClear_Click(object sender, RoutedEventArgs e)
    {
        _targetId = null;
        UpdateTargetFrame();
    }

    private void TargetExpand_Click(object sender, RoutedEventArgs e)
    {
        _targetExpanded = !_targetExpanded;
        TargetExpandButton.Content = _targetExpanded ? "▲" : "▼";
        if (_targetExpanded && _targetId is Guid id)
        {
            _inspectedTarget = id;
            _lastInspectSent = DateTime.UtcNow;
            TargetDetailsText.Text = "…";
            TargetPassivesList.ItemsSource = null;
            TargetDetailsPanel.Visibility = Visibility.Visible;
            _ = _net.InspectTargetAsync(id);
        }
        else
        {
            TargetDetailsPanel.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>Server replied to an inspect request — fill the expanded target panel.
    /// Ignored if it's stale (target changed) or the panel was closed.</summary>
    private void OnTargetDetails(TargetDetails d)
    {
        if (!_targetExpanded || _targetId != d.Id)
            return;

        string text =
            $"HP {d.Hp}/{d.MaxHp}   MP {d.Mp}/{d.MaxMp}\n" +
            $"P.Atk {d.PAtk}   M.Atk {d.MAtk}\n" +
            $"P.Def {d.PDef}   M.Def {d.MDef}\n" +
            $"Acc {d.Accuracy}   Eva {d.Evasion}   Crit {d.CritChance * 100:0.#}%";
        // Active effects (incl. DoT stacks) so you can time a burst, e.g. "Bleed (stacks) x5".
        if (d.Effects.Length > 0)
            text += "\nEffects: " + string.Join(", ", d.Effects);
        TargetDetailsText.Text = text;

        var lines = new List<string>(d.Passives);
        if (d.BowResist > 0f) lines.Add($"Bow Resist +{d.BowResist * 100:0}%");
        if (d.CritResist > 0f) lines.Add($"Crit Resist +{d.CritResist * 100:0}%");
        TargetPassivesList.ItemsSource = lines.Count > 0 ? lines : null;
        TargetDetailsPanel.Visibility = Visibility.Visible;
    }

    private static double Dist(double ax, double ay, double bx, double by)
    {
        double dx = ax - bx, dy = ay - by;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>World-space distance from the player to a point (∞ if we don't have
    /// the player's position yet).</summary>
    private double PlayerDistanceTo(double wx, double wy) =>
        _myDto is null ? double.MaxValue : Dist(_myDto.X, _myDto.Y, wx, wy);

    /// <summary>If we're walking to an NPC to talk, open the dialog once in range.
    /// Cleared if the NPC disappears; clicking elsewhere cancels it (see click).</summary>
    private void PollPendingTalk()
    {
        if (_pendingTalkNpcId is not Guid pid)
            return;
        if (!_visuals.TryGetValue(pid, out var v) || v.Latest is not { } npc)
        {
            _pendingTalkNpcId = null;
            return;
        }
        // Trigger a touch inside TalkRange so the server (which re-checks) agrees.
        if (PlayerDistanceTo(npc.X, npc.Y) <= GameConstants.TalkRange * 0.85)
        {
            _pendingTalkNpcId = null;
            _dialogNpcId = pid;
            _ = _net.TalkToNpcAsync(pid);
        }
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

            var cls = _myThirdClass > 0 ? $" {ThirdClassCatalog.Get(_myThirdClass)?.Name}"
                    : _mySecondClass > 0 ? $" {ClassCatalog.Get(_mySecondClass)?.Name}" : "";
            var zone = _myDto is not null && GameConstants.InSafeZone(_myDto.X, _myDto.Y) ? "  [SAFE]" : "";
            StatusText.Text = $"{_myName}{cls}  Lv{_level}  •  {_gold:N0} {GameConstants.CurrencyName}{zone}";
            UpdateVitalBars();
        }

        PollPendingTalk();

        double cw = WorldCanvas.ActualWidth;
        double ch = WorldCanvas.ActualHeight;
        if (cw <= 0 || ch <= 0)
            return;

        foreach (var visual in _visuals.Values)
        {
            Canvas.SetLeft(visual.Root, (visual.CurX - _camX) * Scale + cw / 2 - 55);
            Canvas.SetTop(visual.Root, (visual.CurY - _camY) * Scale + ch / 2 - 18);
        }

        UpdateClock();
        UpdateSafeZoneVisual(cw, ch);
        UpdateWorldDecor(cw, ch);
        UpdateFloatingTexts(now, cw, ch);
        UpdateGridLines(cw, ch);
        UpdateSkillCooldowns(now);
        UpdatePotionBar(now);
        UpdateCastBar(now);
    }

    private void UpdatePotionBar(double now)
    {
        bool onCd = now < _potionCooldownEndsAt;
        double remaining = _potionCooldownEndsAt - now;
        foreach (var slot in _potionSlots)
        {
            slot.Button.IsEnabled = !onCd && _myDto is not { Dead: true };
            slot.Button.Opacity = onCd ? 0.5 : 1.0;
        }
        // Surface the shared cooldown on the cast text area when idle.
        if (onCd && CastBar.Visibility != Visibility.Visible && _potionSlots.Count > 0)
        {
            // lightweight: no separate label; status line shows it.
        }
    }

    private void UpdateSkillCooldowns(double now)
    {
        foreach (var slot in _skillSlots)
        {
            double remaining = slot.ReadyAt - now;
            if (remaining > 0)
            {
                slot.Button.IsEnabled = false;
                slot.Button.Opacity = 0.5;
                if (slot.CooldownText is { } cd)
                {
                    cd.Text = $"{remaining:0}";
                    cd.Visibility = Visibility.Visible;
                }
            }
            else
            {
                slot.Button.IsEnabled = _myDto is not { Dead: true };
                slot.Button.Opacity = 1.0;
                if (slot.CooldownText is { } cd)
                    cd.Visibility = Visibility.Collapsed;
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

    private const double BarWidth = 240;

    private void UpdateVitalBars()
    {
        if (_myDto is null)
            return;

        double hp = _myDto.MaxHp > 0 ? (double)_myDto.Hp / _myDto.MaxHp : 0;
        double mp = _myDto.MaxMp > 0 ? (double)_myDto.Mp / _myDto.MaxMp : 0;
        double xp = _expToNext > 0 ? (double)_exp / _expToNext : 0;

        HpBarFill.Width = BarWidth * Math.Clamp(hp, 0, 1);
        MpBarFill.Width = BarWidth * Math.Clamp(mp, 0, 1);
        ExpBarFill.Width = BarWidth * Math.Clamp(xp, 0, 1);

        HpBarText.Text = $"HP  {_myDto.Hp} / {_myDto.MaxHp}";
        MpBarText.Text = $"MP  {_myDto.Mp} / {_myDto.MaxMp}";
        ExpBarText.Text = $"EXP  {_exp} / {_expToNext}  (Lv {_level})";
    }

    // -----------------------------------------------------------------------
    // World decor
    // -----------------------------------------------------------------------

    private void BuildWorldDecor()
    {
        foreach (var sz in WorldMap.SafeZones)
        {
            var disc = new Ellipse
            {
                Width = sz.Radius * 2 * Scale,
                Height = sz.Radius * 2 * Scale,
                Fill = new SolidColorBrush(Color.FromArgb(55, 70, 200, 90)),
                Stroke = new SolidColorBrush(Color.FromArgb(120, 90, 220, 110)),
                StrokeThickness = 2,
                IsHitTestVisible = false
            };
            var label = new TextBlock
            {
                Text = sz.Name,
                Foreground = new SolidColorBrush(Color.FromArgb(220, 200, 255, 210)),
                FontSize = 13, FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center, IsHitTestVisible = false
            };
            _safeZoneVisuals.Add((disc, label, sz.X, sz.Y, sz.Radius));
            WorldCanvas.Children.Insert(0, disc);
            WorldCanvas.Children.Add(label);
        }

        // Spawn zones: very light, semi-transparent red discs (placeholder until
        // real environment art). Drawn beneath entities.
        foreach (var zone in WorldMap.SpawnZones)
        {
            // Elites/bosses get a distinct colour so they stand out on the map.
            (byte a, byte r, byte g, byte b) fill = zone.Rank switch
            {
                MobRank.Boss => ((byte)55, (byte)220, (byte)60, (byte)200),   // purple
                MobRank.Elite => ((byte)55, (byte)230, (byte)160, (byte)40),  // amber
                _ => ((byte)38, (byte)225, (byte)70, (byte)70)                // red
            };

            var disc = new Ellipse
            {
                Width = zone.Radius * 2 * Scale,
                Height = zone.Radius * 2 * Scale,
                Fill = new SolidColorBrush(Color.FromArgb(fill.a, fill.r, fill.g, fill.b)),
                Stroke = new SolidColorBrush(Color.FromArgb((byte)120, fill.r, fill.g, fill.b)),
                StrokeThickness = 2,
                IsHitTestVisible = false
            };

            // Label: level band + mob types; elites/bosses also show rank,
            // respawn range "[X ±Y]", and any day/night restriction.
            string text = $"Lv {zone.MinLevel}-{zone.MaxLevel}\n{string.Join(", ", zone.MobTypes)}";
            if (zone.Rank != MobRank.Normal)
                text = $"[{zone.Rank}] {zone.MobTypes[0]}\nLv {zone.MinLevel}\nRespawn {zone.RespawnLabel}";
            if (zone.Active != ActiveTime.Always)
                text += $"\n({zone.Active}-only)";

            var label = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.FromArgb(210, 255, 235, 235)),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                IsHitTestVisible = false
            };

            _spawnZoneVisuals.Add((disc, label, zone.X, zone.Y, zone.Radius));
            WorldCanvas.Children.Insert(0, disc);
            WorldCanvas.Children.Add(label);
        }

        // Roads: thick, semi-transparent grey strips where mobs don't spawn.
        foreach (var road in WorldMap.Roads)
        {
            var line = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromArgb(85, 190, 190, 190)),
                StrokeThickness = road.Width * 2 * Scale,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                IsHitTestVisible = false
            };
            _roadVisuals.Add((line, road.Points));
            WorldCanvas.Children.Insert(0, line);
        }

        // World border outline so the edge is visible (not an invisible wall).
        _borderVisual = new System.Windows.Shapes.Rectangle
        {
            Width = (WorldMap.Border.MaxX - WorldMap.Border.MinX) * Scale,
            Height = (WorldMap.Border.MaxY - WorldMap.Border.MinY) * Scale,
            Stroke = new SolidColorBrush(Color.FromArgb(200, 220, 140, 70)),
            StrokeThickness = 5,
            StrokeDashArray = new DoubleCollection { 6, 4 },
            Fill = null,
            IsHitTestVisible = false
        };
        WorldCanvas.Children.Add(_borderVisual);

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
        foreach (var (visual, label, zx, zy, radius) in _safeZoneVisuals)
        {
            double r = radius * Scale;
            double cx = (zx - _camX) * Scale + cw / 2;
            double cy = (zy - _camY) * Scale + ch / 2;
            Canvas.SetLeft(visual, cx - r);
            Canvas.SetTop(visual, cy - r);
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(label, cx - label.DesiredSize.Width / 2);
            Canvas.SetTop(label, cy - label.DesiredSize.Height / 2);
        }
    }

    private void UpdateClock()
    {
        double hour = GameClock.HourOfDay(DateTime.UtcNow);
        var phase = GameClock.PhaseAt(hour);
        ClockText.Text = $"{GameClock.Format(hour)}  {phase}";
        ClockText.Foreground = phase == DayPhase.Day
            ? new SolidColorBrush(Color.FromRgb(232, 226, 200))   // warm day
            : new SolidColorBrush(Color.FromRgb(150, 180, 230));  // cool night
    }

    private void UpdateWorldDecor(double cw, double ch)
    {
        // Spawn-zone discs + centered labels.
        foreach (var (visual, label, zx, zy, radius) in _spawnZoneVisuals)
        {
            double r = radius * Scale;
            double cx = (zx - _camX) * Scale + cw / 2;
            double cy = (zy - _camY) * Scale + ch / 2;
            Canvas.SetLeft(visual, cx - r);
            Canvas.SetTop(visual, cy - r);

            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(label, cx - label.DesiredSize.Width / 2);
            Canvas.SetTop(label, cy - label.DesiredSize.Height / 2);
        }

        // Road polylines (rebuild points in screen space).
        foreach (var (visual, points) in _roadVisuals)
        {
            var pc = new PointCollection(points.Length);
            foreach (var pt in points)
                pc.Add(new Point((pt.X - _camX) * Scale + cw / 2,
                                 (pt.Y - _camY) * Scale + ch / 2));
            visual.Points = pc;
        }

        // Border rectangle.
        if (_borderVisual is not null)
        {
            Canvas.SetLeft(_borderVisual, (WorldMap.Border.MinX - _camX) * Scale + cw / 2);
            Canvas.SetTop(_borderVisual, (WorldMap.Border.MinY - _camY) * Scale + ch / 2);
        }
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
            var latest = _visuals[targetId].Latest;

            // Clicking an NPC: talk if in range, else walk to it and talk on arrival.
            // (NPCs aren't put in the target frame — they have no real HP bar.)
            if (latest is { Kind: EntityKind.Npc } npc)
            {
                if (PlayerDistanceTo(npc.X, npc.Y) <= GameConstants.TalkRange)
                {
                    _pendingTalkNpcId = null;
                    _dialogNpcId = targetId;
                    await _net.TalkToNpcAsync(targetId);
                }
                else
                {
                    _pendingTalkNpcId = targetId;          // arrive, then OnRenderFrame talks
                    await _net.MoveAsync(npc.X, npc.Y);
                }
                return;
            }

            _pendingTalkNpcId = null;
            _targetId = targetId;
            UpdateTargetFrame();

            // Clicking another player just targets; clicking a mob attacks.
            if (latest is { Kind: EntityKind.Mob })
                await _net.AttackAsync(targetId);
            return;
        }

        _pendingTalkNpcId = null;   // clicking the ground cancels a pending talk-walk
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

        // Admin slash-commands (only sent if server granted admin).
        if (_isAdmin && raw.StartsWith('/') &&
            !raw.StartsWith("/w ", StringComparison.OrdinalIgnoreCase))
        {
            var body = raw[1..].Trim();
            int sp = body.IndexOf(' ');
            string acmd = sp < 0 ? body : body[..sp];
            string aarg = sp < 0 ? "" : body[(sp + 1)..].Trim();
            await _net.AdminCommandAsync(acmd, aarg);
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
        public TextBlock? CooldownText { get; init; }
        public double ReadyAt { get; set; }
    }
}
