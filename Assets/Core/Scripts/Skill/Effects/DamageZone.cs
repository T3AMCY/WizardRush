using System.Collections.Generic;
using UnityEngine;

public class DamageZone : MonoBehaviour
{
    private float damage;
    private float tickRate;
    private new ParticleSystem particleSystem;
    private Dictionary<GameObject, float> lastDamageTime = new Dictionary<GameObject, float>();

    private void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
    }

    public void Initialize(float damage, float duration, float tickRate, float width)
    {
        this.damage = damage;
        this.tickRate = tickRate;

        // ������Ʈ ������ ����
        transform.localScale = new Vector3(width, width, 1f);

        if (particleSystem != null)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particleSystem.main;
            main.duration = duration;
            particleSystem.Play();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (!lastDamageTime.ContainsKey(other.gameObject))
            {
                lastDamageTime[other.gameObject] = -tickRate;
            }

            if (Time.time >= lastDamageTime[other.gameObject] + tickRate)
            {
                Monster enemy = other.GetComponent<Monster>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
                lastDamageTime[other.gameObject] = Time.time;
            }
        }
    }

    private void OnDisable()
    {
        lastDamageTime.Clear();
        if (particleSystem != null)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
