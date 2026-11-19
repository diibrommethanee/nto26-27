using System.Collections.Generic;
using _Project.Develop.Gameplay.Train;
using UnityEngine;
using VContainer;
using UnityCamera = UnityEngine.Camera;

namespace _Project.Develop.Gameplay.Camera.Parallax
{
    [ExecuteInEditMode]
    public class ParallaxBackground : MonoBehaviour
    {
        private ParallaxCamera _parallaxCamera;
        private TrainMovement _train;
        
        private readonly List<ParallaxLayer> _parallaxLayers = new();
        private readonly List<(ParallaxLayer[] tiles, float width)> _strips = new();
        
        [Inject]
        private void Construct(TrainMovement train)
        {
            _train = train;
        }
        
        private void Start()
        {
            _parallaxCamera = UnityCamera.main!.GetComponent<ParallaxCamera>();
            _parallaxCamera.onCameraTranslate += Move;

            SetLayers();
        }
        
        private void Update()
        {
            if (!Application.isPlaying)
                return;

            Move(_train.CurrentSpeed * Time.deltaTime);
        }
        
        private void LateUpdate()
        {
            if (!Application.isPlaying)
                return;

            var cameraX = _parallaxCamera.transform.position.x;

            foreach (var strip in _strips)
            {
                if (strip.width <= 0f)
                    continue;

                var totalWidth = strip.width * strip.tiles.Length;
                var centerOffset = strip.tiles[0].GetComponent<SpriteRenderer>().bounds.center.x - strip.tiles[0].transform.position.x;
                var leftBoundary = cameraX - totalWidth * 0.5f;

                foreach (var tile in strip.tiles)
                {
                    var position = tile.transform.position;
                    var centerX = position.x + centerOffset;

                    position.x += leftBoundary + Mathf.Repeat(centerX - leftBoundary, totalWidth) - centerX;
                    tile.transform.position = position;
                }
            }
        }

        private void SetLayers()
        {
            _parallaxLayers.Clear();
            
            var originalCount = transform.childCount;

            for (var i = 0; i < originalCount; i++)
            {
                if (!transform.GetChild(i).TryGetComponent(out ParallaxLayer layer))
                    continue;

                _parallaxLayers.Add(layer);

                if (!Application.isPlaying)
                    continue;

                var width = layer.GetComponent<SpriteRenderer>().bounds.size.x - 0.01f;

                var tiles = new ParallaxLayer[3];
                tiles[0] = layer;

                for (var j = 1; j < tiles.Length; j++)
                {
                    var copy = Instantiate(layer, transform);
                    copy.name = $"{layer.name}_Tile{j}";

                    var direction = j == 1 ? -1f : 1f;

                    copy.transform.position =
                        layer.transform.position + Vector3.right * (width * direction);

                    tiles[j] = copy;
                    _parallaxLayers.Add(copy);
                }

                _strips.Add((tiles, width));
            }
        }

        private void Move(float delta)
        {
            foreach (var layer in _parallaxLayers)
                layer.Move(delta);
        }

        private void OnDestroy()
        {
            foreach (var strip in _strips)
                for (var i = 1; i < strip.tiles.Length; i++)
                    if (strip.tiles[i] != null)
                        Destroy(strip.tiles[i].gameObject);
            
            _parallaxCamera.onCameraTranslate -= Move;
        }
    }
}