using _Project.Develop.Data;
using UnityEngine;

namespace _Project.Develop.Gameplay
{
    public abstract class SavableBehaviour : MonoBehaviour, ISavable
    {
        [SerializeField, HideInInspector] private string saveId;
        public string SaveId => saveId;
        public string PrefabKey { get; private set; }

        public static T Spawn<T>(T prefab) where T : SavableBehaviour
        {
            var instance = Instantiate(prefab);
            instance.AssignIdentity(prefab.SaveId, System.Guid.NewGuid().ToString("N"));
            return instance;
        }

        public void AssignIdentity(string prefabKey, string instanceId)
        {
            PrefabKey = prefabKey;
            saveId = instanceId;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += UpdateId;
        }

        private void UpdateId()
        {
            if (!this || Application.isPlaying) return;
            if (UnityEditor.EditorUtility.IsPersistent(this))
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(this);
                var key = UnityEditor.AssetDatabase.AssetPathToGUID(path);
                if (saveId != key)
                {
                    saveId = key;
                    UnityEditor.EditorUtility.SetDirty(this);
                }
                return;
            }
            if (!gameObject.scene.IsValid() ||
                string.IsNullOrEmpty(gameObject.scene.path)) return;
            var id = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(this);
            if (id.targetObjectId == 0 || saveId == id.ToString()) return;
            saveId = id.ToString();
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }
}
