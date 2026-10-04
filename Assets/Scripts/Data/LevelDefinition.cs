using System.Collections.Generic;
using UnityEngine;

namespace FPSGame
{
    /// <summary>An axis-aligned box (wall or obstacle) in world space.</summary>
    [System.Serializable]
    public struct BoxDef
    {
        public Vector3 center;
        public Vector3 size;

        public BoxDef(Vector3 center, Vector3 size)
        {
            this.center = center;
            this.size = size;
        }
    }

    public enum EnemyKind
    {
        Grunt,
        Boss
    }

    [System.Serializable]
    public struct EnemySpawnDef
    {
        public Vector3 position;
        public EnemyKind kind;

        public EnemySpawnDef(Vector3 position, EnemyKind kind)
        {
            this.position = position;
            this.kind = kind;
        }
    }

    /// <summary>
    /// Layout of one level: floor size, walls, obstacles, player spawn and enemy spawns.
    /// LevelBuilder turns this into geometry at runtime.
    /// </summary>
    [System.Serializable]
    public class LevelDefinition
    {
        public string displayName;
        public Vector2 floorSize;
        public Vector3 playerSpawn;
        public float playerYaw;
        [Tooltip("Finish within this many seconds for the best time rating.")]
        public float parTime;
        public List<BoxDef> walls = new List<BoxDef>();
        public List<BoxDef> obstacles = new List<BoxDef>();
        public List<EnemySpawnDef> enemies = new List<EnemySpawnDef>();

        public LevelDefinition(string displayName, float width, float depth, float parTime)
        {
            this.displayName = displayName;
            floorSize = new Vector2(width, depth);
            this.parTime = parTime;
        }

        public int EnemyCount => enemies.Count;

        public int BossCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i].kind == EnemyKind.Boss) count++;
                }
                return count;
            }
        }

        public void SetPlayerSpawn(float x, float z, float yaw)
        {
            playerSpawn = new Vector3(x, 0.1f, z);
            playerYaw = yaw;
        }

        /// <summary>Adds four walls around the floor edges.</summary>
        public void AddPerimeter(float height, float thickness = 1f)
        {
            float halfW = floorSize.x * 0.5f;
            float halfD = floorSize.y * 0.5f;
            float y = height * 0.5f;
            walls.Add(new BoxDef(new Vector3(0f, y, halfD + thickness * 0.5f), new Vector3(floorSize.x + thickness * 2f, height, thickness)));
            walls.Add(new BoxDef(new Vector3(0f, y, -halfD - thickness * 0.5f), new Vector3(floorSize.x + thickness * 2f, height, thickness)));
            walls.Add(new BoxDef(new Vector3(halfW + thickness * 0.5f, y, 0f), new Vector3(thickness, height, floorSize.y)));
            walls.Add(new BoxDef(new Vector3(-halfW - thickness * 0.5f, y, 0f), new Vector3(thickness, height, floorSize.y)));
        }

        public void AddWall(float x, float z, float sizeX, float sizeZ, float height)
        {
            walls.Add(new BoxDef(new Vector3(x, height * 0.5f, z), new Vector3(sizeX, height, sizeZ)));
        }

        public void AddObstacle(float x, float z, float sizeX, float sizeZ, float height)
        {
            obstacles.Add(new BoxDef(new Vector3(x, height * 0.5f, z), new Vector3(sizeX, height, sizeZ)));
        }

        public void AddEnemy(float x, float z, EnemyKind kind = EnemyKind.Grunt)
        {
            enemies.Add(new EnemySpawnDef(new Vector3(x, 0f, z), kind));
        }
    }
}
