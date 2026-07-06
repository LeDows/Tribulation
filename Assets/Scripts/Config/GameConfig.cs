using System;
using UnityEngine;

namespace Tribulation.Config
{
    [Serializable]
    public sealed class GameConfig
    {
        public string selectedCharacterId = "sword_cultivator";
        public string selectedMapId = "qingyun_plateau";
        public CharacterConfig[] characters = Array.Empty<CharacterConfig>();
        public EnemyConfig[] enemies = Array.Empty<EnemyConfig>();
        public LevelConfig level = new();
        public MapConfig[] maps = Array.Empty<MapConfig>();
        public WeaponConfig weapon = new();
        public PickupConfig pickup = new();

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
                pickup = PickupConfig.CreateDefault()
            };
        }
    }

    [Serializable]
    public sealed class CharacterConfig
    {
        public string id = "sword_cultivator";
        public string displayName = "Cultivator";
        public float maxHealth = 100f;
        public float moveSpeed = 7f;
        public float damageMultiplier = 1f;
        public Color color = new(0.2f, 0.75f, 1f, 1f);

        public static CharacterConfig CreateDefault()
        {
            return new CharacterConfig();
        }
    }

    [Serializable]
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
        public string id = "hungry_spirit";
        public string displayName = "Hungry Spirit";
        public float maxHealth = 30f;
        public int experienceValue = 1;
        public float eliteChance = 0.15f;
        public int eliteExperienceValue = 2;
        public float minMoveSpeed = 2.6f;
        public float maxMoveSpeed = 3.8f;
        public float contactDamage = 8f;
        public float attackInterval = 0.75f;
        public float attackRange = 1.35f;
        public float healthDifficultySeconds = 90f;
        public float speedDifficultySeconds = 240f;
        public float colliderRadius = 0.45f;
        public Color color = new(0.75f, 0.16f, 0.22f, 1f);

        public static EnemyConfig CreateDefault()
        {
            return new EnemyConfig();
        }
    }

    [Serializable]
    public sealed class MapConfig
    {
        public string id = "qingyun_plateau";
        public string displayName = "Qingyun Prototype Ground";
        public Vector3 playerSpawn = new(0f, 1f, 0f);
        public Vector3 groundScale = new(6f, 1f, 6f);
        public Color groundColor = new(0.15f, 0.18f, 0.16f, 1f);
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
        public Vector3 position = new(0f, 18f, -13f);
        public Vector3 rotation = new(58f, 0f, 0f);
        public float fieldOfView = 35f;
        public float orbitDistance = 35f;
        public float yaw = 0f;
        public float pitch = 45f;
        public float mouseSensitivity = 0.22f;
        public float positionSharpness = 18f;
        public float rotationSharpness = 22f;
        public Vector2 pitchLimits = new(30f, 75f);
    }

    [Serializable]
    public sealed class SpawnConfig
    {
        public string enemyId = "hungry_spirit";
        public float interval = 1.25f;
        public float minimumInterval = 0.28f;
        public float pressureRampSeconds = 180f;
        public float radius = 16f;
        public int maxEnemies = 90;
    }

    [Serializable]
    public sealed class WeaponConfig
    {
        public string projectileName = "Flying Sword";
        public float fireInterval = 0.45f;
        public float range = 14f;
        public float baseDamage = 18f;
        public float projectileSpeed = 18f;
        public float projectileLifetime = 2.2f;
        public float projectileScale = 0.28f;
        public Color projectileColor = new(0.8f, 0.95f, 1f, 1f);

        public static WeaponConfig CreateDefault()
        {
            return new WeaponConfig();
        }
    }

    [Serializable]
    public sealed class PickupConfig
    {
        public string experienceOrbName = "Spirit Qi";
        public float magnetRadius = 5f;
        public float moveSpeed = 10f;
        public float scale = 0.35f;
        public Color color = new(0.35f, 1f, 0.7f, 1f);

        public static PickupConfig CreateDefault()
        {
            return new PickupConfig();
        }
    }
}
