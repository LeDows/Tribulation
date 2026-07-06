using Tribulation.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tribulation.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlayerStats Player { get; private set; }
        public int KillCount { get; private set; }
        public float RunTime { get; private set; }
        public bool IsGameOver { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (!IsGameOver)
            {
                RunTime += Time.deltaTime;
                return;
            }

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
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

        public void EndRun()
        {
            IsGameOver = true;
            Time.timeScale = 0f;
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
