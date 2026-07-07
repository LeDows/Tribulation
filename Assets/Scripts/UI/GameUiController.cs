using System.Globalization;
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
        private Text statsText;
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
            statsText = FindText("StatsValue", false);
            weaponStatsText = FindText("WeaponStatsValue", false);
            lastUpgradeText = FindText("LastUpgradeValue", false);
            attributePointsText = FindText("AttributePointsValue");
            resultTimeText = FindText("ResultTimeValue");
            resultKillsText = FindText("ResultKillsValue");
            resultLevelText = FindText("ResultLevelValue");

            HideLegacyHudRows();

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
            SetText(statsText, FormatCharacterStats(player));
            SetText(weaponStatsText, FormatWeaponStats(player.GetComponent<AutoWeapon>()));
            SetText(lastUpgradeText, FormatRunStats(manager));
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
                return string.Join(
                    "\n",
                    ConfigCenter.Text("ui.hud.weapon_title"),
                    FormatHudLine("ui.hud.weapon_name_label", ConfigCenter.Text("ui.common.none")));
            }

            var shotsPerSecond = weapon.FireInterval > 0f ? 1f / weapon.FireInterval : 0f;
            return string.Join(
                "\n",
                ConfigCenter.Text("ui.hud.weapon_title"),
                FormatHudLine("ui.hud.weapon_name_label", weapon.ProjectileName),
                FormatHudLine("ui.hud.actual_damage_label", FormatDecimal(weapon.CurrentDamage, "0.0")),
                FormatHudLine("ui.hud.base_damage_label", FormatDecimal(weapon.BaseDamage, "0.0")),
                FormatHudLine("ui.hud.cooldown_label", FormatSeconds(weapon.FireInterval)),
                FormatHudLine("ui.hud.fire_rate_label", FormatPerSecond(shotsPerSecond)),
                FormatHudLine("ui.hud.range_label", FormatDecimal(weapon.Range, "0.0")),
                FormatHudLine("ui.hud.projectile_speed_label", FormatDecimal(weapon.ProjectileSpeed, "0.0")),
                FormatHudLine("ui.hud.projectile_scale_label", FormatDecimal(weapon.ProjectileScale, "0.00")),
                FormatHudLine("ui.hud.projectile_lifetime_label", FormatSeconds(weapon.ProjectileLifetime)));
        }

        private static string FormatCharacterStats(PlayerStats player)
        {
            return string.Join(
                "\n",
                ConfigCenter.Text("ui.hud.character_title"),
                FormatHudLine("ui.hud.level_label", player.RealmLayerDisplay),
                FormatHudLine("ui.hud.level_number_label", player.Level.ToString(CultureInfo.InvariantCulture)),
                FormatHudLine("ui.hud.qi_label", $"{player.Experience}/{player.ExperienceToNextLevel}"),
                FormatHudLine("ui.hud.health_label", $"{Mathf.CeilToInt(player.Health)}/{Mathf.CeilToInt(player.MaxHealth)}"),
                FormatHudLine("ui.attribute.spirit_power", player.SpiritPower.ToString(CultureInfo.InvariantCulture)),
                FormatHudLine("ui.attribute.divine_sense", player.DivineSense.ToString(CultureInfo.InvariantCulture)),
                FormatHudLine("ui.attribute.root", player.Root.ToString(CultureInfo.InvariantCulture)),
                FormatHudLine("ui.attribute.insight", player.Insight.ToString(CultureInfo.InvariantCulture)),
                FormatHudLine("ui.attribute.fortune", player.Fortune.ToString(CultureInfo.InvariantCulture)),
                FormatHudLine("ui.attribute.agility", player.Agility.ToString(CultureInfo.InvariantCulture)),
                FormatHudLine("ui.attribute.will", player.Will.ToString(CultureInfo.InvariantCulture)),
                FormatHudLine("ui.hud.move_speed_label", FormatDecimal(player.MoveSpeed, "0.00")),
                FormatHudLine("ui.hud.damage_multiplier_label", ConfigCenter.Text("ui.hud.multiplier_value", FormatDecimal(player.DamageMultiplier, "0.00"))),
                FormatHudLine("ui.hud.crit_chance_label", FormatPercent(player.CritChance)),
                FormatHudLine("ui.hud.pickup_radius_label", FormatDecimal(player.PickupRadiusBonus, "0.0")),
                FormatHudLine("ui.hud.experience_gain_label", ConfigCenter.Text("ui.hud.multiplier_value", FormatDecimal(player.ExperienceGainMultiplier, "0.00"))),
                FormatHudLine("ui.hud.rare_reward_label", FormatPercent(player.RareRewardChanceBonus)),
                FormatHudLine("ui.hud.damage_reduction_label", FormatPercent(player.DamageReduction)));
        }

        private static string FormatRunStats(GameManager manager)
        {
            var lastUpgrade = string.IsNullOrWhiteSpace(manager.LastUpgradeSummary)
                ? ConfigCenter.Text("ui.common.none")
                : manager.LastUpgradeSummary;

            return string.Join(
                "\n",
                ConfigCenter.Text("ui.hud.run_title"),
                FormatHudLine("ui.hud.kills_label", manager.KillCount.ToString(CultureInfo.InvariantCulture)),
                FormatHudLine("ui.hud.time_label", FormatTime(manager.RunTime)),
                FormatHudLine("ui.hud.last_upgrade_label", lastUpgrade));
        }

        private static string FormatHudLine(string labelKey, string value)
        {
            return ConfigCenter.Text("ui.hud.stat_line", ConfigCenter.Text(labelKey), value);
        }

        private static string FormatDecimal(float value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static string FormatSeconds(float seconds)
        {
            return ConfigCenter.Text("ui.hud.seconds_value", FormatDecimal(seconds, "0.00"));
        }

        private static string FormatPerSecond(float value)
        {
            return ConfigCenter.Text("ui.hud.per_second_value", FormatDecimal(value, "0.00"));
        }

        private static string FormatPercent(float value)
        {
            return ConfigCenter.Text("ui.hud.percent_value", Mathf.RoundToInt(value * 100f));
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

        private void HideLegacyHudRows()
        {
            SetChildActive("Level", false);
            SetChildActive("LevelValue", false);
            SetChildActive("Experience", false);
            SetChildActive("ExperienceValue", false);
            SetChildActive("Health", false);
            SetChildActive("HealthValue", false);
            SetChildActive("Kills", false);
            SetChildActive("KillsValue", false);
            SetChildActive("Time", false);
            SetChildActive("TimeValue", false);
        }

        private void SetChildActive(string childName, bool active)
        {
            var child = FindDescendant(childName);
            if (child != null)
            {
                child.gameObject.SetActive(active);
            }
        }

    }
}
