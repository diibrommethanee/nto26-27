using _Project.Develop.Data.Camera.Parallax;
using UnityEngine;

namespace _Project.Develop.Gameplay.Camera.Parallax
{
    [ExecuteInEditMode]
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private ParallaxFactor parallaxFactor;

        public void Move(float delta)
        {
            var newPos = transform.localPosition;
            newPos.x -= delta * parallaxFactor.value;

            transform.localPosition = newPos;
        }
    }
}