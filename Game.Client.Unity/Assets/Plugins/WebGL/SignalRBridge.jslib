// BL-363 — the browser build's wire. C# (Scripts/Net/HubLink.cs, WebGlHubLink) calls these; they drive
// Microsoft's JavaScript SignalR client, which the page loads as the global `signalR` (WebGLTemplates/Vardelar).
// Arguments and pushes cross as JSON text, so the C# side decodes them exactly as the .NET client would.
var SignalRBridge = {
  HubLink_Init: function (onMessage, onResult, onState) {
    function str(s) { return stringToNewUTF8(s == null ? "" : String(s)); }
    Module.hubLink = {
      conn: null,
      message: function (method, argsJson) {
        var a = str(method), b = str(argsJson);
        {{{ makeDynCall('vii', 'onMessage') }}}(a, b);
        _free(a); _free(b);
      },
      result: function (id, ok, text) {
        var t = str(text);
        {{{ makeDynCall('viii', 'onResult') }}}(id, ok ? 1 : 0, t);
        _free(t);
      },
      state: function (kind, text) {
        var t = str(text);
        {{{ makeDynCall('vii', 'onState') }}}(kind, t);
        _free(t);
      },
      errText: function (e) { return (e && e.message) ? e.message : String(e); }
    };
  },

  HubLink_Start: function (id, urlPtr, methodsPtr) {
    var h = Module.hubLink, url = UTF8ToString(urlPtr), methods = JSON.parse(UTF8ToString(methodsPtr));
    if (typeof signalR === "undefined") { h.result(id, false, "signalr.min.js is not loaded by the page"); return; }
    var conn = new signalR.HubConnectionBuilder()
      .withUrl(url)
      // Same numbers as HubLink.cs RideOutRestart: at once, every 2s for 30s, then every 5s, give up at 3 min.
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: function (ctx) {
          if (ctx.elapsedMilliseconds > 180000) return null;
          if (ctx.previousRetryCount === 0) return 0;
          return ctx.elapsedMilliseconds < 30000 ? 2000 : 5000;
        }
      })
      .configureLogging(signalR.LogLevel.Warning)
      .build();
    // Every push C# listens for is handed back by name with its arguments as one JSON array.
    methods.forEach(function (name) {
      conn.on(name, function () { h.message(name, JSON.stringify(Array.prototype.slice.call(arguments))); });
    });
    conn.onclose(function (e) { h.state(0, e ? h.errText(e) : ""); });
    conn.onreconnecting(function () { h.state(1, ""); });
    conn.onreconnected(function () { h.state(2, ""); });
    h.conn = conn;
    conn.start().then(function () { h.result(id, true, ""); },
                      function (e) { h.result(id, false, h.errText(e)); });
  },

  HubLink_Send: function (id, methodPtr, argsPtr) {
    var h = Module.hubLink, args = JSON.parse(UTF8ToString(argsPtr));
    var c = h.conn;
    if (!c) { h.result(id, false, "not connected"); return; }
    c.send.apply(c, [UTF8ToString(methodPtr)].concat(args))
      .then(function () { h.result(id, true, ""); }, function (e) { h.result(id, false, h.errText(e)); });
  },

  HubLink_Invoke: function (id, methodPtr, argsPtr) {
    var h = Module.hubLink, args = JSON.parse(UTF8ToString(argsPtr));
    var c = h.conn;
    if (!c) { h.result(id, false, "not connected"); return; }
    c.invoke.apply(c, [UTF8ToString(methodPtr)].concat(args))
      .then(function (r) { h.result(id, true, r === undefined ? "null" : JSON.stringify(r)); },
            function (e) { h.result(id, false, h.errText(e)); });
  },

  HubLink_Stop: function () {
    var h = Module.hubLink;
    if (h && h.conn) { h.conn.stop(); h.conn = null; }
  }
};

mergeInto(LibraryManager.library, SignalRBridge);
