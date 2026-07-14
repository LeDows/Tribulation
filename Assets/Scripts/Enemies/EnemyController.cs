using Tribulation.Combat;
using Tribulation.Config;
using Tribulation.Core;
using Tribulation.Player;
using UnityEngine;

namespace Tribulation.Enemies
{
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyController : MonoBehaviour
    {
        public EnemyBehavior Behavior = EnemyBehavior.Chase;
        public float MoveSpeed = 3.1f;
        public float ContactDamage = 8f;
        public float AttackInterval = 0.75f;
        public float AttackRange = 1.35f;
        public float PreferredRange = 7f;
        public float RetreatRange = 4f;
        public float FleeHealthFraction = 0.3f;
        public float SkillRange = 8f;
        public float SkillCooldown = 4f;
        public float WindupDuration = 0.75f;
        public float SkillDuration = 0.5f;
        public float SkillSpeedMultiplier = 3f;
        public float ProjectileSpeed = 10f;
        public float ProjectileScale = 0.24f;
        public Color ProjectileColor = new(0.45f, 0.75f, 1f, 1f);

        private EnemyHealth health;
        private float attackCooldown;
        private float skillCooldownTimer;
        private float skillStateTimer;
        private Vector3 lockedSkillDirection;
        private bool skillHitPlayer;
        private SkillState skillState;

        private enum SkillState
        {
            None,
            Windup,
            Dash,
            Retreat
        }

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
        }

        public void Configure(EnemyConfig config)
        {
            if (config == null)
            {
                return;
            }

            var minimumSpeed = Mathf.Min(config.minMoveSpeed, config.maxMoveSpeed);
            var maximumSpeed = Mathf.Max(config.minMoveSpeed, config.maxMoveSpeed);
            var runTime = GameManager.Instance != null ? GameManager.Instance.RunTime : 0f;

            Behavior = config.behavior;
            MoveSpeed = Random.Range(minimumSpeed, maximumSpeed) +
                runTime / Mathf.Max(0.01f, config.speedDifficultySeconds);
            ContactDamage = Mathf.Max(0f, config.contactDamage);
            AttackInterval = Mathf.Max(0.05f, config.attackInterval);
            AttackRange = Mathf.Max(0.1f, config.attackRange);
            PreferredRange = Mathf.Max(0f, config.preferredRange);
            RetreatRange = Mathf.Clamp(config.retreatRange, 0f, PreferredRange);
            FleeHealthFraction = Mathf.Clamp01(config.fleeHealthFraction);
            SkillRange = Mathf.Max(AttackRange, config.skillRange);
            SkillCooldown = Mathf.Max(0.05f, config.skillCooldown);
            WindupDuration = Mathf.Max(0f, config.windupDuration);
            SkillDuration = Mathf.Max(0.05f, config.skillDuration);
            SkillSpeedMultiplier = Mathf.Max(0f, config.skillSpeedMultiplier);
            ProjectileSpeed = Mathf.Max(0.1f, config.projectileSpeed);
            ProjectileScale = Mathf.Max(0.05f, config.projectileScale);
            ProjectileColor = config.projectileColor;

            attackCooldown = 0f;
            skillCooldownTimer = 0f;
            skillStateTimer = 0f;
            lockedSkillDirection = Vector3.zero;
            skillHitPlayer = false;
            skillState = SkillState.None;
        }

        private void Update()
        {
            var manager = GameManager.Instance;
            if (manager == null || manager.State != GameState.Running || manager.Player == null)
            {
                return;
            }

            attackCooldown -= Time.deltaTime;
            skillCooldownTimer -= Time.deltaTime;

            var player = manager.Player;
            var toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;

            switch (Behavior)
            {
                case EnemyBehavior.Charger:
                    UpdateCharger(player, toPlayer);
                    break;
                case EnemyBehavior.RangedKite:
                    UpdateRanged(toPlayer, retreatWhenClose: true);
                    break;
                case EnemyBehavior.WoundedFlee:
                    UpdateWoundedFlee(toPlayer);
                    break;
                case EnemyBehavior.Ambusher:
                    UpdateAmbusher(player, toPlayer);
                    break;
                default:
                    Move(toPlayer);
                    TryContactAttack(player, toPlayer.sqrMagnitude);
                    break;
            }
        }

        private void UpdateCharger(PlayerStats player, Vector3 toPlayer)
        {
            if (UpdateActiveSkill(player))
            {
                return;
            }

            if (skillCooldownTimer <= 0f && toPlayer.sqrMagnitude <= SkillRange * SkillRange)
            {
                BeginSkill(toPlayer);
                return;
            }

            Move(toPlayer);
            TryContactAttack(player, toPlayer.sqrMagnitude);
        }

