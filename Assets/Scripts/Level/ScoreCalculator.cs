using UnityEngine;

namespace FPSGame
{
    /// <summary>
    /// Score and star rules.
    /// Time: within par = 2 points, within 2x par = 1 point.
    /// Health left: 70% or more = 2 points, 35% or more = 1 point.
    /// Stars: finishing gives 1, 2+ points give 2, 3+ points give 3.
    /// </summary>
    public static class ScoreCalculator
    {
        public static int CalculateStars(float time, float healthPercent, float parTime)
        {
            int points = 0;
            if (time <= parTime) points += 2;
            else if (time <= parTime * 2f) points += 1;

            if (healthPercent >= 0.7f) points += 2;
            else if (healthPercent >= 0.35f) points += 1;

            if (points >= 3) return 3;
            if (points >= 2) return 2;
            return 1;
        }

        public static int TimeBonus(float time, float parTime)
        {
            return Mathf.Max(0, Mathf.RoundToInt((parTime * 2f - time) * 10f));
        }

        public static int HealthBonus(float healthPercent)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(healthPercent) * 500f);
        }
    }
}
