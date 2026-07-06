using Tribulation.Core;
using Tribulation.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Tribulation.UI
{
    public sealed class GameUiController : MonoBehaviour
    {
        private GameObject mainMenuPanel;
        private GameObject hudPanel;
        private GameObject resultPanel;
        private Text levelText;
        private Text experienceText;
        private Text healthText;
        private Text killsText;
        private Text timeText;
        private Text resultTimeText;
        private Text resultKillsText;
        private Text resultLevelText;

        private void Awake()
        {
            mainMenuPanel = FindChild("MainMenuPanel");
            hudPanel = FindChild("HudPanel");
            resultPanel = FindChild("ResultPanel");

            levelText = FindText("LevelValue");
            experienceText = FindText("ExperienceValue");
            healthText = FindText("HealthValue");
            killsText = FindText("KillsValue");
            timeText = FindText("TimeValue");
            resultTimeText = FindText("ResultTimeValue");
            resultKillsText = FindText("ResultKillsValue");
            resultLevelText = FindText("ResultLevelValue");

            BindButton("StartButton", () => GameManager.Instance.StartRun());
            BindButton("QuitButton", () => GameManager.Instance.QuitGame());
            BindButton("RestartButton", () => GameManager.Instance.RestartRun());
            BindButton("ReturnMenuButton", () => GameManager.Instance.ReturnToMainMenu());
        }

        private void Update()
        {
            var manager = GameManager.Instance;
            if (manager == null || manager.State != GameState.Running)
            {
                return;
            }

            RefreshHud(manager);
        }

        public void ShowMainMenu()
        {
            SetPanel(mainMenuPanel, true);
            SetPanel(hudPanel, false);
            SetPanel(resultPanel, false);
        }

        public void ShowHud()
        {
            SetPanel(mainMenuPanel, false);
            SetPanel(hudPanel, true);
            SetPanel(resultPanel, false);
            RefreshHud(GameManager.Instance);
        }

        public void ShowResult()
        {
            var manager = GameManager.Instance;
            SetPanel(mainMenuPanel, false);
            SetPanel(hudPanel, false);
            SetPanel(resultPanel, true);

            if (manager == null)
            {
                return;
            }

            SetText(resultTimeText, FormatTime(manager.RunTime));
            SetText(resultKillsText, manager.KillCount.ToString());
            SetText(resultLevelText, manager.Player != null ? manager.Player.Level.ToString() : "0");
        }

        private void RefreshHud(GameManager manager)
        {
            if (manager == null)
            {
                return;
            }

            PlayerStats player = manager.Player;
            if (player == null)
            {
                return;
            }

            SetText(levelText, player.Level.ToString());
            SetText(experienceText, $"{player.Experience}/{player.ExperienceToNextLevel}");
            SetText(healthText, $"{Mathf.CeilToInt(player.Health)}/{Mathf.CeilToInt(player.MaxHealth)}");
            SetText(killsText, manager.KillCount.ToString());
            SetText(timeText, FormatTime(manager.RunTime));
        }

        private GameObject FindChild(string childName)
        {
            var child = FindDescendant(childName);
            if (child == null)
            {
                Debug.LogError($"UI child not found: {childName}", this);
                return null;
            }

            return child.gameObject;
        }

        private Text FindText(string childName)
        {
            var child = FindDescendant(childName);
            if (child == null || !child.TryGetComponent<Text>(out var text))
            {
                Debug.LogError($"UI text not found: {childName}", this);
                return null;
            }

            return text;
        }

        private void BindButton(string childName, UnityEngine.Events.UnityAction action)
        {
            var child = FindDescendant(childName);
            if (child == null || !child.TryGetComponent<Button>(out var button))
            {
                Debug.LogError($"UI button not found: {childName}", this);
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private Transform FindDescendant(string childName)
        {
            var children = GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static void SetPanel(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static string FormatTime(float seconds)
        {
            var totalSeconds = Mathf.FloorToInt(seconds);
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
