// Quit in the browser (owner, 2026-11-06): Application.Quit does nothing on a web page, so the Quit button leaves for the
// game's main page instead. The player quit, they did not log out — the site is where they came from. GameUi.World.cs
// calls this after the graceful logout.
var PageNav = {
  PageNav_GoHome: function () {
    window.location.href = "/";
  }
};

mergeInto(LibraryManager.library, PageNav);
