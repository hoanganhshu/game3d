using UnityEngine;

namespace FPSGame
{
    /// <summary>
    /// Builds the floor, walls and obstacles of a LevelDefinition out of cubes. Everything goes
    /// under this object, which also holds the NavMeshSurface (it collects children only), so
    /// the player and enemies are not baked into the NavMesh.
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        private const float FloorTileSize = 4f;

        public Transform GeometryRoot { get; private set; }

        public void Build(LevelDefinition definition, GameAssets assets)
        {
            if (GeometryRoot != null) Destroy(GeometryRoot.gameObject);

            GeometryRoot = new GameObject("Geometry").transform;
            GeometryRoot.SetParent(transform, false);

            Vector3 floorSize = new Vector3(definition.floorSize.x, 1f, definition.floorSize.y);
            GameObject floor = CreateBox("Floor", new BoxDef(new Vector3(0f, -0.5f, 0f), floorSize), assets.floorMaterial);
            ApplyFloorTiling(floor.GetComponent<Renderer>(), definition.floorSize);

            for (int i = 0; i < definition.walls.Count; i++)
            {
                CreateBox("Wall_" + i, definition.walls[i], assets.wallMaterial);
            }

            for (int i = 0; i < definition.obstacles.Count; i++)
            {
                CreateBox("Obstacle_" + i, definition.obstacles[i], assets.obstacleMaterial);
            }
        }

        private GameObject CreateBox(string objectName, BoxDef box, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = objectName;
            go.transform.SetParent(GeometryRoot, false);
            go.transform.localPosition = box.center;
            go.transform.localScale = box.size;

            Renderer boxRenderer = go.GetComponent<Renderer>();
            if (material != null) boxRenderer.sharedMaterial = material;
            return go;
        }

        /// <summary>Repeats the checker texture over the floor without making a new material.</summary>
        private static void ApplyFloorTiling(Renderer floorRenderer, Vector2 size)
        {
            if (floorRenderer == null) return;
            var block = new MaterialPropertyBlock();
            floorRenderer.GetPropertyBlock(block);
            block.SetVector("_MainTex_ST", new Vector4(size.x / FloorTileSize, size.y / FloorTileSize, 0f, 0f));
            floorRenderer.SetPropertyBlock(block);
        }
    }
}
