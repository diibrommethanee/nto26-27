using System;
using System.Threading.Tasks;
using UnityEngine.Scripting;

namespace _Project.Develop.Data
{
    public interface ISavable { }
    public interface IAfterRestore { void AfterRestore(); }
    public interface ICheckpointService { Task SaveAsync(); }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SaveAttribute : PreserveAttribute { }
}
