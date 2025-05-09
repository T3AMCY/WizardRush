using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Monster : MonoBehaviour
{
    protected MonsterData monsterData;
    protected MonsterSetting monsterSetting;
    private Transform target;
    public Transform Target => target;
    public Rigidbody2D rb;
    public ParticleSystem attackParticle;
    public Collider2D enemyCollider;
    public StatSystem stat;
    protected float lastAttackTime;
    public bool isStunned = false;
    public bool isAttacking = false;
    public bool isInitialized = false;
    protected Coroutine slowEffectCoroutine;
    protected Coroutine stunCoroutine;
    protected Coroutine dotDamageCoroutine;
    protected Coroutine defenseDebuffCoroutine;
    public Slider hpBar;

    #region Unity Lifecycle

    public virtual void Initialize(MonsterData monsterData, MonsterSetting monsterSetting)
    {
        this.monsterData = monsterData;
        this.monsterSetting = monsterSetting;
        stat.Initialize(monsterData.statData);
        enemyCollider = GetComponent<Collider2D>();
        if (GameManager.Instance?.PlayerSystem?.Player != null)
        {
            target = GameManager.Instance.PlayerSystem.Player.transform;
            isInitialized = true;
        }
        if (
            Application.isPlaying
            && GameManager.Instance != null
            && !GameManager.Instance.Monsters.Contains(this)
        )
        {
            GameManager.Instance.Monsters.Add(this);
        }

        hpBar.maxValue = stat.GetStat(StatType.MaxHp);
        hpBar.value = stat.GetStat(StatType.CurrentHp);
    }

    protected virtual void Update()
    {
        if (isInitialized)
        {
            UpdateVisuals();

            float distanceToPlayer = Vector2.Distance(transform.position, Target.position);

            if (distanceToPlayer <= stat.GetStat(StatType.AttackRange))
            {
                Attack();
            }
            else
            {
                isAttacking = false;
            }
        }
    }

    private void FixedUpdate()
    {
        Move();
    }

    public void Move()
    {
        if (!isAttacking)
        {
            rb.MovePosition(target.transform.position);
        }
    }

    protected virtual void OnDisable()
    {
        if (slowEffectCoroutine != null)
        {
            StopCoroutine(slowEffectCoroutine);
            slowEffectCoroutine = null;
        }

        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }

        if (dotDamageCoroutine != null)
        {
            StopCoroutine(dotDamageCoroutine);
            dotDamageCoroutine = null;
        }

        if (defenseDebuffCoroutine != null)
        {
            StopCoroutine(defenseDebuffCoroutine);
            defenseDebuffCoroutine = null;
        }
        stat.RemoveAllModifiers();
        isStunned = false;

        if (GameManager.Instance != null && GameManager.Instance.Monsters.Contains(this))
        {
            GameManager.Instance.Monsters.Remove(this);
        }
    }

    #endregion

    #region Combat
    public virtual void TakeDamage(float damage)
    {
        stat.SetCurrentHp(stat.GetStat(StatType.CurrentHp) - damage);

        if (stat.GetStat(StatType.CurrentHp) <= 0)
        {
            if (dotDamageCoroutine != null)
            {
                StopCoroutine(dotDamageCoroutine);
                dotDamageCoroutine = null;
            }
            Die();
        }
    }

    public virtual void Die()
    {
        DropItems();

        if (GameManager.Instance?.Monsters != null)
        {
            GameManager.Instance.Monsters.Remove(this);
        }

        PoolManager.Instance.Despawn(this);
    }

    protected virtual void DropItems()
    {
        Vector2 dropPosition = CalculateDropPosition();
        GameManager.Instance.ItemSystem.DropItem(dropPosition, monsterData.type);
    }

    protected virtual Vector2 CalculateDropPosition()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(
            monsterSetting.dropRadiusRange.x,
            monsterSetting.dropRadiusRange.y
        );

        Vector2 offset = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

        return (Vector2)transform.position + offset;
    }

    protected virtual void Attack()
    {
        var attackSpeed = stat.GetStat(StatType.AttackSpeed);
        var damageInterval = 1f / attackSpeed;
        if (Time.time >= lastAttackTime + damageInterval)
        {
            float distanceToTarget = Vector2.Distance(transform.position, Target.position);

            if (distanceToTarget <= stat.GetStat(StatType.AttackRange))
            {
                isAttacking = true;
                if (this is RangedMonster || this is BossMonster)
                {
                    PerformRangedAttack();
                }
                else
                {
                    PerformMeleeAttack();
                }
                lastAttackTime = Time.time;
            }
            else
            {
                isAttacking = false;
            }
        }
    }

    protected virtual void PerformMeleeAttack() { }

    protected virtual void PerformRangedAttack() { }

    public virtual void ApplySlowEffect(float amount, float duration)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (slowEffectCoroutine != null)
        {
            StopCoroutine(slowEffectCoroutine);
        }

        slowEffectCoroutine = StartCoroutine(SlowEffectCoroutine(amount, duration));
    }

    protected virtual IEnumerator SlowEffectCoroutine(float amount, float duration)
    {
        float movespeedReduction;

        var moveSpeed = stat.GetStat(StatType.MoveSpeed);
        if (moveSpeed - amount > 0)
        {
            movespeedReduction = -amount;
        }
        else
        {
            movespeedReduction = -moveSpeed;
        }

        var moveSpeedDebuff = new StatModifier(
            StatType.MoveSpeed,
            this,
            CalcType.Flat,
            movespeedReduction
        );

        if (this != null && gameObject.activeInHierarchy)
        {
            stat.AddModifier(moveSpeedDebuff);
        }

        yield return new WaitForSeconds(duration);

        if (this != null && gameObject.activeInHierarchy)
        {
            stat.RemoveModifier(moveSpeedDebuff);
        }

        slowEffectCoroutine = null;
    }

    public virtual void ApplyDotDamage(float damagePerTick, float tickInterval, float duration)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (dotDamageCoroutine != null)
        {
            StopCoroutine(dotDamageCoroutine);
        }

        dotDamageCoroutine = StartCoroutine(
            DotDamageCoroutine(damagePerTick, tickInterval, duration)
        );
    }

    protected virtual IEnumerator DotDamageCoroutine(
        float damagePerTick,
        float tickInterval,
        float duration
    )
    {
        float endTime = Time.time + duration;

        while (
            Time.time < endTime
            && stat.GetStat(StatType.CurrentHp) > 0
            && gameObject.activeInHierarchy
        )
        {
            if (this != null && gameObject.activeInHierarchy)
            {
                TakeDamage(damagePerTick);
            }
            yield return new WaitForSeconds(tickInterval);
        }

        dotDamageCoroutine = null;
    }

    public virtual void ApplyStun(float power, float duration)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
        }

        stunCoroutine = StartCoroutine(StunCoroutine(duration));
    }

    protected virtual IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        var moveSpeed = stat.GetStat(StatType.MoveSpeed);
        var moveSpeedDebuff = new StatModifier(StatType.MoveSpeed, this, CalcType.Flat, -moveSpeed);

        yield return new WaitForSeconds(duration);

        if (this != null && gameObject.activeInHierarchy)
        {
            isStunned = false;
            stat.RemoveModifier(moveSpeedDebuff);
        }
        stunCoroutine = null;
    }
    #endregion

    #region Collision
    public virtual void Contact()
    {
        var particle = Instantiate(attackParticle, Target.position, Quaternion.identity);
        particle.Play();
        Destroy(particle.gameObject, 0.3f);
        Attack();
    }
    #endregion

    #region UI
    protected virtual void UpdateHPBar()
    {
        if (hpBar != null)
        {
            hpBar.value = stat.GetStat(StatType.CurrentHp);
        }
    }

    protected virtual void UpdateVisuals()
    {
        UpdateHPBar();
    }
    #endregion

    #region Utility
    public virtual void SetCollisionState(bool isOutOfView)
    {
        if (enemyCollider != null)
        {
            enemyCollider.enabled = !isOutOfView;
        }
    }
    #endregion
}
