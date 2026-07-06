using UnityEngine;

namespace Tribulation.Core
{
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BuildPrototypeScene()
        {
            Time.timeScale = 1f;

            if (Object.FindFirstObjectByType<GameManager>() != null)
            {
                return;
            }

            RuntimePrefabCatalog.Instantiate(RuntimePrefabCatalog.GameBootstrap);
        }
    }
}
