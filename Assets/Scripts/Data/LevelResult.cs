namespace FPSGame
{
    /// <summary>Summary of a finished level, sent to the Win screen.</summary>
    public struct LevelResult
    {
        public int levelIndex;
        public int score;
        public int stars;
        public float time;
        public float healthPercent;
        public int kills;
        public bool isNewBest;
        public int bestScore;
        public bool hasNextLevel;
    }
}