        private void UpdateRanged(Vector3 toPlayer, bool retreatWhenClose)
        {
            var distance = toPlayer.magnitude;
            if (retreatWhenClose && distance < RetreatRange)
            {
                Move(-toPlayer);
            }
            else if (distance > PreferredRange)
            {
                Move(toPlayer);
            }
            else
            {
                Face(toPlayer);
            }

            if (distance <= AttackRange && attackCooldown <= 0f && FireProjectile(toPlayer))
            {
                attackCooldown = AttackInterval;
            }
        }

        private void UpdateWoundedFlee(Vector3 toPlayer)
        {
            if (health != null && health.HealthFraction <= FleeHealthFraction)
            {
                Move(-toPlayer);
                return;
            }

            UpdateRanged(toPlayer, retreatWhenClose: false);
        }

        private void UpdateAmbusher(PlayerStats player, Vector3 toPlayer)
        {
            if (UpdateActiveSkill(player))
            {
                return;
            }

            var distance = toPlayer.magnitude;
            if (skillCooldownTimer <= 0f && distance <= SkillRange)
            {
                BeginSkill(toPlayer);
                return;
            }

            if (distance > SkillRange)
            {
                Move(toPlayer, 0.45f);
            }
            else
            {
                Face(toPlayer);
            }
        }

        private bool UpdateActiveSkill(PlayerStats player)
        {
            if (skillState == SkillState.None)
            {
                return false;
            }

            skillStateTimer -= Time.deltaTime;
            switch (skillState)
            {
                case SkillState.Windup:
                    Face(lockedSkillDirection);
                    if (skillStateTimer <= 0f)
                    {
                        StartDash();
                    }
                    break;
                case SkillState.Dash:
                    Move(lockedSkillDirection, SkillSpeedMultiplier);
                    TrySkillHit(player);
                    if (skillStateTimer <= 0f)
                    {
                        if (Behavior == EnemyBehavior.Ambusher)
                        {
                            skillState = SkillState.Retreat;
                            skillStateTimer = SkillDuration;
                        }
                        else
                        {
                            FinishSkill();
                        }
                    }
                    break;
                case SkillState.Retreat:
                    Move(-lockedSkillDirection, SkillSpeedMultiplier);
                    if (skillStateTimer <= 0f)
                    {
                        FinishSkill();
                    }
                    break;
            }

            return true;
        }

        private void BeginSkill(Vector3 toPlayer)
        {
            lockedSkillDirection = GetDirection(toPlayer);
            skillHitPlayer = false;
            if (WindupDuration > 0f)
            {
                skillState = SkillState.Windup;
                skillStateTimer = WindupDuration;
                Face(lockedSkillDirection);
                return;
            }

            StartDash();
        }

        private void StartDash()
        {
            skillState = SkillState.Dash;
            skillStateTimer = SkillDuration;
        }

        private void FinishSkill()
        {
            skillState = SkillState.None;
            skillStateTimer = 0f;
            skillCooldownTimer = SkillCooldown;
        }

        private void TrySkillHit(PlayerStats player)
        {
            if (skillHitPlayer)
            {
                return;
            }

            var toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > AttackRange * AttackRange)
            {
                return;
            }

            player.TakeDamage(ContactDamage);
            skillHitPlayer = true;
        }

        private void TryContactAttack(PlayerStats player, float distanceSquared)
        {
            if (distanceSquared > AttackRange * AttackRange || attackCooldown > 0f)
            {
                return;
            }

            player.TakeDamage(ContactDamage);
            attackCooldown = AttackInterval;
        }

        private bool FireProjectile(Vector3 toPlayer)
        {
            var manager = GameManager.Instance;
            if (manager == null)
            {
                return false;
            }

            var projectileObject = RuntimePrefabCatalog.InstantiatePooled(RuntimePrefabCatalog.Projectile, manager.RunRoot);
            if (projectileObject == null)
            {
                return false;
            }

            projectileObject.name = gameObject.name;
            projectileObject.transform.position = transform.position + Vector3.up * 0.7f;
            projectileObject.transform.localScale = Vector3.one * ProjectileScale;
            if (projectileObject.TryGetComponent<Renderer>(out var renderer))
            {
                RuntimePrefabCatalog.SetRendererColor(renderer, ProjectileColor);
            }

            var projectile = projectileObject.GetComponent<Projectile>();
            if (projectile == null)
            {
                RuntimePrefabCatalog.ReleasePooled(projectileObject, RuntimePrefabCatalog.Projectile);
                return false;
            }

            var duration = AttackRange / ProjectileSpeed + 0.5f;
            projectile.LaunchAgainstPlayer(GetDirection(toPlayer), ContactDamage, ProjectileSpeed, duration);
            return true;
        }

        private void Move(Vector3 direction, float speedMultiplier = 1f)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var normalized = direction.normalized;
            transform.position += normalized * (MoveSpeed * Mathf.Max(0f, speedMultiplier) * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(normalized, Vector3.up);
        }

        private void Face(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        private Vector3 GetDirection(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        }
    }
}
