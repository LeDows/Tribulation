using UnityEngine;

namespace Tribulation.Core
{
    public static class RuntimePrefabCatalog
    {
        private const string RootPath = "Prefabs/";

        public static GameObject GameBootstrap => Load("GameBootstrap");
        public static GameObject RunRoot => Load("RunRoot");
        public static GameObject Player => Load("Player");
        public static GameObject Enemy => Load("Enemy");
        public static GameObject Projectile => Load("Projectile");
        public static GameObject ExperienceOrb => Load("ExperienceOrb");
        public static GameObject Ground => Load("Ground");
        public static GameObject WaveSpawner => Load("WaveSpawner");
        public static GameObject GameUi => Load("GameUi");

        public static GameObject Instantiate(GameObject prefab, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogError("Cannot instantiate a missing runtime prefab.");
                return null;
            }

            return Object.Instantiate(prefab, parent);
        }

        private static GameObject Load(string prefabName)
        {
            var prefab = Resources.Load<GameObject>(RootPath + prefabName);
            if (prefab == null)
            {
                Debug.LogError($"Runtime prefab not found at Resources/{RootPath}{prefabName}.");
            }

            return prefab;
        }
    }
}
