using _Project.Develop.Data.Camera;
using NaughtyAttributes;
using UnityEngine;

namespace _Project.Develop.Gameplay.Camera.Parallax
{
    [ExecuteInEditMode]
    public class ParallaxCamera : MonoBehaviour 
    {
        public delegate void ParallaxCameraDelegate(float deltaMovement);
        public ParallaxCameraDelegate onCameraTranslate;
        
        [SerializeField, Expandable] private CameraSettings settings;

        private float _oldPosition;

        private void Start()
        {
            _oldPosition = transform.position.x;
        }

        private void Update()
        {
            if (!Mathf.Approximately(transform.position.x, _oldPosition))
            {
                if (onCameraTranslate != null)
                {
                    var delta = _oldPosition - transform.position.x;
                    onCameraTranslate(delta * settings.cameraInfluence);
                }
                _oldPosition = transform.position.x;
            }
        }
    }
}