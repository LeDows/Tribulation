using System.Collections.Generic;
using UnityEngine;

namespace Tribulation.Core
{
    public static class RuntimePrefabCatalog
    {
        private const string RootPath = "Prefabs/";
        private static readonly Dictionary<string, GameObject> PrefabCache = new();
        private static readonly Dictionary<GameObject, Stack<GameObject>> Pools = new();
        private static readonly MaterialPropertyBlock ColorPropertyBlock = new();
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

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

        public static GameObject InstantiatePooled(GameObject prefab, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogError("Cannot instantiate a missing runtime prefab.");
                return null;
            }

            if (Pools.TryGetValue(prefab, out var pool))
            {
                while (pool.Count > 0)
                {
                    var instance = pool.Pop();
                    if (instance == null)
                    {
                        continue;
                    }

                    ResetTransform(instance.transform, prefab.transform, parent);
                    instance.SetActive(true);
                    return instance;
                }
            }

            return Object.Instantiate(prefab, parent);
        }

        public static void ReleasePooled(GameObject instance, GameObject prefab)
        {
            if (instance == null)
            {
                return;
            }

            if (prefab == null)
            {
                Object.Destroy(instance);
                return;
            }

            if (!instance.activeSelf)
            {
                return;
            }

            instance.SetActive(false);
            if (!Pools.TryGetValue(prefab, out var pool))
            {
                pool = new Stack<GameObject>();
                Pools.Add(prefab, pool);
            }

            pool.Push(instance);
        }

        public static void ClearPools()
        {
            Pools.Clear();
        }

        public static void PreloadDynamicPrefabs()
        {
            _ = Enemy;
            _ = Projectile;
            _ = ExperienceOrb;
        }

        public static int CountTransforms(GameObject prefab)
        {
            return prefab != null ? prefab.GetComponentsInChildren<Transform>(true).Length : 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            PrefabCache.Clear();
            Pools.Clear();
        }

        public static bool PrewarmStep(GameObject prefab, Transform parent, int targetCount, int batchSize)
        {
            if (prefab == null || targetCount <= 0)
            {
                return true;
            }

            if (!Pools.TryGetValue(prefab, out var pool))
            {
                pool = new Stack<GameObject>();
                Pools.Add(prefab, pool);
            }

            if (pool.Count >= targetCount)
            {
                return true;
            }

            var createCount = Mathf.Min(Mathf.Max(1, batchSize), targetCount - pool.Count);
            for (var i = 0; i < createCount; i++)
            {
                var instance = Object.Instantiate(prefab, parent);
                instance.SetActive(false);
                pool.Push(instance);
            }

            return pool.Count >= targetCount;
        }

        public static void SetRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            ColorPropertyBlock.Clear();
            ColorPropertyBlock.SetColor(BaseColorPropertyId, color);
            ColorPropertyBlock.SetColor(ColorPropertyId, color);
            renderer.SetPropertyBlock(ColorPropertyBlock);
        }

        private static GameObject Load(string prefabName)
        {
            if (PrefabCache.TryGetValue(prefabName, out var cachedPrefab) && cachedPrefab != null)
            {
                return cachedPrefab;
            }

            var prefab = Resources.Load<GameObject>(RootPath + prefabName);
            if (prefab == null)
            {
                Debug.LogError($"Runtime prefab not found at Resources/{RootPath}{prefabName}.");
            }
            else
            {
                PrefabCache[prefabName] = prefab;
            }

            return prefab;
        }

        private static void ResetTransform(Transform instance, Transform prefab, Transform parent)
        {
            instance.SetParent(parent, false);
            instance.localPosition = prefab.localPosition;
            instance.localRotation = prefab.localRotation;
            instance.localScale = prefab.localScale;
        }
    }
}
