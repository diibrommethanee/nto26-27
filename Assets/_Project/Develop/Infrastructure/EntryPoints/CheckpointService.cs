using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Project.Develop.Data;
using _Project.Develop.Gameplay;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace _Project.Develop.Infrastructure.EntryPoints
{
    public class CheckpointService : ICheckpointService
    {
        private readonly IReadOnlyList<ISavable> _services;
        public static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");
        private Task _pending = Task.CompletedTask;
        public event Action<bool> SavingChanged;

        public CheckpointService(IReadOnlyList<ISavable> services) => _services = services;

        private Dictionary<string, ISavable> Participants()
        {
            var result = _services.ToDictionary(service => service.GetType().FullName);
            foreach (var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include))
            {
                if (component is SavableBehaviour entity)
                {
                    if (string.IsNullOrEmpty(entity.SaveId))
                        throw new InvalidDataException($"Missing checkpoint ID on {entity.name}");
                    result.Add(entity.SaveId, entity);
                }
                else if (component is ISavable singleton)
                    result.Add(component.GetType().FullName, singleton);
            }
            return result;
        }

        private static IEnumerable<MemberInfo> Members(Type type)
        {
            while (type != null)
            {
                foreach (var member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public |
                             BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (member.IsDefined(typeof(SaveAttribute), false)) yield return member;
                type = type.BaseType;
            }
        }

        public Task SaveAsync()
        {
            var records = new JObject();
            var prefabs = new JObject();
            foreach (var pair in Participants())
            {
                if (pair.Value is SavableBehaviour entity && entity.PrefabKey != null)
                    prefabs.Add(entity.SaveId, entity.PrefabKey);
                var record = new JObject();
                foreach (var member in Members(pair.Value.GetType()))
                {
                    var value = member is FieldInfo field ? field.GetValue(pair.Value)
                        : ((PropertyInfo)member).GetValue(pair.Value);
                    record.Add(member.Name, value == null ? JValue.CreateNull() : JToken.FromObject(value));
                }
                records.Add(pair.Key, record);
            }
            var json = new JObject { ["version"] = 1, ["prefabs"] = prefabs, ["records"] = records }.ToString();
            return _pending = WriteAsync(_pending, json);
        }

        private async Task WriteAsync(Task previous, string json)
        {
            try { await previous; } catch { /* Each caller observes its own write failure. */ }
            SavingChanged?.Invoke(true);
            try
            {
                await File.WriteAllTextAsync(SavePath + ".tmp", json);
                if (File.Exists(SavePath)) File.Replace(SavePath + ".tmp", SavePath, null);
                else File.Move(SavePath + ".tmp", SavePath);
            }
            finally { SavingChanged?.Invoke(false); }
        }

        public void Restore()
        {
            if (!File.Exists(SavePath)) return;
            var root = JObject.Parse(File.ReadAllText(SavePath));
            if ((int?)root["version"] != 1) throw new InvalidDataException("Unsupported checkpoint version.");
            var participants = Participants();
            RestorePrefabs(root["prefabs"] as JObject, participants);
            foreach (var pair in participants)
            {
                var record = root["records"][pair.Key];
                if (record != null) RestoreMembers(pair.Value, record);
            }
            foreach (var participant in participants.Values.OfType<IAfterRestore>()) participant.AfterRestore();
        }

        private static void RestorePrefabs(JObject prefabs, Dictionary<string, ISavable> participants)
        {
            if (prefabs == null || !prefabs.HasValues) return;

            var templates = Resources.LoadAll<GameObject>("")
                .Select(template => template.GetComponent<SavableBehaviour>())
                .Where(template => template != null).ToDictionary(template => template.SaveId);
            foreach (var entry in prefabs.Properties())
            {
                if (participants.ContainsKey(entry.Name)) continue;
                var template = templates[(string)entry.Value];
                var instance = UnityEngine.Object.Instantiate(template);
                instance.AssignIdentity(template.SaveId, entry.Name);
                participants.Add(entry.Name, instance);
            }
        }

        private static void RestoreMembers(ISavable participant, JToken record)
        {
            foreach (var member in Members(participant.GetType()))
            {
                var token = record[member.Name];
                if (token == null) continue;
                if (member is FieldInfo field) field.SetValue(participant, token.ToObject(field.FieldType));
                else
                {
                    var property = (PropertyInfo)member;
                    property.SetValue(participant, token.ToObject(property.PropertyType));
                }
            }
        }
    }
}
