using _Project.Develop.Gameplay.Train;
using R3;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Gameplay.Camera
{
    [RequireComponent(typeof(CinemachineBasicMultiChannelPerlin))]
    public class TrainCameraShake : MonoBehaviour
    {
        private TrainMovement _train;

        [Inject]
        private void Construct(TrainMovement train)
        {
            _train = train;
        }

        private void Start()
        {
            var noise = GetComponent<CinemachineBasicMultiChannelPerlin>();
            var initialAmplitude = noise.AmplitudeGain;

            _train.Speed01.Subscribe(speed => noise.AmplitudeGain = initialAmplitude * speed).AddTo(this);
        }
    }
}