using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

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
        // Reduced to one third of the previous revision while preserving authored layout ratios.
        public const float ArenaZoom = 0.5f;

        public float ArenaDiameter { get; private set; }
        private float layoutScale = ArenaZoom;

        public Transform GeometryRoot { get; private set; }

        private readonly System.Collections.Generic.List<Collider> arenaColliders =
            new System.Collections.Generic.List<Collider>();

        public void Build(LevelDefinition definition, GameAssets assets)
        {
            if (GeometryRoot != null) Destroy(GeometryRoot.gameObject);

            GeometryRoot = new GameObject("Geometry").transform;
            GeometryRoot.SetParent(transform, false);
            SetupGloomyAtmosphere();

            int levelIndex = LevelManager.Instance != null ? LevelManager.Instance.LevelIndex : 0;
            float levelScaleMultiplier = levelIndex == 2 ? 2f : 1f;
            layoutScale = ArenaZoom * levelScaleMultiplier;
            // Level 1 intentionally reuses Arena 3's cleaner combat deck. Its final diameter
            // is 75% of Level 3 (90m versus 120m), while the authored Level 1 gameplay layout
            // remains unchanged.
            float arenaDiameter = levelIndex == 0
                ? 90f
                : (levelIndex == 1 ? 90f : 120f) * layoutScale;
            ArenaDiameter = arenaDiameter;
            BuildArena(levelIndex, arenaDiameter);
            BuildNavigationFloor(arenaDiameter);
            BuildObstacleProps(definition, levelIndex);
        }

        private static void SetupGloomyAtmosphere()
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader != null)
            {
                Material sky = new Material(shader) { name = "Runtime_GloomyArenaSky" };
                sky.SetFloat("_SunDisk", 1f);
                sky.SetFloat("_SunSize", 0.015f);
                sky.SetFloat("_SunSizeConvergence", 8f);
                sky.SetFloat("_AtmosphereThickness", 1.65f);
                sky.SetColor("_SkyTint", new Color(0.12f, 0.14f, 0.18f));
                sky.SetColor("_GroundColor", new Color(0.035f, 0.04f, 0.055f));
                sky.SetFloat("_Exposure", 0.55f);
                RenderSettings.skybox = sky;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.20f, 0.24f);
            RenderSettings.ambientEquatorColor = new Color(0.09f, 0.10f, 0.13f);
            RenderSettings.ambientGroundColor = new Color(0.025f, 0.028f, 0.035f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.10f, 0.115f, 0.14f);
            RenderSettings.fogDensity = 0.00045f;
        }

        private void BuildArena(int levelIndex, float targetDiameter)
        {
            int arenaModelIndex = levelIndex == 0 ? 2 : levelIndex;
            GameObject prefab = Resources.Load<GameObject>("ArenaKit/Arena" + (arenaModelIndex + 1));
            if (prefab == null)
            {
                Debug.LogError("[LevelBuilder] Meshy arena prefab is missing. Run Tools > Graphics > Integrate Meshy Arenas.");
                return;
            }

            GameObject arena = Instantiate(prefab, GeometryRoot);
            arena.name = "MeshyArena_Level" + (levelIndex + 1) + "_Arena" + (arenaModelIndex + 1);
            NormalizeArena(arena.transform, targetDiameter, arenaModelIndex);
            arenaColliders.Clear();
            foreach (Collider collider in arena.GetComponentsInChildren<Collider>(true))
            {
                // Dense Meshy collision is enabled after baking. Feeding the high-poly mesh
                // to NavMeshSurface can stall the Editor for minutes.
                collider.enabled = false;
                arenaColliders.Add(collider);
            }

            // Add accurate player collision for Meshy render meshes that did not import one.
            // It is enabled after the lightweight proxy NavMesh has been baked.
            foreach (MeshFilter filter in arena.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.GetComponent<Collider>() != null) continue;
                // AddComponent automatically copies the sibling MeshFilter mesh and cooks it
                // immediately with Fast Midphase. Detach it briefly so reliable options are in
                // place before the two-million-triangle arena mesh is assigned.
                Mesh sourceMesh = filter.sharedMesh;
                filter.sharedMesh = null;
                MeshCollider meshCollider = filter.gameObject.AddComponent<MeshCollider>();
                meshCollider.convex = false;
                // The imported arena exceeds two million triangles. Unity's Fast Midphase
                // can miss collisions on meshes this large, which allowed agents/players to
                // pass through visible structures. Use the reliable cooking path instead.
                meshCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation |
                                              MeshColliderCookingOptions.EnableMeshCleaning |
                                              MeshColliderCookingOptions.WeldColocatedVertices;
                meshCollider.sharedMesh = sourceMesh;
                filter.sharedMesh = sourceMesh;
                meshCollider.enabled = false;
                arenaColliders.Add(meshCollider);
            }
        }

        public void EnableArenaCollisions()
        {
            foreach (Collider collider in arenaColliders)
                if (collider != null) collider.enabled = true;
            Physics.SyncTransforms();
        }

        private void BuildNavigationFloor(float diameter)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "NavigationFloor";
            floor.transform.SetParent(GeometryRoot, false);
            floor.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(diameter, 0.2f, diameter);
            Renderer renderer = floor.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;

            // Meshy meshes include a wide decorative outer shell. Keep actors inside the
            // actual combat deck instead of letting them walk behind the visible arena wall.
            // The normalized arena diameter describes the complete visible model, so the
            // authored outer wall lies close to radius = diameter / 2. Keep these emergency
            // limits just inside that wall instead of cutting the playable floor in half.
            float half = diameter * 0.48f;
            CreateInvisibleBoundary("Boundary_North", new Vector3(0f, 2.5f, half), new Vector3(diameter, 5f, 1f));
            CreateInvisibleBoundary("Boundary_South", new Vector3(0f, 2.5f, -half), new Vector3(diameter, 5f, 1f));
            CreateInvisibleBoundary("Boundary_East", new Vector3(half, 2.5f, 0f), new Vector3(1f, 5f, diameter));
            CreateInvisibleBoundary("Boundary_West", new Vector3(-half, 2.5f, 0f), new Vector3(1f, 5f, diameter));
            // The clean central deck is the combat area. This proxy ring participates in the
            // NavMesh bake and keeps agents away from dense decorative rocks/walls.
            // This circular proxy is only an AI/player failsafe around the outer perimeter;
            // the dense MeshCollider still provides the exact collision against visible walls.
            CreateCircularArenaBoundary(diameter * 0.455f);
        }

        private void CreateCircularArenaBoundary(float radius)
        {
            const int segmentCount = 24;
            float segmentLength = 2f * radius * Mathf.Tan(Mathf.PI / segmentCount) + 0.25f;
            for (int i = 0; i < segmentCount; i++)
            {
                float angle = i * Mathf.PI * 2f / segmentCount;
                Vector3 position = new Vector3(Mathf.Sin(angle) * radius, 2.5f, Mathf.Cos(angle) * radius);
                GameObject wall = new GameObject("ArenaCollision_" + i);
                wall.transform.SetParent(GeometryRoot, false);
                wall.transform.localPosition = position;
                wall.transform.localRotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
                BoxCollider collider = wall.AddComponent<BoxCollider>();
                collider.size = new Vector3(segmentLength, 5f, 0.8f);
            }
        }

        private void CreateInvisibleBoundary(string objectName, Vector3 position, Vector3 scale)
        {
            GameObject boundary = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boundary.name = objectName;
            boundary.transform.SetParent(GeometryRoot, false);
            boundary.transform.localPosition = position;
            boundary.transform.localScale = scale;
            Renderer renderer = boundary.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
        }

        private void BuildObstacleProps(LevelDefinition definition, int levelIndex)
        {
            GameObject barricade = Resources.Load<GameObject>("ArenaKit/Barricade");
            GameObject reactor = Resources.Load<GameObject>("ArenaKit/Reactor");

            for (int i = 0; i < definition.obstacles.Count; i++)
            {
                BoxDef box = definition.obstacles[i];
                bool useReactor = reactor != null && i % (levelIndex == 2 ? 4 : 5) == 0;
                GameObject prefab = useReactor ? reactor : barricade;
                if (prefab == null) continue;

                GameObject prop = Instantiate(prefab, GeometryRoot);
                prop.name = (useReactor ? "Reactor_" : "Barricade_") + i;
                Vector3 authoredPosition = new Vector3(box.center.x, 0f, box.center.z) * layoutScale;
                prop.transform.localPosition = ClampToPlayableArea(authoredPosition, 0.8f);
                prop.transform.localRotation = Quaternion.Euler(0f, (i * 47f + levelIndex * 23f) % 180f, 0f);
                float propScale = 0.36f;
                float targetWidth = useReactor ? Mathf.Max(3.2f, box.size.y * 1.5f) : Mathf.Max(3.5f, box.size.x * 1.6f);
                NormalizeProp(prop.transform, targetWidth * propScale);
            }
        }

        public Vector3 ScaleAndClampToArena(Vector3 authoredPosition, float edgePadding)
        {
            return ClampToPlayableArea(authoredPosition * layoutScale, edgePadding);
        }

        private Vector3 ClampToPlayableArea(Vector3 position, float edgePadding)
        {
            // Meshy arenas are circular. Keep gameplay objects away from the decorative
            // outer wall even though the hidden navigation floor is square.
            float safeRadius = Mathf.Max(1f, ArenaDiameter * 0.44f - edgePadding);
            Vector2 flat = new Vector2(position.x, position.z);
            if (flat.sqrMagnitude > safeRadius * safeRadius)
            {
                flat = flat.normalized * safeRadius;
                position.x = flat.x;
                position.z = flat.y;
            }
            return position;
        }

        private static void NormalizeArena(Transform root, float targetDiameter, int levelIndex)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            float scale = targetDiameter / Mathf.Max(bounds.size.x, bounds.size.z);
            root.localScale = Vector3.one * scale;

            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);

            // Measured from the dense, flat central vertices of each Meshy mesh. Do not use
            // the full renderer height here: it includes tall walls and a deep decorative
            // underside, neither of which is the playable floor.
            // Arena 1's combat deck is close to the top of its source bounds. The previous
            // 0.15 ratio aligned the decorative underside to y=0 and left the real floor
            // 2.64 metres above the player. Arena 2/3 were measured correctly already.
            float[] deckHeightRatios = { 0.80f, 0.30f, 0.24f };
            float deckRatio = deckHeightRatios[Mathf.Clamp(levelIndex, 0, deckHeightRatios.Length - 1)];
            float deckWorldY = bounds.min.y + bounds.size.y * deckRatio;
            root.position -= new Vector3(bounds.center.x, deckWorldY, bounds.center.z);

            Debug.Log($"[LevelBuilder] Arena {levelIndex + 1} aligned: size={bounds.size}, deckOffset={bounds.size.y * deckRatio:F2}m");
        }

        private static void NormalizeProp(Transform root, float targetWidth)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            float scale = targetWidth / Mathf.Max(bounds.size.x, bounds.size.z);
            root.localScale = Vector3.one * scale;

            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            root.position += Vector3.up * (-bounds.min.y);

            // Imported props often contain a collider authored for the unscaled source model.
            // Replace it with one calculated from the final visible bounds so the controller
            // cannot enter the metal barricades/reactors.
            foreach (Collider oldCollider in root.GetComponentsInChildren<Collider>(true))
                oldCollider.enabled = false;

            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            BoxCollider proxy = root.GetComponent<BoxCollider>();
            if (proxy == null) proxy = root.gameObject.AddComponent<BoxCollider>();
            proxy.center = root.InverseTransformPoint(bounds.center);
            Vector3 scaleAbs = root.lossyScale;
            scaleAbs = new Vector3(Mathf.Abs(scaleAbs.x), Mathf.Abs(scaleAbs.y), Mathf.Abs(scaleAbs.z));
            proxy.size = new Vector3(
                bounds.size.x / Mathf.Max(0.0001f, scaleAbs.x),
                bounds.size.y / Mathf.Max(0.0001f, scaleAbs.y),
                bounds.size.z / Mathf.Max(0.0001f, scaleAbs.z));
            proxy.enabled = true;
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
