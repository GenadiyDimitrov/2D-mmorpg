using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using AOT;
using Microsoft.AspNetCore.SignalR.Client;

namespace Game.Client
{
    /// <summary>
    /// `BL-363` — the wire under <see cref="NetworkChannel"/>. Android/Windows/Editor run the .NET SignalR client
    /// (<see cref="DotNetHubLink"/>); a browser build cannot (no threads, no .NET sockets), so it runs Microsoft's
    /// JavaScript SignalR client in the page through <c>Plugins/WebGL/SignalRBridge.jslib</c>
    /// (<see cref="WebGlHubLink"/>). Both speak the same JSON hub protocol, so the server cannot tell them apart.
    /// </summary>
    public interface IHubLink : IAsyncDisposable
    {
        bool IsConnected { get; }
        event Action<Exception> Closed;
        event Action Reconnecting;
        event Action Reconnected;

        void On<T>(string method, Action<T> handler);
        Task StartAsync(string url);
        Task SendCoreAsync(string method, object[] args);
        Task<T> InvokeCoreAsync<T>(string method, object[] args);
    }

    /// <summary>
    /// Fixed-arity overloads, NOT <c>params object[]</c>: a lone <c>string[]</c> argument (the skill bar, a
    /// trade offer) would otherwise be EXPANDED into the argument list by array covariance.
    /// </summary>
    public static class HubLinkCalls
    {
        public static Task SendAsync(this IHubLink l, string m) => l.SendCoreAsync(m, Array.Empty<object>());
        public static Task SendAsync(this IHubLink l, string m, object a) => l.SendCoreAsync(m, new[] { a });
        public static Task SendAsync(this IHubLink l, string m, object a, object b) => l.SendCoreAsync(m, new[] { a, b });
        public static Task SendAsync(this IHubLink l, string m, object a, object b, object c) =>
            l.SendCoreAsync(m, new[] { a, b, c });
        public static Task SendAsync(this IHubLink l, string m, object a, object b, object c, object d) =>
            l.SendCoreAsync(m, new[] { a, b, c, d });
        public static Task SendAsync(this IHubLink l, string m, object a, object b, object c, object d, object e) =>
            l.SendCoreAsync(m, new[] { a, b, c, d, e });
        public static Task SendAsync(this IHubLink l, string m, object a, object b, object c, object d, object e, object f) =>
            l.SendCoreAsync(m, new[] { a, b, c, d, e, f });

        public static Task<T> InvokeAsync<T>(this IHubLink l, string m) => l.InvokeCoreAsync<T>(m, Array.Empty<object>());
        public static Task<T> InvokeAsync<T>(this IHubLink l, string m, object a) => l.InvokeCoreAsync<T>(m, new[] { a });
        public static Task<T> InvokeAsync<T>(this IHubLink l, string m, object a, object b) =>
            l.InvokeCoreAsync<T>(m, new[] { a, b });

