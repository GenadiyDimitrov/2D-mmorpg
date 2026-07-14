using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Game.Client.Wpf;

/// <summary>
/// Makes every popup panel MOVABLE and CLOSABLE, and lets you raise one above the others.
///
/// The panels are plain <see cref="Border"/>s sitting in the root Grid, positioned by
/// alignment + margin, and they overlapped each other with no way to shift them — the Debug
/// window sat on top of the Inventory, and Stats/Skills covered each other.
///
/// The chrome is added at RUNTIME rather than authored into the XAML thirteen times. That keeps
/// one copy of the drag code instead of thirteen, means a new panel gets the behaviour by adding a
/// single line to <see cref="EnableMovablePanels"/>, and it does not disturb the existing layout:
/// each panel keeps its authored "home" position and is nudged from there by a RenderTransform.
/// </summary>
public partial class MainWindow
{
    /// <summary>Next Z-order to hand out. Clicking a panel raises it above the rest, which is the
    /// other half of "the Debug window is covering my inventory".</summary>
    private int _panelZ = 10;

    /// <summary>Give every popup a drag strip, a close button and click-to-raise. Called once.</summary>
    private void EnableMovablePanels()
    {
        // Every panel is a plain Visibility toggle, so the default ✕ (just collapse it) is correct —
        // EXCEPT EquipPopup, whose real close ALSO clears the item it is acting on. Collapsing that one
        // without clearing would leave a stale instance id behind, so it gets its own close action.
        EnableChrome(InventoryPanel);
        EnableChrome(StatsPanel);
        EnableChrome(SkillsPanel);
        EnableChrome(DebugPanel);
        EnableChrome(SettingsPanel);
        EnableChrome(PartyPanel);
        EnableChrome(AutoHuntPanel);
        EnableChrome(ShopPanel);
        EnableChrome(DialogPanel);
        EnableChrome(ClassPanel);
        EnableChrome(SkillDetailPopup);
        EnableChrome(EquipPopup, () => EquipPopupClose_Click(this, new RoutedEventArgs()));
    }

    /// <summary>Wrap one panel: a slim drag strip across the top (with a ✕ on the right), a
    /// TranslateTransform to move it by, and raise-on-click.
    /// <paramref name="onClose"/> overrides the default "just collapse it" for panels whose close
    /// has side effects.</summary>
    private void EnableChrome(Border panel, Action? onClose = null)
    {
        var move = new TranslateTransform();
        panel.RenderTransform = move;

        // Raise this panel above the others whenever it is touched. PREVIEW, so it fires even when
        // a child control (a button, a list) handles the click itself.
        panel.PreviewMouseLeftButtonDown += (_, _) => Panel.SetZIndex(panel, ++_panelZ);

        var strip = new Grid
        {
            Height = 18,
            Background = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)),
            Cursor = Cursors.SizeAll,
            Margin = new Thickness(0, 0, 0, 4),
        };
        strip.Children.Add(new TextBlock
        {
            Text = "⠿ drag",
            Foreground = new SolidColorBrush(Color.FromArgb(0xB0, 0xFF, 0xFF, 0xFF)),
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(4, 0, 0, 0),
            IsHitTestVisible = false,          // clicks pass through to the strip, which does the drag
        });

        var close = new Button
        {
            Content = "✕",
            Width = 18, Height = 16, FontSize = 9, Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 2, 0),
            ToolTip = "Close",
        };
        close.Click += (_, _) =>
        {
            if (onClose is not null) onClose();
            else panel.Visibility = Visibility.Collapsed;
        };
        strip.Children.Add(close);

        // Drag by the strip. A Grid takes no capture of its own (unlike ButtonBase — see the
        // skill-bar drag saga), so a plain capture here behaves.
        Point origin = default;
        bool dragging = false;
        strip.MouseLeftButtonDown += (_, e) =>
        {
            origin = e.GetPosition(this);
            dragging = true;
            strip.CaptureMouse();
        };
        strip.MouseMove += (_, e) =>
        {
            if (!dragging) return;
            var now = e.GetPosition(this);
            move.X += now.X - origin.X;
            move.Y += now.Y - origin.Y;
            origin = now;
        };
        strip.MouseLeftButtonUp += (_, _) =>
        {
            dragging = false;
            strip.ReleaseMouseCapture();
        };

        // Re-parent the panel's existing content under the strip. The panel keeps everything it had;
        // it just gains a header row.
        var content = panel.Child;
        panel.Child = null;
        var dock = new DockPanel();
        DockPanel.SetDock(strip, Dock.Top);
        dock.Children.Add(strip);
        if (content is not null)
            dock.Children.Add(content);
        panel.Child = dock;
    }
}
