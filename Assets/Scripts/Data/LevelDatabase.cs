using UnityEngine;

namespace FPSGame
{
    /// <summary>
    /// Data for the three levels, written as C# so nothing has to be set up in the Editor.
    /// Level 1: 5 enemies, small map. Level 2: 7 enemies with obstacles.
    /// Level 3: 7 enemies plus a boss in a double-sized arena.
    /// </summary>
    public static class LevelDatabase
    {
        public const int Count = 3;

        private static LevelDefinition[] levels;

        public static LevelDefinition Get(int index)
        {
            if (levels == null)
            {
                levels = new[] { CreateLevel1(), CreateLevel2(), CreateLevel3() };
            }
            return levels[Mathf.Clamp(index, 0, Count - 1)];
        }

        private static LevelDefinition CreateLevel1()
        {
            var level = new LevelDefinition("Training Yard", 30f, 30f, 90f);
            // Arena 1 has a raised, solid central combat plate. Spawn on that plate instead
            // of the decorative lower ring that appears open from the first-person camera.
            level.SetPlayerSpawn(0f, 0f, 0f);
            level.AddPerimeter(3f);

            level.AddObstacle(-5f, 0f, 2f, 2f, 2f);
            level.AddObstacle(5f, 3f, 2f, 2f, 2f);
            level.AddObstacle(0f, 6f, 4f, 1f, 2f);
            level.AddObstacle(-8f, 8f, 3f, 1f, 2f);
            level.AddObstacle(8f, -6f, 1.5f, 1.5f, 1.5f);

            level.AddEnemy(-10f, 10f);
            level.AddEnemy(10f, 10f);
            level.AddEnemy(0f, 11f);
            level.AddEnemy(-11f, 0f);
            level.AddEnemy(11f, -2f);
            return level;
        }

        private static LevelDefinition CreateLevel2()
        {
            var level = new LevelDefinition("Warehouse", 50f, 50f, 180f);
            level.SetPlayerSpawn(0f, -20f, 0f);
            level.AddPerimeter(3f);

            // Inner walls
            level.AddWall(-10f, 0f, 1f, 20f, 3f);
            level.AddWall(10f, 5f, 1f, 20f, 3f);
            level.AddWall(0f, 12f, 12f, 1f, 3f);
            level.AddWall(0f, -8f, 10f, 1f, 3f);

            // Crates
            level.AddObstacle(-18f, -15f, 2f, 2f, 2f);
            level.AddObstacle(18f, -12f, 2f, 2f, 2f);
            level.AddObstacle(-5f, 3f, 1.5f, 1.5f, 1.5f);
            level.AddObstacle(4f, 0f, 1.5f, 1.5f, 1.5f);
            level.AddObstacle(-18f, 15f, 3f, 3f, 2f);
            level.AddObstacle(18f, 18f, 2f, 4f, 2f);

            // Pillars
            level.AddObstacle(-4f, -15f, 1f, 1f, 4f);
            level.AddObstacle(4f, -15f, 1f, 1f, 4f);
            level.AddObstacle(15f, -20f, 1f, 1f, 4f);

            level.AddEnemy(-20f, 0f);
            level.AddEnemy(-15f, 10f);
            level.AddEnemy(-20f, 20f);
            level.AddEnemy(0f, 5f);
            level.AddEnemy(5f, 18f);
            level.AddEnemy(20f, 0f);
            level.AddEnemy(15f, -10f);
            return level;
        }

        private static LevelDefinition CreateLevel3()
        {
            var level = new LevelDefinition("Fortress", 70f, 70f, 300f);
            level.SetPlayerSpawn(0f, -30f, 0f);
            level.AddPerimeter(4f);

            // Boss arena (north), entered through a gap at x = -3..3, z = 5
            level.AddWall(-12f, 15f, 1f, 20f, 4f);
            level.AddWall(12f, 15f, 1f, 20f, 4f);
            level.AddWall(-7.5f, 5f, 9f, 1f, 4f);
            level.AddWall(7.5f, 5f, 9f, 1f, 4f);
            level.AddWall(0f, 25f, 25f, 1f, 4f);

            // Outer cover walls
            level.AddWall(-25f, -10f, 1f, 20f, 3f);
            level.AddWall(25f, -10f, 1f, 20f, 3f);
            level.AddWall(0f, -15f, 16f, 1f, 3f);
            level.AddWall(-18f, -25f, 10f, 1f, 3f);
            level.AddWall(18f, -25f, 10f, 1f, 3f);

            // Crates and pillars
            level.AddObstacle(-30f, -30f, 2f, 2f, 2f);
            level.AddObstacle(30f, -30f, 2f, 2f, 2f);
            level.AddObstacle(-5f, -5f, 2f, 2f, 2f);
            level.AddObstacle(5f, -8f, 2f, 2f, 2f);
            level.AddObstacle(-20f, 10f, 3f, 3f, 2f);
            level.AddObstacle(20f, 10f, 3f, 3f, 2f);
            level.AddObstacle(-28f, 25f, 2f, 6f, 2f);
            level.AddObstacle(28f, 25f, 2f, 6f, 2f);
            level.AddObstacle(0f, -25f, 4f, 1f, 1.5f);
            level.AddObstacle(-5f, 12f, 1.2f, 1.2f, 4f);
            level.AddObstacle(5f, 12f, 1.2f, 1.2f, 4f);
            level.AddObstacle(-30f, 0f, 1.5f, 1.5f, 4f);
            level.AddObstacle(30f, 0f, 1.5f, 1.5f, 4f);

            level.AddEnemy(-30f, -15f);
            level.AddEnemy(30f, -15f);
            level.AddEnemy(-15f, -5f);
            level.AddEnemy(15f, -5f);
            level.AddEnemy(0f, -10f);
            level.AddEnemy(-30f, 10f);
            level.AddEnemy(30f, 10f);
            level.AddEnemy(0f, 20f, EnemyKind.Boss);
            return level;
        }
    }
}
