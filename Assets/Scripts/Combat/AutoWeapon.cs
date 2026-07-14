using System.Collections.Generic;
using Tribulation.Config;
using Tribulation.Core;
using Tribulation.Enemies;
using Tribulation.Player;
using UnityEngine;

namespace Tribulation.Combat
{
    [RequireComponent(typeof(PlayerStats))]
    public sealed class AutoWeapon : MonoBehaviour
    {
        public float FireInterval = 0.45f;
        public float Range = 14f;
        public float BaseDamage = 18f;
        public float ProjectileSpeed = 18f;
        public float ProjectileLifetime = 2.2f;
        public float ProjectileScale = 0.28f;
        public string ProjectileName = "Flying Sword";
        public Color ProjectileColor = new(0.8f, 0.95f, 1f, 1f);

        private readonly List<EquippedWeaponState> equippedWeapons = new();
        private readonly List<EnemyHealth> selectedTargets = new();
        private readonly List<EnemyHealth> targetCandidates = new();
        private WeaponCatalogConfig catalog = WeaponCatalogConfig.CreateDefault();
        private PlayerStats stats;
        private float flatDamageBonus;
        private float fireIntervalMultiplier = 1f;
        private float rangeBonus;
        private float projectileSpeedBonus;
        private float projectileScaleBonus;
        private int projectileCountBonus;

        public int EquippedCount => equippedWeapons.Count;
        public int MaxEquippedWeapons => Mathf.Max(1, catalog != null ? catalog.maxEquipped : 3);
        public bool HasAnyWeapon => equippedWeapons.Count > 0;
        public float CurrentDamage => equippedWeapons.Count > 0 ? GetDamage(equippedWeapons[0]) : BaseDamage * (stats != null ? stats.DamageMultiplier : 1f);

        private void Awake()
        {
            stats = GetComponent<PlayerStats>();
            ApplyConfig(ConfigCenter.Weapon);
        }

        public void ApplyConfig(WeaponCatalogConfig config)
        {
            catalog = config ?? WeaponCatalogConfig.CreateDefault();
            equippedWeapons.Clear();

            foreach (var weaponId in catalog.GetDefaultWeaponIds())
            {
                var weapon = catalog.FindWeapon(weaponId);
                if (weapon != null)
                {
                    EquipWeaponInternal(weapon);
                }
            }

            if (equippedWeapons.Count == 0)
            {
                EquipWeaponInternal(catalog.GetFirstWeapon());
            }

            RefreshLegacyFields();
        }

        private void Update()
        {
            if (!GameManager.IsSimulationRunning)
            {
                return;
            }

            ApplyPassiveTick(Time.deltaTime);

            foreach (var weapon in equippedWeapons)
            {
                weapon.Cooldown -= Time.deltaTime;
                if (weapon.Cooldown > 0f)
                {
                    continue;
                }

                FireWeapon(weapon);
                weapon.Cooldown = GetFireInterval(weapon);
            }

        }

        public bool CanApplyUpgrade(UpgradeOptionConfig option)
        {
            if (option == null)
            {
                return false;
            }

            var effectType = NormalizeEffectType(option);
            return effectType switch
            {
                "equip_weapon" => CanEquipWeapon(option.weaponId) || CanEnhanceWeapon(option.weaponId),
                "random_weapon" => CanEquipAnyWeapon(),
                "enhance_weapon" => CanEnhanceWeapon(option.weaponId),
                "weapon_fire_rate" => HasAnyWeapon && CanReduceFireIntervalPercent(option.value),
                "projectile_count" => HasProjectileWeapon() && Mathf.RoundToInt(option.value) > 0,
                "weapon_damage" or "weapon_range" or "projectile_speed" or "projectile_scale" => HasAnyWeapon,
                _ => true
            };
        }

        public bool EquipWeapon(string weaponId)
        {
            var weapon = catalog.FindWeapon(weaponId);
            if (weapon == null)
            {
                Debug.LogWarning($"Weapon not found: {weaponId}", this);
                return false;
            }

            var equipped = FindEquippedWeapon(weapon.id);
            if (equipped != null)
            {
                return EnhanceWeapon(weapon.id);
            }

            if (equippedWeapons.Count >= MaxEquippedWeapons)
            {
                return false;
            }

            EquipWeaponInternal(weapon);
            RefreshLegacyFields();
            return true;
        }

