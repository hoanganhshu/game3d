using UnityEngine;

namespace FPSGame
{
    /// <summary>
    /// World-space health bar made of two thin cubes that always faces the camera.
    /// Cheaper than one world-space canvas per enemy.
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Transform fill;

        private float fullWidth = 1f;
        private Vector3 fillBasePosition;
        private Transform cameraTransform;

        private void Awake()
        {
            if (fill != null)
            {
                fullWidth = fill.localScale.x;
                fillBasePosition = fill.localPosition;
            }
        }

        public void SetValue(float normalized)
        {
            if (fill == null) return;

            normalized = Mathf.Clamp01(normalized);
            Vector3 scale = fill.localScale;
            scale.x = Mathf.Max(0.0001f, fullWidth * normalized);
            fill.localScale = scale;

            Vector3 position = fillBasePosition;
            position.x -= fullWidth * (1f - normalized) * 0.5f;
            fill.localPosition = position;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                Camera main = Camera.main;
                if (main == null) return;
                cameraTransform = main.transform;
            }

            transform.rotation = cameraTransform.rotation;
        }
    }
}
