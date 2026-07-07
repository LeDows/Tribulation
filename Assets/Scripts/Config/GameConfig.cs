using System;
using System.Xml.Serialization;
using UnityEngine;

namespace Tribulation.Config
{
    [Serializable]
    [XmlRoot("settings")]
    public sealed class GameConfigSettings
    {
        [XmlAttribute] public string selectedCharacterId = "sword_cultivator";
        [XmlAttribute] public string selectedMapId = "qingyun_plateau";
        [XmlAttribute] public string language = "zh-CN";

        public static GameConfigSettings CreateDefault()
        {
            return new GameConfigSettings();
        }
    }

    [Serializable]
    [XmlRoot("characters")]
    public sealed class CharacterConfigList
    {
        [XmlElement("character")] public CharacterConfig[] items = Array.Empty<CharacterConfig>();

        public static CharacterConfigList CreateDefault()
        {
            return new CharacterConfigList { items = new[] { CharacterConfig.CreateDefault() } };
        }
    }

    [Serializable]
    [XmlRoot("enemies")]
    public sealed class EnemyConfigList
    {
        [XmlElement("enemy")] public EnemyConfig[] items = Array.Empty<EnemyConfig>();

        public static EnemyConfigList CreateDefault()
        {
            return new EnemyConfigList { items = new[] { EnemyConfig.CreateDefault() } };
        }
    }

    [Serializable]
    [XmlRoot("maps")]
    public sealed class MapConfigList
    {
        [XmlElement("map")] public MapConfig[] items = Array.Empty<MapConfig>();

        public static MapConfigList CreateDefault()
        {
            return new MapConfigList { items = new[] { MapConfig.CreateDefault() } };
        }
    }

    [Serializable]
    [XmlRoot("upgrades")]
    public sealed class UpgradeOptionConfigList
    {
        [XmlElement("upgrade")] public UpgradeOptionConfig[] items = Array.Empty<UpgradeOptionConfig>();

        public static UpgradeOptionConfigList CreateDefault()
        {
            return new UpgradeOptionConfigList { items = UpgradeOptionConfig.CreateDefaults() };
        }
    }

    [Serializable]
    public sealed class GameConfig
    {
        public string selectedCharacterId = "sword_cultivator";
        public string selectedMapId = "qingyun_plateau";
        public string language = "zh-CN";
        public CharacterConfig[] characters = Array.Empty<CharacterConfig>();
        public EnemyConfig[] enemies = Array.Empty<EnemyConfig>();
        public LevelConfig level = new();
        public MapConfig[] maps = Array.Empty<MapConfig>();
        public WeaponConfig weapon = new();
        public PickupConfig pickup = new();
        public UpgradeOptionConfig[] upgradeOptions = Array.Empty<UpgradeOptionConfig>();

        public CharacterConfig GetSelectedCharacter()
        {
            return FindCharacter(selectedCharacterId) ?? (characters.Length > 0 ? characters[0] : CharacterConfig.CreateDefault());
        }

        public CharacterConfig FindCharacter(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || characters == null)
            {
                return null;
            }

            foreach (var character in characters)
            {
                if (character != null && character.id == id)
                {
                    return character;
                }
            }

            return null;
        }

        public EnemyConfig GetEnemy(string id)
        {
            if (!string.IsNullOrWhiteSpace(id) && enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    if (enemy != null && enemy.id == id)
                    {
                        return enemy;
                    }
                }
            }

