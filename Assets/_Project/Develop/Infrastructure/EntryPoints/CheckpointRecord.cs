using System;
using System.Collections.Generic;
using System.Reflection;
using _Project.Develop.Data;
using Newtonsoft.Json.Linq;

namespace _Project.Develop.Infrastructure.EntryPoints
{
    internal static class CheckpointRecord
    {
        public static JObject Capture(ISavable participant)
        {
            var record = new JObject();
            foreach (var member in Members(participant.GetType()))
            {
                var value = member is FieldInfo field ? field.GetValue(participant)
                    : ((PropertyInfo)member).GetValue(participant);
                record.Add(member.Name, value == null ? JValue.CreateNull() : JToken.FromObject(value));
            }
            return record;
        }

        public static void Restore(ISavable participant, JToken record)
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
    }
}
