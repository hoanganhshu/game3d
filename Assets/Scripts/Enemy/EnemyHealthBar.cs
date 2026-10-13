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
        private Renderer fillRenderer;
        private Renderer backgroundRenderer;
        private Renderer[] barRenderers;
        private MaterialPropertyBlock fillBlock;
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            // Keep the world-space bar compact even when an imported enemy is scaled up.
            transform.localScale = Vector3.Scale(transform.localScale, new Vector3(0.25f, 0.25f, 0.25f));
            if (fill != null)
            {
                fullWidth = fill.localScale.x;
                fillBasePosition = fill.localPosition;
                fillRenderer = fill.GetComponent<Renderer>();
            }
            Transform background = transform.Find("Background");
            if (background != null) backgroundRenderer = background.GetComponent<Renderer>();
            fillBlock = new MaterialPropertyBlock();
            if (backgroundRenderer != null)
            {
                var block = new MaterialPropertyBlock();
                backgroundRenderer.GetPropertyBlock(block);
                block.SetColor(ColorId, new Color(0.015f, 0.018f, 0.025f, 1f));
                backgroundRenderer.SetPropertyBlock(block);
                background.localScale = new Vector3(background.localScale.x * 1.06f, background.localScale.y * 1.2f, background.localScale.z);
            }
            barRenderers = GetComponentsInChildren<Renderer>(true);
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

            if (fillRenderer != null)
            {
                Color low = new Color(1f, 0.08f, 0.05f, 1f);
                Color mid = new Color(1f, 0.62f, 0.05f, 1f);
                Color high = new Color(0.15f, 1f, 0.35f, 1f);
                Color color = normalized > 0.5f
                    ? Color.Lerp(mid, high, (normalized - 0.5f) * 2f)
                    : Color.Lerp(low, mid, normalized * 2f);
                fillRenderer.GetPropertyBlock(fillBlock);
                fillBlock.SetColor(ColorId, color);
                fillRenderer.SetPropertyBlock(fillBlock);
            }
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

            float sqrDistance = (cameraTransform.position - transform.position).sqrMagnitude;
            bool visible = sqrDistance < 45f * 45f;
            if (barRenderers != null)
            {
                foreach (Renderer renderer in barRenderers)
                    if (renderer != null && renderer.enabled != visible) renderer.enabled = visible;
            }
        }
    }
}
