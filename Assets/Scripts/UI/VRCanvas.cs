using System.Collections.Generic;
using UnityEngine;

namespace VRBase
{
    /// <summary>Marks a World Space canvas as something UIPointers can point at.</summary>
    [RequireComponent(typeof(Canvas))]
    public class VRCanvas : MonoBehaviour
    {
        public static readonly List<VRCanvas> All = new();

        public Canvas Canvas { get; private set; }

        private void Awake()
        {
            Canvas = GetComponent<Canvas>();
            if (Canvas.renderMode != RenderMode.WorldSpace)
                Debug.LogWarning($"{name}: VRCanvas needs Render Mode = World Space.", this);
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);
    }
}
