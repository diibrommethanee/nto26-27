using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public string Key(ScriptableObject asset)
        {
            var entry = Array.Find(entries, candidate => candidate.asset == asset);
            return entry?.key ?? throw new InvalidDataException($"No checkpoint key for '{asset}'.");
        }

        public string[] Keys(IEnumerable<ScriptableObject> assets) => assets.Select(Key).ToArray();

        public T[] ResolveAll<T>(IEnumerable<string> keys) where T : ScriptableObject =>
            keys.Select(Resolve<T>).ToArray();

        public T Resolve<T>(string key) where T : ScriptableObject
        {
            var entry = Array.Find(entries, candidate => candidate.key == key);
            return entry?.asset is T asset ? asset :
                throw new InvalidDataException($"Checkpoint key '{key}' does not identify a {typeof(T).Name}.");
        }
    }
}
