using System;
using System.Collections.Generic;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: `BL-365` — the MINIMAP (a round corner map under the action buttons) and the WORLD MAP window
    /// (key N, a tap on the minimap, or Menu → Map).
    ///
    /// <para>Both draw <see cref="MapTexture"/>, the overworld's own ground colours, and put the markers on top from what
    /// the client already holds: entities come from <see cref="EntityManager"/> (so only what the server sends you, i.e.
    /// inside the entity distance), towns and fields from the shared <see cref="Towns"/> / <see cref="RegionMap"/>. Nothing
    /// here asks the server for anything. Party members show where they are in view: the party roster carries no
    /// position.</para>
    ///
    /// <para>The minimap is north-up by default; "Rotate" turns it with the camera. Its range is one of
    /// <see cref="MiniRanges"/>. Its settings are LOOK, saved per device in PlayerPrefs.</para>
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        // ----- minimap ---------------------------------------------------------------------------

        private const float MiniSize = 170f;
        private static readonly float[] MiniRanges = { 2000f, 4000f, 8000f };   // server units from centre to edge
        private const string PrefMiniShown = "ui.minimap", PrefMiniRotate = "ui.minimap.rotate", PrefMiniRange = "ui.minimap.range";

        private RectTransform _mini, _miniSpin, _miniDots, _miniArrow, _miniNorth;
        private RawImage _miniTerrain;
        private Button _miniZoomIn, _miniZoomOut;
        private readonly List<Image> _miniDotPool = new List<Image>();
        private readonly List<string> _miniDotNames = new List<string>();   // `BL-366`: parallel to the pool, for the hover
        private SafeZone _miniTown;   // the town you stand in (its "+" opens the city view), or null
        private bool _miniShown = true, _miniRotate;
        private int _miniRange = 1;

        private static readonly Color DotSelfParty = new Color(0.35f, 0.95f, 0.55f);
        private static readonly Color DotPlayer = new Color(0.92f, 0.94f, 0.98f);
        private static readonly Color DotMob = new Color(0.95f, 0.66f, 0.25f);
        private static readonly Color DotAggro = new Color(0.95f, 0.25f, 0.22f);
        private static readonly Color DotNpc = new Color(1.00f, 0.88f, 0.30f);
        private static readonly Color MapUnderground = new Color(0.09f, 0.09f, 0.10f, 1f);

        private void BuildMinimap()
        {
            _miniShown = PlayerPrefs.GetInt(PrefMiniShown, 1) == 1;
            _miniRotate = PlayerPrefs.GetInt(PrefMiniRotate, 0) == 1;
            _miniRange = Mathf.Clamp(PlayerPrefs.GetInt(PrefMiniRange, 1), 0, MiniRanges.Length - 1);

            // The frame: a border-coloured disc, a tap on it opens the world map.
            var frame = UiKit.Box(_worldRoot, "Minimap", UiKit.Border);
            frame.sprite = MapTexture.Circle;
            _mini = UiKit.Rect(frame.gameObject);
            UiKit.Place(_mini, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -152f),
                        new Vector2(MiniSize, MiniSize));
            var open = frame.gameObject.AddComponent<Button>();
            open.targetGraphic = frame;
            open.onClick.AddListener(OpenWorldMap);

            // The window: a circle MASK a few px inside the frame. Everything that scrolls sits under it.
            var hole = UiKit.Box(_mini, "Mask", MapUnderground, blocksInput: false);
            hole.sprite = MapTexture.Circle;
            var holeRect = UiKit.Stretch(UiKit.Rect(hole.gameObject), 3f, 3f, 3f, 3f);
            hole.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            // The spinning layer: north-up = no rotation; "Rotate" turns it with the camera.
            var spinGo = new GameObject("Spin", typeof(RectTransform));
            spinGo.transform.SetParent(holeRect, false);
            _miniSpin = (RectTransform)spinGo.transform;
            UiKit.Place(_miniSpin, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // 1.5× the window, so a turned square still covers the circle (√2 would do; the rest is margin).
            var terrainGo = new GameObject("Terrain", typeof(RectTransform), typeof(RawImage));
            terrainGo.transform.SetParent(_miniSpin, false);
            _miniTerrain = terrainGo.GetComponent<RawImage>();
            _miniTerrain.texture = MapTexture.Texture;
            _miniTerrain.raycastTarget = false;
            UiKit.Place(UiKit.Rect(terrainGo), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                        new Vector2(MiniSize * 1.5f, MiniSize * 1.5f));

            var dotsGo = new GameObject("Dots", typeof(RectTransform));
            dotsGo.transform.SetParent(_miniSpin, false);
            _miniDots = (RectTransform)dotsGo.transform;
            UiKit.Place(_miniDots, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // You: always the centre, never rotated with the ground, pointing where you face.
            var arrow = UiKit.Box(holeRect, "You", DotSelfParty, blocksInput: false);
            arrow.sprite = MapTexture.Arrow;
            _miniArrow = UiKit.Rect(arrow.gameObject);
            UiKit.Place(_miniArrow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 18f));

            var north = UiKit.Label(_mini, "N", 15f, new Color(0.95f, 0.35f, 0.30f), TextAlignmentOptions.Center);
            north.fontStyle = FontStyles.Bold;
            _miniNorth = UiKit.Rect(north.gameObject);
            UiKit.Place(_miniNorth, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f));

            // Range: two small buttons on the lower-left rim. ASCII only — the atlas has no minus sign or magnifier.
            // `BL-366`: at the closest range, inside a town, "+" goes one step further — the city view.
            _miniZoomIn = UiKit.TextButton(_mini, "+", () =>
            {
                if (_miniRange == 0 && _miniTown != null) OpenCityView(_miniTown);
                else SetMiniRange(_miniRange - 1);
            }, 18f);
            UiKit.Place(UiKit.Rect(_miniZoomIn.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(-6f, 24f), new Vector2(30f, 30f));
            _miniZoomOut = UiKit.TextButton(_mini, "-", () => SetMiniRange(_miniRange + 1), 18f);
            UiKit.Place(UiKit.Rect(_miniZoomOut.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(-6f, -10f), new Vector2(30f, 30f));
            RefreshMiniZoomButtons();
        }

        private void SetMiniRange(int index)
        {
            _miniRange = Mathf.Clamp(index, 0, MiniRanges.Length - 1);
            PlayerPrefs.SetInt(PrefMiniRange, _miniRange);
            RefreshMiniZoomButtons();
        }

        private void RefreshMiniZoomButtons()
        {
            _miniZoomIn.interactable = _miniRange > 0 || _miniTown != null;
            _miniZoomOut.interactable = _miniRange < MiniRanges.Length - 1;
        }

        private void SetMiniShown(bool on)
        {
            _miniShown = on;
            PlayerPrefs.SetInt(PrefMiniShown, on ? 1 : 0);
            RefreshWorldMapButtons();
        }

        private void SetMiniRotate(bool on)
        {
            _miniRotate = on;
            PlayerPrefs.SetInt(PrefMiniRotate, on ? 1 : 0);
            RefreshWorldMapButtons();
        }

        /// <summary>Where you are, in server units, read off the drawn (interpolated) body so the map moves as smoothly as
        /// you do. False before the world has a body for you.</summary>
        private bool SelfMapPosition(out Vector2 pos, out float facing)
        {
            pos = default;
            facing = 0f;
            var view = Boot.Entities != null ? Boot.Entities.Find(Boot.SelfId) : null;
            if (view == null) return false;
            pos = WorldMapper.ToServer(view.transform.position);
            facing = view.transform.eulerAngles.y;   // degrees clockwise from north (+Z), see EntityView.DriveModel
            return true;
        }

        private void RefreshMinimap()
        {
            MapTexture.Step();

            bool has = SelfMapPosition(out var me, out float facing);
            bool show = _miniShown && has;
            if (_mini.gameObject.activeSelf != show) _mini.gameObject.SetActive(show);
            if (!show) return;

            var town = TownAt(me);
            if (town != _miniTown) { _miniTown = town; RefreshMiniZoomButtons(); }

            float yaw = _miniRotate && Boot.CameraRig != null ? Boot.CameraRig.Yaw : 0f;
            float radius = MiniSize / 2f - 3f;
            float range = MiniRanges[_miniRange];
            float pxPerUnit = radius / range;

            // The camera turned right by Yaw, so north sits Yaw counter-clockwise of up — the compass's rule.
            _miniSpin.localRotation = Quaternion.Euler(0f, 0f, yaw);
            _miniArrow.localRotation = Quaternion.Euler(0f, 0f, yaw - facing);
            float r = yaw * Mathf.Deg2Rad;
            _miniNorth.anchoredPosition = new Vector2(-Mathf.Sin(r), Mathf.Cos(r)) * (radius - 10f);

            // The ground: the texture window around you. Underground (a dungeon, the jail) has no overworld under it.
            bool above = MapTexture.InOverworld(me.x, me.y);
            _miniTerrain.enabled = above;
            if (above)
            {
                float span = MiniSize * 1.5f / pxPerUnit;   // server units the terrain square covers
                var uv = MapTexture.Uv(me.x, me.y);
                float w = span / MapTexture.WorldW, h = span / MapTexture.WorldH;
                _miniTerrain.uvRect = new Rect(uv.x - w / 2f, uv.y - h / 2f, w, h);
            }

            // The dots: everyone the server has sent you, inside the circle.
            int used = 0;
            if (Boot.Entities != null)
            {
                float limit = radius - 4f;
                foreach (var pair in Boot.Entities.States)
                {
                    if (pair.Key == Boot.SelfId) continue;
                    var e = pair.Value;
                    if (e.Dead) continue;
                    var view = Boot.Entities.Find(pair.Key);
                    if (view == null) continue;
                    var p = WorldMapper.ToServer(view.transform.position);
                    // Server Y grows DOWN, the UI's grows up.
                    var ui = new Vector2(p.x - me.x, me.y - p.y) * pxPerUnit;
                    if (ui.sqrMagnitude > limit * limit) continue;

                    bool party = IsPartyMember(e.Id);
                    Color colour = party ? DotSelfParty
                                 : e.Kind == EntityKind.Player ? DotPlayer
                                 : e.Kind == EntityKind.Npc ? DotNpc
                                 : e.Aggressive ? DotAggro : DotMob;
                    float size = party ? 9f : e.Kind == EntityKind.Mob ? 5f : 7f;
                    while (_miniDotNames.Count <= used) _miniDotNames.Add(null);
                    _miniDotNames[used] = e.Kind == EntityKind.Npc ? e.Name : e.Name + "  Lv " + e.Level;
                    var dot = MiniDot(used++);
                    dot.color = colour;
                    var rt = dot.rectTransform;
                    rt.anchoredPosition = ui;
                    rt.sizeDelta = new Vector2(size, size);
                    if (party) rt.SetAsLastSibling();
                }
            }
            for (int i = used; i < _miniDotPool.Count; i++)
                if (_miniDotPool[i].enabled) _miniDotPool[i].enabled = false;
        }

        private Image MiniDot(int index)
        {
            while (_miniDotPool.Count <= index)
            {
                var img = UiKit.Box(_miniDots, "Dot", Color.white, blocksInput: false);
                img.sprite = MapTexture.Circle;
                UiKit.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f));
                _miniDotPool.Add(img);
            }
            var dot = _miniDotPool[index];
            if (!dot.enabled) dot.enabled = true;
            return dot;
        }

        private bool IsPartyMember(Guid id)
        {
            var party = Boot.Party;
            if (party == null) return false;
            foreach (var m in party)
                if (m.Id == id) return true;
            return false;
        }

        // ----- world map -------------------------------------------------------------------------

        private RectTransform _mapPanel, _mapView, _mapContent, _mapSelf;
        private TextMeshProUGUI _mapStatus;
        private Button _mapMiniToggle, _mapRotateToggle;
        private readonly List<(TextMeshProUGUI Label, bool Town)> _mapLabels = new List<(TextMeshProUGUI, bool)>();
        private readonly List<Image> _mapPartyDots = new List<Image>();
        private Vector2 _mapCentre;     // the server point at the middle of the view
        private float _mapZoom;         // UI units per server unit; 0 = not yet fitted
        private bool _mapFollow = true; // re-centre on you until you pan away

        private const float MapMaxZoom = 0.2f;
        /// <summary>Field names appear once a field is at least this many UI units across — below it they are a pile of text.</summary>
        private const float FieldLabelZoom = 0.025f;

        private void BuildWorldMap()
        {
            _mapPanel = UiKit.PanelBox(_worldRoot, "Map");
            UiKit.Place(_mapPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(760f, 700f));
            var inner = _mapPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_mapPanel, "Map", () => CloseWindow(_mapPanel));
            UiKit.MakeAdjustable(_mapPanel, "map", new Vector2(420f, 360f));
            const float footer = 56f;

            var view = UiKit.Box(inner, "View", MapUnderground);
            _mapView = UiKit.Stretch(UiKit.Rect(view.gameObject), 8f, chrome + 6f, 8f, footer);
            view.gameObject.AddComponent<RectMask2D>();
            var input = view.gameObject.AddComponent<MapViewInput>();
            input.Pan = PanWorldMap;
            input.Zoom = ZoomWorldMap;
            input.Click = TapWorldMap;   // `BL-366`: a tap names what is under it

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(RawImage));
            contentGo.transform.SetParent(_mapView, false);
            _mapContent = (RectTransform)contentGo.transform;
            UiKit.Place(_mapContent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
            var ground = contentGo.GetComponent<RawImage>();
            ground.texture = MapTexture.Texture;
            ground.raycastTarget = false;
            BuildMapPlans();   // `BL-366`: plans + NPCs, behind the town and field labels

            // Towns: name in white. Fields: name + level band, dimmer, shown only when zoomed in enough to read.
            foreach (var town in Towns.All)
            {
                if (!MapTexture.InOverworld(town.X, town.Y)) continue;   // dungeon entrances live underground
                var dot = MapMarker(UiKit.Box(_mapContent, "Town", new Color(0.95f, 0.92f, 0.80f), blocksInput: false).rectTransform,
                                    town.X, town.Y, new Vector2(10f, 10f));
                dot.GetComponent<Image>().sprite = MapTexture.Circle;
                var label = UiKit.Label(_mapContent, town.Name, 17f, Color.white, TextAlignmentOptions.Bottom);
                label.fontStyle = FontStyles.Bold;
                label.outlineWidth = 0.2f;
                label.outlineColor = new Color32(0, 0, 0, 255);
                MapMarker(label.rectTransform, town.X, town.Y, new Vector2(240f, 26f)).pivot = new Vector2(0.5f, 0f);
                label.rectTransform.anchoredPosition = new Vector2(0f, 6f);
                _mapLabels.Add((label, true));
                AddCityButton(town);   // `BL-366`
            }
            foreach (var field in RegionMap.Fields)
            {
                if (field.Outline.Length < 3) continue;
                float cx = 0f, cy = 0f;
                foreach (var p in field.Outline) { cx += p.X; cy += p.Y; }
                cx /= field.Outline.Length;
                cy /= field.Outline.Length;
                if (!MapTexture.InOverworld(cx, cy)) continue;
                var band = RegionMap.LevelBand(field.Id);
                string text = field.Name + (band is { } b && b.Max > 0 ? "\n<size=12>Lv " + b.Min + "-" + b.Max + "</size>" : "");
                var label = UiKit.Label(_mapContent, text, 14f, new Color(0.92f, 0.93f, 0.85f), TextAlignmentOptions.Center);
                label.outlineWidth = 0.2f;
                label.outlineColor = new Color32(0, 0, 0, 255);
                MapMarker(label.rectTransform, cx, cy, new Vector2(200f, 40f));
                _mapLabels.Add((label, false));
            }

            // Party first, you last, so you are drawn on top.
            var self = UiKit.Box(_mapContent, "You", DotSelfParty, blocksInput: false);
            self.sprite = MapTexture.Arrow;
            _mapSelf = MapMarker(self.rectTransform, 0f, 0f, new Vector2(22f, 22f));
            BuildCityBack(_mapView);   // over the content, so it never pans away
            BuildMapTip();

            _mapStatus = UiKit.Label(inner, "", 14f, UiKit.TextDim, TextAlignmentOptions.Left);
            UiKit.Place(_mapStatus.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(14f, 8f), new Vector2(240f, 40f));

            // The footer, right to left: zoom, the two minimap switches, centre-on-me.
            float x = -60f;   // clear of the resize grip
            Button Footer(string text, Action click, float width)
            {
                var b = UiKit.TextButton(inner, text, click, 15f);
                UiKit.Place(UiKit.Rect(b.gameObject), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(x, 8f), new Vector2(width, 40f));
                x -= width + 6f;
                return b;
            }
            Footer("+", () => ZoomWorldMap(1.5f, null), 40f);
            Footer("-", () => ZoomWorldMap(1f / 1.5f, null), 40f);
            _mapRotateToggle = Footer("", () => SetMiniRotate(!_miniRotate), 130f);
            _mapMiniToggle = Footer("", () => SetMiniShown(!_miniShown), 130f);
            Footer("Me", () => { _mapFollow = true; }, 60f);

            RefreshWorldMapButtons();
            _mapPanel.gameObject.SetActive(false);
        }

        /// <summary>Pin <paramref name="rt"/> to a server point on the map. Anchored by FRACTION, so zooming (which only
        /// resizes the content) carries every marker with it and no label ever scales.</summary>
        private static RectTransform MapMarker(RectTransform rt, float x, float y, Vector2 size)
        {
            var uv = MapTexture.Uv(x, y);
            rt.anchorMin = rt.anchorMax = uv;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            return rt;
        }

        private void RefreshWorldMapButtons()
        {
            if (_mapMiniToggle == null) return;
            UiKit.SetButtonText(_mapMiniToggle, _miniShown ? "Minimap: on" : "Minimap: off");
            UiKit.SetButtonText(_mapRotateToggle, _miniRotate ? "Rotate: on" : "Rotate: off");
        }

        private void OpenWorldMap()
        {
            if (_mapPanel.gameObject.activeSelf) { _mapPanel.SetAsLastSibling(); return; }
            _mapFollow = true;
            OpenWindow(_mapPanel);
        }

        private void ToggleWorldMap()
        {
            if (_mapPanel.gameObject.activeSelf) CloseWindow(_mapPanel);
            else OpenWorldMap();
        }

        private float MapFitZoom()
        {
            var size = _mapView.rect.size;
            return Mathf.Max(0.0001f, Mathf.Min(size.x / MapTexture.WorldW, size.y / MapTexture.WorldH));
        }

        private void PanWorldMap(Vector2 screenDelta)
        {
            if (_mapZoom <= 0f) return;
            float scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            var d = screenDelta / scale;
            _mapCentre.x -= d.x / _mapZoom;
            _mapCentre.y += d.y / _mapZoom;   // UI up = server Y down
            _mapFollow = false;
        }

        /// <summary>Zoom by <paramref name="factor"/>, keeping the point under <paramref name="screenPoint"/> still (the
        /// view's centre when null — the footer buttons).</summary>
        private void ZoomWorldMap(float factor, Vector2? screenPoint)
        {
            if (_mapZoom <= 0f) return;
            float next = Mathf.Clamp(_mapZoom * factor, MapFitZoom(), MapMaxZoom);
            if (screenPoint is { } sp && RectTransformUtility.ScreenPointToLocalPointInRectangle(_mapView, sp, null, out var local))
            {
                var off = local - _mapView.rect.center;
                float wx = _mapCentre.x + off.x / _mapZoom, wy = _mapCentre.y - off.y / _mapZoom;
                _mapCentre = new Vector2(wx - off.x / next, wy + off.y / next);
                _mapFollow = false;
            }
            _mapZoom = next;
        }

        private void RefreshWorldMap()
        {
            if (_mapPanel == null || !_mapPanel.gameObject.activeInHierarchy) return;

            float fit = MapFitZoom();
            if (_mapZoom <= 0f) _mapZoom = Mathf.Min(MapMaxZoom, fit * 3f);   // first open: a region, not the whole world
            _mapZoom = Mathf.Clamp(_mapZoom, fit, MapMaxZoom);

            bool has = SelfMapPosition(out var me, out float facing);
            bool above = has && MapTexture.InOverworld(me.x, me.y);
            if (_mapFollow && above) _mapCentre = me;

            // Keep the view over the world: the centre may not leave it by more than half a view.
            var view = _mapView.rect.size;
            float halfW = view.x / 2f / _mapZoom, halfH = view.y / 2f / _mapZoom;
            _mapCentre.x = halfW * 2f >= MapTexture.WorldW ? MapTexture.WorldW / 2f
                         : Mathf.Clamp(_mapCentre.x, halfW, MapTexture.WorldW - halfW);
            _mapCentre.y = halfH * 2f >= MapTexture.WorldH ? MapTexture.WorldH / 2f
                         : Mathf.Clamp(_mapCentre.y, halfH, MapTexture.WorldH - halfH);

            _mapContent.sizeDelta = new Vector2(MapTexture.WorldW, MapTexture.WorldH) * _mapZoom;
            _mapContent.anchoredPosition = new Vector2((MapTexture.WorldW / 2f - _mapCentre.x) * _mapZoom,
                                                       (_mapCentre.y - MapTexture.WorldH / 2f) * _mapZoom);

            RefreshMapCity();   // `BL-366`
            bool fields = _mapZoom >= FieldLabelZoom;
            foreach (var (label, town) in _mapLabels)
                if (!town && label.enabled != fields) label.enabled = fields;

            _mapSelf.gameObject.SetActive(above);
            if (above)
            {
                MapMarker(_mapSelf, me.x, me.y, new Vector2(22f, 22f));
                _mapSelf.localRotation = Quaternion.Euler(0f, 0f, -facing);
                _mapSelf.SetAsLastSibling();
            }

            // Party members the server is sending you (in view); the roster has no position for the rest.
            int used = 0, total = 0;
            if (Boot.Party != null && Boot.Entities != null)
                foreach (var m in Boot.Party)
                {
                    if (m.Id == Boot.SelfId) continue;
                    total++;
                    var v = Boot.Entities.Find(m.Id);
                    if (v == null) continue;
                    var p = WorldMapper.ToServer(v.transform.position);
                    if (!MapTexture.InOverworld(p.x, p.y)) continue;
                    while (_mapPartyDots.Count <= used)
                    {
                        var img = UiKit.Box(_mapContent, "Party", DotSelfParty, blocksInput: false);
                        img.sprite = MapTexture.Circle;
                        _mapPartyDots.Add(img);
                    }
                    var dot = _mapPartyDots[used++];
                    dot.gameObject.SetActive(true);
                    MapMarker(dot.rectTransform, p.x, p.y, new Vector2(12f, 12f));
                }
            for (int i = used; i < _mapPartyDots.Count; i++) _mapPartyDots[i].gameObject.SetActive(false);
            if (above) _mapSelf.SetAsLastSibling();

            _mapStatus.text = !has ? ""
                : !above ? "Underground: this map shows the overworld."
                : !MapTexture.Ready ? "Drawing the map..."
                : total > 0 ? "Party in view: " + used + " / " + total
                : "";
        }

        // ----- `BL-366`: NPCs, the city view, names under the pointer ------------------------------
        //
        // His words (2026-11-06): roads outside towns; NPC positions outside towns; a city view with houses and NPC
        // positions behind "+" on each city and "-" to leave it; hovering the city view or a minimap dot names it.
        //
        // The city view is the world map ZOOMED onto one town with its plan drawn crisp on top (the same TownLayout the
        // 3D town is built from). Below CityShowZoom the plan is just the painted texels and the town's NPCs are hidden,
        // so the world view stays one marker per town. Inside a town the minimap's "+" at its closest range opens it.

        /// <summary>The plan overlay and the town NPC dots appear at this zoom (a 6000-wide city = 240 UI px).</summary>
        private const float CityShowZoom = 0.04f;

        private readonly List<(RectTransform Plan, SafeZone Town)> _mapPlans = new List<(RectTransform, SafeZone)>();
        private readonly List<(GameObject Dot, bool InTown)> _mapNpcDots = new List<(GameObject, bool)>();
        /// <summary>Everything on the world map that answers "what is this?": NPCs (in-town ones only in the city view).</summary>
        private readonly List<(Vector2 Pos, string Name, bool InTown)> _mapNames = new List<(Vector2, string, bool)>();
        private SafeZone _mapCity;                 // the town the city view is on (null = the world)
        private float _mapZoomBeforeCity;
        private Vector2 _mapCentreBeforeCity;
        private bool _mapFollowBeforeCity;
        private Button _mapCityBack;

        /// <summary>The town (with a plan) whose wall <paramref name="p"/> is inside, or null.</summary>
        private static SafeZone TownAt(Vector2 p)
        {
            foreach (var t in Towns.All)
                if (TownLayout.HasPlan(t) && (p - new Vector2(t.X, t.Y)).sqrMagnitude <= t.Radius * t.Radius) return t;
            return null;
        }

        /// <summary>The plans first (behind every marker), then the NPCs. Called from BuildWorldMap before the labels.</summary>
        private void BuildMapPlans()
        {
            foreach (var plan in TownLayout.Plans)
            {
                var town = Towns.ById(plan.TownId);
                if (town == null || !MapTexture.InOverworld(town.X, town.Y)) continue;
                var go = new GameObject("Plan", typeof(RectTransform));
                go.transform.SetParent(_mapContent, false);
                var root = MapMarker((RectTransform)go.transform, town.X, town.Y, Vector2.zero);

                // Children in WORLD units off the town centre (UI y up = server y down); the root's scale is the zoom.
                Vector2 Local(Vec2 v) => new Vector2(v.X - town.X, town.Y - v.Y);
                Image Piece(Color col, Vector2 at, Vector2 size, float uiDeg, Sprite sprite = null)
                {
                    var img = UiKit.Box(root, "P", col, blocksInput: false);
                    if (sprite != null) img.sprite = sprite;
                    var rt = img.rectTransform;
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = at;
                    rt.sizeDelta = size;
                    rt.localRotation = Quaternion.Euler(0f, 0f, uiDeg);
                    return img;
                }

                var road = new Color(0.62f, 0.54f, 0.40f);
                var path = new Color(0.50f, 0.43f, 0.32f);
                foreach (var s in plan.Roads)
                {
                    Vector2 a = Local(s.A), b = Local(s.B), d = b - a;
                    Piece(s.Path ? path : road, (a + b) / 2f, new Vector2(d.magnitude + s.Width, s.Width),
                          Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                }
                Piece(new Color(0.70f, 0.63f, 0.48f), Local(plan.Centre), Vector2.one * plan.PlazaRadius * 2f, 0f, MapTexture.Circle);
                foreach (var lot in plan.Lots)
                {
                    // An edge under a smaller fill, as the 3D town does. A server angle turns clockwise on the map = -z in the UI.
                    Piece(new Color(0.10f, 0.12f, 0.13f), Local(lot.Centre), new Vector2(lot.W, lot.H), -lot.AngleDeg);
                    Piece(LotColour(lot.Kind), Local(lot.Centre), new Vector2(lot.W - 36f, lot.H - 36f), -lot.AngleDeg);
                }
                go.SetActive(false);
                _mapPlans.Add((root, town));
            }

            // NPCs. Out in the world: a dot and a name, shown with the field names. In a town: a dot for the city view.
            foreach (var npc in WorldMap.Npcs)
            {
                if (!MapTexture.InOverworld(npc.X, npc.Y)) continue;
                bool inTown = TownAt(new Vector2(npc.X, npc.Y)) != null;
                var dot = UiKit.Box(_mapContent, "Npc", DotNpc, blocksInput: false);
                dot.sprite = MapTexture.Circle;
                MapMarker(dot.rectTransform, npc.X, npc.Y, new Vector2(inTown ? 10f : 9f, inTown ? 10f : 9f));
                _mapNpcDots.Add((dot.gameObject, inTown));
                _mapNames.Add((new Vector2(npc.X, npc.Y), npc.Name, inTown));
                if (inTown) { dot.gameObject.SetActive(false); continue; }

                var label = UiKit.Label(_mapContent, npc.Name, 12f, DotNpc, TextAlignmentOptions.Top);
                label.outlineWidth = 0.2f;
                label.outlineColor = new Color32(0, 0, 0, 255);
                MapMarker(label.rectTransform, npc.X, npc.Y, new Vector2(180f, 18f)).pivot = new Vector2(0.5f, 1f);
                label.rectTransform.anchoredPosition = new Vector2(0f, -6f);
                _mapLabels.Add((label, false));   // with the field names: only once zoomed in enough to read
            }
        }

        /// <summary>The "+" beside a town's name on the world map.</summary>
        private void AddCityButton(SafeZone town)
        {
            if (!TownLayout.HasPlan(town)) return;
            var plus = UiKit.TextButton(_mapContent, "+", () => OpenCityView(town), 16f);
            MapMarker(UiKit.Rect(plus.gameObject), town.X, town.Y, new Vector2(26f, 26f)).pivot = new Vector2(0f, 0f);
            UiKit.Rect(plus.gameObject).anchoredPosition = new Vector2(10f, 6f);
            _mapCityButtons.Add(plus.gameObject);
        }

        private readonly List<GameObject> _mapCityButtons = new List<GameObject>();

        private void BuildCityBack(Transform view)
        {
            _mapCityBack = UiKit.TextButton(view, "", ExitCityView, 15f);
            UiKit.Place(UiKit.Rect(_mapCityBack.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -8f),
                        new Vector2(260f, 36f));
            _mapCityBack.gameObject.SetActive(false);
        }

        /// <summary>The world map, on <paramref name="town"/>'s plan. From a world-map "+" or the minimap's.</summary>
        private void OpenCityView(SafeZone town)
        {
            if (!_mapPanel.gameObject.activeSelf) OpenWorldMap();
            if (_mapCity == null)
            {
                _mapZoomBeforeCity = _mapZoom;
                _mapCentreBeforeCity = _mapCentre;
                _mapFollowBeforeCity = _mapFollow;
            }
            _mapCity = town;
            _mapFollow = false;
            _mapCentre = new Vector2(town.X, town.Y);
            var view = _mapView.rect.size;
            float fit = Mathf.Min(view.x, view.y) / (town.Radius * 2.2f);
            _mapZoom = Mathf.Clamp(fit, CityShowZoom, MapMaxZoom);
            UiKit.SetButtonText(_mapCityBack, "-   " + town.Name + "  (world map)");
        }

        private void ExitCityView()
        {
            if (_mapCity == null) return;
            _mapCity = null;
            _mapZoom = _mapZoomBeforeCity > 0f && _mapZoomBeforeCity < CityShowZoom ? _mapZoomBeforeCity : MapFitZoom() * 3f;
            _mapCentre = _mapCentreBeforeCity;
            _mapFollow = _mapFollowBeforeCity;
        }

        /// <summary>Per frame while the map is open, after the zoom is settled.</summary>
        private void RefreshMapCity()
        {
            // Zooming out of a city by hand leaves it, exactly as "-" does (minus the restore).
            if (_mapCity != null && _mapZoom < CityShowZoom * 0.999f) _mapCity = null;
            if (_mapCityBack.gameObject.activeSelf != (_mapCity != null)) _mapCityBack.gameObject.SetActive(_mapCity != null);

            bool city = _mapZoom >= CityShowZoom;
            foreach (var plus in _mapCityButtons)
                if (plus.activeSelf == city) plus.SetActive(!city);   // in the city view, "-" is the way out
            var viewSize = _mapView.rect.size;
            foreach (var (plan, town) in _mapPlans)
            {
                // Only the plans near the view: five towns, but each is hundreds of images.
                bool near = city && Mathf.Abs(town.X - _mapCentre.x) * _mapZoom < viewSize.x / 2f + town.Radius * _mapZoom
                                 && Mathf.Abs(town.Y - _mapCentre.y) * _mapZoom < viewSize.y / 2f + town.Radius * _mapZoom;
                if (plan.gameObject.activeSelf != near) plan.gameObject.SetActive(near);
                if (near) plan.localScale = new Vector3(_mapZoom, _mapZoom, 1f);
            }
            foreach (var (dot, inTown) in _mapNpcDots)
                if (inTown && dot.activeSelf != city) dot.SetActive(city);
        }

        // ----- the name under the pointer (hover on a desktop, a tap anywhere) ------------------------

        private RectTransform _mapTip;
        private TextMeshProUGUI _mapTipLabel;
        private string _mapTipShown;
        private string _mapTapName;
        private Vector2 _mapTapAt;
        private float _mapTapUntil;

        private void BuildMapTip()
        {
            _mapTip = UiKit.PanelBox(_worldRoot, "MapTip");
            var group = _mapTip.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            _mapTipLabel = UiKit.Label(_mapTip.GetChild(0), "", 14f, UiKit.Text, TextAlignmentOptions.Center);
            UiKit.Stretch(_mapTipLabel.rectTransform, 8f, 4f, 8f, 4f);
            _mapTip.gameObject.SetActive(false);
        }

        /// <summary>A tap on the world map (a phone's hover): name what is under it for a moment.</summary>
        private void TapWorldMap(Vector2 screen)
        {
            var name = WorldMapNameAt(screen);
            if (name == null) return;
            _mapTapName = name;
            _mapTapAt = screen;
            _mapTapUntil = Time.unscaledTime + 2.5f;
        }

        /// <summary>The world-map marker within ~10 UI px of a screen point, or null.</summary>
        private string WorldMapNameAt(Vector2 screen)
        {
            if (_mapPanel == null || !_mapPanel.gameObject.activeInHierarchy || _mapZoom <= 0f) return null;
            if (!RectTransformUtility.RectangleContainsScreenPoint(_mapView, screen, null)) return null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_mapView, screen, null, out var local)) return null;
            var off = local - _mapView.rect.center;
            var world = new Vector2(_mapCentre.x + off.x / _mapZoom, _mapCentre.y - off.y / _mapZoom);
            bool city = _mapZoom >= CityShowZoom;
            float best = 10f / _mapZoom;
            best *= best;
            string name = null;
            foreach (var (pos, n, inTown) in _mapNames)
            {
                if (inTown && !city) continue;
                float d = (pos - world).sqrMagnitude;
                if (d < best) { best = d; name = n; }
            }
            if (Boot.Party != null && Boot.Entities != null)
                foreach (var m in Boot.Party)
                {
                    if (m.Id == Boot.SelfId) continue;
                    var v = Boot.Entities.Find(m.Id);
                    if (v == null) continue;
                    var p = WorldMapper.ToServer(v.transform.position);
                    float d = (p - world).sqrMagnitude;
                    if (d < best) { best = d; name = m.Name; }
                }
            return name;
        }

        /// <summary>The minimap dot within ~7 UI px of the pointer, or null. The dots layer turns with the minimap, so the
        /// pointer is read in ITS space.</summary>
        private string MinimapNameAt(Vector2 screen)
        {
            if (_mini == null || !_mini.gameObject.activeInHierarchy) return null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_miniDots, screen, null, out var local)) return null;
            float radius = MiniSize / 2f - 3f;
            if (local.sqrMagnitude > radius * radius) return null;
            float best = 7f * 7f;
            string name = null;
            for (int i = 0; i < _miniDotNames.Count && i < _miniDotPool.Count; i++)
            {
                if (!_miniDotPool[i].enabled) continue;
                float d = (_miniDotPool[i].rectTransform.anchoredPosition - local).sqrMagnitude;
                if (d < best) { best = d; name = _miniDotNames[i]; }
            }
            return name;
        }

        private void RefreshMapTip()
        {
            if (_mapTip == null) return;
            string text = null;
            Vector2 at = default;
            if (HoverTip.Wanted)
            {
                at = Input.mousePosition;
                text = MinimapNameAt(at) ?? WorldMapNameAt(at);
            }
            if (text == null && _mapTapName != null && Time.unscaledTime < _mapTapUntil
                && _mapPanel.gameObject.activeInHierarchy)
            {
                text = _mapTapName;
                at = _mapTapAt;
            }

            if (text == null)
            {
                if (_mapTip.gameObject.activeSelf) _mapTip.gameObject.SetActive(false);
                _mapTipShown = null;
                return;
            }
            if (text != _mapTipShown)
            {
                _mapTipLabel.text = text;
                _mapTipShown = text;
                var pref = _mapTipLabel.GetPreferredValues(text, 400f, 0f);
                _mapTip.sizeDelta = new Vector2(Mathf.Min(pref.x, 400f) + 16f, pref.y + 8f);
            }
            PlaceNearScreenPoint(_mapTip, _mapTip.sizeDelta, at, 14f);
            if (!_mapTip.gameObject.activeSelf)
            {
                _mapTip.gameObject.SetActive(true);
                _mapTip.SetAsLastSibling();
            }
        }

        /// <summary>Put a card above-right of a screen point (flipped where it would leave the screen). Shared by the map's
        /// name tip and the skill bar's hover card (`BL-367`).</summary>
        private void PlaceNearScreenPoint(RectTransform card, Vector2 size, Vector2 screen, float gap)
        {
            var root = _worldRoot;
            card.anchorMin = card.anchorMax = root.pivot;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var p)) return;
            var r = root.rect;
            bool right = p.x + gap + size.x <= r.xMax;
            bool up = p.y + gap + size.y <= r.yMax;
            card.pivot = new Vector2(right ? 0f : 1f, up ? 0f : 1f);
            var at = p + new Vector2(right ? gap : -gap, up ? gap : -gap);
            at.x = Mathf.Clamp(at.x, r.xMin + (right ? 0f : size.x), r.xMax - (right ? size.x : 0f));
            at.y = Mathf.Clamp(at.y, r.yMin + (up ? 0f : size.y), r.yMax - (up ? size.y : 0f));
            card.anchoredPosition = at;
        }
    }

    /// <summary>The world map's input: drag to pan, the wheel to zoom, two fingers to pinch. It only reports; GameUi does
    /// the arithmetic.</summary>
    public sealed class MapViewInput : MonoBehaviour, IDragHandler, IScrollHandler, IPointerClickHandler
    {
        public Action<Vector2> Pan;
        public Action<float, Vector2?> Zoom;
        public Action<Vector2> Click;

        /// <summary>A tap or click that was not the end of a drag.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging) Click?.Invoke(eventData.position);
        }
        private float _pinchFrom;

        public void OnDrag(PointerEventData eventData)
        {
            if (Input.touchCount >= 2) return;   // a pinch, read in Update
            Pan?.Invoke(eventData.delta);
        }

        public void OnScroll(PointerEventData eventData)
        {
            float y = eventData.scrollDelta.y;
            if (Mathf.Abs(y) < 0.01f) return;
            Zoom?.Invoke(Mathf.Pow(1.15f, Mathf.Clamp(y, -5f, 5f)), eventData.position);
        }

        private void Update()
        {
            if (Input.touchCount != 2) { _pinchFrom = 0f; return; }
            var a = Input.GetTouch(0);
            var b = Input.GetTouch(1);
            var rt = (RectTransform)transform;
            if (!RectTransformUtility.RectangleContainsScreenPoint(rt, a.position, null)
                || !RectTransformUtility.RectangleContainsScreenPoint(rt, b.position, null)) { _pinchFrom = 0f; return; }
            float d = Vector2.Distance(a.position, b.position);
            if (_pinchFrom > 1f && d > 1f) Zoom?.Invoke(d / _pinchFrom, (a.position + b.position) / 2f);
            _pinchFrom = d;
        }
    }
}