        public bool EquipRandomWeapon()
        {
            if (equippedWeapons.Count >= MaxEquippedWeapons)
            {
                return false;
            }

            for (var i = 0; i < 30; i++)
            {
                var weapon = catalog.GetRandomWeaponByQualityWeight();
                if (weapon != null && FindEquippedWeapon(weapon.id) == null)
                {
                    EquipWeaponInternal(weapon);
                    RefreshLegacyFields();
                    return true;
                }
            }

            if (catalog.weapons != null)
            {
                foreach (var weapon in catalog.weapons)
                {
                    if (weapon != null && FindEquippedWeapon(weapon.id) == null)
                    {
                        EquipWeaponInternal(weapon);
                        RefreshLegacyFields();
                        return true;
                    }
                }
            }

            return false;
        }

        public bool EnhanceWeapon(string weaponId = null, int levels = 1)
        {
            var weapon = string.IsNullOrWhiteSpace(weaponId) ? FindFirstEnhanceableWeapon() : FindEquippedWeapon(weaponId);
            if (!EnhanceWeapon(weapon, levels))
            {
                return false;
            }

            RefreshLegacyFields();
            return true;
        }

        public bool EnhanceAllWeapons(int levels = 1)
        {
            var applied = false;
            foreach (var weapon in equippedWeapons)
            {
                applied |= EnhanceWeapon(weapon, levels);
            }

            if (applied)
            {
                RefreshLegacyFields();
            }

            return applied;
        }

        public bool CanEquipWeapon(string weaponId)
        {
            return equippedWeapons.Count < MaxEquippedWeapons &&
                catalog.FindWeapon(weaponId) != null &&
                FindEquippedWeapon(weaponId) == null;
        }

