using System.Collections.Generic;
using Tribulation.Core;
using Tribulation.Config;
using Tribulation.Combat;
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
        private GameObject levelUpPanel;
        private GameObject attributePanel;
        private Text levelText;
        private Text experienceText;
        private Text healthText;
        private Text killsText;
        private Text timeText;
        private Text weaponStatsText;
        private Text lastUpgradeText;
        private Text attributePointsText;
        private Text resultTimeText;
        private Text resultKillsText;
        private Text resultLevelText;
        private readonly Button[] levelUpOptionButtons = new Button[3];
        private readonly Text[] levelUpOptionTitleTexts = new Text[3];
        private readonly Text[] levelUpOptionDescriptionTexts = new Text[3];
        private readonly CultivationAttribute[] attributeOrder =
        {
            CultivationAttribute.SpiritPower,
            CultivationAttribute.DivineSense,
            CultivationAttribute.Root,
            CultivationAttribute.Insight,
            CultivationAttribute.Fortune,
            CultivationAttribute.Agility,
            CultivationAttribute.Will
        };
        private readonly Button[] attributeButtons = new Button[7];
        private readonly Text[] attributeValueTexts = new Text[7];

        private void Awake()
        {
            mainMenuPanel = FindChild("MainMenuPanel");
            hudPanel = FindChild("HudPanel");
            resultPanel = FindChild("ResultPanel");
            levelUpPanel = FindChild("LevelUpPanel");
            attributePanel = FindChild("AttributePanel");

            levelText = FindText("LevelValue");
            experienceText = FindText("ExperienceValue");
            healthText = FindText("HealthValue");
            killsText = FindText("KillsValue");
            timeText = FindText("TimeValue");
            weaponStatsText = FindText("WeaponStatsValue", false);
            lastUpgradeText = FindText("LastUpgradeValue", false);
            attributePointsText = FindText("AttributePointsValue");
            resultTimeText = FindText("ResultTimeValue");
            resultKillsText = FindText("ResultKillsValue");
            resultLevelText = FindText("ResultLevelValue");

            BindButton("StartButton", () => GameManager.Instance.StartRun());
            BindButton("QuitButton", () => GameManager.Instance.QuitGame());
            BindButton("RestartButton", () => GameManager.Instance.RestartRun());
            BindButton("ReturnMenuButton", () => GameManager.Instance.ReturnToMainMenu());

            for (var i = 0; i < levelUpOptionButtons.Length; i++)
            {
                var optionNumber = i + 1;
                levelUpOptionButtons[i] = FindButton($"LevelUpOption{optionNumber}Button");
                levelUpOptionTitleTexts[i] = FindText($"LevelUpOption{optionNumber}Title");
                levelUpOptionDescriptionTexts[i] = FindText($"LevelUpOption{optionNumber}Description");
                var optionIndex = i;
                if (levelUpOptionButtons[i] != null)
                {
                    levelUpOptionButtons[i].onClick.RemoveAllListeners();
                    levelUpOptionButtons[i].onClick.AddListener(() => GameManager.Instance.ChooseUpgradeOption(optionIndex));
                }
            }

            for (var i = 0; i < attributeOrder.Length; i++)
            {
                var suffix = GetAttributeObjectSuffix(attributeOrder[i]);
                attributeButtons[i] = FindButton($"Attribute{suffix}Button");
                attributeValueTexts[i] = FindText($"Attribute{suffix}Value");
                var attribute = attributeOrder[i];
                if (attributeButtons[i] != null)
                {
                    attributeButtons[i].onClick.RemoveAllListeners();
                    attributeButtons[i].onClick.AddListener(() => GameManager.Instance.ChooseAttribute(attribute));
                }
            }
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
            SetPanel(levelUpPanel, false);
            SetPanel(attributePanel, false);
        }

        public void ShowHud()
        {
            SetPanel(mainMenuPanel, false);
            SetPanel(hudPanel, true);
            SetPanel(resultPanel, false);
            SetPanel(levelUpPanel, false);
            SetPanel(attributePanel, false);
            RefreshHud(GameManager.Instance);
        }

        public void ShowResult()
        {
            var manager = GameManager.Instance;
            SetPanel(mainMenuPanel, false);
            SetPanel(hudPanel, false);
            SetPanel(resultPanel, true);
            SetPanel(levelUpPanel, false);
            SetPanel(attributePanel, false);

            if (manager == null)
            {
                return;
            }

            SetText(resultTimeText, FormatTime(manager.RunTime));
            SetText(resultKillsText, manager.KillCount.ToString());
            SetText(resultLevelText, manager.Player != null ? manager.Player.RealmLayerDisplay : "0");
        }

        public void ShowLevelUpOptions(UpgradeOptionConfig[] options)
        {
            SetPanel(mainMenuPanel, false);
            SetPanel(hudPanel, true);
            SetPanel(resultPanel, false);
            SetPanel(levelUpPanel, true);
            SetPanel(attributePanel, false);
            RefreshHud(GameManager.Instance);

            for (var i = 0; i < levelUpOptionButtons.Length; i++)
            {
                var hasOption = options != null && i < options.Length && options[i] != null;
                if (levelUpOptionButtons[i] != null)
                {
                    levelUpOptionButtons[i].gameObject.SetActive(hasOption);
                }

                SetText(levelUpOptionTitleTexts[i], hasOption ? ConfigCenter.Text(options[i].titleKey) : string.Empty);
                SetText(
                    levelUpOptionDescriptionTexts[i],
                    hasOption ? ConfigCenter.Text(options[i].descriptionKey, GetUpgradeDisplayValue(options[i])) : string.Empty);
            }
        }

        public void ShowAttributeAllocation(PlayerStats player)
        {
            SetPanel(mainMenuPanel, false);
            SetPanel(hudPanel, true);
            SetPanel(resultPanel, false);
            SetPanel(levelUpPanel, false);
            SetPanel(attributePanel, true);
            RefreshHud(GameManager.Instance);
            RefreshAttributePanel(player);
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

            SetText(levelText, player.RealmLayerDisplay);
            SetText(experienceText, $"{player.Experience}/{player.ExperienceToNextLevel}");
            SetText(healthText, $"{Mathf.CeilToInt(player.Health)}/{Mathf.CeilToInt(player.MaxHealth)}");
            SetText(killsText, manager.KillCount.ToString());
            SetText(timeText, FormatTime(manager.RunTime));
            SetText(weaponStatsText, FormatWeaponStats(player.GetComponent<AutoWeapon>()));
            SetText(lastUpgradeText, FormatLastUpgrade(manager));
        }

        private void RefreshAttributePanel(PlayerStats player)
        {
            if (player == null)
            {
                return;
            }

            SetText(attributePointsText, ConfigCenter.Text("ui.attribute.points_value", player.UnspentAttributePoints));
            for (var i = 0; i < attributeOrder.Length; i++)
            {
                SetText(
                    attributeValueTexts[i],
                    ConfigCenter.Text(
                        "ui.attribute.row_value",
                        ConfigCenter.Text(GetAttributeLabelKey(attributeOrder[i])),
                        player.GetAttributePoints(attributeOrder[i])));
                if (attributeButtons[i] != null)
                {
                    attributeButtons[i].interactable = player.UnspentAttributePoints > 0;
                }
            }
        }

        private static object GetUpgradeDisplayValue(UpgradeOptionConfig option)
        {
            var effectType = option.effectType?.Trim().ToLowerInvariant();
            if (effectType == "damage_multiplier" || effectType == "weapon_fire_rate")
            {
                return Mathf.RoundToInt(option.value * 100f);
            }

            if (effectType == "equip_weapon")
            {
                var weapon = ConfigCenter.Weapon.FindWeapon(option.weaponId);
                return weapon != null ? ConfigCenter.Text(weapon.nameKey) : option.weaponId;
            }

            if (effectType == "enhance_weapon" || effectType == "projectile_count")
            {
                return Mathf.Max(1, Mathf.RoundToInt(option.value));
            }

            if (effectType == "random_weapon")
            {
                return ConfigCenter.Text("upgrade.random_weapon.value");
            }

            return option.value;
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

        private Text FindText(string childName, bool required = true)
        {
            var child = FindDescendant(childName);
            if (child == null || !child.TryGetComponent<Text>(out var text))
            {
                if (required)
                {
                    Debug.LogError($"UI text not found: {childName}", this);
                }

                return null;
            }

            return text;
        }

        private void BindButton(string childName, UnityEngine.Events.UnityAction action)
        {
            var button = FindButton(childName);
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private Button FindButton(string childName)
        {
            var child = FindDescendant(childName);
            if (child == null || !child.TryGetComponent<Button>(out var button))
            {
                Debug.LogError($"UI button not found: {childName}", this);
                return null;
            }

            return button;
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

        private static string FormatWeaponStats(AutoWeapon weapon)
        {
            if (weapon == null)
            {
                return string.Empty;
            }

            var lines = new List<string>
            {
                ConfigCenter.Text("ui.hud.weapon_compact_header", weapon.EquippedCount, weapon.MaxEquippedWeapons)
            };

            if (!weapon.HasAnyWeapon)
            {
                lines.Add(ConfigCenter.Text("ui.common.none"));
                return string.Join("\n", lines);
            }

            foreach (var view in weapon.GetEquippedWeaponViews())
            {
                lines.Add(ConfigCenter.Text(
                    "ui.hud.weapon_compact_summary",
                    view.QualityName,
                    view.Name,
                    view.EnhancementLevel));
            }

            return string.Join("\n", lines);
        }

        private static string FormatLastUpgrade(GameManager manager)
        {
            var lastUpgrade = string.IsNullOrWhiteSpace(manager.LastUpgradeSummary)
                ? ConfigCenter.Text("ui.common.none")
                : manager.LastUpgradeSummary;

            return FormatHudLine("ui.hud.last_upgrade_label", lastUpgrade);
        }

        private static string FormatHudLine(string labelKey, string value)
        {
            return ConfigCenter.Text("ui.hud.stat_line", ConfigCenter.Text(labelKey), value);
        }

        private static string GetAttributeObjectSuffix(CultivationAttribute attribute)
        {
            return attribute switch
            {
                CultivationAttribute.SpiritPower => "SpiritPower",
                CultivationAttribute.DivineSense => "DivineSense",
                CultivationAttribute.Root => "Root",
                CultivationAttribute.Insight => "Insight",
                CultivationAttribute.Fortune => "Fortune",
                CultivationAttribute.Agility => "Agility",
                CultivationAttribute.Will => "Will",
                _ => string.Empty
            };
        }

        private static string GetAttributeLabelKey(CultivationAttribute attribute)
        {
            return attribute switch
            {
                CultivationAttribute.SpiritPower => "ui.attribute.spirit_power",
                CultivationAttribute.DivineSense => "ui.attribute.divine_sense",
                CultivationAttribute.Root => "ui.attribute.root",
                CultivationAttribute.Insight => "ui.attribute.insight",
                CultivationAttribute.Fortune => "ui.attribute.fortune",
                CultivationAttribute.Agility => "ui.attribute.agility",
                CultivationAttribute.Will => "ui.attribute.will",
                _ => string.Empty
            };
        }

    }
}
