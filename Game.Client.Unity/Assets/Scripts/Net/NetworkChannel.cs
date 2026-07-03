using System;
using System.Threading.Tasks;
using Game.Shared;
using Microsoft.AspNetCore.SignalR.Client;

namespace Game.Client
{
    /// <summary>
    /// The single seam between game code and the wire — identical to the WPF client's
    /// NetworkChannel (no UI dependencies). SignalR callbacks arrive on a BACKGROUND thread;
    /// consumers (GameBoot) must marshal event handlers onto Unity's main thread
    /// (see UnityMainThreadDispatcher) before touching any UnityEngine API.
    /// </summary>
    public class NetworkChannel : IAsyncDisposable
    {
        private HubConnection _connection;

        public event Action<WorldSnapshot> SnapshotReceived;
        public event Action<ChatMessage> ChatReceived;
        public event Action<CombatEvent> CombatReceived;
        public event Action<ProgressUpdate> ProgressReceived;
        public event Action<CastInfo> CastReceived;
        public event Action<InventoryUpdate> InventoryReceived;
        public event Action<StatsUpdate> StatsReceived;
        public event Action<BuffUpdate> BuffsReceived;
        public event Action<GoldUpdate> GoldReceived;
        public event Action<TargetDetails> TargetDetailsReceived;
        public event Action<LearnedSkills> LearnedReceived;
        public event Action<NpcDialog> DialogReceived;
        public event Action<QuestLog> QuestLogReceived;
        public event Action<string> Disconnected;
        public event Action<string> ForceDisconnected;

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public async Task ConnectAsync(string url)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();

            _connection.On<WorldSnapshot>("Snapshot", s => SnapshotReceived?.Invoke(s));
            _connection.On<ChatMessage>("Chat", m => ChatReceived?.Invoke(m));
            _connection.On<CombatEvent>("Combat", c => CombatReceived?.Invoke(c));
            _connection.On<ProgressUpdate>("Progress", p => ProgressReceived?.Invoke(p));
            _connection.On<CastInfo>("Cast", c => CastReceived?.Invoke(c));
            _connection.On<InventoryUpdate>("Inventory", i => InventoryReceived?.Invoke(i));
            _connection.On<StatsUpdate>("Stats", st => StatsReceived?.Invoke(st));
            _connection.On<LearnedSkills>("Learned", l => LearnedReceived?.Invoke(l));
            _connection.On<NpcDialog>("Dialog", d => DialogReceived?.Invoke(d));
            _connection.On<QuestLog>("QuestLog", q => QuestLogReceived?.Invoke(q));
            _connection.On<BuffUpdate>("Buffs", b => BuffsReceived?.Invoke(b));
            _connection.On<GoldUpdate>("Gold", g => GoldReceived?.Invoke(g));
            _connection.On<TargetDetails>("TargetDetails", d => TargetDetailsReceived?.Invoke(d));
            _connection.On<string>("ForceDisconnect", reason => ForceDisconnected?.Invoke(reason));
            _connection.Closed += ex =>
            {
                Disconnected?.Invoke(ex?.Message ?? "Connection closed.");
                return Task.CompletedTask;
            };

            await _connection.StartAsync();
        }

        // ----- Auth + character flow -----
        public Task<AuthResponse> RegisterAsync(string username, string password) =>
            _connection.InvokeAsync<AuthResponse>("Register", new AuthRequest(username, password));

        public Task<AuthResponse> LoginAsync(string username, string password) =>
            _connection.InvokeAsync<AuthResponse>("Login", new AuthRequest(username, password));

        public Task<CharacterList> ListCharactersAsync() =>
            _connection.InvokeAsync<CharacterList>("ListCharacters");

        public Task<string> CreateCharacterAsync(string name, Race race, BaseClass baseClass) =>
            _connection.InvokeAsync<string>("CreateCharacter", new CreateCharacterRequest(name, race, baseClass));

        public Task<LoginResult> EnterWorldAsync(int characterId) =>
            _connection.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(characterId));

        // ----- In-world commands (the slice uses Move + Attack; the rest are ready for later) -----
        public Task MoveAsync(float targetX, float targetY) =>
            _connection.SendAsync("Move", new MoveCommand(targetX, targetY));

        public Task AttackAsync(Guid targetId) =>
            _connection.SendAsync("Attack", targetId);

        public Task UseSkillAsync(string skillId, Guid? targetId) =>
            _connection.SendAsync("UseSkill", skillId, targetId);

        public Task SetMoveStateAsync(MoveState state) =>
            _connection.SendAsync("SetMoveState", (int)state);

        public Task LeaveWorldAsync() =>
            _connection.SendAsync("LeaveWorld");

        public async ValueTask DisposeAsync()
        {
            if (_connection is not null)
                await _connection.DisposeAsync();
        }
    }
}