        public bool CanEquipAnyWeapon()
        {
            if (equippedWeapons.Count >= MaxEquippedWeapons || catalog.weapons == null)
            {
                return false;
            }

            foreach (var weapon in catalog.weapons)
            {
                if (weapon != null && FindEquippedWeapon(weapon.id) == null)
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanEnhanceWeapon(string weaponId = null)
        {
            var weapon = string.IsNullOrWhiteSpace(weaponId) ? FindFirstEnhanceableWeapon() : FindEquippedWeapon(weaponId);
            return CanEnhanceWeapon(weapon);
        }

        public EquippedWeaponView[] GetEquippedWeaponViews()
        {
            var views = new EquippedWeaponView[equippedWeapons.Count];
            for (var i = 0; i < equippedWeapons.Count; i++)
            {
                var weapon = equippedWeapons[i];
                var config = weapon.Config;
                var quality = catalog.FindQuality(config.quality);
                views[i] = new EquippedWeaponView(
                    config.id,
                    ConfigCenter.Text(config.nameKey),
                    ConfigCenter.Text(quality.displayNameKey),
                    ConfigCenter.Text(GetSchoolNameKey(config.school)),
                    BuildAffixDisplay(weapon),
                    weapon.EnhancementLevel,
                    GetDamage(weapon),
                    GetFireInterval(weapon),
                    GetRange(weapon),
                    GetSetPieceCount(config.school));
            }

            return views;
        }

        public void AddBaseDamage(float amount)
        {
            flatDamageBonus += amount;
            RefreshLegacyFields();
        }

        public bool ReduceFireIntervalPercent(float percent)
        {
            if (!CanReduceFireIntervalPercent(percent))
            {
                return false;
            }

            var previousMultiplier = fireIntervalMultiplier;
            fireIntervalMultiplier = Mathf.Max(0.2f, fireIntervalMultiplier * Mathf.Clamp01(1f - percent));
            if (Mathf.Approximately(previousMultiplier, fireIntervalMultiplier))
            {
                return false;
            }

            RefreshLegacyFields();
            return true;
        }

        public bool CanReduceFireIntervalPercent(float percent)
        {
            return percent > 0f && fireIntervalMultiplier > 0.2f + Mathf.Epsilon;
        }

        public void AddRange(float amount)
        {
            rangeBonus += amount;
            RefreshLegacyFields();
        }

        public void AddProjectileSpeed(float amount)
        {
            projectileSpeedBonus += amount;
            RefreshLegacyFields();
        }

        public void AddProjectileScale(float amount)
        {
            projectileScaleBonus += amount;
            RefreshLegacyFields();
        }

        public bool AddProjectileCount(int amount)
        {
            if (amount <= 0 || !HasProjectileWeapon())
            {
                return false;
            }

            projectileCountBonus += amount;
            return true;
        }

        private static string NormalizeEffectType(UpgradeOptionConfig option)
        {
            var effectType = option.effectType?.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(effectType))
            {
                return effectType;
            }

            return option.id?.Trim().ToLowerInvariant() switch
            {
                "sharpened_sword" => "weapon_damage",
                "quickened_blade" => "weapon_fire_rate",
                _ => string.Empty
            };
        }

        private void EquipWeaponInternal(WeaponConfig weapon)
        {
            if (weapon == null || equippedWeapons.Count >= MaxEquippedWeapons)
            {
                return;
            }

            var state = new EquippedWeaponState(this, weapon);
            RollAffixes(state);
            equippedWeapons.Add(state);

            if (stats != null && weapon.experienceGainBonus > 0f)
            {
                stats.AddExperienceGainMultiplier(weapon.experienceGainBonus);
            }
        }

        private void RollAffixes(EquippedWeaponState weapon)
        {
            var quality = catalog.FindQuality(weapon.Config.quality);
            var slots = Mathf.Max(0, quality.affixSlots);
            if (quality.daoRhyme && slots == 0)
            {
                slots = 1;
            }

            var pool = catalog.GetAffixesFor(weapon.Config.quality);
            for (var i = 0; i < slots && pool.Length > 0; i++)
            {
                var affix = pool[Random.Range(0, pool.Length)];
                if (affix == null || weapon.AffixIds.Contains(affix.id))
                {
                    continue;
                }

                weapon.AffixIds.Add(affix.id);
            }

            if (quality.daoRhyme && !weapon.AffixIds.Contains("dao_rhyme") && catalog.FindAffix("dao_rhyme") != null)
            {
                weapon.AffixIds.Add("dao_rhyme");
            }
        }

        private void ApplyPassiveTick(float deltaTime)
        {
            if (stats == null || deltaTime <= 0f)
            {
                return;
            }

            foreach (var weapon in equippedWeapons)
            {
                var regen = Mathf.Max(0f, weapon.Config.healthRegenPercentPerSecond);
                if (regen > 0f)
                {
                    stats.Heal(stats.MaxHealth * regen * deltaTime);
                }
            }
        }

        private void FireWeapon(EquippedWeaponState weapon)
        {
            PerformAttack(weapon);

            var extraAttackChance = GetAffixValue(weapon, "extra_attack_chance");
            if (extraAttackChance > 0f && Random.value < extraAttackChance)
            {
                PerformAttack(weapon);
            }
        }

        private void PerformAttack(EquippedWeaponState weapon)
        {
            switch (weapon.Config.attackPattern)
            {
                case WeaponAttackPattern.BurstProjectiles:
                case WeaponAttackPattern.Projectile:
                    FireProjectiles(weapon);
                    break;
                case WeaponAttackPattern.Cone:
                    DamageCone(weapon);
                    break;
                case WeaponAttackPattern.AreaAtTarget:
                    DamageAreaAtTarget(weapon);
                    break;
                case WeaponAttackPattern.Lightning:
                    DamageLightning(weapon);
                    break;
                case WeaponAttackPattern.Aura:
                    DamageArea(transform.position, GetHitRadius(weapon), weapon);
                    break;
            }
        }

        private void FireProjectiles(EquippedWeaponState weapon)
        {
            var target = FindTarget(weapon.Config.targetMode, GetRange(weapon));
            if (target == null)
            {
                return;
            }

            var count = CalculateProjectileCount(
                weapon.Config.projectileCount,
                projectileCountBonus,
                GetExtraProjectiles(weapon));
            var baseDirection = target.transform.position - transform.position;
            baseDirection.y = 0f;
            if (baseDirection.sqrMagnitude <= 0.01f)
            {
                baseDirection = transform.forward;
            }

            for (var i = 0; i < count; i++)
            {
                var direction = GetSpreadDirection(baseDirection.normalized, weapon.Config.spreadAngle, i, count);
                SpawnProjectile(weapon, direction);
            }
        }

        private void SpawnProjectile(EquippedWeaponState weapon, Vector3 direction)
        {
            var projectileObject = RuntimePrefabCatalog.InstantiatePooled(RuntimePrefabCatalog.Projectile, GameManager.Instance.RunRoot);
            if (projectileObject == null)
            {
                return;
            }

            projectileObject.name = ConfigCenter.Text(weapon.Config.nameKey);
            projectileObject.transform.position = transform.position + Vector3.up * 0.7f;
            projectileObject.transform.localScale = Vector3.one * GetProjectileScale(weapon);
            if (projectileObject.TryGetComponent<Renderer>(out var renderer))
            {
                RuntimePrefabCatalog.SetRendererColor(renderer, weapon.Config.projectileColor);
            }

            var projectile = projectileObject.GetComponent<Projectile>();
            var hitDamage = RollDamage(weapon);
            projectile.Launch(
                direction,
                hitDamage,
                Mathf.Max(0f, weapon.Config.projectileSpeed + projectileSpeedBonus),
                Mathf.Max(0.05f, weapon.Config.projectileLifetime),
                Mathf.Max(1, weapon.Config.maxProjectileHits),
                weapon.Config.areaOnImpact ? GetHitRadius(weapon) : 0f,
                weapon.ProjectileHitHandler);
        }

        private void DamageCone(EquippedWeaponState weapon)
        {
            var target = FindTarget(weapon.Config.targetMode, GetRange(weapon));
            if (target == null)
            {
                return;
            }

            var forward = target.transform.position - transform.position;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.01f)
            {
                return;
            }

            var enemies = EnemyHealth.ActiveEnemies;
            var range = GetRange(weapon);
            var rangeSqr = range * range;
            var normalizedForward = forward.normalized;
            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                if (enemy == null)
                {
                    continue;
                }

                var toEnemy = enemy.transform.position - transform.position;
                toEnemy.y = 0f;
                if (toEnemy.sqrMagnitude > rangeSqr)
                {
                    continue;
                }

                var angle = Vector3.Angle(normalizedForward, toEnemy.normalized);
                if (angle <= weapon.Config.coneAngle * 0.5f)
                {
                    DamageEnemy(enemy, weapon);
                }
            }
        }

