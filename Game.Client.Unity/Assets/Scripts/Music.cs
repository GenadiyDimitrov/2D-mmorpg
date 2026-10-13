using System.Collections.Generic;
using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// `BL-45` — the background music: one track at a time, picked by WHERE YOU STAND, crossfaded when that changes.
    ///
    /// <para>🔑 <b>A PLACE IS A FOLDER</b>, like the effects: <c>Resources/Music/&lt;place&gt;/</c>, every track in it
    /// a candidate, played in a shuffled order and crossfaded into the next when one ends. Places: <c>field</c>,
    /// <c>dungeon</c> (inside a dungeon or its entrance), and three town regions — <see cref="TownMusic"/> says which
    /// city plays which. The login screen plays the starting town's. An empty folder is silence.</para>
    ///
    /// <para>Client-only, read off your position with the shared map (<see cref="WorldDomain"/>,
    /// <see cref="WorldMap"/>), so the server knows nothing about it.</para>
    /// </summary>
    public class Music : MonoBehaviour
    {
        public GameBoot Boot;

        /// <summary>Which town region each city belongs to (`Towns.cs` ids). A city not listed plays <c>town_south</c>.
        /// Brackenford / Stonewatch = the green south where you start; Greymarsh / Ironreach = the middle lands;
        /// Frostmere = the cold north.</summary>
        private static readonly Dictionary<string, string> TownMusic = new Dictionary<string, string>
        {
            { "town_brackenford", "town_south" }, { "town_stonewatch", "town_south" }, { "outpost_training", "town_south" },
            { "town_greymarsh", "town_middle" }, { "castle_ironreach", "town_middle" },
            { "town_frostmere", "town_north" },
        };

        private const float Fade = 2.5f;          // seconds of crossfade
        private const float Recheck = 0.5f;       // how often the place is re-read

        private AudioSource _a, _b;               // _a is the one playing; _b fades out
        private string _place;
        private float _nextCheck;
        private readonly Dictionary<string, AudioClip[]> _tracks = new Dictionary<string, AudioClip[]>();
        private readonly Dictionary<string, int> _lastIndex = new Dictionary<string, int>();

        private void Awake()
        {
            _a = Make();
            _b = Make();
        }

        private AudioSource Make()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = false;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        private void Update()
        {
            float level = Sfx.MusicLevel;
            float dt = Time.unscaledDeltaTime;

            if (Time.unscaledTime >= _nextCheck)
            {
                _nextCheck = Time.unscaledTime + Recheck;
                var place = PlaceNow();
                if (place != _place) { _place = place; Next(); }
            }

            // A track about to end hands over to the next one from the same place.
            if (_a.clip != null && _a.isPlaying && _a.clip.length > Fade * 2f && _a.time >= _a.clip.length - Fade) Next();
            else if (_a.clip != null && !_a.isPlaying && level > 0f) Next();

            _a.volume = Mathf.MoveTowards(_a.volume, level, dt / Fade * Mathf.Max(level, 0.01f));
            _b.volume = Mathf.MoveTowards(_b.volume, 0f, dt / Fade * Mathf.Max(_b.volume, 0.3f));
            if (_b.isPlaying && _b.volume <= 0.001f) _b.Stop();
            if (_a.volume > level) _a.volume = level;   // a slider pulled down takes effect at once
        }

        /// <summary>The folder for where you are right now.</summary>
        private string PlaceNow()
        {
            if (Boot == null || Boot.Entities == null || Boot.Phase != ClientPhase.InWorld) return "town_south";
            var self = Boot.Entities.Find(Boot.SelfId);
            if (self == null) return _place ?? "town_south";
            var p = WorldMapper.ToServer(self.transform.position);
            if (WorldDomain.At(p.x, p.y).Kind == DomainKind.Dungeon) return "dungeon";
            var zone = WorldMap.SafeZoneAt(p.x, p.y);
            if (zone != null) return TownMusic.TryGetValue(zone.Id, out var m) ? m : "town_south";
            return "field";
        }

        /// <summary>Crossfade to another track of the current place: the playing source becomes the fading one.</summary>
        private void Next()
        {
            var clip = Pick(_place);
            var old = _a;
            _a = _b;
            _b = old;
            _a.Stop();
            _a.clip = clip;
            _a.volume = 0f;
            if (clip != null) _a.Play();
        }

        /// <summary>A track from the folder, never the one just played when there is a choice.</summary>
        private AudioClip Pick(string place)
        {
            if (string.IsNullOrEmpty(place)) return null;
            if (!_tracks.TryGetValue(place, out var clips))
            {
                clips = Resources.LoadAll<AudioClip>("Music/" + place) ?? new AudioClip[0];
                _tracks[place] = clips;
            }
            if (clips.Length == 0) return null;
            int last = _lastIndex.TryGetValue(place, out var l) ? l : -1;
            int i = Random.Range(0, clips.Length);
            if (clips.Length > 1 && i == last) i = (i + 1) % clips.Length;
            _lastIndex[place] = i;
            return clips[i];
        }
    }
}
