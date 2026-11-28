using UnityEngine;

namespace _Project.Develop.UI.Evidence
{
    internal class PageTurnGesture
    {
        private const float DragDeadZone = 4f;
        private const float FlickMemory = .1f;
        private const float FlickSpeed = 1.8f;

        private float _dragStartX;
        private float _dragSpan;
        private float _lastDragX;
        private float _lastMotionTime;
        private float _dragVelocity;

        public int Direction { get; private set; }
        public float GripHeight { get; private set; }
        public float Progress { get; private set; }

        public bool IsFlick => Time.unscaledTime - _lastMotionTime <= FlickMemory &&
            Progress >= .2f && _dragVelocity * Direction >= FlickSpeed;

        public void Begin(int direction, Vector2 start, RectTransform leaf)
        {
            _dragStartX = _lastDragX = start.x;
            _lastMotionTime = Time.unscaledTime;
            _dragVelocity = 0f;
            Progress = 0f;
            Direction = direction;
            GripHeight = Mathf.Clamp((start.y - leaf.localPosition.y) / (leaf.rect.height * .5f), -1f, 1f);
            var distanceFromSpine = Mathf.Abs(start.x - leaf.localPosition.x);
            var gripPosition = distanceFromSpine / leaf.rect.width;
            var grip = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.2f, .95f, gripPosition));
            var resistedTravel = leaf.rect.width * Mathf.Lerp(1.45f, .72f, grip);
            var edgeGrip = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.65f, .95f, gripPosition));
            // A projected point moves twice its distance from the spine in a complete turn.
            _dragSpan = Mathf.Lerp(resistedTravel, 2f * distanceFromSpine, edgeGrip);
        }

        public bool Move(float x)
        {
            var forwardDistance = (_dragStartX - x) * Direction;
            SampleDragVelocity(x);
            if (forwardDistance <= DragDeadZone)
            {
                Progress = 0f;
                return false;
            }

            var travel = Mathf.Clamp01((forwardDistance - DragDeadZone) / _dragSpan);
            // Convert the horizontal travel to the leaf's rotation angle.
            Progress = Mathf.Acos(1f - 2f * travel) / Mathf.PI;
            return true;
        }

        private void SampleDragVelocity(float x)
        {
            var movement = _lastDragX - x;
            if (Mathf.Abs(movement) < .5f)
                return;

            var now = Time.unscaledTime;
            var elapsed = now - _lastMotionTime;
            var speed = movement / (_dragSpan * Mathf.Max(elapsed, 1f / 120f));
            _dragVelocity = Mathf.Lerp(_dragVelocity, speed, elapsed < FlickMemory ? .55f : 1f);
            _lastDragX = x;
            _lastMotionTime = now;
        }
    }
}