        public static IHubLink CreateForPlatform()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new WebGlHubLink();
#else
            return new DotNetHubLink();
#endif
        }
    }

    /// <summary>The .NET SignalR client — what every native build has always used, unchanged.</summary>
    public sealed class DotNetHubLink : IHubLink
    {
        private HubConnection _c;
        private readonly List<Action<HubConnection>> _pending = new List<Action<HubConnection>>();

        public bool IsConnected => _c?.State == HubConnectionState.Connected;
        public event Action<Exception> Closed;
        public event Action Reconnecting;
        public event Action Reconnected;

        public void On<T>(string method, Action<T> handler) => _pending.Add(c => c.On(method, handler));

        public Task StartAsync(string url)
        {
            _c = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect(new RideOutRestart())
                .Build();
            foreach (var register in _pending) register(_c);
            _c.Closed += ex => { Closed?.Invoke(ex); return Task.CompletedTask; };
            _c.Reconnecting += _ => { Reconnecting?.Invoke(); return Task.CompletedTask; };
            _c.Reconnected += _ => { Reconnected?.Invoke(); return Task.CompletedTask; };
            return _c.StartAsync();
        }

        public Task SendCoreAsync(string method, object[] args) => _c.SendCoreAsync(method, args);
        public Task<T> InvokeCoreAsync<T>(string method, object[] args) => _c.InvokeCoreAsync<T>(method, args);

        public async ValueTask DisposeAsync()
        {
            if (_c is not null) await _c.DisposeAsync();
        }

        /// <summary>
        /// How long a dropped link keeps trying (owner, 2026-10-14: *"a reboot … at 0 just saves + restarts
        /// and players reconnect"*). SignalR's default gives up after 0/2/10/30 s — 42 seconds in all, which
        /// a server restart on a small box can outlast, and then the player sat on a dead screen. This
        /// tries at once, then every 2 s for the first half-minute, then every 5 s, for three minutes:
        /// long enough to ride out a reboot, short enough that a server that is really gone ends in the
        /// login screen. A SHUTDOWN never gets here — it sends everyone out first (SignOutByServer).
        /// ⚠ <c>SignalRBridge.jslib</c> carries the same numbers for the browser — change both.
        /// </summary>
        private sealed class RideOutRestart : IRetryPolicy
        {
            public TimeSpan? NextRetryDelay(RetryContext context)
            {
                if (context.ElapsedTime > TimeSpan.FromMinutes(3)) return null;
                if (context.PreviousRetryCount == 0) return TimeSpan.Zero;
                return context.ElapsedTime < TimeSpan.FromSeconds(30)
                    ? TimeSpan.FromSeconds(2) : TimeSpan.FromSeconds(5);
            }
        }
    }

    /// <summary>
    /// The browser half. Every message crosses as JSON text: C# serialises the argument array, the page's
    /// SignalR client sends it; a push arrives as <c>(method, "[args…]")</c> and is decoded here with the
    /// options the .NET JSON hub protocol uses (camelCase, case-insensitive). Everything runs on the ONE
    /// browser thread, so the callbacks need no marshalling — consumers still go through
    /// <see cref="UnityMainThreadDispatcher"/>, which is harmless here.
    /// The class compiles on every platform so the Editor type-check covers it; only
    /// <see cref="HubLinkCalls.CreateForPlatform"/> picks it.
    /// 🔴 The <c>DllImport("__Internal")</c> externs exist ONLY in a WebGL player. Under IL2CPP an
    /// <c>__Internal</c> import is a STATIC link — Android's linker demands the symbol and failed the 0.272.0
    /// APK on it (it is not "only a declaration") — so every other platform gets throwing stubs instead.
    /// </summary>
    public sealed class WebGlHubLink : IHubLink
    {
        private delegate void MessageCb(IntPtr method, IntPtr argsJson);
        private delegate void ResultCb(int id, int ok, IntPtr text);
        private delegate void StateCb(int kind, IntPtr text);

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void HubLink_Init(MessageCb onMessage, ResultCb onResult, StateCb onState);
        [DllImport("__Internal")] private static extern void HubLink_Start(int id, string url, string methodsJson);
        [DllImport("__Internal")] private static extern void HubLink_Send(int id, string method, string argsJson);
        [DllImport("__Internal")] private static extern void HubLink_Invoke(int id, string method, string argsJson);
        [DllImport("__Internal")] private static extern void HubLink_Stop();
#else
        private static Exception NotWebGl() => new PlatformNotSupportedException("WebGlHubLink runs only in a WebGL player.");
        private static void HubLink_Init(MessageCb onMessage, ResultCb onResult, StateCb onState) => throw NotWebGl();
        private static void HubLink_Start(int id, string url, string methodsJson) => throw NotWebGl();
        private static void HubLink_Send(int id, string method, string argsJson) => throw NotWebGl();
        private static void HubLink_Invoke(int id, string method, string argsJson) => throw NotWebGl();
        private static void HubLink_Stop() => throw NotWebGl();
#endif

        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };

        // The JS side calls static functions, so the one live link is static too (a page has one connection).
        private static WebGlHubLink _live;
        private readonly Dictionary<string, Action<JsonElement[]>> _handlers =
            new Dictionary<string, Action<JsonElement[]>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, TaskCompletionSource<string>> _calls = new Dictionary<int, TaskCompletionSource<string>>();
        private int _nextId;
        private bool _connected;

        public bool IsConnected => _connected;
        public event Action<Exception> Closed;
        public event Action Reconnecting;
        public event Action Reconnected;

        public void On<T>(string method, Action<T> handler) =>
            _handlers[method] = args => handler(args.Length > 0 ? args[0].Deserialize<T>(Json) : default);

        public async Task StartAsync(string url)
        {
            _live = this;
            HubLink_Init(OnMessage, OnResult, OnState);
            var (id, task) = NewCall();
            HubLink_Start(id, url, JsonSerializer.Serialize(new List<string>(_handlers.Keys)));
            await task;
            _connected = true;
        }

        public Task SendCoreAsync(string method, object[] args)
        {
            var (id, task) = NewCall();
            HubLink_Send(id, method, JsonSerializer.Serialize(args, Json));
            return task;
        }

        public async Task<T> InvokeCoreAsync<T>(string method, object[] args)
        {
            var (id, task) = NewCall();
            HubLink_Invoke(id, method, JsonSerializer.Serialize(args, Json));
            string result = await task;
            return string.IsNullOrEmpty(result) ? default : JsonSerializer.Deserialize<T>(result, Json);
        }

        public ValueTask DisposeAsync()
        {
            _connected = false;
            HubLink_Stop();
            if (_live == this) _live = null;
            return default;
        }

        private (int, Task<string>) NewCall()
        {
            int id = ++_nextId;
            var tcs = new TaskCompletionSource<string>();
            _calls[id] = tcs;
            return (id, tcs.Task);
        }

        [MonoPInvokeCallback(typeof(MessageCb))]
        private static void OnMessage(IntPtr methodPtr, IntPtr argsPtr)
        {
            var self = _live;
            if (self == null) return;
            string method = Marshal.PtrToStringUTF8(methodPtr);
            if (!self._handlers.TryGetValue(method, out var handler)) return;
            try
            {
                using var doc = JsonDocument.Parse(Marshal.PtrToStringUTF8(argsPtr));
                var args = new List<JsonElement>();
                foreach (var a in doc.RootElement.EnumerateArray()) args.Add(a);
                handler(args.ToArray());
            }
            catch (Exception ex) { UnityEngine.Debug.LogError($"[hub] {method}: {ex}"); }
        }

        [MonoPInvokeCallback(typeof(ResultCb))]
        private static void OnResult(int id, int ok, IntPtr textPtr)
        {
            var self = _live;
            if (self == null || !self._calls.Remove(id, out var tcs)) return;
            string text = Marshal.PtrToStringUTF8(textPtr);
            if (ok != 0) tcs.SetResult(text);
            else tcs.SetException(new Exception(text));
        }

        /// <summary>kind: 0 = closed for good, 1 = reconnecting, 2 = reconnected.</summary>
        [MonoPInvokeCallback(typeof(StateCb))]
        private static void OnState(int kind, IntPtr textPtr)
        {
            var self = _live;
            if (self == null) return;
            switch (kind)
            {
                case 0:
                    self._connected = false;
                    string text = Marshal.PtrToStringUTF8(textPtr);
                    self.Closed?.Invoke(string.IsNullOrEmpty(text) ? null : new Exception(text));
                    break;
                case 1: self._connected = false; self.Reconnecting?.Invoke(); break;
                case 2: self._connected = true; self.Reconnected?.Invoke(); break;
            }
        }
    }
}
