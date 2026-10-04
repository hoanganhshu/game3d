using System;
using UnityEngine;

namespace FPSGame
{
    /// <summary>
    /// Gameplay events. UI subscribes here and never talks to gameplay components directly.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<float, float> PlayerHealthChanged;
        public static event Action PlayerDamaged;
        public static event Action PlayerDied;
        public static event Action<Weapon> AmmoChanged;
        public static event Action<bool, float> ReloadStateChanged;
        public static event Action<Enemy> EnemyKilled;
        public static event Action<int, int> EnemiesRemainingChanged;
        public static event Action<int> ScoreChanged;
        public static event Action<bool> HitConfirmed;
        public static event Action<LevelResult> LevelCompleted;
        public static event Action LevelFailed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            PlayerHealthChanged = null;
            PlayerDamaged = null;
            PlayerDied = null;
            AmmoChanged = null;
            ReloadStateChanged = null;
            EnemyKilled = null;
            EnemiesRemainingChanged = null;
            ScoreChanged = null;
            HitConfirmed = null;
            LevelCompleted = null;
            LevelFailed = null;
        }

        public static void RaisePlayerHealthChanged(float current, float max) => PlayerHealthChanged?.Invoke(current, max);
        public static void RaisePlayerDamaged() => PlayerDamaged?.Invoke();
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaiseAmmoChanged(Weapon weapon) => AmmoChanged?.Invoke(weapon);
        public static void RaiseReloadStateChanged(bool reloading, float duration) => ReloadStateChanged?.Invoke(reloading, duration);
        public static void RaiseEnemyKilled(Enemy enemy) => EnemyKilled?.Invoke(enemy);
        public static void RaiseEnemiesRemainingChanged(int remaining, int total) => EnemiesRemainingChanged?.Invoke(remaining, total);
        public static void RaiseScoreChanged(int score) => ScoreChanged?.Invoke(score);
        public static void RaiseHitConfirmed(bool killed) => HitConfirmed?.Invoke(killed);
        public static void RaiseLevelCompleted(LevelResult result) => LevelCompleted?.Invoke(result);
        public static void RaiseLevelFailed() => LevelFailed?.Invoke();
    }
}
