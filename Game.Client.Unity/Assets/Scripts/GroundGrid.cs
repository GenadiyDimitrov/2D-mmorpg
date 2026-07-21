using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// A wireframe grid over the server's world footprint.
    ///
    /// It is not decoration: with a flat untextured ground and a camera that follows the player,
    /// walking looks EXACTLY like standing still — the player billboard stays centred and nothing
    /// else in frame changes. The grid gives the eye something stationary to move against, which is
    /// the difference between "the client is frozen" and "I'm walking north".
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GroundGrid : MonoBehaviour
    {
        [Tooltip("Grid spacing in SERVER units (1000 = a comfortable landmark spacing).")]
        public float ServerSpacing = 1000f;

        [Tooltip("Server world size (square).")]
        public float ServerWorldSize = 24000f;

        public Color LineColor = new Color(0.32f, 0.36f, 0.42f, 1f);

        private void Start()
        {
            float step = ServerSpacing * WorldMapper.Scale;
            float size = ServerWorldSize * WorldMapper.Scale;
            int lines = Mathf.Max(1, Mathf.RoundToInt(size / step));

            var verts = new Vector3[(lines + 1) * 4];
            var indices = new int[(lines + 1) * 4];
            int v = 0;

            // Z is NEGATIVE across the whole grid: server Y is a screen-style axis that grows
            // DOWNWARD, so WorldMapper maps it to -Z (see that class). A grid drawn over +Z would sit
            // in the empty half of the scene while every entity stood in the other one.
            for (int i = 0; i <= lines; i++)
            {
                float p = i * step;
                verts[v] = new Vector3(p, 0f, 0f);     indices[v] = v; v++;
                verts[v] = new Vector3(p, 0f, -size);  indices[v] = v; v++;
                verts[v] = new Vector3(0f, 0f, -p);    indices[v] = v; v++;
                verts[v] = new Vector3(size, 0f, -p);  indices[v] = v; v++;
            }

            var mesh = new Mesh { name = "GroundGrid" };
            // The world is 240 Unity units across at 0.01 scale — well under the 16-bit index limit,
            // but set it explicitly so raising ServerWorldSize later can't silently truncate.
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();

            GetComponent<MeshFilter>().mesh = mesh;

            var renderer = GetComponent<MeshRenderer>();
            var material = UnlitMaterials.Create(LineColor);
            if (material != null) renderer.material = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // Just above y=0 so it doesn't z-fight with the scene's ground plane.
            transform.position = new Vector3(0f, 0.02f, 0f);
        }
    }
}
