using Tribulation.Core;
using UnityEngine;

namespace Tribulation.UI
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private GUIStyle labelStyle;
        private GUIStyle titleStyle;

        private void OnGUI()
        {
            EnsureStyles();

            var manager = GameManager.Instance;
            var player = manager.Player;
            if (player == null)
            {
                return;
            }

            GUI.Box(new Rect(18f, 18f, 320f, 132f), GUIContent.none);
            GUI.Label(new Rect(34f, 28f, 280f, 28f), "Tribulation Prototype", titleStyle);
            GUI.Label(new Rect(34f, 62f, 280f, 24f), $"Level {player.Level}   Qi {player.Experience}/{player.ExperienceToNextLevel}", labelStyle);
            GUI.Label(new Rect(34f, 88f, 280f, 24f), $"HP {Mathf.CeilToInt(player.Health)}/{Mathf.CeilToInt(player.MaxHealth)}   Kills {manager.KillCount}", labelStyle);
            GUI.Label(new Rect(34f, 114f, 280f, 24f), $"Time {Mathf.FloorToInt(manager.RunTime)}s   WASD/Arrow Keys", labelStyle);

            if (!manager.IsGameOver)
            {
                return;
            }

            var width = Screen.width;
            var height = Screen.height;
            GUI.Box(new Rect(width * 0.5f - 180f, height * 0.5f - 58f, 360f, 116f), GUIContent.none);
            GUI.Label(new Rect(width * 0.5f - 150f, height * 0.5f - 34f, 300f, 32f), "Run Over", titleStyle);
            GUI.Label(new Rect(width * 0.5f - 150f, height * 0.5f + 6f, 300f, 28f), "Press R to restart", labelStyle);
        }

        private void EnsureStyles()
        {
            labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = Color.white }
            };

            titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 1f, 0.95f) }
            };
        }
    }
}
