using UnityEngine;

namespace Tribulation.Config
{
    public static class GameConfigService
    {
        private const string ConfigResourcePath = "Config/game_config";
        private static GameConfig config;

        public static GameConfig Config => config ??= Load();

        public static void Reload()
        {
            config = Load();
        }

        private static GameConfig Load()
        {
            var asset = Resources.Load<TextAsset>(ConfigResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"Game config not found at Resources/{ConfigResourcePath}. Using code defaults.");
                return GameConfig.CreateDefault();
            }

            try
            {
                var loaded = JsonUtility.FromJson<GameConfig>(asset.text);
                return loaded ?? GameConfig.CreateDefault();
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Failed to parse game config: {exception.Message}");
                return GameConfig.CreateDefault();
            }
        }
    }
}
