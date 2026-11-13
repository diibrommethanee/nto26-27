using _Project.Develop.Data.Train;
using System;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace _Project.Develop.Gameplay.Train
{
    public class TrainMovement : IStartable, ITickable, IDisposable
    {
        private enum State
        {
            Travelling,
            Braking,
            Stopped,
            Accelerating
        }
        
        private readonly TrainSettings _settings;
        private readonly IPublisher<TrainStopped> _stoppedPublisher;
        private readonly IPublisher<TrainDeparted> _departedPublisher;

        private readonly ReactiveProperty<float> _speed = new(1f);

        private State _state;
        private float _elapsed;
        private float _duration;

        public ReadOnlyReactiveProperty<float> Speed01 => _speed;
        public float CurrentSpeed => _settings.travelSpeed * _speed.Value;

        public TrainMovement(TrainSettings settings, IPublisher<TrainStopped> stoppedPublisher, IPublisher<TrainDeparted> departedPublisher)
        {
            _settings = settings;
            _stoppedPublisher = stoppedPublisher;
            _departedPublisher = departedPublisher;
        }

        public void Start()
        {
            BeginTravelling();
        }

        public void Tick()
        {
            _elapsed += Time.deltaTime;

            var progress = Mathf.Clamp01(_elapsed / _duration);

            switch (_state)
            {
                case State.Travelling:
                    if (progress >= 1f)
                        EnterState(State.Braking, _settings.brakingDuration);
                    break;
                
                case State.Braking:
                    _speed.Value = 1f - Mathf.SmoothStep(0f, 1f, progress);
                    
                    if (progress >= 1f)
                    {
                        EnterState(State.Stopped, _settings.stopDuration);
                        _stoppedPublisher.Publish(new TrainStopped());
                    }
                    break;
                
                case State.Stopped:
                    if (progress >= 1f)
                    {
                        EnterState(State.Accelerating, _settings.accelerationDuration);
                        _departedPublisher.Publish(new TrainDeparted());
                    }
                    break;
                
                case State.Accelerating:
                    _speed.Value = Mathf.SmoothStep(0f, 1f, progress);

                    if (progress >= 1f)
                        BeginTravelling();
                    break;
            }
        }

        private void BeginTravelling()
        {
            var travelTime = UnityEngine.Random.Range(_settings.minTravelTime, _settings.maxTravelTime);
            EnterState(State.Travelling, travelTime);
        }
        
        private void EnterState(State state, float duration)
        {
            _state = state;
            _elapsed = 0f;
            _duration = duration;
        }

        public void Dispose()
        {
            _speed.Dispose();
        }
    }
}
