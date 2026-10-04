using UnityEngine;
using UnityEngine.UI;

namespace FPSGame
{
    /// <summary>
    /// An invisible Graphic that only receives raycasts. Its mesh is empty, so it costs no fill
    /// rate. Used for the joystick touch area.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class TouchZone : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}
