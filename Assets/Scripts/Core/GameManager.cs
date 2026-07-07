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

            RefreshActiveProjectileDamage(Player.GetComponent<AutoWeapon>());
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

            var count = Mathf.Min(3, pool.Length);
            var selected = new UpgradeOptionConfig[count];
            var used = new bool[pool.Length];

            for (var i = 0; i < count; i++)
            {
                var index = Random.Range(0, pool.Length);
                var guard = 0;
                while (used[index] && guard < pool.Length * 2)
                {
                    index = Random.Range(0, pool.Length);
                    guard++;
                }

                used[index] = true;
                selected[i] = pool[index];
            }

            return selected;
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
                    RefreshActiveProjectileDamage(weapon);
                    break;
                case "weapon_damage":
                    weapon?.AddBaseDamage(option.value);
                    RefreshActiveProjectileDamage(weapon);
                    break;
                case "weapon_fire_rate":
                    weapon?.ReduceFireIntervalPercent(option.value);
                    break;
                case "weapon_range":
                    weapon?.AddRange(option.value);
                    break;
                case "projectile_speed":
                    weapon?.AddProjectileSpeed(option.value);
                    break;
                case "projectile_scale":
                    weapon?.AddProjectileScale(option.value);
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

        private void RefreshActiveProjectileDamage(AutoWeapon weapon)
        {
            if (weapon == null || runRoot == null)
            {
                return;
            }

            foreach (var projectile in runRoot.GetComponentsInChildren<Projectile>())
            {
                projectile.SetDamage(weapon.CurrentDamage);
            }
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
