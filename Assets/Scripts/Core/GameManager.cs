using System.Collections.Generic;
using Tribulation.Config;
using Tribulation.Combat;
using Tribulation.Enemies;
using Tribulation.Player;
using Tribulation.UI;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tribulation.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlayerStats Player { get; private set; }
        public int KillCount { get; private set; }
        public float RunTime { get; private set; }
        public string LastUpgradeSummary { get; private set; } = string.Empty;
        public bool IsGameOver => State == GameState.GameOver;
        public GameState State { get; private set; } = GameState.MainMenu;

        public Transform RunRoot => runRoot;

        private Transform runRoot;
        private GameUiController ui;
        private UpgradeOptionConfig[] currentUpgradeOptions = System.Array.Empty<UpgradeOptionConfig>();
        private int pendingLevelUpSelections;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ui = GetComponentInChildren<GameUiController>(true);
        }

        private void Start()
        {
            Time.timeScale = 1f;
            State = GameState.MainMenu;
            ui?.ShowMainMenu();
        }

        private void Update()
        {
            if (State == GameState.Running)
            {
                RunTime += Time.deltaTime;
                return;
            }

            if (State == GameState.GameOver && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartRun();
            }
        }

        public void RegisterPlayer(PlayerStats player)
        {
            Player = player;
        }

        public void RegisterKill()
        {
            KillCount++;
        }

        public void StartRun()
        {
            ClearRun();
            Time.timeScale = 1f;
            State = GameState.Running;
            KillCount = 0;
            RunTime = 0f;
            LastUpgradeSummary = string.Empty;
            pendingLevelUpSelections = 0;
            currentUpgradeOptions = System.Array.Empty<UpgradeOptionConfig>();

            var map = ConfigCenter.GetSelectedMap();

            var runRootObject = RuntimePrefabCatalog.Instantiate(RuntimePrefabCatalog.RunRoot);
            if (runRootObject == null)
            {
                return;
            }

            runRoot = runRootObject.transform;
            var player = CreatePlayer(ConfigCenter.GetSelectedCharacter(), map);
            CreateGround(map, player.transform);
            CreateCamera(player.transform, map.camera);
            CreateSpawner(map);
            ui?.ShowHud();
        }

        public void RestartRun()
        {
            StartRun();
        }

        public void ReturnToMainMenu()
        {
            ClearRun();
            Time.timeScale = 1f;
            State = GameState.MainMenu;
            KillCount = 0;
            RunTime = 0f;
            LastUpgradeSummary = string.Empty;
            Player = null;
            ui?.ShowMainMenu();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void EndRun()
        {
            if (State != GameState.Running)
            {
                return;
            }

            State = GameState.GameOver;
            Time.timeScale = 0f;
            ui?.ShowResult();
        }

        public void RequestLevelUpSelection()
        {
            if (Player == null || IsGameOver)
            {
                return;
            }

            pendingLevelUpSelections++;
            if (State == GameState.LevelUpSelection || State == GameState.AttributeAllocation)
            {
                return;
            }

            ShowNextLevelUpStep();
        }

        public void ChooseUpgradeOption(int optionIndex)
        {
            if (State != GameState.LevelUpSelection || optionIndex < 0 || optionIndex >= currentUpgradeOptions.Length)
            {
                return;
            }

            ApplyUpgradeOption(currentUpgradeOptions[optionIndex]);
            pendingLevelUpSelections = Mathf.Max(0, pendingLevelUpSelections - 1);
            currentUpgradeOptions = System.Array.Empty<UpgradeOptionConfig>();
            ShowNextLevelUpStep();
        }

        public void ChooseAttribute(CultivationAttribute attribute)
        {
            if (State != GameState.AttributeAllocation || Player == null)
            {
                return;
            }

            if (!Player.AddAttributePoint(attribute))
            {
                return;
            }

            ShowNextLevelUpStep();
        }

        private PlayerStats CreatePlayer(CharacterConfig character, MapConfig map)
        {
            var playerObject = RuntimePrefabCatalog.Instantiate(RuntimePrefabCatalog.Player, runRoot);
            playerObject.name = ConfigCenter.Text(character.displayNameKey);
            playerObject.transform.position = map.playerSpawn;

            if (playerObject.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = character.color;
            }

            var stats = playerObject.GetComponent<PlayerStats>();
            stats.Configure(character, ConfigCenter.Level, ConfigCenter.Cultivation);
            RegisterPlayer(stats);
            return stats;
        }

        private void CreateGround(MapConfig map, Transform target)
        {
            var groundRoot = new GameObject(ConfigCenter.Text("runtime.ground.root_name", ConfigCenter.Text(map.displayNameKey)));
            groundRoot.transform.SetParent(runRoot, false);
            groundRoot.AddComponent<InfiniteGround>().Configure(target, map);
        }

        private void CreateSpawner(MapConfig map)
        {
            var spawnerObject = RuntimePrefabCatalog.Instantiate(RuntimePrefabCatalog.WaveSpawner, runRoot);
            spawnerObject.GetComponent<WaveSpawner>().Configure(map.spawn);
        }

        private static void CreateCamera(Transform target, CameraConfig config)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.position = config.position;
            camera.transform.rotation = Quaternion.Euler(config.rotation);
            camera.orthographic = false;
            camera.fieldOfView = config.fieldOfView;

            var follow = camera.GetComponent<CameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<CameraFollow>();
            }

            follow.Configure(target, config);
        }

        private void ClearRun()
        {
            if (runRoot != null)
            {
                Destroy(runRoot.gameObject);
                runRoot = null;
            }

            Player = null;
            pendingLevelUpSelections = 0;
            currentUpgradeOptions = System.Array.Empty<UpgradeOptionConfig>();
        }

        private void ShowNextLevelUpStep()
        {
            if (Player != null && Player.UnspentAttributePoints > 0)
            {
                ShowAttributeAllocation();
                return;
            }

            if (pendingLevelUpSelections > 0)
            {
                ShowLevelUpSelection();
                return;
            }

            currentUpgradeOptions = System.Array.Empty<UpgradeOptionConfig>();
            State = GameState.Running;
            Time.timeScale = 1f;
            ui?.ShowHud();
        }

        private void ShowAttributeAllocation()
        {
            State = GameState.AttributeAllocation;
            Time.timeScale = 0f;
            ui?.ShowAttributeAllocation(Player);
        }

        private void ShowLevelUpSelection()
        {
            State = GameState.LevelUpSelection;
            Time.timeScale = 0f;
            currentUpgradeOptions = PickUpgradeOptions();
            ui?.ShowLevelUpOptions(currentUpgradeOptions);
        }

        private UpgradeOptionConfig[] PickUpgradeOptions()
        {
            var pool = ConfigCenter.Upgrades;
            if (pool == null || pool.Length == 0)
            {
                pool = UpgradeOptionConfig.CreateDefaults();
            }

            var weapon = Player != null ? Player.GetComponent<AutoWeapon>() : null;
            var availableOptions = new List<UpgradeOptionConfig>();
            foreach (var option in pool)
            {
                if (IsUpgradeOptionAvailable(option, weapon))
                {
                    availableOptions.Add(option);
                }
            }

            if (availableOptions.Count == 0)
            {
                Debug.LogWarning("No configured upgrade options are currently applicable. Falling back to default upgrades.", this);
                foreach (var fallbackOption in UpgradeOptionConfig.CreateDefaults())
                {
                    if (IsUpgradeOptionAvailable(fallbackOption, weapon))
                    {
                        availableOptions.Add(fallbackOption);
                    }
                }
            }

            var count = Mathf.Min(3, availableOptions.Count);
            var selected = new UpgradeOptionConfig[count];

            for (var i = 0; i < count; i++)
            {
                var index = Random.Range(0, availableOptions.Count);
                selected[i] = availableOptions[index];
                availableOptions.RemoveAt(index);
            }

            return selected;
        }

        private static bool IsUpgradeOptionAvailable(UpgradeOptionConfig option, AutoWeapon weapon)
        {
            if (option == null)
            {
                return false;
            }

            var effectType = option.effectType?.Trim().ToLowerInvariant();
            if (effectType == "equip_weapon" || effectType == "random_weapon" || effectType == "enhance_weapon" ||
                effectType == "weapon_damage" || effectType == "weapon_fire_rate" || effectType == "weapon_range" ||
                effectType == "projectile_speed" || effectType == "projectile_scale" || effectType == "projectile_count")
            {
                return weapon != null && weapon.CanApplyUpgrade(option);
            }

            return true;
        }

        private void ApplyUpgradeOption(UpgradeOptionConfig option)
        {
            if (option == null || Player == null)
            {
                return;
            }

            var effectType = option.effectType?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(effectType))
            {
                effectType = option.id?.Trim().ToLowerInvariant() switch
                {
                    "spirit_power" => "damage_multiplier",
                    "iron_body" => "max_health",
                    "cloud_step" => "move_speed",
                    "sharpened_sword" => "weapon_damage",
                    "quickened_blade" => "weapon_fire_rate",
                    _ => string.Empty
                };
            }

            var weapon = Player.GetComponent<AutoWeapon>();
            var applied = true;

            switch (effectType)
            {
                case "max_health":
                    Player.IncreaseMaxHealth(option.value, true);
                    break;
                case "heal":
                    Player.Heal(option.value);
                    break;
                case "move_speed":
                    Player.AddMoveSpeed(option.value);
                    break;
                case "damage_multiplier":
                    Player.AddDamageMultiplier(option.value);
                    break;
                case "weapon_damage":
                    applied = weapon != null;
                    weapon?.AddBaseDamage(option.value);
                    break;
                case "weapon_fire_rate":
                    applied = weapon != null && weapon.ReduceFireIntervalPercent(option.value);
                    break;
                case "weapon_range":
                    applied = weapon != null;
                    weapon?.AddRange(option.value);
                    break;
                case "projectile_speed":
                    applied = weapon != null;
                    weapon?.AddProjectileSpeed(option.value);
                    break;
                case "projectile_scale":
                    applied = weapon != null;
                    weapon?.AddProjectileScale(option.value);
                    break;
                case "projectile_count":
                    applied = weapon != null && weapon.AddProjectileCount(Mathf.RoundToInt(option.value));
                    break;
                case "equip_weapon":
                    applied = weapon != null && weapon.EquipWeapon(option.weaponId);
                    break;
                case "random_weapon":
                    applied = weapon != null && weapon.EquipRandomWeapon();
                    break;
                case "enhance_weapon":
                    applied = ApplyWeaponEnhancement(weapon, option);
                    break;
                default:
                    applied = false;
                    Debug.LogWarning($"Unknown upgrade effect type: {option.effectType}");
                    break;
            }

            if (applied)
            {
                LastUpgradeSummary = ConfigCenter.Text(option.titleKey);
                Debug.Log($"Applied upgrade '{option.id}' ({effectType}) value={option.value}.", this);
            }
        }

        private static bool ApplyWeaponEnhancement(AutoWeapon weapon, UpgradeOptionConfig option)
        {
            if (weapon == null || option == null)
            {
                return false;
            }

            var levels = Mathf.Max(1, Mathf.RoundToInt(option.value));
            return string.IsNullOrWhiteSpace(option.weaponId)
                ? weapon.EnhanceAllWeapons(levels)
                : weapon.EnhanceWeapon(option.weaponId, levels);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Time.timeScale = 1f;
            }
        }
    }
}
