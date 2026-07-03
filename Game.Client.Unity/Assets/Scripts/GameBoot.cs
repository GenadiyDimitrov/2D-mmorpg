using System;
using System.Threading.Tasks;
using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// The slice's orchestrator: connect → (auto) login/register a dev account → create/enter a
    /// character → stream snapshots into the EntityManager and point the camera at "me". Auth runs
    /// once; SnapshotReceived fires every server tick (marshaled to the main thread). Wire the
    /// EntityManager + CameraRig references in the Inspector and set the Server URL for your device.
    /// </summary>
    public class GameBoot : MonoBehaviour
    {
        [Header("Server")]
        [Tooltip("Emulator: http://10.0.2.2:5238/game   Real phone (same Wi-Fi): http://<PC-LAN-IP>:5238/game")]
        public string ServerUrl = "http://10.0.2.2:5238/game";

        [Header("Dev auto-login")]
        public string Username = "phonedev";
        public string Password = "phonedev1";
        public string CharacterName = "Pathfinder";
        public Race Race = Race.Human;
        public BaseClass BaseClass = BaseClass.Fighter;

        [Header("Scene refs")]
        public EntityManager Entities;
        public CameraRig CameraRig;

        private NetworkChannel _net;
        private Guid _selfId;

        private async void Start()
        {
            // Ensure the dispatcher exists before any background callback needs it.
            _ = UnityMainThreadDispatcher.Instance;

            _net = new NetworkChannel();
            _net.SnapshotReceived += OnSnapshot;
            _net.Disconnected += m => Debug.LogWarning("[Net] Disconnected: " + m);
            _net.ForceDisconnected += m => Debug.LogWarning("[Net] Force disconnect: " + m);

            try { await ConnectAndEnter(); }
            catch (Exception ex) { Debug.LogError("[Net] Connect failed: " + ex); }
        }

        private async Task ConnectAndEnter()
        {
            await _net.ConnectAsync(ServerUrl);
            Debug.Log("[Net] Connected to " + ServerUrl);

            var auth = await _net.LoginAsync(Username, Password);
            if (!auth.Success) auth = await _net.RegisterAsync(Username, Password);
            if (!auth.Success) { Debug.LogError("[Net] Auth failed: " + auth.Error); return; }

            var chars = await _net.ListCharactersAsync();
            int charId;
            if (chars.Characters.Length > 0)
            {
                charId = chars.Characters[0].Id;
            }
            else
            {
                var err = await _net.CreateCharacterAsync(CharacterName, Race, BaseClass);
                if (err != null) { Debug.LogError("[Net] Create failed: " + err); return; }
                chars = await _net.ListCharactersAsync();
                charId = chars.Characters[0].Id;
            }

            var result = await _net.EnterWorldAsync(charId);
            if (!result.Success) { Debug.LogError("[Net] Enter failed: " + result.Error); return; }
            _selfId = result.EntityId;

            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (Entities != null) Entities.SelfId = _selfId;
                Debug.Log("[Net] In world as " + _selfId + " at (" + result.X + ", " + result.Y + ")");
            });
        }

        private void OnSnapshot(WorldSnapshot snapshot)
        {
            // SignalR delivers this off the main thread — hop back before touching Unity objects.
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (Entities == null) return;
                Entities.ApplySnapshot(snapshot.Entities);

                if (CameraRig != null && CameraRig.Target == null)
                {
                    var self = Entities.Find(_selfId);
                    if (self != null) CameraRig.Target = self.transform;
                }
            });
        }

        public async void Move(float serverX, float serverY)
        {
            try { await _net.MoveAsync(serverX, serverY); }
            catch (Exception ex) { Debug.LogWarning("[Net] Move: " + ex.Message); }
        }

        public async void Attack(Guid targetId)
        {
            try { await _net.AttackAsync(targetId); }
            catch (Exception ex) { Debug.LogWarning("[Net] Attack: " + ex.Message); }
        }

        private async void OnDestroy()
        {
            if (_net != null) await _net.DisposeAsync();
        }
    }
}
