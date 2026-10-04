namespace FPSGame
{
    /// <summary>
    /// Scene names shared by runtime code and the editor bootstrapper. The order in
    /// <see cref="All"/> is the order used in Build Settings.
    /// </summary>
    public static class SceneNames
    {
        public const string Boot = "Boot";
        public const string MainMenu = "MainMenu";
        public const string LevelSelect = "LevelSelect";
        public const string Level1 = "Level1";
        public const string Level2 = "Level2";
        public const string Level3 = "Level3";

        public static readonly string[] Levels = { Level1, Level2, Level3 };

        public static readonly string[] All = { Boot, MainMenu, LevelSelect, Level1, Level2, Level3 };

        public static string GetLevel(int index)
        {
            if (index < 0) index = 0;
            if (index >= Levels.Length) index = Levels.Length - 1;
            return Levels[index];
        }
    }
}
