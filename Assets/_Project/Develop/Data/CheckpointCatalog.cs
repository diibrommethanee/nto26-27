using System;
using UnityEngine;

namespace _Project.Develop.Data
{
    public class CheckpointCatalog : ScriptableObject
    {
        [Serializable] private class Entry { public string key; public ScriptableObject asset; }
        [SerializeField] private Entry[] entries;
        public Sprite savingIcon;
        private static CheckpointCatalog _instance;
        public static CheckpointCatalog Instance => _instance ? _instance : _instance = Resources.Load<CheckpointCatalog>("CheckpointCatalog");
        public string Key(ScriptableObject asset) => Array.Find(entries, entry => entry.asset == asset).key;
        public T Resolve<T>(string key) where T : ScriptableObject =>
            (T)Array.Find(entries, entry => entry.key == key).asset;
    }
}
