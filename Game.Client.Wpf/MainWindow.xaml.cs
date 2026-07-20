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

    /// <summary>Live cooldown readouts in the SKILLS WINDOW (skill id → the TextBlock showing its
    /// remaining seconds). Rebuilt with the window; ticked by UpdateSkillCooldowns off _skillReadyAt,
    /// which is keyed by SKILL so it survives a bar re-render or a move.</summary>
    private readonly List<(string Id, TextBlock Text)> _skillWindowCooldowns = new();
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
    /// <summary>Staff role of the CHARACTER currently in the world (roles are per-character, so this is
    /// set at EnterWorld, not at login). Used only to decide which commands are worth sending — the
    /// server authorizes every one of them regardless.</summary>
    private AccountRole _role = AccountRole.Player;
    private bool _isAdmin => _role != AccountRole.Player;
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

    // Party (roster is server-authoritative; refreshed by PartyUpdate).
    private readonly HashSet<Guid> _partyMemberIds = new();
    private bool _partyIsLeader;
    private Guid? _pendingPartyFrom;
    private bool _suppressLootCombo;   // guard: programmatic combo updates must not re-send to server

    private readonly ClientSettings _settings = ClientSettings.Load();

    public MainWindow()
    {
        InitializeComponent();

        // Restore the saved window geometry + popup positions; persist them on close (not on every
        // move — see SavePanelPositions). Edit client-settings.json (next to the exe) to hand-set them.
        Left = _settings.Window.Position.X;
        Top = _settings.Window.Position.Y;
        Width = _settings.Window.Size.X;
        Height = _settings.Window.Size.Y;
        Closing += (_, _) =>
        {
            if (WindowState == System.Windows.WindowState.Normal)
            {
                _settings.Window.Position = new Vec2 { X = Left, Y = Top };
                _settings.Window.Size = new Vec2 { X = Width, Y = Height };
            }
            SavePanelPositions();   // fold every popup's current drag offset into the settings
            _settings.Save();
        };

        WhisperNames.ItemsSource = _whisperNames;
        VersionText.Text = $"v{GameConstants.GameVersion}";
        EnableMovablePanels();   // drag strip + ✕ + click-to-raise on every popup
        InitTargetRangeSlider();   // after _settings is loaded — setting Value fires ValueChanged
        BuildCreationTree();
        _ = ConnectToServerAsync();

        _net.SnapshotReceived += s => Dispatcher.BeginInvoke(() => ApplySnapshot(s));
        _net.SnapshotDeltaReceived += d => Dispatcher.BeginInvoke(() => ApplyDelta(d));
        _net.SetTargetReceived += id => Dispatcher.BeginInvoke(() =>
        {
            _targetId = id;   // server set our target (e.g. Assist took an ally's foe)
            UpdateTargetFrame();
        });
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
        _net.MobCastReceived += c => Dispatcher.BeginInvoke(() => OnMobCast(c));
        _net.ResurrectOfferReceived += o => Dispatcher.BeginInvoke(() => OnResurrectOffer(o));
        _net.ResurrectOfferExpired += () => Dispatcher.BeginInvoke(HideResurrectPrompt);
        _net.PartyInviteReceived += p => Dispatcher.BeginInvoke(() => OnPartyInvite(p));
        _net.PartyReceived += p => Dispatcher.BeginInvoke(() => OnParty(p));
        _net.PartyLootVoteReceived += v => Dispatcher.BeginInvoke(() => OnPartyLootVote(v));
        _net.AutoHuntReceived += s => Dispatcher.BeginInvoke(() => OnAutoHuntStatus(s));
        _net.AutoConfigReceived += c => Dispatcher.BeginInvoke(() => OnAutoConfig(c));
        _net.SkillBarReceived += b => Dispatcher.BeginInvoke(() => OnSkillBar(b));
        _net.SubclassesReceived += s => Dispatcher.BeginInvoke(() => OnSubclasses(s));
        _net.LogoutResultReceived += r => Dispatcher.BeginInvoke(() => OnLogoutResult(r));
        _net.PvpStateReceived += s => Dispatcher.BeginInvoke(() => OnPvpState(s));
        _net.DebugConfigReceived += c => Dispatcher.BeginInvoke(() => OnDebugConfig(c));
        _net.EnchantReceived += en => Dispatcher.BeginInvoke(() => OnEnchant(en));
        _net.RerollReceived += r => Dispatcher.BeginInvoke(() => OnReroll(r));
        _net.AdminStateReceived += s => Dispatcher.BeginInvoke(() => OnAdminState(s));
        _net.AdminBagReceived += b => Dispatcher.BeginInvoke(() => ShowAdminBagWindow(b));
        _net.AdminGivePickerReceived += b => Dispatcher.BeginInvoke(() => ShowAdminGiveWindow(b));
        _net.ForceDisconnected += reason => Dispatcher.BeginInvoke(() =>
        {
            // Leave the world FIRST, then explain (owner). Showing the modal first left the kicked
            // player staring at a dialog on top of a world they'd already been removed from, and the
            // "OK" read as a confirmation they could decline. Now: back to the login page → then why.
            _inGame = false;
            _role = AccountRole.Player;
            UpdateAdminIndicator();
            ShowAccountPanel();
            MessageBox.Show(reason, "Disconnected");
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

            // Staff role belongs to the CHARACTER now, so it arrives with EnterWorld, not with login.
            _role = AccountRole.Player;
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

    // Both formatters carry a SECONDS case: a DEBUG build collapses the delete delay to ~10s, which the
    // old hour/minute floors rendered as "0 hour(s)" and "0m".
    private static string FormatDelay(TimeSpan t) =>
        t.TotalDays >= 1 ? $"{t.TotalDays:0} day(s)"
        : t.TotalHours >= 1 ? $"{t.TotalHours:0} hour(s)"
        : t.TotalMinutes >= 1 ? $"{t.TotalMinutes:0} minute(s)"
        : $"{t.TotalSeconds:0} second(s)";

    private static string FormatRemaining(DateTime utcWhen)
    {
        var r = utcWhen - DateTime.UtcNow;
        if (r <= TimeSpan.Zero) return "moments";
        if (r.TotalDays >= 1) return $"{(int)r.TotalDays}d {r.Hours}h";
        if (r.TotalHours >= 1) return $"{(int)r.TotalHours}h {r.Minutes}m";
        if (r.TotalMinutes >= 1) return $"{(int)r.TotalMinutes}m";
        return $"{(int)r.TotalSeconds}s";
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
            _role = result.Role;   // per-CHARACTER staff role
            UpdateAdminIndicator();
            _serverEpoch = result.ServerEpochUtc == default ? DateTime.UtcNow : result.ServerEpochUtc;
            GameClock.Epoch = _serverEpoch;
            ClockPanel.Visibility = Visibility.Visible;

            // The bar is saved per character — forget the last one's layout/cooldowns. The real
            // restore happens on the first snapshot, which is where we learn our name.
            Array.Clear(_skillBar);
            _skillReadyAt.Clear();
            _skillBarLoaded = false;
            _myName = "";

            EnsureSkillBarSlots();

            CharacterSelectPanel.Visibility = Visibility.Collapsed;
            CreatePanel.Visibility = Visibility.Collapsed;
            AccountPanel.Visibility = Visibility.Collapsed;

            MenuBackdrop.Visibility = Visibility.Collapsed;   // reveal the world again
            HudPanel.Visibility = Visibility.Visible;
            ChatPanel.Visibility = Visibility.Visible;
            ChatToggle.Visibility = Visibility.Visible;
            SkillsButton.Visibility = Visibility.Visible;
            StatsButton.Visibility = Visibility.Visible;
            InventoryButton.Visibility = Visibility.Visible;
            AutoHuntButton.Visibility = Visibility.Visible;
            PvpButton.Visibility = Visibility.Visible;
            CounterButton.Visibility = Visibility.Visible;
            SettingsButton.Visibility = Visibility.Visible;
#if DEBUG
            DebugButton.Visibility = Visibility.Visible;
#endif

            AppendChat(new ChatMessage("SYSTEM",
                "Click ground = move, click target = attack, 1-8 = skills, I = inventory." +
                (_isAdmin ? $" {_role}: type /help in chat." : ""),
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

    /// <summary>Log out of the ACCOUNT from character select, back to the login screen. Char select
    /// previously had no way out at all — you had to close the window.</summary>
    private void CharSelectLogout_Click(object sender, RoutedEventArgs e)
    {
        _myName = "";
        CharacterSlots.Children.Clear();
        LoginError.Visibility = Visibility.Collapsed;
        ShowAccountPanel();
    }

    private void ShowAccountPanel()
    {
        MenuBackdrop.Visibility = Visibility.Visible;   // never show the live world behind a menu
        HudPanel.Visibility = Visibility.Collapsed;
        AutoHuntButton.Visibility = Visibility.Collapsed;
        PvpButton.Visibility = Visibility.Collapsed;
        CounterButton.Visibility = Visibility.Collapsed;
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

        // Hide every in-game button / panel / overlay, and put an opaque backdrop over the world —
        // the char-select screen used to sit on top of the LIVE world with the HUD still showing.
        MenuBackdrop.Visibility = Visibility.Visible;
        HudPanel.Visibility = Visibility.Collapsed;      // HP / MP / EXP bars + buff rows
        AutoHuntButton.Visibility = Visibility.Collapsed;
        PvpButton.Visibility = Visibility.Collapsed;
        CounterButton.Visibility = Visibility.Collapsed;
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
        // The res OFFER must die with the session, not just get covered up. It lives INSIDE the death
        // overlay, so collapsing the overlay hides it on screen while leaving its own Visibility=Visible:
        // an unanswered offer therefore came back on the next login, riding the overlay that reappears
        // because you log in dead — but Accept did nothing, since a resurrection offer is runtime-only
        // state on the entity and the entity was rebuilt from the DB with no offer on it. The rescuer and
        // their cast are long gone by then anyway, and a res is meant to stand you up where you FELL, so
        // the right answer is to forget the offer and leave you the Respawn button (owner's option A).
        HideResurrectPrompt();
        InventoryPanel.Visibility = Visibility.Collapsed;
        StatsPanel.Visibility = Visibility.Collapsed;
        SkillsPanel.Visibility = Visibility.Collapsed;
        DebugPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Collapsed;
        ClassPanel.Visibility = Visibility.Collapsed;
        DialogPanel.Visibility = Visibility.Collapsed;
        ShopPanel.Visibility = Visibility.Collapsed;
        BuyQtyPanel.Visibility = Visibility.Collapsed;
        PartyPanel.Visibility = Visibility.Collapsed;
        PartyInvitePrompt.Visibility = Visibility.Collapsed;
        PartyMembers.Children.Clear();
        _partyMemberIds.Clear();
        _partyIsLeader = false;
        _pendingPartyFrom = null;

        await ShowCharacterSelectAsync();
    }


    // -----------------------------------------------------------------------
    // Skill bar (rebuilt on class change to include the signature skill)
    // -----------------------------------------------------------------------

    // Shared with the server, which now owns the bar and does the auto-placement (SyncSkillBar).
    // Two independent "24"s would silently disagree the day one of them changed.
    private const int SkillBarSlots = GameConstants.SkillBarSlots;   // 2 rows of 12 square slots

    /// <summary>The skill (string id) assigned to each bar slot (null = empty).</summary>
    private readonly string?[] _skillBar = new string?[SkillBarSlots];

    /// <summary>False until the SERVER has sent this character's saved bar. Nothing may auto-place
    /// into the bar, or save it, before then — otherwise a Learned push that arrives first would
    /// re-fill an empty bar from scratch and persist that over the player's real layout.</summary>
    private bool _skillBarLoaded;

    /// <summary>Cooldowns survive a re-render. RenderSkillBar() rebuilds every SkillSlot object,
    /// so ReadyAt (which lives on the slot) used to be lost — levelling up, changing class or
    /// simply dragging a skill silently cleared every cooldown on the bar.</summary>
    private readonly Dictionary<string, double> _skillReadyAt = new();

    /// <summary>Learned skill ids + current SP (from the server). _learnedLevels holds
    /// the per-skill level (for the learn window's "next level" logic); _learnedSkills
    /// is the id set the skill bar / availability checks use.</summary>
    private readonly HashSet<string> _learnedSkills = new();
    private readonly Dictionary<string, int> _learnedLevels = new();
    private int _skillPoints;
    private MoveState _moveState = MoveState.Running;

    private void EnsureSkillBarSlots() => RenderSkillBar();

    /// <summary>The SERVER sent this class's bar. This is the ONLY thing that populates the bar.
    ///
    /// The client no longer auto-places newly-learned skills, and no longer writes the bar back on its
    /// own. The SERVER owns it (GameLoopService.SyncSkillBar) and pushes it alongside the skills. That
    /// is deliberate: while auto-placement lived here, ANY server push of Learned that arrived while the
    /// client still held a different bar — a fresh login, a subclass swap — made the client re-park
    /// skills against the WRONG bar and SAVE the result, destroying the real layout on the server while
    /// the client went on to receive the correct bar and look perfectly fine. It bit twice before it was
    /// understood. The client now only writes the bar when the PLAYER edits it.</summary>
    private void OnSkillBar(SkillBarDto dto)
    {
        Array.Clear(_skillBar);
        for (int i = 0; i < _skillBar.Length && i < dto.Slots.Length; i++)
            _skillBar[i] = string.IsNullOrEmpty(dto.Slots[i]) ? null : dto.Slots[i];

        _skillBarLoaded = true;
        RenderSkillBar();
    }

    /// <summary>Persist a bar the PLAYER just edited (drag, assign, remove). Nothing else may call this
    /// — see OnSkillBar for what happens when the client writes a bar it didn't author.</summary>
    private void SaveSkillBar()
    {
        if (!_inGame || !_skillBarLoaded) return;   // never save a bar we haven't loaded yet
        _ = _net.SetSkillBarAsync(_skillBar.Select(x => x ?? "").ToArray());
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
        SaveSkillBar();
        RenderSkillBar();
        if (SkillsPanel.Visibility == Visibility.Visible)
            RefreshSkillsWindow();
    }

    private void RemoveSkillFromBar(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < _skillBar.Length)
        {
            _skillBar[slotIndex] = null;
            SaveSkillBar();
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

    /// <summary>The per-class EMOJI/glyph for a skill, or "" if none. Per-class override
    /// (ClassSkills.Icon) wins over the skill's own SkillDef.Icon — a cleric's Holy Speed can differ
    /// from a mage's Wind Walk even though they share the id.</summary>
    private string SkillIcon(SkillDef def)
    {
        Archetype? arch = _mySecondClass > 0 ? ClassCatalog.Get(_mySecondClass)?.Archetype : null;
        Discipline? disc = _myThirdClass > 0 ? ThirdClassCatalog.Get(_myThirdClass)?.Discipline : null;
        string? classIcon = ClassSkills.Icon(def.Id, _myRace, _myBaseClass, arch, disc);
        return !string.IsNullOrWhiteSpace(classIcon) ? classIcon!
             : !string.IsNullOrWhiteSpace(def.Icon) ? def.Icon
             : SkillIcons.For(def.Id);
    }

    /// <summary>Short LETTERS label for a skill square (the fallback when it has no icon). The skill's
    /// authored Abbrev wins; otherwise it comes from <see cref="Abbreviations"/>, which resolves the
    /// whole catalog at once so no two skills or consumables can share a label. Deriving it here, one
    /// skill at a time, is what gave three different heal-over-time skills the same "HOT" square.</summary>
    private string SkillAbbrev(SkillDef def)
    {
        if (!string.IsNullOrWhiteSpace(def.Abbrev)) return def.Abbrev;
        return Abbreviations.For(SkillDisplayName(def.Id, def.Name));
    }

    /// <summary>The face of a skill square: the emoji icon if one is set (bigger, black on the light
    /// slot), else the letters. Returns (text, isIcon) so the caller can size them differently.</summary>
    private (string Text, bool IsIcon) SkillFace(SkillDef def)
    {
        string icon = SkillIcon(def);
        return icon.Length > 0 ? (icon, true) : (SkillAbbrev(def), false);
    }

    private bool _skillBarDragWired;
    private TranslateTransform? _skillBarMove;
    private const int MaxBarRows = 5;

    private void RenderSkillBar()
    {
        // Wire the PANEL once (RenderSkillBar runs on every stats push — subscribing per-render would
        // stack handlers). The panel catches the 3px gaps between slots, so a drag that crosses one
        // still gets its move events instead of stalling.
        if (!_skillBarDragWired)
        {
            _skillBarDragWired = true;
            SkillBar.PreviewMouseMove += (_, e) => SkillSlot_MouseMove(e);
            SkillBar.PreviewMouseLeftButtonUp += (_, _) => _dragFromIndex = -1;
        }

        // Whole-stack MOVE: a RenderTransform on the bar container, restored once from settings (like the
        // popups). The owner moves the WHOLE bar, not individual rows.
        if (_skillBarMove is null)
        {
            _skillBarMove = new TranslateTransform();
            SkillBar.RenderTransform = _skillBarMove;
            if (_settings.Panels.TryGetValue("SkillBar", out var saved))
            {
                _skillBarMove.X = saved.X;
                _skillBarMove.Y = saved.Y;
            }
        }

        // How many rows to show: the player's saved choice, else auto-fit to the highest occupied slot.
        int rows = _settings.SkillBarRows > 0
            ? Math.Clamp(_settings.SkillBarRows, 1, MaxBarRows)
            : AutoFitBarRows();

        SkillBar.Children.Clear();
        _skillSlots.Clear();

        // Control strip (+/- expander + drag handle) sits ABOVE the rows.
        SkillBar.Children.Add(BuildSkillBarControlStrip(rows));

        // Rows are drawn TOP-DOWN so row 0 (which carries hotkeys 1-9/0) ends up at the BOTTOM and each
        // new row the player expands opens ABOVE the previous one.
        for (int r = rows - 1; r >= 0; r--)
        {
            var rowPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            for (int c = 0; c < GameConstants.SkillBarColumns; c++)
                rowPanel.Children.Add(BuildSkillSlot(r * GameConstants.SkillBarColumns + c));
            SkillBar.Children.Add(rowPanel);
        }

        SkillBar.Visibility = Visibility.Visible;
    }

    /// <summary>Fewest rows that still show every assigned slot (min 1). Used until the player picks a
    /// row count with the +/- expander.</summary>
    private int AutoFitBarRows()
    {
        int last = -1;
        for (int i = 0; i < _skillBar.Length; i++)
            if (_skillBar[i] is not null) last = i;
        int rows = last < 0 ? 1 : (last / GameConstants.SkillBarColumns) + 1;
        return Math.Clamp(rows, 1, MaxBarRows);
    }

    /// <summary>The +/- row expander and the drag strip that moves the whole bar. `+` opens one more row
    /// up to 5; at 5 it becomes `−` and collapses back to a single row (owner's rule).</summary>
    private FrameworkElement BuildSkillBarControlStrip(int rows)
    {
        var strip = new Grid { Height = 16, Margin = new Thickness(3, 0, 3, 2) };

        var handle = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)),
            CornerRadius = new CornerRadius(3),
            Cursor = System.Windows.Input.Cursors.SizeAll,
            Child = new TextBlock
            {
                Text = "⠿ drag bar",
                Foreground = new SolidColorBrush(Color.FromArgb(0xB0, 0xFF, 0xFF, 0xFF)),
                FontSize = 9, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false
            }
        };
        WireSkillBarDrag(handle);
        strip.Children.Add(handle);

        var expander = new Button
        {
            Content = rows >= MaxBarRows ? "−" : "+",
            Width = 22, Height = 14, FontSize = 10, Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Left,
            ToolTip = rows >= MaxBarRows ? "Collapse to one row" : "Show another row"
        };
        expander.Click += (_, _) =>
        {
            _settings.SkillBarRows = rows >= MaxBarRows ? 1 : rows + 1;
            _settings.Save();
            RenderSkillBar();
        };
        strip.Children.Add(expander);
        return strip;
    }

    /// <summary>Drag the WHOLE bar by its handle (a Grid takes no capture of its own, unlike ButtonBase —
    /// see the slot-drag saga). Offset persisted to settings on release, like the popups.</summary>
    private void WireSkillBarDrag(UIElement handle)
    {
        Point origin = default;
        bool dragging = false;
        handle.MouseLeftButtonDown += (_, e) =>
        {
            origin = e.GetPosition(this); dragging = true; handle.CaptureMouse(); e.Handled = true;
        };
        handle.MouseMove += (_, e) =>
        {
            if (!dragging || _skillBarMove is null) return;
            var now = e.GetPosition(this);
            _skillBarMove.X += now.X - origin.X;
            _skillBarMove.Y += now.Y - origin.Y;
            origin = now;
        };
        handle.MouseLeftButtonUp += (_, _) =>
        {
            if (!dragging) return;
            dragging = false; handle.ReleaseMouseCapture();
            if (_skillBarMove is not null)
            {
                _settings.Panels["SkillBar"] = new Vec2 { X = _skillBarMove.X, Y = _skillBarMove.Y };
                _settings.Save();
            }
        };
    }

    /// <summary>One bar slot: a skill, an inventory ITEM ("item:&lt;defId&gt;"), or empty. A Border, NOT
    /// a Button — WPF's ButtonBase captures the mouse on press, which broke slot drag three times (see
    /// the drag saga below). Left-click uses it, right-click removes it, and it is a drag source + drop
    /// target so slots rearrange.</summary>
    private Border BuildSkillSlot(int slotIndex)
    {
        int hotkey = slotIndex + 1;
        var button = new Border
        {
            Width = 46, Height = 46, Margin = new Thickness(3),
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x5A, 0x6A, 0x80)),
            Background = new SolidColorBrush(Color.FromRgb(0xC8, 0xCF, 0xD6)),
            AllowDrop = true,
        };
        button.PreviewMouseLeftButtonDown += (_, e) => SkillSlot_MouseDown(slotIndex, e);
        button.PreviewMouseMove += (_, e) => SkillSlot_MouseMove(e);
        button.DragOver += SkillSlot_DragOver;
        button.Drop += (_, e) => SkillSlot_Drop(slotIndex, e);

        var hk = new TextBlock
        {
            Text = HotkeyLabel(hotkey),
            Foreground = Brushes.DimGray, FontSize = 9,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(3, 1, 0, 0), IsHitTestVisible = false
        };

        string? entry = _skillBar[slotIndex];
        if (ActionCatalog.FromToken(entry) is ActionDef action)
            BuildActionSlotFace(button, hk, slotIndex, action);
        else if (GameConstants.IsItemSlot(entry))
            BuildItemSlotFace(button, hk, slotIndex, GameConstants.ItemSlotDefId(entry!));
        else if (entry is string id && SkillCatalog.Get(id) is SkillDef def)
        {
            var (faceText, isIcon) = SkillFace(def);
            var abbrev = new TextBlock
            {
                Text = faceText,
                Foreground = Brushes.Black, FontSize = isIcon ? 22 : 15,
                FontWeight = isIcon ? FontWeights.Normal : FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false
            };
            var cd = new TextBlock
            {
                Foreground = Brushes.DarkGoldenrod, FontSize = 16, FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed, IsHitTestVisible = false
            };
            var grid = new Grid();
            grid.Children.Add(abbrev);
            grid.Children.Add(cd);
            grid.Children.Add(hk);
            button.Child = grid;

            var slot = new SkillSlot { Def = def, Button = button, Key = hotkey, CooldownText = cd };
            if (_skillReadyAt.TryGetValue(def.Id, out double readyAt)) slot.ReadyAt = readyAt;

            // Cast on mouse-UP, and ONLY if no drag happened — otherwise finishing a drag would also cast
            // the skill you just moved. The "did a drag happen" test MUST be its own flag (_dragStarted),
            // NOT `_dragFromIndex < 0`: the panel clears _dragFromIndex from a TUNNELING handler that runs
            // before this bubbling one, so reading it here always saw -1 and clicks never cast.
            button.MouseLeftButtonUp += (_, _) =>
            {
                if (_dragStarted) return;
                UseSkill(slot);
            };
            button.MouseRightButtonUp += (_, _) => RemoveSkillFromBar(slotIndex);
            button.ToolTip = $"{SkillDisplayName(def.Id, def.Name)}\n{def.Description}".TrimEnd();
            _skillSlots.Add(slot);
        }
        else
        {
            var grid = new Grid { Background = Brushes.Transparent };   // hit-testable for drop
            grid.Children.Add(hk);
            button.Child = grid;
            button.Opacity = 0.4;
        }
        return button;
    }

    /// <summary>Face for an ITEM bar slot: the item's initials + a live count, greyed out when you have
    /// none (like a skill on cooldown). Left-click USES the item — no opening the inventory to find it.</summary>
    /// <summary>A built-in ACTION slot. No cooldown and no count — an action is always ready — so the
    /// face is just its icon, tinted to read as "not a skill" at a glance.</summary>
    private void BuildActionSlotFace(Border button, TextBlock hk, int slotIndex, ActionDef action)
    {
        button.Background = new SolidColorBrush(Color.FromRgb(0xB8, 0xC8, 0xD8));

        var face = new TextBlock
        {
            Text = action.Icon,
            Foreground = Brushes.Black, FontSize = 22,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
        };
        var grid = new Grid();
        grid.Children.Add(face);
        grid.Children.Add(hk);
        button.Child = grid;
        button.ToolTip = $"{action.Name}\n{action.Description}";

        button.MouseLeftButtonUp += (_, _) =>
        {
            if (_dragStarted) return;
            RunAction(action);
        };
        button.MouseRightButtonUp += (_, _) => RemoveSkillFromBar(slotIndex);
    }

    private void BuildItemSlotFace(Border button, TextBlock hk, int slotIndex, string defId)
    {
        var def = ItemCatalog.Get(defId);
        int count = _inventory.Where(i => i.DefId == defId).Sum(i => i.Quantity);

        var face = new TextBlock
        {
            Text = ItemBarAbbrev(def),
            Foreground = Brushes.Black, FontSize = 13, FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false
        };
        var countText = new TextBlock
        {
            Text = count >= 100 ? "99+" : count.ToString(),
            Foreground = count > 0 ? Brushes.DarkGreen : Brushes.DarkRed,
            FontSize = 10, FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 3, 1), IsHitTestVisible = false
        };
        var grid = new Grid();
        grid.Children.Add(face);
        grid.Children.Add(countText);
        grid.Children.Add(hk);
        button.Child = grid;
        button.Opacity = count > 0 ? 1.0 : 0.4;   // greyed when you have none, like a cooldown
        button.ToolTip = (def?.Name ?? defId) + (count > 0 ? $"\nx{count}" : "\n(none left)");

        button.MouseLeftButtonUp += (_, _) =>
        {
            if (_dragStarted) return;
            var stack = _inventory.FirstOrDefault(i => i.DefId == defId && i.Quantity > 0);
            if (stack is null)
            {
                AppendChat(new ChatMessage("SYSTEM", $"No {def?.Name ?? defId} left.", ChatChannel.System));
                return;
            }
            // Same path the BAG uses — a res scroll needs its target, which this slot used to drop.
            if (def is not null) UseConsumable(stack.InstanceId, def);
            else _ = _net.UsePotionAsync(stack.InstanceId);
        };
        button.MouseRightButtonUp += (_, _) => RemoveSkillFromBar(slotIndex);
    }

    /// <summary>Label for an item bar slot (items have no emoji table yet). Shares the catalog-wide
    /// resolver with skills, so a potion can't collide with a skill square either — the two scrolls both
    /// showed the same letters before this.</summary>
    private static string ItemBarAbbrev(ItemDef? def) =>
        def is null ? "?" : Abbreviations.For(def.Name);

    /// <summary>Put an inventory item on the bar's first free slot (from the inventory's "To Bar"
    /// button). Stored as an "item:&lt;defId&gt;" token; the SERVER keeps it (SyncSkillBar skips item
    /// slots) and the client uses/greys it by live count.</summary>
    private void AssignItemToBar(string defId) =>
        AssignTokenToBar(GameConstants.ItemSlotToken(defId));

    /// <summary>Put any bar TOKEN — an item ("item:…") or a built-in action ("action:…") — in the first
    /// free slot. This is a PLAYER edit, so saving the bar here is correct; the server never authors
    /// one (see the skill-bar note in CLAUDE.md).</summary>
    private void AssignTokenToBar(string token)
    {
        if (_skillBar.Any(x => x == token)) return;   // already on the bar
        int free = Array.IndexOf(_skillBar, null);
        if (free < 0)
        {
            AppendChat(new ChatMessage("SYSTEM", "Skill bar is full.", ChatChannel.System));
            return;
        }
        _skillBar[free] = token;
        SaveSkillBar();
        RenderSkillBar();
        if (SkillsPanel.Visibility == Visibility.Visible)
            RefreshSkillsWindow();   // flip the row's button to "On Bar"
    }

    private void ChatToggle_Click(object sender, RoutedEventArgs e)
    {
        ChatPanel.Visibility = ChatPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
        if (ChatPanel.Visibility != Visibility.Visible) BlurChatInput();
    }

    /// <summary>Reveal the chat panel if hidden and put the caret in its input box (Enter).</summary>
    private void FocusChatInput()
    {
        ChatPanel.Visibility = Visibility.Visible;
        ChatInput.Focus();
        Keyboard.Focus(ChatInput);
        ChatInput.CaretIndex = ChatInput.Text.Length;
    }

    /// <summary>Give keyboard focus back to the game. WPF has no "unfocus", so focus is moved onto the
    /// window itself — that is what makes the hotkeys live again, since OnPreviewKeyDown ignores every
    /// key while a TextBox holds focus.</summary>
    private void BlurChatInput()
    {
        if (!ChatInput.IsKeyboardFocusWithin) return;
        Keyboard.ClearFocus();
        Focus();
    }

    // ---- Skill-bar drag & drop (rearrange slots) --------------------------
    //
    // WHY THIS LOOKS PARANOID (it is fixing a real, reproduced bug):
    //
    // A WPF Button CAPTURES the mouse on press. Two things follow, and together they were the
    // whole drag-and-drop bug:
    //
    //   1. Calling DragDrop.DoDragDrop from a control that currently holds capture is unreliable —
    //      the drag often just doesn't start. That was the "it's very hard to even begin a drag".
    //   2. When that capture IS lost, MouseMove stops being routed to the button you pressed and
    //      starts going to whatever button is now UNDER THE CURSOR. That button's handler then fires
    //      with ITS OWN slot index — so the drag picks up the skill you dragged ONTO, not the one you
    //      grabbed. That was "it moves a different skill", and why the next attempt grabbed yet
    //      another one.
    //
    // So we must NOT trust the slot index of whichever button happens to raise MouseMove. The origin
    // is recorded once, at mouse-DOWN (_dragFromIndex), and every later step reads that. We also drop
    // capture before starting the drag so it begins on the first move. Carrying the skill id in the
    // payload (the previous attempt at a fix) could never work: the WRONG id was being picked up in
    // the first place. We still carry it, because it also guards the other hazard — a re-render
    // mid-drag (level-up / class change / server stats push all call RenderSkillBar) invalidating the
    // index before the drop lands.
    private Point _dragStart;
    private int _dragFromIndex = -1;

    /// <summary>Did the CURRENT press turn into a real drag? Armed (false) at mouse-down, set true only
    /// once the drag threshold is crossed. Separate from <see cref="_dragFromIndex"/> because that gets
    /// cleared by a tunneling handler before the slot's click handler runs — see the cast wiring in
    /// RenderSkillBar.</summary>
    private bool _dragStarted;

    private const string SkillDragFormat = "L2Clone.SkillBarSlot";
    private sealed record SkillDrag(int FromIndex, string SkillId);

    /// <summary>Mouse-down on a slot: remember WHERE and WHICH SLOT the gesture started on. This is
    /// the only place the drag origin is ever established.</summary>
    private void SkillSlot_MouseDown(int slotIndex, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _dragFromIndex = slotIndex;
        _dragStarted = false;   // a fresh gesture: a click until it crosses the drag threshold
    }

    /// <summary>Deliberately ignores the slot index of the button that raised this event — see the
    /// note above. The origin is <see cref="_dragFromIndex"/>, captured at mouse-down.</summary>
    private void SkillSlot_MouseMove(MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) { _dragFromIndex = -1; return; }
        if (_dragFromIndex < 0 || _dragFromIndex >= _skillBar.Length) return;
        if (_skillBar[_dragFromIndex] is not string skillId) return;

        var pos = e.GetPosition(this);
        if (Math.Abs(pos.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        int fromIndex = _dragFromIndex;
        _dragFromIndex = -1;              // one drag per press; don't re-enter while DoDragDrop pumps
        _dragStarted = true;              // suppress the cast that would otherwise fire on the drop's mouse-up

        // The pressed Button still holds capture, and DoDragDrop is unreliable from a captured
        // element. Hand it back before starting the drag.
        if (Mouse.Captured is not null) Mouse.Capture(null);

        var data = new DataObject(SkillDragFormat, new SkillDrag(fromIndex, skillId));
        DragDrop.DoDragDrop(SkillBar, data, DragDropEffects.Move);
    }

    private void SkillSlot_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(SkillDragFormat) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void SkillSlot_Drop(int toIndex, DragEventArgs e)
    {
        if (e.Data.GetData(SkillDragFormat) is not SkillDrag drag) return;
        e.Handled = true;

        // Trust the skill ID over the index. If the bar changed under us, find where the dragged
        // skill actually sits now; if it's gone entirely, drop the drag rather than move a
        // bystander.
        int fromIndex = drag.FromIndex;
        if (fromIndex < 0 || fromIndex >= _skillBar.Length || _skillBar[fromIndex] != drag.SkillId)
            fromIndex = Array.IndexOf(_skillBar, drag.SkillId);
        if (fromIndex < 0 || fromIndex == toIndex) return;

        // Swap (moving into an empty slot leaves the source empty).
        (_skillBar[toIndex], _skillBar[fromIndex]) = (_skillBar[fromIndex], _skillBar[toIndex]);
        SaveSkillBar();
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
        _skillReadyAt[slot.Def.Id] = slot.ReadyAt;   // keyed by SKILL, so it survives a re-render/move
        LogSkillUse(SkillDisplayName(slot.Def.Id, slot.Def.Name));
        await _net.UseSkillAsync(slot.Def.Id, _targetId);
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_inGame)
            return;

        // Never steal keys from a text field. This only excused ChatInput, so typing into ANY other
        // box (auto-hunt HP%/MP%, farm range, skill reuse, debug tuning) fired the game hotkeys
        // instead and swallowed the keystroke — "5" cast skill 5, "i" opened the inventory, and the
        // digit never arrived. Hence "I can't write in the auto-potion boxes, it uses skills".
        if (Keyboard.FocusedElement is TextBox or PasswordBox)
            return;

        // ENTER jumps to the chat box (revealing it first if it's hidden) — the MMO reflex. Focus is
        // released again by clicking anywhere in the world; see OnWorldMouseDown.
        if (e.Key is Key.Enter)
        {
            FocusChatInput();
            e.Handled = true;
            return;
        }

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
            EscapeCancel();
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

        // The skill NAME only. The cast bar used to append the effective cast-speed offset
        // ("Heal  (-71% cast)"), which is noise while you're watching a bar fill — and misleading on a
        // FIXED-cast skill, where the offset does not apply at all. The real, current cast time lives in
        // the skill details; the Cast Speed stat lives in the stats window (owner, 2026-07-17).
        CastText.Text = cast.SkillName;
        CastFill.Width = 0;
        CastBar.Visibility = Visibility.Visible;
    }

    /// <summary>A mob/boss started (or cleared) a visible cast. Anchors a cast bar under the
    /// caster's nameplate; the render loop fills it and hides it when the cast completes.</summary>
    private void OnMobCast(MobCastInfo cast)
    {
        if (!_visuals.TryGetValue(cast.CasterId, out var visual))
            return;

        if (cast.Seconds <= 0)
        {
            visual.CastDuration = 0;
            visual.CastBar.Visibility = Visibility.Collapsed;
            return;
        }

        visual.CastStart = _clock.Elapsed.TotalSeconds;
        visual.CastDuration = cast.Seconds;
        visual.CastText.Text = cast.SkillName;
        visual.CastFill.Width = 0;
        visual.CastBar.Visibility = Visibility.Visible;
    }

    // -----------------------------------------------------------------------
    // Snapshots
    // -----------------------------------------------------------------------

    /// <summary>Legacy FULL-snapshot path (WorldSnapshot): absence means "removed". The live server now
    /// sends <see cref="ApplyDelta"/> instead; this is kept for compatibility.</summary>
    private void ApplySnapshot(WorldSnapshot snapshot)
    {
        var seen = new HashSet<Guid>();
        foreach (var dto in snapshot.Entities)
        {
            seen.Add(dto.Id);
            ApplyEntityDto(dto);
        }
        foreach (var id in _visuals.Keys.Where(id => !seen.Contains(id)).ToList())
            DespawnVisual(id);
        UpdateTargetFrame();
    }

    /// <summary>The live DELTA path. Spawns = full DTOs (new in view / static change); Updates = lean
    /// dynamic-only changes merged onto the cached DTO; Despawns = left view. An entity in none of the
    /// three is UNCHANGED and kept as-is — that's the whole saving.</summary>
    private void ApplyDelta(SnapshotDelta delta)
    {
        foreach (var dto in delta.Spawns)
            ApplyEntityDto(dto);

        foreach (var u in delta.Updates)
        {
            // Merge the lean dynamic fields onto the last full DTO we hold, then run the SAME per-entity
            // logic — so nothing about how an entity is applied differs between spawn and update.
            if (_visuals.TryGetValue(u.Id, out var v) && v.Latest is { } prev)
                ApplyEntityDto(prev with
                {
                    X = u.X, Y = u.Y, Speed = u.Speed,
                    Hp = u.Hp, Mp = u.Mp, Dead = u.Dead,
                    Disconnected = u.Disconnected, Flag = u.Flag
                });
            // An update for an entity we never spawned (missed spawn) is ignored; its next static change
            // or re-entry will spawn it fresh.
        }

        foreach (var id in delta.Despawns)
            DespawnVisual(id);

        UpdateTargetFrame();
    }

    /// <summary>Create or update one entity's visual from a full DTO. Shared by spawn (delta), the merged
    /// update path, and the legacy full snapshot — so the behaviour is identical across all three.</summary>
    private void ApplyEntityDto(EntityDto dto)
    {
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
            // Our own name was never actually assigned (it stayed ""), so the status line
            // showed no name and the whisper self-check never matched. The first snapshot is
            // where we learn it — and the skill bar is saved PER CHARACTER, so it can only be
            // restored once we know who we are.
            if (_myName != dto.Name)
            {
                _myName = dto.Name;
                EnsureSkillBarSlots();
            }
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
            // Alive again (revived, respawned, or freshly logged in): make sure no stale offer is
            // parked inside the overlay waiting to reappear on the next death.
            if (!dto.Dead) HideResurrectPrompt();
        }
    }

    private void DespawnVisual(Guid id)
    {
        if (_visuals.TryGetValue(id, out var v))
            WorldCanvas.Children.Remove(v.Root);
        _visuals.Remove(id);
        if (_targetId == id)
            _targetId = null;
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

    /// <summary>The " Lv12" suffix for a nameplate / target frame — empty for every player but YOU
    /// (owner, 2026-07-20: your level is yours and the party's, not an enemy's). Mobs always show it.
    /// The server backs this up by sending Level=0 for other players, so a modified client gains
    /// nothing; this method only decides what is DRAWN.</summary>
    private string LevelTag(EntityDto dto) =>
        dto.Kind == EntityKind.Mob || dto.Id == _myId ? $" Lv{dto.Level}" : "";

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
        else if (dto.Kind == EntityKind.Player && dto.Disconnected)
        {
            // Link-dead: a clear title above the head so nearby players know it's a network drop
            // (not an active player). Offline-FARMING players are NOT flagged (look normal).
            visual.Label.Text = $"{dto.Name}{LevelTag(dto)}  ⚠ Disconnected";
            visual.Label.Foreground = Brushes.OrangeRed;
        }
        else
        {
            string classTag = dto.Kind == EntityKind.Player && dto.SecondClass > 0
                ? $" {ClassCatalog.Get(dto.SecondClass)?.Name}" : "";
            // "*" = this mob attacks on sight. A passive mob is safe to walk past; an aggressive one is
            // not, and you could only find out the hard way (owner).
            string aggro = dto.Aggressive ? " *" : "";   // space before it (owner) — "Wolf *", not "Wolf*"
            visual.Label.Text = dto.Dead
                ? $"{dto.Name}{aggro}{LevelTag(dto)} (dead)"
                : $"{dto.Name}{aggro}{classTag}{LevelTag(dto)}";
            // Player name colour follows the PvP flag: red = PK, purple = flagged, white = innocent.
            visual.Label.Foreground = dto.Kind == EntityKind.Mob ? MobNameBrush(dto.Level)
                : dto.Flag switch
                {
                    PvpFlag.Pk      => Brushes.Red,
                    PvpFlag.Flagged => new SolidColorBrush(Color.FromRgb(0xC8, 0x6C, 0xE8)),
                    _               => Brushes.White
                };
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

        // Mob/boss cast bar — hidden until a MobCast event arrives for this entity.
        var castFill = new Rectangle
        {
            Fill = new SolidColorBrush(Color.FromRgb(0xDD, 0x88, 0x44)),
            HorizontalAlignment = HorizontalAlignment.Left, Width = 0,
            RadiusX = 2, RadiusY = 2
        };
        var castText = new TextBlock
        {
            Foreground = Brushes.White, FontSize = 9,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var castBar = new Border
        {
            Width = 90, Height = 11, Margin = new Thickness(0, 2, 0, 0),
            Background = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)),
            CornerRadius = new CornerRadius(2),
            HorizontalAlignment = HorizontalAlignment.Center,
            Visibility = Visibility.Collapsed,
            Child = new Grid { Children = { castFill, castText } }
        };

        var stack = new StackPanel { Width = 110 };
        stack.Children.Add(label);
        stack.Children.Add(dot);
        stack.Children.Add(hpBar);
        stack.Children.Add(castBar);

        return new EntityVisual
        {
            Root = stack, Dot = dot, Label = label, HpFill = hpFill,
            CastBar = castBar, CastFill = castFill, CastText = castText
        };
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
                // A dead PLAYER stays targeted so you can resurrect the ally you were just healing —
                // dropping the target the instant they fell meant re-selecting a corpse you could no
                // longer click. The frame renders the dead now (see UpdateTargetFrame). A dead MOB is
                // still dropped: its corpse is about to despawn and there's nothing left to do with it.
                bool deadPlayer = _visuals.TryGetValue(evt.TargetId, out var v) &&
                                  v.Latest is { Kind: EntityKind.Player };
                if (!deadPlayer) _targetId = null;
                UpdateTargetFrame();
            }
            return;
        }

        if (anchor is null)
            return;

        LogCombatToSystem(evt);

        bool onMe = evt.TargetId == _myId;

        // NO "!" suffix on crits. "8000!" reads as 80000 — the owner genuinely misread an 8k crit as
        // 80k. A crit is signalled by SIZE and COLOUR instead, never by a character glued to a number.
        (string text, Brush brush) = evt.Outcome switch
        {
            CombatOutcome.Miss     => ("miss", Brushes.Gray),
            CombatOutcome.Fail     => ("resist", Brushes.MediumPurple),
            CombatOutcome.Crit     => (evt.Damage.ToString(),
                                       onMe ? Brushes.DarkRed : Brushes.Lime),
            CombatOutcome.Heal     => ($"+{evt.Damage}", Brushes.Orange),
            CombatOutcome.ManaHeal => ($"+{evt.Damage} MP", Brushes.DeepSkyBlue),
            CombatOutcome.Buff     => (evt.Skill ?? "buff", Brushes.LightSkyBlue),
            CombatOutcome.Block    => ($"{evt.Damage} (block)", Brushes.LightSteelBlue),
            _                      => (evt.Damage.ToString(),
                                       onMe ? Brushes.OrangeRed : Brushes.White)
        };

        var tb = new TextBlock
        {
            Text = text, Foreground = brush,
            FontSize = evt.Outcome == CombatOutcome.Crit ? 19 : 14,
            FontWeight = FontWeights.Bold
        };
        WorldCanvas.Children.Add(tb);

        // FAN THEM OUT. Several numbers can land on one entity in the same instant (a hit, its crit,
        // the lifesteal it healed you for) and they used to draw exactly on top of each other, so you
        // could not tell damage from vamp from crit. Stack each new number against the ones already
        // floating on that same entity.
        int stacked = _floatingTexts.Count(f => f.AnchorId == evt.TargetId);
        _floatingTexts.Add(new FloatingText
        {
            Visual = tb, WorldX = anchor.CurX, WorldY = anchor.CurY,
            AnchorId = evt.TargetId,
            OffsetX = (stacked % 3 - 1) * 26,     // -26 / 0 / +26, cycling
            OffsetY = -14 * (stacked % 4),        // and step upward
            Born = _clock.Elapsed.TotalSeconds
        });
    }

    // ---- Combat log (System tab) -------------------------------------------------------------
    //
    // A readable transcript of YOUR fight only — every event in the world would drown it. The
    // floating numbers over the world tell you WHEN something happened; this tells you WHAT, and it
    // survives long enough to read. Colour carries the meaning, so the eye can scan it:
    //
    //   you  -> enemy   green   (lime on a crit)
    //   enemy -> you    red     (dark red on a crit)
    //   enemy avoided   purple  (evaded / blocked / resisted a spell or debuff)
    //   you avoided     light blue
    //   healing         orange (HP) / blue (MP)
    private static readonly Brush LogOut     = Brushes.LimeGreen;
    private static readonly Brush LogOutCrit = Brushes.Lime;
    private static readonly Brush LogIn      = Brushes.IndianRed;
    private static readonly Brush LogInCrit  = Brushes.Firebrick;
    private static readonly Brush LogAvoided = Brushes.MediumPurple;
    private static readonly Brush LogIAvoid  = Brushes.LightSkyBlue;
    private static readonly Brush LogHeal    = Brushes.Orange;
    private static readonly Brush LogMana    = Brushes.DeepSkyBlue;

    /// <summary>Log the start of a cast. Called when YOU begin casting — there is deliberately no
    /// "finished"/"cancelled" line, per the owner: the result speaks for itself.</summary>
    private void LogSkillUse(string skillName) =>
        AddChatLine(SystemList, SystemScroll, $"Use skill {skillName}", Brushes.Gainsboro);

    private void LogCombatToSystem(CombatEvent evt)
    {
        bool mine = evt.AttackerId == _myId;
        bool onMe = evt.TargetId == _myId;
        if (!mine && !onMe) return;             // someone else's fight — not our business

        string skill = string.IsNullOrEmpty(evt.Skill) ? "" : $" [{evt.Skill}]";
        string me = "You";

        (string text, Brush brush) = evt.Outcome switch
        {
            // Healing / mana. A heal on yourself reads "self", on someone else their name.
            CombatOutcome.Heal when mine =>
                ($"{me} healed {(onMe ? "self" : evt.TargetName)} for {evt.Damage}{skill}", LogHeal),
            CombatOutcome.ManaHeal when mine =>
                ($"{me} restored {(onMe ? "self" : evt.TargetName)} {evt.Damage} MP{skill}", LogMana),
            CombatOutcome.Heal     => ($"{evt.AttackerName} healed you for {evt.Damage}{skill}", LogHeal),
            CombatOutcome.ManaHeal => ($"{evt.AttackerName} restored you {evt.Damage} MP{skill}", LogMana),
            CombatOutcome.Buff     => ($"{(mine ? me : evt.AttackerName)} → {(onMe ? "you" : evt.TargetName)}: {evt.Skill}",
                                       mine ? LogOut : LogAvoided),

            // Avoided. Purple when the ENEMY avoids you; light blue when YOU avoid them.
            CombatOutcome.Miss  => mine
                ? ($"{evt.TargetName} evaded{skill}", LogAvoided)
                : ($"{me} evaded {evt.AttackerName}{skill}", LogIAvoid),
            CombatOutcome.Fail  => mine
                ? ($"{evt.TargetName} resisted{skill}", LogAvoided)
                : ($"{me} resisted {evt.AttackerName}{skill}", LogIAvoid),
            CombatOutcome.Block => mine
                ? ($"{evt.TargetName} blocked — {evt.Damage}{skill}", LogAvoided)
                : ($"{me} blocked {evt.AttackerName} — {evt.Damage}{skill}", LogIAvoid),

            // Damage.
            CombatOutcome.Crit => mine
                ? ($"{me} → {evt.TargetName}: {evt.Damage} (critical){skill}", LogOutCrit)
                : ($"{evt.AttackerName} → you: {evt.Damage} (critical){skill}", LogInCrit),
            _ => mine
                ? ($"{me} → {evt.TargetName}: {evt.Damage}{skill}", LogOut)
                : ($"{evt.AttackerName} → you: {evt.Damage}{skill}", LogIn),
        };

        AddChatLine(SystemList, SystemScroll, text, brush);
    }

    private void OnGold(GoldUpdate update)
    {
        _gold = update.Gold;   // shown in the status line on the next HUD refresh
        RefreshInventoryGold();
        if (ShopPanel.Visibility == Visibility.Visible)
            RenderShop();      // keep buy affordability + gold line current
        if (BuyQtyPanel.Visibility == Visibility.Visible)
            RefreshBuyQty();   // the prompt stays open across buys, so its amounts must re-check gold
    }

    /// <summary>Show gold in the inventory, COLOUR-TIERED by amount (owner): white &lt;1kk (1M),
    /// yellow &lt;100kk (100M), green &lt;1kkk (1B), purple ≥1kkk.</summary>
    private void RefreshInventoryGold()
    {
        InventoryGoldText.Text = $"{_gold:N0} {GameConstants.CurrencyName}";
        InventoryGoldText.Foreground =
            _gold >= 1_000_000_000L ? new SolidColorBrush(Color.FromRgb(0xC0, 0x6B, 0xE6))   // purple ≥1kkk
          : _gold >= 100_000_000L   ? new SolidColorBrush(Color.FromRgb(0x5C, 0xD6, 0x5C))   // green  <1kkk
          : _gold >= 1_000_000L     ? new SolidColorBrush(Color.FromRgb(0xE6, 0xCC, 0x44))   // yellow <100kk
          :                           Brushes.White;                                          // white  <1kk
    }

    private void OnProgress(ProgressUpdate progress)
    {
        bool leveled = progress.Level != _level;
        _level = progress.Level;
        _exp = progress.Exp;
        _expToNext = progress.ExpToNext;

        if (leveled)
        {
            // A level-up can unlock skills — but the SERVER parks them and pushes the new bar with its
            // Learned message (see GameLoopService.SendLearned). Nothing to place here; just repaint.
            RenderSkillBar();
            if (SkillsPanel.Visibility == Visibility.Visible)
                RefreshSkillsWindow();
        }
    }

    private void UpdateTargetFrame()
    {
        // A DEAD target still shows its frame (owner, 2026-07-17). It used to require `Dead: false`, so
        // selecting a fallen ally left you with a ghost target — the res landed on someone you couldn't
        // see or confirm. You must be able to read who you're about to resurrect.
        if (_targetId is Guid id &&
            _visuals.TryGetValue(id, out var visual) &&
            visual.Latest is { } dto)
        {
            TargetFrame.Visibility = Visibility.Visible;
            string classTag = dto.SecondClass > 0
                ? $" {ClassCatalog.Get(dto.SecondClass)?.Name}" : "";
            string deadTag = dto.Dead ? "  [DEAD]" : "";
            string aggroTag = dto.Aggressive ? " *" : "";   // attacks on sight (space before it, owner)
            TargetNameText.Text = $"{dto.Name}{aggroTag}{classTag}{LevelTag(dto)}  {dto.Hp}/{dto.MaxHp}{deadTag}";
            double ratio = dto.MaxHp > 0 ? Math.Clamp((double)dto.Hp / dto.MaxHp, 0, 1) : 0;
            TargetHpFill.Width = 218 * ratio;

            // Actions (behind the [...] toggle). Each applies only to another LIVING player.
            bool otherLivingPlayer = dto.Kind == EntityKind.Player && !dto.Dead && id != _myId;
            bool canTrade = otherLivingPlayer && !_tradeActive &&
                _myDto is not null &&
                Dist(dto.X, dto.Y, _myDto.X, _myDto.Y) <= GameConstants.TradeRange;
            bool canInvite = otherLivingPlayer &&
                !_partyMemberIds.Contains(id) &&
                (_partyMemberIds.Count == 0 || _partyIsLeader);

            TradeButton.Visibility = canTrade ? Visibility.Visible : Visibility.Collapsed;
            PartyInviteButton.Visibility = canInvite ? Visibility.Visible : Visibility.Collapsed;
            FollowButton.Visibility = otherLivingPlayer ? Visibility.Visible : Visibility.Collapsed;
            AssistButton.Visibility = otherLivingPlayer ? Visibility.Visible : Visibility.Collapsed;

            // The [...] button (and its panel) only appear when there's at least one action to offer.
            bool anyAction = otherLivingPlayer;   // follow/assist always apply to a living player
            TargetActionsButton.Visibility = anyAction ? Visibility.Visible : Visibility.Collapsed;
            TargetActionsPanel.Visibility = anyAction && _targetActionsOpen
                ? Visibility.Visible : Visibility.Collapsed;

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
            PartyInviteButton.Visibility = Visibility.Collapsed;
            TargetDetailsPanel.Visibility = Visibility.Collapsed;
            TargetMobDetailsButton.Visibility = Visibility.Collapsed;
            TargetActionsButton.Visibility = Visibility.Collapsed;
            TargetActionsPanel.Visibility = Visibility.Collapsed;
            // No target, no card — otherwise the popup lingers describing a mob you've stopped fighting.
            MobInfoPanel.Visibility = Visibility.Collapsed;
            _mobDetailsExpanded = false;
        }
    }

    private bool _targetActionsOpen;

    private void TargetActions_Click(object sender, RoutedEventArgs e)
    {
        _targetActionsOpen = !_targetActionsOpen;
        UpdateTargetFrame();
    }

    private async void FollowButton_Click(object sender, RoutedEventArgs e)
    {
        if (_targetId is Guid id) await _net.FollowAsync(id);
    }

    private async void AssistButton_Click(object sender, RoutedEventArgs e)
    {
        if (_targetId is Guid id) await _net.AssistAsync(id);
    }

    /// <summary>What ESC does: cancel the cast in progress AND drop the target. The target frame's ✕ is
    /// wired to this too — closing that window IS an Escape (owner, 2026-07-17), which is why the frame
    /// is draggable rather than something you close to get it out of the way. Previously the ✕ only
    /// cleared the target and left a cast running, despite its "Clear target (Esc)" tooltip.</summary>
    private void EscapeCancel()
    {
        _ = _net.CancelCastAsync();
        if (_targetId is not null)
        {
            _targetId = null;
            UpdateTargetFrame();
        }
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

        // A PLAYER's expand shows IDENTITY only — you don't get to read a rival's full stat sheet (P.Atk,
        // defences, crit) off a click (owner, 2026-07-17). A MOB shows the full combat card: it's a
        // target you're fighting and you want its defences to plan a burst.
        //
        // LEVEL is deliberately absent (owner, 2026-07-20): it is intelligence a player does not want to
        // hand an enemy, and unlike a name it isn't already on screen. Class stays — it's visible from
        // the gear and the spells they cast anyway. Title and clan name/rank join this line once clans
        // exist; the level does not come back.
        if (!d.IsMob)
        {
            string classTag = TargetClassLabel();
            TargetDetailsText.Text = classTag.Length > 0 ? $"{d.Name}\n{classTag}" : d.Name;
            TargetPassivesList.ItemsSource = null;
            TargetMobDetailsButton.Visibility = Visibility.Collapsed;
            TargetDetailsPanel.Visibility = Visibility.Visible;
            return;
        }

        // MOB: the base card is COMPACT — just the four combat stats + a [Details] button (owner). The
        // full stat line, effects, passives and the DROP list live behind that button. A NEW mob starts
        // compact and drops the cached drop list; the 1s auto-refresh of the same mob keeps whatever
        // level you'd toggled to.
        if (_lastMobDetails?.Id != d.Id) { _mobDetailsExpanded = false; _cachedDrops = null; }
        // Drops arrive only from a WithDrops request (the Details click); cache them so the drop-less
        // 1s refreshes don't blank the list.
        if (d.Drops is { Length: > 0 }) _cachedDrops = d.Drops;
        _lastMobDetails = d;
        RenderMobDetails();
        TargetDetailsPanel.Visibility = Visibility.Visible;
    }

    private TargetDetails? _lastMobDetails;
    private bool _mobDetailsExpanded;
    private string[]? _cachedDrops;   // drops for the current target, fetched once on the Details click

    private void TargetMobDetails_Click(object sender, RoutedEventArgs e)
    {
        _mobDetailsExpanded = !_mobDetailsExpanded;
        if (!_mobDetailsExpanded)
        {
            MobInfoPanel.Visibility = Visibility.Collapsed;
            return;
        }
        _mobInfoShowingDrops = false;   // Details is the default tab (owner)
        RenderMobInfoPopup();
    }

    /// <summary>Draw the mob inspect panel from the last details, at the current detail level: compact =
    /// P.Def/M.Def · P.Atk/M.Atk only; expanded = full stats + effects + passives + the DROP list.</summary>
    private void RenderMobDetails()
    {
        if (_lastMobDetails is not { } d) return;

        TargetMobDetailsButton.Visibility = Visibility.Visible;
        TargetMobDetailsButton.Content = "Details ▸";

        // The target frame itself keeps ONLY these two rows (owner) — everything else moved into the
        // MobInfo popup. Attack above defence, in the SAME order the popup uses: the two views used to
        // disagree, so the rows appeared to swap places every time you expanded or collapsed the card.
        TargetDetailsText.Text =
            $"P.Atk {d.PAtk}   M.Atk {d.MAtk}\n" +
            $"P.Def {d.PDef}   M.Def {d.MDef}";
        TargetPassivesList.ItemsSource = null;

        if (_mobDetailsExpanded) RenderMobInfoPopup();
    }

    /// <summary>Which tab the mob-info popup is showing. Details is the default (owner) — the drop list
    /// is what you check once, the stats are what you check mid-fight.</summary>
    private bool _mobInfoShowingDrops;

    private void MobInfoDetailsTab_Click(object sender, RoutedEventArgs e)
    {
        _mobInfoShowingDrops = false;
        RenderMobInfoPopup();
    }

    private void MobInfoDropTab_Click(object sender, RoutedEventArgs e)
    {
        _mobInfoShowingDrops = true;
        // Drops are static, so they're fetched once, lazily — the 1s stat refresh never carries them.
        if (_cachedDrops is null && _targetId is Guid id)
            _ = _net.InspectTargetAsync(id, withDrops: true);
        RenderMobInfoPopup();
    }

    /// <summary>Draw the movable mob card: a Details tab (full stats, effects, passives) and a Drop tab.
    /// Modelled on the player stats window — more stats can simply be appended to the Details tab.</summary>
    private void RenderMobInfoPopup()
    {
        if (_lastMobDetails is not { } d)
        {
            MobInfoPanel.Visibility = Visibility.Collapsed;
            return;
        }

        MobInfoTitle.Text = $"{d.Name}   Lv {d.Level}";
        HighlightMobInfoTab();
        MobInfoBody.Children.Clear();

        if (_mobInfoShowingDrops)
        {
            if (_cachedDrops is { Length: > 0 })
                foreach (var drop in _cachedDrops) MobInfoBody.Children.Add(MobInfoLine(drop));
            else
                MobInfoBody.Children.Add(MobInfoLine("(no drops known)", dim: true));
        }
        else
        {
            MobInfoBody.Children.Add(MobInfoLine($"HP {d.Hp}/{d.MaxHp}    MP {d.Mp}/{d.MaxMp}"));
            MobInfoBody.Children.Add(MobInfoLine($"P.Atk {d.PAtk}    M.Atk {d.MAtk}"));
            MobInfoBody.Children.Add(MobInfoLine($"P.Def {d.PDef}    M.Def {d.MDef}"));
            MobInfoBody.Children.Add(MobInfoLine(
                $"Acc {d.Accuracy}    Eva {d.Evasion}    Crit {d.CritChance * 100:0.#}%"));
            if (d.BowResist > 0f) MobInfoBody.Children.Add(MobInfoLine($"Bow Resist +{d.BowResist * 100:0}%"));
            if (d.CritResist > 0f) MobInfoBody.Children.Add(MobInfoLine($"Crit Resist +{d.CritResist * 100:0}%"));
            if (d.Effects.Length > 0)
            {
                MobInfoBody.Children.Add(MobInfoLine("— Effects —", dim: true));
                foreach (var effect in d.Effects) MobInfoBody.Children.Add(MobInfoLine(effect));
            }
            if (d.Passives.Length > 0)
            {
                MobInfoBody.Children.Add(MobInfoLine("— Passives —", dim: true));
                foreach (var passive in d.Passives) MobInfoBody.Children.Add(MobInfoLine(passive));
            }
        }

        Panel.SetZIndex(MobInfoPanel, ++_panelZ);
        MobInfoPanel.Visibility = Visibility.Visible;
    }

    private void HighlightMobInfoTab()
    {
        MobInfoDetailsTab.FontWeight = _mobInfoShowingDrops ? FontWeights.Normal : FontWeights.Bold;
        MobInfoDropTab.FontWeight = _mobInfoShowingDrops ? FontWeights.Bold : FontWeights.Normal;
    }

    private static TextBlock MobInfoLine(string text, bool dim = false) => new()
    {
        Text = text,
        Foreground = new SolidColorBrush(dim
            ? Color.FromRgb(0x8F, 0x9A, 0xA4)
            : Color.FromRgb(0xD8, 0xE0, 0xE6)),
        FontSize = 12,
        FontFamily = new FontFamily("Consolas"),
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 1, 0, 1),
    };

    /// <summary>The 2nd/3rd-class label for the current target (from its snapshot), or "" if none.</summary>
    private string TargetClassLabel()
    {
        if (_targetId is Guid id && _visuals.TryGetValue(id, out var v) && v.Latest is { } dto
            && dto.SecondClass > 0)
            return ClassCatalog.Get(dto.SecondClass)?.Name ?? "";
        return "";
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
            var karma = _myKarma > 0 ? $"  •  KARMA {_myKarma:N0}" : "";
            // Gold is shown (colour-tiered) in the Inventory now, not here on the vitals line.
            StatusText.Text = $"{_myName}{cls}  Lv{_level}{zone}{karma}";
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
            UpdateMobCastBar(visual, now);
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

    /// <summary>Paint the cooldown countdown on the bar, and mirror it into the Skills window.
    ///
    /// A cooling-down slot is NEVER disabled. It used to set Button.IsEnabled = false, and a disabled
    /// WPF button receives no mouse input at all — so while a skill was on cooldown you could not drag
    /// it, nor right-click it off the bar. Cooldown is a CAST restriction, not a "you may not rearrange
    /// your UI" restriction; UseSkill already refuses to fire an unready skill, so the button only
    /// needs to LOOK unavailable (dimmed + a countdown), not be inert.</summary>
    private void UpdateSkillCooldowns(double now)
    {
        foreach (var slot in _skillSlots)
        {
            double remaining = slot.ReadyAt - now;
            bool cooling = remaining > 0;

            slot.Button.Opacity = cooling ? 0.5 : 1.0;   // dim only — still draggable / removable
            if (slot.CooldownText is { } cd)
            {
                cd.Text = cooling ? $"{remaining:0}" : "";
                cd.Visibility = cooling ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // Mirror the same countdown into the Skills window, so you don't have to read it off the bar.
        foreach (var (id, text) in _skillWindowCooldowns)
        {
            double remaining = _skillReadyAt.TryGetValue(id, out double readyAt) ? readyAt - now : 0;
            bool cooling = remaining > 0;
            text.Text = cooling ? $"{remaining:0}s" : "";
            text.Visibility = cooling ? Visibility.Visible : Visibility.Collapsed;
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

    private static void UpdateMobCastBar(EntityVisual visual, double now)
    {
        if (visual.CastDuration <= 0 || visual.CastBar.Visibility != Visibility.Visible)
            return;

        double progress = (now - visual.CastStart) / visual.CastDuration;
        if (progress >= 1)
        {
            visual.CastBar.Visibility = Visibility.Collapsed;
            visual.CastDuration = 0;
            return;
        }
        visual.CastFill.Width = 86 * Math.Clamp(progress, 0, 1);
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
            Canvas.SetLeft(ft.Visual, (ft.WorldX - _camX) * Scale + cw / 2 - 8 + ft.OffsetX);
            Canvas.SetTop(ft.Visual, (ft.WorldY - _camY) * Scale + ch / 2 - 34 - age * 38 + ft.OffsetY);
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
        // Clicking the world takes focus BACK off the chat box (owner) — otherwise Enter drops you into
        // chat and every hotkey stays dead until you notice why. Done before the dead/not-in-game guard
        // so it works even while you're lying on the floor.
        BlurChatInput();

        if (!_inGame || _myDto is { Dead: true })
            return;

        Point click = e.GetPosition(WorldCanvas);
        double cw = WorldCanvas.ActualWidth;
        double ch = WorldCanvas.ActualHeight;

        // SHIFT = select, never act (owner, 2026-07-17). It is the only way to pick a DEAD player — a
        // corpse you mean to resurrect — and on the living it targets WITHOUT attacking. A corpse is
        // deliberately unreachable by a plain click: it lies among the mobs that killed it, and a stray
        // click there must never stop being an attack.
        bool selectOnly = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

        Guid? hit = null;
        double best = ClickRadiusPx * ClickRadiusPx;

        foreach (var (id, visual) in _visuals)
        {
            if (id == _myId || visual.Latest is null)
                continue;
            if (visual.Latest is { Dead: true } && !selectOnly)
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

            // Shift-click selects and stops there: no attack, no walk, no NPC dialog.
            if (selectOnly)
            {
                if (latest is { Kind: EntityKind.Npc })
                    return;                            // NPCs have no target frame — nothing to select
                _pendingTalkNpcId = null;
                _targetId = targetId;
                UpdateTargetFrame();
                return;
            }

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

            // Clicking a mob attacks. Clicking another player attacks when PvP is on OR the target is
            // already flagged/red (justice / self-defense) — otherwise it just targets (trade/party).
            // Skills also fire on the current target (server enforces the same rules).
            //
            // ALL classes click-to-attack, mages included (owner): a mage out of MP needs to melee a
            // mob to finish it. The old "mage sprints into melee and won't stop" annoyance is handled
            // the RIGHT way now — casting a skill CANCELS the walk-to-target (see the cast path), so a
            // mage who clicks to melee and then casts stops walking, instead of the click being denied.
            if (latest is { Kind: EntityKind.Mob } ||
                (latest is { Kind: EntityKind.Player } && latest.Id != _myId &&
                 (_pvpEnabled || latest.Flag != PvpFlag.Innocent)))
                await _net.AttackAsync(targetId);
            return;
        }

        _pendingTalkNpcId = null;   // clicking the ground cancels a pending talk-walk
        double worldX = _camX + (click.X - cw / 2) / Scale;
        double worldY = _camY + (click.Y - ch / 2) / Scale;
        await _net.MoveAsync((float)worldX, (float)worldY);
    }

    private async void RespawnButton_Click(object sender, RoutedEventArgs e)
    {
        // Respawning in town abandons any pending offer (the server drops it too) — clear it here so the
        // block can't linger over the world for a second after you've already stood up in town.
        HideResurrectPrompt();
        await _net.RespawnAsync();
    }

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

        // Friend slash-commands (ANY player): /fadd <name>, /frem <name>, /flist.
        if (raw.StartsWith("/fadd ", StringComparison.OrdinalIgnoreCase))
        {
            await _net.FriendCommandAsync("add", raw[6..].Trim());
            return;
        }
        if (raw.StartsWith("/frem ", StringComparison.OrdinalIgnoreCase))
        {
            await _net.FriendCommandAsync("remove", raw[6..].Trim());
            return;
        }
        if (raw.Equals("/flist", StringComparison.OrdinalIgnoreCase))
        {
            await _net.FriendCommandAsync("list", "");
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
        public required Border CastBar { get; init; }
        public required Rectangle CastFill { get; init; }
        public required TextBlock CastText { get; init; }
        public double CastStart { get; set; }
        public double CastDuration { get; set; }
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
        /// <summary>The entity this number belongs to — used to fan out several numbers landing on
        /// the same target in the same instant, which otherwise draw on top of each other.</summary>
        public Guid AnchorId { get; init; }
        public double OffsetX { get; init; }
        public double OffsetY { get; init; }
        public double Born { get; init; }
    }

    private class SkillSlot
    {
        public required SkillDef Def { get; init; }
        /// <summary>The slot face. A Border, NOT a Button — ButtonBase's mouse capture is what broke
        /// skill-bar drag & drop (see RenderSkillBar).</summary>
        public required Border Button { get; init; }
        public required int Key { get; init; }
        public TextBlock? CooldownText { get; init; }
        public double ReadyAt { get; set; }
    }
}
