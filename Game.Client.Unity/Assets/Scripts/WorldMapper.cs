using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Maps the server's 2D world (X/Y, ~24000×24000) onto Unity's ground plane. The server is
    /// pure 2D, so server-X → Unity-X and server-Y → Unity-Z (Unity's Y is UP, used only for the
    /// billboards' height and the camera). Nothing here changes when the camera later tilts to
    /// 2.5D — that's the whole point.
    ///
    /// **Server Y is NEGATED.** The server's coordinates are screen-style, like the WPF harness that
    /// defined them: origin top-left, Y increasing DOWNWARD. Unity's ground plane under a top-down
    /// camera has +Z going UP the screen, so mapping Y→Z directly mirrors the whole world vertically:
    /// the training ground rendered BELOW its dummies, and every map the owner knows from WPF was
    /// upside down. One sign here fixes it for entities, taps and the grid at once — which is exactly
    /// why every conversion goes through this class and nothing does the multiply inline.
    /// </summary>
    public static class WorldMapper
    {
        public const float Scale = 0.01f;   // 24000 server units → 240 Unity units

        public static Vector3 ToUnity(float serverX, float serverY) =>
            new Vector3(serverX * Scale, 0f, -serverY * Scale);

        public static Vector2 ToServer(Vector3 unity) =>
            new Vector2(unity.x / Scale, -unity.z / Scale);
    }
}
