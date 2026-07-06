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
            GUI.Label(new Rect(34f, 28f, 280f, 28f), "渡劫录 Prototype", titleStyle);
            GUI.Label(new Rect(34f, 62f, 280f, 24f), $"境界 Lv.{player.Level}   灵气 {player.Experience}/{player.ExperienceToNextLevel}", labelStyle);
            GUI.Label(new Rect(34f, 88f, 280f, 24f), $"气血 {Mathf.CeilToInt(player.Health)}/{Mathf.CeilToInt(player.MaxHealth)}   击杀 {manager.KillCount}", labelStyle);
            GUI.Label(new Rect(34f, 114f, 280f, 24f), $"存活 {Mathf.FloorToInt(manager.RunTime)}s   WASD/方向键移动", labelStyle);

            if (!manager.IsGameOver)
            {
                return;
            }

            var width = Screen.width;
            var height = Screen.height;
            GUI.Box(new Rect(width * 0.5f - 180f, height * 0.5f - 58f, 360f, 116f), GUIContent.none);
            GUI.Label(new Rect(width * 0.5f - 150f, height * 0.5f - 34f, 300f, 32f), "道消身陨", titleStyle);
            GUI.Label(new Rect(width * 0.5f - 150f, height * 0.5f + 6f, 300f, 28f), "按 R 重开一局", labelStyle);
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
