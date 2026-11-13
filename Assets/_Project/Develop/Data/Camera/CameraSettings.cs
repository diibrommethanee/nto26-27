using NaughtyAttributes;
using UnityEngine;

namespace _Project.Develop.Data.Camera
{
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "Camera Settings")]
    public class CameraSettings : ScriptableObject
    {
        [BoxGroup("Scrolling")] public float panSensitivity = 0.7f;
        [BoxGroup("Scrolling"), Min(0f)] public float maxPanOffset = 5f;
        [BoxGroup("Scrolling"), Min(0.01f)] public float panSmoothTime = 0.15f;

        [BoxGroup("Zoom"), Min(0.1f)] public float minZoom = 3f;
        [BoxGroup("Zoom"), Min(0.1f)] public float maxZoom = 8f;
        [BoxGroup("Zoom")] public float zoomSensitivity = 0.7f;
        [BoxGroup("Zoom"), Min(0.01f)] public float zoomSmoothTime = 0.15f;
        
        [BoxGroup("Parallax"), Min(0.01f)] public float cameraInfluence = 0.1f;
    }
}