        private void DamageAreaAtTarget(EquippedWeaponState weapon)
        {
            var target = FindTarget(weapon.Config.targetMode, GetRange(weapon));
            if (target == null)
            {
                return;
            }

            DamageArea(target.transform.position, GetHitRadius(weapon), weapon);
        }

        private void DamageLightning(EquippedWeaponState weapon)
        {
            var count = CalculateLightningTargetCount(
                weapon.Config.targetCount,
                weapon.Config.chainCount,
                GetExtraChains(weapon));
            DamageTargets(FindTargets(weapon.Config.targetMode, count, GetRange(weapon)), weapon);
        }

        private static int CalculateLightningTargetCount(int targetCount, int chainCount, int extraChains)
        {
            return Mathf.Max(1, targetCount) + Mathf.Max(0, chainCount) + Mathf.Max(0, extraChains);
        }

        private void DamageArea(Vector3 center, float radius, EquippedWeaponState weapon)
        {
            var enemies = EnemyHealth.ActiveEnemies;
            var radiusSqr = radius * radius;
            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                if (enemy == null)
                {
                    continue;
                }

                if ((enemy.transform.position - center).sqrMagnitude <= radiusSqr)
                {
                    DamageEnemy(enemy, weapon);
                }
            }
        }

        private void DamageTargets(List<EnemyHealth> targets, EquippedWeaponState weapon)
        {
            foreach (var target in targets)
            {
                DamageEnemy(target, weapon);
            }
        }

        private void DamageEnemy(EnemyHealth enemy, EquippedWeaponState weapon)
        {
            DamageEnemy(enemy, weapon, RollDamage(weapon));
        }

        private void DamageEnemy(EnemyHealth enemy, EquippedWeaponState weapon, float damage)
        {
            if (enemy == null)
            {
                return;
            }

            if (damage <= 0f)
            {
                return;
            }

            enemy.TakeDamage(damage);

            var dotDamagePerSecond = GetDotDamagePerSecond(weapon);
            if (enemy.Health > 0f && dotDamagePerSecond > 0f && weapon.Config.dotDuration > 0f)
            {
                enemy.ApplyDamageOverTime(dotDamagePerSecond, weapon.Config.dotDuration);
            }

            var lifeSteal = GetAffixValue(weapon, "life_steal");
            if (stats != null && lifeSteal > 0f)
            {
                stats.Heal(damage * lifeSteal);
            }
        }

        private EnemyHealth FindTarget(WeaponTargetMode targetMode, float range)
        {
            var targets = FindTargets(targetMode, 1, range);
            return targets.Count > 0 ? targets[0] : null;
        }

        private List<EnemyHealth> FindTargets(WeaponTargetMode targetMode, int count, float range)
        {
            selectedTargets.Clear();
            targetCandidates.Clear();

            var enemies = EnemyHealth.ActiveEnemies;
            var rangeSqr = range * range;

            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || (enemy.transform.position - transform.position).sqrMagnitude > rangeSqr)
                {
                    continue;
                }

                targetCandidates.Add(enemy);
            }

            count = Mathf.Min(Mathf.Max(1, count), targetCandidates.Count);
            while (selectedTargets.Count < count && targetCandidates.Count > 0)
            {
                var index = targetMode switch
                {
                    WeaponTargetMode.Random => Random.Range(0, targetCandidates.Count),
                    WeaponTargetMode.Strongest => FindStrongestIndex(targetCandidates),
                    _ => FindNearestIndex(targetCandidates)
                };

                selectedTargets.Add(targetCandidates[index]);
                targetCandidates.RemoveAt(index);
            }

            return selectedTargets;
        }

        private int FindNearestIndex(List<EnemyHealth> enemies)
        {
            var nearestIndex = 0;
            var nearestDistance = float.MaxValue;
            for (var i = 0; i < enemies.Count; i++)
            {
                var distance = (enemies[i].transform.position - transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        private static int FindStrongestIndex(List<EnemyHealth> enemies)
        {
            var strongestIndex = 0;
            var strongestHealth = float.MinValue;
            for (var i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].Health > strongestHealth)
                {
                    strongestHealth = enemies[i].Health;
                    strongestIndex = i;
                }
            }

            return strongestIndex;
        }

        private float RollDamage(EquippedWeaponState weapon)
        {
            var damage = GetDamage(weapon);
            var critChance = stats != null ? stats.CritChance : 0f;
            critChance += GetSetCritChance(weapon);
            if (critChance > 0f && Random.value < Mathf.Clamp01(critChance))
            {
                damage *= 2f + GetAffixValue(weapon, "crit_damage");
            }

            return damage;
        }

        private float GetDamage(EquippedWeaponState weapon)
        {
            return ScaleDamage(weapon, weapon.Config.baseDamage + flatDamageBonus);
        }

        private float GetDotDamagePerSecond(EquippedWeaponState weapon)
        {
            return ScaleDamage(weapon, weapon.Config.dotDamagePerSecond);
        }

        private float ScaleDamage(EquippedWeaponState weapon, float baseDamage)
        {
            var damage = Mathf.Max(0f, baseDamage);
            damage *= stats != null ? stats.DamageMultiplier : 1f;
            damage *= 1f + Mathf.Max(0, weapon.EnhancementLevel) * Mathf.Max(0f, catalog.enhancementDamageBonusPerLevel);
            damage *= 1f + GetAffixValue(weapon, "damage_multiplier");
            damage *= GetSetDamageMultiplier(weapon);
            return damage;
        }

        private float GetFireInterval(EquippedWeaponState weapon)
        {
            var interval = Mathf.Max(0.05f, weapon.Config.fireInterval) * fireIntervalMultiplier;
            interval *= Mathf.Max(0.25f, 1f - weapon.EnhancementLevel * Mathf.Max(0f, catalog.enhancementCooldownReductionPerLevel));
            interval *= GetSetFireIntervalMultiplier(weapon);

            var symbiosis = GetAffixValue(weapon, "low_health_fire_rate");
            if (stats != null && symbiosis > 0f && stats.MaxHealth > 0f)
            {
                var missingHealth = 1f - Mathf.Clamp01(stats.Health / stats.MaxHealth);
                interval *= Mathf.Max(0.25f, 1f - missingHealth * symbiosis);
            }

            return Mathf.Max(0.08f, interval);
        }

        private float GetRange(EquippedWeaponState weapon)
        {
            return Mathf.Max(0f, weapon.Config.range + rangeBonus) * GetSetRangeMultiplier(weapon);
        }

        private float GetHitRadius(EquippedWeaponState weapon)
        {
            return Mathf.Max(0.1f, weapon.Config.hitRadius) * GetSetRangeMultiplier(weapon);
        }

        private float GetProjectileScale(EquippedWeaponState weapon)
        {
            return Mathf.Max(0.05f, weapon.Config.projectileScale + projectileScaleBonus);
        }

        private int GetSetPieceCount(WeaponSchool school)
        {
            var count = 0;
            foreach (var weapon in equippedWeapons)
            {
                if (weapon.Config.school == school)
                {
                    count++;
                }
            }

            return count;
        }

        private WeaponSetBonusConfig GetActiveSet(WeaponSchool school)
        {
            return GetSetPieceCount(school) >= 2 ? catalog.FindSet(school) : null;
        }

        private float GetSetDamageMultiplier(EquippedWeaponState weapon)
        {
            var set = GetActiveSet(weapon.Config.school);
            return set != null ? Mathf.Max(0f, set.twoPieceDamageMultiplier) : 1f;
        }

        private float GetSetFireIntervalMultiplier(EquippedWeaponState weapon)
        {
            var set = GetActiveSet(weapon.Config.school);
            return set != null ? Mathf.Max(0.1f, set.twoPieceFireIntervalMultiplier) : 1f;
        }

        private float GetSetRangeMultiplier(EquippedWeaponState weapon)
        {
            var set = GetActiveSet(weapon.Config.school);
            return set != null ? Mathf.Max(0.1f, set.twoPieceRangeMultiplier) : 1f;
        }

        private float GetSetCritChance(EquippedWeaponState weapon)
        {
            var set = GetActiveSet(weapon.Config.school);
            return set != null ? Mathf.Max(0f, set.twoPieceCritChance) : 0f;
        }

        private int GetExtraProjectiles(EquippedWeaponState weapon)
        {
            var set = catalog.FindSet(weapon.Config.school);
            return set != null && GetSetPieceCount(weapon.Config.school) >= 3 ? Mathf.Max(0, set.threePieceExtraProjectiles) : 0;
        }

        private bool HasProjectileWeapon()
        {
            foreach (var weapon in equippedWeapons)
            {
                if (SupportsProjectileCount(weapon.Config.attackPattern))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SupportsProjectileCount(WeaponAttackPattern attackPattern)
        {
            return attackPattern == WeaponAttackPattern.Projectile ||
                attackPattern == WeaponAttackPattern.BurstProjectiles;
        }

        private static int CalculateProjectileCount(int baseCount, int upgradeBonus, int setBonus)
        {
            return Mathf.Max(1, baseCount + Mathf.Max(0, upgradeBonus) + Mathf.Max(0, setBonus));
        }

        private int GetExtraChains(EquippedWeaponState weapon)
        {
            var set = catalog.FindSet(weapon.Config.school);
            return set != null && GetSetPieceCount(weapon.Config.school) >= 3 ? Mathf.Max(0, set.threePieceExtraChains) : 0;
        }

        private float GetAffixValue(EquippedWeaponState weapon, string effectType)
        {
            var value = 0f;
            foreach (var affixId in weapon.AffixIds)
            {
                var affix = catalog.FindAffix(affixId);
                if (affix != null && affix.effectType == effectType)
                {
                    value += affix.value;
                }
            }

            return value;
        }

        private bool CanEnhanceWeapon(EquippedWeaponState weapon)
        {
            if (weapon == null)
            {
                return false;
            }

            return weapon.EnhancementLevel < catalog.GetMaxEnhancementLevel(weapon.Config, weapon.AffixIds);
        }

        private bool EnhanceWeapon(EquippedWeaponState weapon, int levels)
        {
            if (weapon == null || levels <= 0)
            {
                return false;
            }

            var applied = false;
            for (var i = 0; i < levels && CanEnhanceWeapon(weapon); i++)
            {
                weapon.EnhancementLevel++;
                applied = true;
            }

            return applied;
        }

        private EquippedWeaponState FindFirstEnhanceableWeapon()
        {
            foreach (var weapon in equippedWeapons)
            {
                if (CanEnhanceWeapon(weapon))
                {
                    return weapon;
                }
            }

            return null;
        }

        private EquippedWeaponState FindEquippedWeapon(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return null;
            }

            foreach (var weapon in equippedWeapons)
            {
                if (weapon.Config.id == weaponId)
                {
                    return weapon;
                }
            }

            return null;
        }

        private string BuildAffixDisplay(EquippedWeaponState weapon)
        {
            if (weapon.AffixIds.Count == 0)
            {
                return string.Empty;
            }

            var names = new string[weapon.AffixIds.Count];
            for (var i = 0; i < weapon.AffixIds.Count; i++)
            {
                var affix = catalog.FindAffix(weapon.AffixIds[i]);
                names[i] = affix != null ? ConfigCenter.Text(affix.displayNameKey) : weapon.AffixIds[i];
            }

            return string.Join(", ", names);
        }

        private void RefreshLegacyFields()
        {
            if (equippedWeapons.Count == 0)
            {
                return;
            }

            var weapon = equippedWeapons[0];
            FireInterval = GetFireInterval(weapon);
            Range = GetRange(weapon);
            BaseDamage = Mathf.Max(0f, weapon.Config.baseDamage + flatDamageBonus);
            ProjectileSpeed = Mathf.Max(0f, weapon.Config.projectileSpeed + projectileSpeedBonus);
            ProjectileLifetime = Mathf.Max(0.05f, weapon.Config.projectileLifetime);
            ProjectileScale = GetProjectileScale(weapon);
            ProjectileName = ConfigCenter.Text(weapon.Config.nameKey);
            ProjectileColor = weapon.Config.projectileColor;
        }

        private static Vector3 GetSpreadDirection(Vector3 baseDirection, float spreadAngle, int index, int count)
        {
            if (count <= 1 || spreadAngle <= 0f)
            {
                return baseDirection;
            }

            var angle = spreadAngle >= 359f
                ? 360f * index / count
                : Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, (float)index / (count - 1));
            return Quaternion.Euler(0f, angle, 0f) * baseDirection;
        }

        private static string GetSchoolNameKey(WeaponSchool school)
        {
            return school switch
            {
                WeaponSchool.Fire => "weapon.school.fire",
                WeaponSchool.Thunder => "weapon.school.thunder",
                WeaponSchool.Poison => "weapon.school.poison",
                WeaponSchool.Body => "weapon.school.body",
                WeaponSchool.Formation => "weapon.school.formation",
                WeaponSchool.Water => "weapon.school.water",
                _ => "weapon.school.sword"
            };
        }

        private sealed class EquippedWeaponState
        {
            private readonly AutoWeapon owner;

            public readonly WeaponConfig Config;
            public readonly List<string> AffixIds = new();
            public readonly System.Action<EnemyHealth, float> ProjectileHitHandler;
            public int EnhancementLevel;
            public float Cooldown;

            public EquippedWeaponState(AutoWeapon owner, WeaponConfig config)
            {
                this.owner = owner;
                Config = config;
                ProjectileHitHandler = HandleProjectileHit;
                Cooldown = Random.Range(0f, Mathf.Max(0.05f, config.fireInterval));
            }

            private void HandleProjectileHit(EnemyHealth enemy, float damage)
            {
                owner.DamageEnemy(enemy, this, damage);
            }
        }
    }

    public readonly struct EquippedWeaponView
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string QualityName;
        public readonly string SchoolName;
        public readonly string Affixes;
        public readonly int EnhancementLevel;
        public readonly float CurrentDamage;
        public readonly float FireInterval;
        public readonly float Range;
        public readonly int SetPieceCount;

        public EquippedWeaponView(
            string id,
            string name,
            string qualityName,
            string schoolName,
            string affixes,
            int enhancementLevel,
            float currentDamage,
            float fireInterval,
            float range,
            int setPieceCount)
        {
            Id = id;
            Name = name;
            QualityName = qualityName;
            SchoolName = schoolName;
            Affixes = affixes;
            EnhancementLevel = enhancementLevel;
            CurrentDamage = currentDamage;
            FireInterval = fireInterval;
            Range = range;
            SetPieceCount = setPieceCount;
        }
    }
}
