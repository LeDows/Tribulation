using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace Tribulation.Config
{
    public static class ConfigCenter
    {
        private const string ConfigRoot = "Config/";
        private const string DefaultLanguage = "zh-CN";
        private const string FallbackLanguage = "en-US";

        private static RuntimeConfig runtime;

        public static string Language => Data.Settings.language;
        public static CharacterConfig[] Characters => Data.Characters;
        public static EnemyConfig[] Enemies => Data.Enemies;
        public static LevelConfig Level => Data.Level;
        public static MapConfig[] Maps => Data.Maps;
        public static WeaponConfig Weapon => Data.Weapon;
        public static PickupConfig Pickup => Data.Pickup;
        public static UpgradeOptionConfig[] Upgrades => Data.Upgrades;

        private static RuntimeConfig Data => runtime ??= LoadAll();

        public static void Reload()
        {
            runtime = LoadAll();
        }

        public static CharacterConfig GetSelectedCharacter()
        {
            var selectedId = Data.Settings.selectedCharacterId;
            return GetCharacter(selectedId) ?? (Characters.Length > 0 ? Characters[0] : CharacterConfig.CreateDefault());
        }

        public static CharacterConfig GetCharacter(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return Data.CharacterById.TryGetValue(id, out var character) ? character : null;
        }

        public static EnemyConfig GetEnemy(string id)
        {
            if (!string.IsNullOrWhiteSpace(id) && Data.EnemyById.TryGetValue(id, out var enemy))
            {
                return enemy;
            }

            return Enemies.Length > 0 ? Enemies[0] : EnemyConfig.CreateDefault();
        }

        public static MapConfig GetSelectedMap()
        {
            var selectedId = Data.Settings.selectedMapId;
            if (!string.IsNullOrWhiteSpace(selectedId) && Data.MapById.TryGetValue(selectedId, out var map))
            {
                return map;
            }

            return Maps.Length > 0 ? Maps[0] : MapConfig.CreateDefault();
        }

        public static string Text(string key, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var template = ResolveText(key);
            if (args == null || args.Length == 0)
            {
                return template;
            }

            try
            {
                return string.Format(CultureInfo.InvariantCulture, template, args);
            }
            catch (FormatException exception)
            {
                Debug.LogError($"Failed to format localization key '{key}': {exception.Message}");
                return template;
            }
        }

        private static string ResolveText(string key)
        {
            if (Data.ActiveLocalization.TryGetValue(key, out var localized))
            {
                return localized;
            }

            if (Data.FallbackLocalization.TryGetValue(key, out var fallback))
            {
                return fallback;
            }

            Debug.LogWarning($"Missing localization key: {key}");
            return key;
        }

        private static RuntimeConfig LoadAll()
        {
            var settings = LoadXml("settings", GameConfigSettings.CreateDefault);
            if (string.IsNullOrWhiteSpace(settings.language))
            {
                settings.language = DefaultLanguage;
            }

            var characters = LoadXml("characters", CharacterConfigList.CreateDefault).items ?? Array.Empty<CharacterConfig>();
            var enemies = LoadXml("enemies", EnemyConfigList.CreateDefault).items ?? Array.Empty<EnemyConfig>();
            var maps = LoadXml("maps", MapConfigList.CreateDefault).items ?? Array.Empty<MapConfig>();
            var level = LoadXml("level", LevelConfig.CreateDefault);
            var weapon = LoadXml("weapon", WeaponConfig.CreateDefault);
            var pickup = LoadXml("pickup", PickupConfig.CreateDefault);
            var upgrades = LoadXml("upgrades", UpgradeOptionConfigList.CreateDefault).items ?? Array.Empty<UpgradeOptionConfig>();

            var fallbackLocalization = LoadLocalization(FallbackLanguage);
            var activeLocalization = settings.language == FallbackLanguage
                ? fallbackLocalization
                : LoadLocalization(settings.language);

            return new RuntimeConfig(
                settings,
                characters,
                enemies,
                maps,
                level,
                weapon,
                pickup,
                upgrades,
                activeLocalization,
                fallbackLocalization);
        }

        private static T LoadXml<T>(string resourceName, Func<T> createDefault)
        {
            var asset = Resources.Load<TextAsset>(ConfigRoot + resourceName);
            if (asset == null)
            {
                Debug.LogError($"Config file missing at Resources/{ConfigRoot}{resourceName}.xml. Using defaults.");
                return createDefault();
            }

            try
            {
                var serializer = new XmlSerializer(typeof(T));
                using var reader = new StringReader(asset.text);
                return serializer.Deserialize(reader) is T loaded ? loaded : createDefault();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to parse Resources/{ConfigRoot}{resourceName}.xml: {exception.Message}");
                return createDefault();
            }
        }

        private static Dictionary<string, string> LoadLocalization(string language)
        {
            var table = LoadXml("localization." + language, LocalizationTable.CreateDefault);
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            if (table.entries == null)
            {
                return values;
            }

            foreach (var entry in table.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                {
                    continue;
                }

                values[entry.key] = entry.value ?? string.Empty;
            }

            return values;
        }

        private sealed class RuntimeConfig
        {
            public readonly GameConfigSettings Settings;
            public readonly CharacterConfig[] Characters;
            public readonly EnemyConfig[] Enemies;
            public readonly MapConfig[] Maps;
            public readonly LevelConfig Level;
            public readonly WeaponConfig Weapon;
            public readonly PickupConfig Pickup;
            public readonly UpgradeOptionConfig[] Upgrades;
            public readonly Dictionary<string, string> ActiveLocalization;
            public readonly Dictionary<string, string> FallbackLocalization;
            public readonly Dictionary<string, CharacterConfig> CharacterById;
            public readonly Dictionary<string, EnemyConfig> EnemyById;
            public readonly Dictionary<string, MapConfig> MapById;

            public RuntimeConfig(
                GameConfigSettings settings,
                CharacterConfig[] characters,
                EnemyConfig[] enemies,
                MapConfig[] maps,
                LevelConfig level,
                WeaponConfig weapon,
                PickupConfig pickup,
                UpgradeOptionConfig[] upgrades,
                Dictionary<string, string> activeLocalization,
                Dictionary<string, string> fallbackLocalization)
            {
                Settings = settings;
                Characters = characters;
                Enemies = enemies;
                Maps = maps;
                Level = level;
                Weapon = weapon;
                Pickup = pickup;
                Upgrades = upgrades;
                ActiveLocalization = activeLocalization;
                FallbackLocalization = fallbackLocalization;
                CharacterById = BuildLookup(characters, item => item.id);
                EnemyById = BuildLookup(enemies, item => item.id);
                MapById = BuildLookup(maps, item => item.id);
            }

            private static Dictionary<string, T> BuildLookup<T>(IEnumerable<T> items, Func<T, string> getId)
            {
                var lookup = new Dictionary<string, T>(StringComparer.Ordinal);
                foreach (var item in items)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    var id = getId(item);
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        lookup[id] = item;
                    }
                }

                return lookup;
            }
        }
    }

    [Serializable]
    [XmlRoot("localization")]
    public sealed class LocalizationTable
    {
        [XmlElement("text")] public LocalizationEntry[] entries = Array.Empty<LocalizationEntry>();

        public static LocalizationTable CreateDefault()
        {
            return new LocalizationTable();
        }
    }

    [Serializable]
    public sealed class LocalizationEntry
    {
        [XmlAttribute] public string key;
        [XmlText] public string value;
    }
}
