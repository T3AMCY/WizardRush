using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Player : MonoBehaviour
{
    #region Members

    #region Status
    public enum Status
    {
        Alive = 1,
        Dead,
        Attacking,
    }

    private Status _playerStatus;
    public Status playerStatus
    {
        get { return _playerStatus; }
        set { _playerStatus = value; }
    }

    #endregion

    #region References
    private Vector2 moveInput;
    private Vector2 velocity;
    public StatSystem playerStat;
    public Rigidbody2D rb;
    public List<Skill> skills;
    public Inventory inventory;
    public PlayerInput playerInput;

    public Action<float, float> OnHpChanged;
    #endregion

    #endregion

    public bool IsInitialized { get; private set; }

    public void Initialize(StatData saveData, InventoryData inventoryData)
    {
        gameObject.name = "Player";
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        playerStat.Initialize(saveData);
        inventory.Initialize(this, inventoryData);
        playerInput.Initialize(this);
        StartCombatSystems();
        IsInitialized = true;
    }

    private void OnDisable()
    {
        CleanupPlayer();
    }

    private void CleanupPlayer()
    {
        StopAllCoroutines();

        if (autoAttackCoroutine != null)
        {
            StopCoroutine(autoAttackCoroutine);
            autoAttackCoroutine = null;
        }

        if (healthRegenCoroutine != null)
        {
            StopCoroutine(healthRegenCoroutine);
            healthRegenCoroutine = null;
        }

        if (skills != null)
        {
            foreach (var skill in skills)
            {
                if (skill != null)
                {
                    Destroy(skill.gameObject);
                }
            }
            skills.Clear();
        }

        playerInput.Cleanup();

        playerStatus = Status.Dead;
        IsInitialized = false;
    }

    public void StartCombatSystems()
    {
        if (playerStatus != Status.Dead)
        {
            if (playerStat == null)
            {
                Logger.LogError(typeof(Player), "PlayerStat is null!");
                return;
            }

            StartHealthRegeneration();
            StartAutoAttack();
        }
    }

    private void FixedUpdate()
    {
        Move();
    }

    #region Methods

    #region Move&Skills
    public void Move()
    {
        float moveSpeed = playerStat.GetStat(StatType.MoveSpeed);
        velocity = moveInput * moveSpeed;

        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    public void SetMoveInput(Vector2 moveDirection)
    {
        moveInput = moveDirection;
    }

    #endregion

    #endregion

    #region Interactions

    public void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.TryGetComponent<IContactable>(out var contact))
        {
            contact.Contact();
        }

        if (other.gameObject.CompareTag("Enemy"))
        {
            rb.constraints = RigidbodyConstraints2D.FreezePosition;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.TryGetComponent<IContactable>(out var contact))
        {
            contact.Contact();
        }
    }

    public void TakeHeal(float heal)
    {
        float currentHp = playerStat.GetStat(StatType.CurrentHp);
        float maxHp = playerStat.GetStat(StatType.MaxHp);

        currentHp = Mathf.Min(currentHp + heal, maxHp);
        playerStat.SetCurrentHp(currentHp);
        OnHpChanged?.Invoke(currentHp, maxHp);
    }

    public void TakeDamage(float damage)
    {
        float currentHp = playerStat.GetStat(StatType.CurrentHp);
        float maxHp = playerStat.GetStat(StatType.MaxHp);
        currentHp -= damage;
        playerStat.SetCurrentHp(currentHp);

        OnHpChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        playerStatus = Status.Dead;
        StopAllCoroutines();
    }

    #endregion

    #region Combat
    private Coroutine autoAttackCoroutine;
    private float attackAngle = 120f;

    private void PerformAttack(Monster targetEnemy)
    {
        Vector2 directionToTarget = (
            targetEnemy.transform.position - transform.position
        ).normalized;

        playerStatus = Status.Attacking;

        float attackRange = playerStat.GetStat(StatType.AttackRange);
        float damage = playerStat.GetStat(StatType.Damage);

        var enemiesInRange = GameManager
            .Instance.Monsters.Where(enemy => enemy != null)
            .Where(enemy =>
            {
                Vector2 directionToEnemy = enemy.transform.position - transform.position;
                float distanceToEnemy = directionToEnemy.magnitude;
                float angle = Vector2.Angle(directionToTarget, directionToEnemy);

                return distanceToEnemy <= attackRange && angle <= attackAngle / 2f;
            })
            .ToList();

        foreach (Monster enemy in enemiesInRange)
        {
            float random = Random.Range(0f, 100f);

            if (random <= playerStat.GetStat(StatType.CriticalChance))
            {
                damage *= playerStat.GetStat(StatType.CriticalDamage);
            }

            enemy.TakeDamage(damage);
            playerStat.SetCurrentHp(
                playerStat.GetStat(StatType.CurrentHp)
                    + playerStat.GetStat(StatType.LifeSteal) / 100 * damage
            );
        }
    }

    private Monster FindNearestEnemy()
    {
        return GameManager
            .Instance.Monsters?.Where(enemy => enemy != null)
            .OrderBy(enemy => Vector2.Distance(transform.position, enemy.transform.position))
            .FirstOrDefault();
    }

    #endregion

    #region Passive Skill Effects
    public void ActivateHoming(bool activate)
    {
        foreach (var skill in skills)
        {
            if (skill is ProjectileSkills ProjectileSkills)
            {
                ProjectileSkills.UpdateHomingState(activate);
            }
        }
    }

    public void ResetPassiveEffects()
    {
        var passiveSkills = skills.Where(skill => skill is PassiveSkill).ToList();
        foreach (var skill in passiveSkills)
        {
            var passiveSkill = skill as PassiveSkill;
            foreach (var modifier in passiveSkill.statModifiers)
            {
                playerStat.RemoveModifier(modifier);
            }
        }
    }
    #endregion

    #region Health Regeneration
    private Coroutine healthRegenCoroutine;
    private const float REGEN_TICK_RATE = 1f;

    private void StartHealthRegeneration()
    {
        if (healthRegenCoroutine != null)
            StopCoroutine(healthRegenCoroutine);

        healthRegenCoroutine = StartCoroutine(HealthRegenCoroutine());
    }

    private IEnumerator HealthRegenCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(REGEN_TICK_RATE);

        while (true)
        {
            if (playerStat != null)
            {
                float regenAmount = playerStat.GetStat(StatType.HpRegenRate);
                if (regenAmount > 0)
                {
                    TakeHeal(regenAmount);
                }
            }
            yield return wait;
        }
    }
    #endregion

    #region Skills
    public bool AddOrUpgradeSkill(SkillData skillData)
    {
        if (skillData == null)
            return false;
        GameManager.Instance.SkillSystem.AddOrUpgradeSkill(skillData);
        return true;
    }

    public void RemoveSkill(SkillID skillID)
    {
        var skillToRemove = skills.Find(s => s.skillData.ID == skillID);
        if (skillToRemove != null)
        {
            skills.Remove(skillToRemove);
            Destroy(skillToRemove.gameObject);
        }
    }
    #endregion

    private void StartAutoAttack()
    {
        if (autoAttackCoroutine != null)
        {
            StopCoroutine(autoAttackCoroutine);
            autoAttackCoroutine = null;
        }
        autoAttackCoroutine = StartCoroutine(AutoAttackCoroutine());
    }

    private IEnumerator AutoAttackCoroutine()
    {
        while (true)
        {
            if (playerStatus != Status.Dead)
            {
                Monster nearestEnemy = FindNearestEnemy();
                if (nearestEnemy != null)
                {
                    float distanceToEnemy = Vector2.Distance(
                        transform.position,
                        nearestEnemy.transform.position
                    );
                    float attackRange = playerStat.GetStat(StatType.AttackRange);

                    if (distanceToEnemy <= attackRange)
                    {
                        PerformAttack(nearestEnemy);
                    }
                }
            }

            float attackDelay = 1f / playerStat.GetStat(StatType.AttackSpeed);
            yield return new WaitForSeconds(attackDelay);
        }
    }
}
