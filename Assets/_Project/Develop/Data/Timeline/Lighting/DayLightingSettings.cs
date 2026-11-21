using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace _Project.Develop.Data.Timeline.Lighting
{
    [CreateAssetMenu(menuName = "Timeline/Day Lighting Settings")]
    public class DayLightingSettings : ScriptableObject
    {
        [Serializable]
        public struct LightingPreset
        {
            public VolumeProfile profile;
            public Color color;
            [Min(0)] public float intensity;
        }

        public LightingPreset morning;
        public LightingPreset daytime;
        public LightingPreset evening;
        [Min(0.01f)] public float transitionDuration = 1.5f;
    }
}
