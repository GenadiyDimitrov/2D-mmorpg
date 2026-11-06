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
            _miniZoomIn = UiKit.TextButton(_mini, "+", () => SetMiniRange(_miniRange - 1), 18f);
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
            _miniZoomIn.interactable = _miniRange > 0;
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

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(RawImage));
            contentGo.transform.SetParent(_mapView, false);
            _mapContent = (RectTransform)contentGo.transform;
            UiKit.Place(_mapContent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
            var ground = contentGo.GetComponent<RawImage>();
            ground.texture = MapTexture.Texture;
            ground.raycastTarget = false;

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
    }

    /// <summary>The world map's input: drag to pan, the wheel to zoom, two fingers to pinch. It only reports; GameUi does
    /// the arithmetic.</summary>
    public sealed class MapViewInput : MonoBehaviour, IDragHandler, IScrollHandler
    {
        public Action<Vector2> Pan;
        public Action<float, Vector2?> Zoom;
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