            return enemies != null && enemies.Length > 0 ? enemies[0] : EnemyConfig.CreateDefault();
        }

        public MapConfig GetSelectedMap()
        {
            if (!string.IsNullOrWhiteSpace(selectedMapId) && maps != null)
            {
                foreach (var map in maps)
                {
                    if (map != null && map.id == selectedMapId)
                    {
                        return map;
                    }
                }
            }

            return maps != null && maps.Length > 0 ? maps[0] : MapConfig.CreateDefault();
        }

        public static GameConfig CreateDefault()
        {
            return new GameConfig
            {
                characters = new[] { CharacterConfig.CreateDefault() },
                enemies = new[] { EnemyConfig.CreateDefault() },
                maps = new[] { MapConfig.CreateDefault() },
                level = LevelConfig.CreateDefault(),
                weapon = WeaponConfig.CreateDefault(),
                pickup = PickupConfig.CreateDefault(),
                upgradeOptions = UpgradeOptionConfig.CreateDefaults()
            };
        }
    }

    [Serializable]
    public sealed class CharacterConfig
    {
        [XmlAttribute] public string id = "sword_cultivator";
        [XmlAttribute] public string displayNameKey = "character.sword_cultivator.name";
        public float maxHealth = 100f;
        public float moveSpeed = 7f;
        public float damageMultiplier = 1f;
        public ConfigColor color = new(0.2f, 0.75f, 1f, 1f);

        public static CharacterConfig CreateDefault()
        {
            return new CharacterConfig();
        }
    }

    [Serializable]
    [XmlRoot("level")]
    public sealed class LevelConfig
    {
        public int startLevel = 1;
        public int firstLevelExperience = 6;
        public float experienceGrowthMultiplier = 1.24f;
        public int experienceGrowthFlat = 2;
        public float maxHealthPerLevel = 12f;
        public float healOnLevelUp = 28f;
        public float moveSpeedPerLevel = 0.12f;
        public float damageMultiplierPerLevel = 0.08f;

        public static LevelConfig CreateDefault()
        {
            return new LevelConfig();
        }
    }

    [Serializable]
    public sealed class EnemyConfig
    {
        [XmlAttribute] public string id = "hungry_spirit";
        [XmlAttribute] public string displayNameKey = "enemy.hungry_spirit.name";
        public float maxHealth = 30f;
        public int experienceValue = 1;
        public float eliteChance = 0.15f;
        public int eliteExperienceValue = 2;
        public float minMoveSpeed = 2.6f;
        public float maxMoveSpeed = 3.8f;
        public float contactDamage = 8f;
        public float attackInterval = 0.75f;
        public float attackRange = 1.35f;
        public float speedDifficultySeconds = 240f;
        public float colliderRadius = 0.45f;
        public ConfigColor color = new(0.75f, 0.16f, 0.22f, 1f);

        public static EnemyConfig CreateDefault()
        {
            return new EnemyConfig();
        }
    }

    [Serializable]
    public sealed class MapConfig
    {
        [XmlAttribute] public string id = "qingyun_plateau";
        [XmlAttribute] public string displayNameKey = "map.qingyun_plateau.name";
        public ConfigVector3 playerSpawn = new(0f, 1f, 0f);
        public ConfigVector3 groundScale = new(6f, 1f, 6f);
        public ConfigColor groundColor = new(0.15f, 0.18f, 0.16f, 1f);
        public CameraConfig camera = new();
        public SpawnConfig spawn = new();

        public static MapConfig CreateDefault()
        {
            return new MapConfig();
        }
    }

    [Serializable]
    public sealed class CameraConfig
    {
        public ConfigVector3 position = new(0f, 18f, -13f);
        public ConfigVector3 rotation = new(58f, 0f, 0f);
        public float fieldOfView = 35f;
        public float orbitDistance = 35f;
        public float yaw = 0f;
        public float pitch = 45f;
        public float mouseSensitivity = 0.22f;
        public float positionSharpness = 18f;
        public float rotationSharpness = 22f;
        public ConfigVector2 pitchLimits = new(30f, 75f);
    }

    [Serializable]
    public sealed class SpawnConfig
    {
        [XmlAttribute] public string enemyId = "hungry_spirit";
        public float interval = 1.25f;
        public float minimumInterval = 0.28f;
        public float pressureRampSeconds = 180f;
        public float radius = 16f;
        public int maxEnemies = 90;
    }

    [Serializable]
    [XmlRoot("weapon")]
    public sealed class WeaponConfig
    {
        [XmlAttribute] public string nameKey = "weapon.flying_sword.name";
        public float fireInterval = 0.45f;
        public float range = 14f;
        public float baseDamage = 18f;
        public float projectileSpeed = 18f;
        public float projectileLifetime = 2.2f;
        public float projectileScale = 0.28f;
        public ConfigColor projectileColor = new(0.8f, 0.95f, 1f, 1f);

        public static WeaponConfig CreateDefault()
        {
            return new WeaponConfig();
        }
    }

    [Serializable]
    [XmlRoot("pickup")]
    public sealed class PickupConfig
    {
        [XmlAttribute] public string nameKey = "pickup.spirit_qi.name";
        public float magnetRadius = 5f;
        public float moveSpeed = 10f;
        public float scale = 0.35f;
        public ConfigColor color = new(0.35f, 1f, 0.7f, 1f);

        public static PickupConfig CreateDefault()
        {
            return new PickupConfig();
        }
    }

    [Serializable]
    public sealed class UpgradeOptionConfig
    {
        [XmlAttribute] public string id = "spirit_power";
        [XmlAttribute] public string titleKey = "upgrade.spirit_power.title";
        [XmlAttribute] public string descriptionKey = "upgrade.damage_multiplier.description";
        [XmlAttribute] public string effectType = "damage_multiplier";
        [XmlAttribute] public float value = 0.1f;

        public static UpgradeOptionConfig[] CreateDefaults()
        {
            return new[]
            {
                new UpgradeOptionConfig(),
                new UpgradeOptionConfig
                {
                    id = "iron_body",
                    titleKey = "upgrade.iron_body.title",
                    descriptionKey = "upgrade.max_health.description",
                    effectType = "max_health",
                    value = 20f
                },
                new UpgradeOptionConfig
                {
                    id = "cloud_step",
                    titleKey = "upgrade.cloud_step.title",
                    descriptionKey = "upgrade.move_speed.description",
                    effectType = "move_speed",
                    value = 0.35f
                },
                new UpgradeOptionConfig
                {
                    id = "sharpened_sword",
                    titleKey = "upgrade.sharpened_sword.title",
                    descriptionKey = "upgrade.weapon_damage.description",
                    effectType = "weapon_damage",
                    value = 6f
                },
                new UpgradeOptionConfig
                {
                    id = "quickened_blade",
                    titleKey = "upgrade.quickened_blade.title",
                    descriptionKey = "upgrade.weapon_fire_rate.description",
                    effectType = "weapon_fire_rate",
                    value = 0.12f
                }
            };
        }
    }

    [Serializable]
    public sealed class ConfigVector2
    {
        [XmlAttribute] public float x;
        [XmlAttribute] public float y;

        public ConfigVector2()
        {
        }

        public ConfigVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static implicit operator Vector2(ConfigVector2 value)
        {
            return value == null ? default : new Vector2(value.x, value.y);
        }
    }

    [Serializable]
    public sealed class ConfigVector3
    {
        [XmlAttribute] public float x;
        [XmlAttribute] public float y;
        [XmlAttribute] public float z;

        public ConfigVector3()
        {
        }

        public ConfigVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3(ConfigVector3 value)
        {
            return value == null ? default : new Vector3(value.x, value.y, value.z);
        }
    }

    [Serializable]
    public sealed class ConfigColor
    {
        [XmlAttribute] public float r;
        [XmlAttribute] public float g;
        [XmlAttribute] public float b;
        [XmlAttribute] public float a = 1f;

        public ConfigColor()
        {
        }

        public ConfigColor(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static implicit operator Color(ConfigColor value)
        {
            return value == null ? Color.white : new Color(value.r, value.g, value.b, value.a);
        }
    }
}
