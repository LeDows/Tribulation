using Tribulation.Config;
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
        public bool IsGameOver => State == GameState.GameOver;
        public GameState State { get; private set; } = GameState.MainMenu;

        public Transform RunRoot => runRoot;

        private Transform runRoot;
        private GameUiController ui;

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

            var config = GameConfigService.Config;
            var map = config.GetSelectedMap();

            var runRootObject = RuntimePrefabCatalog.Instantiate(RuntimePrefabCatalog.RunRoot);
            if (runRootObject == null)
            {
                return;
            }

            runRoot = runRootObject.transform;
            CreateGround(map);
            var player = CreatePlayer(config.GetSelectedCharacter(), map);
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

        private PlayerStats CreatePlayer(CharacterConfig character, MapConfig map)
        {
            var playerObject = RuntimePrefabCatalog.Instantiate(RuntimePrefabCatalog.Player, runRoot);
            playerObject.name = character.displayName;
            playerObject.transform.position = map.playerSpawn;

            if (playerObject.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = character.color;
            }

            var stats = playerObject.GetComponent<PlayerStats>();
            stats.Configure(character, GameConfigService.Config.level);
            RegisterPlayer(stats);
            return stats;
        }

        private void CreateGround(MapConfig map)
        {
            var ground = RuntimePrefabCatalog.Instantiate(RuntimePrefabCatalog.Ground, runRoot);
            ground.name = map.displayName;
            ground.transform.localScale = map.groundScale;

            if (ground.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = map.groundColor;
            }
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
