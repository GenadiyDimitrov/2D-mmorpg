// Right-drag camera turn in the browser (owner, 2026-11-05: "the mouse is moving just invisible .. when i release the
// right click it just appear on other monitors ... its not infinite rotation"). CameraRig.cs calls these.
//
// A browser grants pointer lock ONLY inside the input event that asked for it. Unity's Cursor.lockState asks one event
// late (on the release), so the cursor was merely hidden: it kept travelling, left the canvas, and the turn stopped at
// the screen edge. So the lock is requested HERE, inside the right-button mousedown, and the turn reads the raw
// movementX/Y the browser reports while locked. When the lock ends, the browser puts the cursor back where it was.
var PointerLock = {
  PointerLock_Init: function () {
    if (Module.pointerLock) return;
    var canvas = Module.canvas;
    var pl = Module.pointerLock = { allowed: 0, dx: 0, dy: 0 };
    canvas.addEventListener("mousedown", function (e) {
      if (e.button === 2 && pl.allowed && document.pointerLockElement !== canvas) {
        pl.dx = 0; pl.dy = 0;
        try {
          var p = canvas.requestPointerLock();
          if (p && p.catch) p.catch(function () {});   // refused (e.g. too soon after Esc): the turn falls back
        } catch (err) {}
      }
    }, true);
    document.addEventListener("mousemove", function (e) {
      if (document.pointerLockElement === canvas) { pl.dx += e.movementX || 0; pl.dy += e.movementY || 0; }
    }, true);
  },

  // C# says, every frame, whether a right press at the cursor would turn the camera (not over the HUD).
  PointerLock_Allow: function (on) {
    if (Module.pointerLock) Module.pointerLock.allowed = on;
  },

  PointerLock_IsLocked: function () {
    return document.pointerLockElement === Module.canvas ? 1 : 0;
  },

  // The movement since the last call, in DEVICE pixels (+y = down), and reset. The browser reports CSS pixels;
  // Unity's Screen.width is the canvas in device pixels, so scaling by devicePixelRatio keeps the turn speed the same
  // as the Windows build's.
  PointerLock_TakeX: function () {
    var pl = Module.pointerLock; if (!pl) return 0;
    var v = pl.dx; pl.dx = 0; return v * (window.devicePixelRatio || 1);
  },
  PointerLock_TakeY: function () {
    var pl = Module.pointerLock; if (!pl) return 0;
    var v = pl.dy; pl.dy = 0; return v * (window.devicePixelRatio || 1);
  },

  PointerLock_Release: function () {
    if (document.pointerLockElement) document.exitPointerLock();
  }
};

mergeInto(LibraryManager.library, PointerLock);
