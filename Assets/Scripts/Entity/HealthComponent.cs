using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using SmallTimeRogue.Stats;

namespace SmallTimeRogue
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        #region Variables
        [BoxGroup("Read Only")] [ReadOnly] [SerializeField] int _health = 100;
        [BoxGroup("Read Only")] [ReadOnly] [SerializeField] float _lastTimeTookDamage = 0;
        [BoxGroup("Read Only")] [ReadOnly] [SerializeField] bool _dead = false;
        [BoxGroup("Read Only")] [ReadOnly] [SerializeField] bool _damageImmune = false;
        [BoxGroup("Read Only")] [ReadOnly] [SerializeField] bool _collisionDamageImmune = false;
        [BoxGroup("Read Only")] [ReadOnly] [SerializeField] float _collisionDamageImmunityTime = 0f;

        [HideInInspector] public EntityStat maxHealth;

        [BoxGroup("Settings")] public float immunityFrameLength = 0.05f;
        [BoxGroup("Settings")] public float collisionDamageThreshold = 1f;
        [BoxGroup("Settings")] public bool flashOnHit = true;
        [BoxGroup("Settings")] public Color hitColor = Color.red;
        [BoxGroup("Settings")] public SpriteRenderer bodySprite;

        Coroutine _immunityCoroutine;
        Rigidbody2D _rb;

        public Action<DamageInfo> OnTakeDamage;
        public Action<DamageReport> OnDeathAction;
        public Action OnHealthChange;
        #endregion

        #region Getters
        public int Health
        {
            get => _health;
            set
            {
                _health = value;
                OnHealthChange?.Invoke();
            }
        }
        public int MaxHealth => Mathf.RoundToInt(maxHealth.Value);
        public bool DamageImmune => _damageImmune;
        public bool Dead => _dead || _health <= 0;
        public bool CollisionDamageImmune => _collisionDamageImmunityTime >= 0f || _collisionDamageImmune || DamageImmune;
        #endregion

        #region Main Functions
        public DamageReport TakeDamage(DamageInfo damageInfo)
        {
            if (_damageImmune || damageInfo.damage == 0 || _dead) { return new DamageReport(); }
            if (flashOnHit) { StartCoroutine(DoDamagedColor()); }
            ImmunityFrames(immunityFrameLength);

            bool isCrit = UnityEngine.Random.Range(0, 100) < damageInfo.critChance;
            _lastTimeTookDamage = Time.realtimeSinceStartup;
            int damage = isCrit ? Mathf.RoundToInt(damageInfo.damage * (damageInfo.critDamage + 150) / 100f) : damageInfo.damage;

            Health -= damage;
            OnTakeDamage?.Invoke(damageInfo);

            GameEffectsManager.Instance.SpawnFloatingNumber(damage, isCrit, transform.position + Vector3.up);

            DamageReport report = new DamageReport() { damageTaken = damageInfo.damage };
            if (_health <= 0)
            {
                _dead = true;
                OnDeathAction?.Invoke(report);
            }

            return report;

            IEnumerator DoDamagedColor()
            {
                bodySprite.color = hitColor;
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                bodySprite.color = new Color(1, 1, 1, bodySprite.color.a);
            }
        }
        public void KnockBack(Vector2 force)
        {
            _rb.AddForce(force, ForceMode2D.Impulse);
        }
        public void DieSilently()
        {
            _health = 0;
            _dead = true;
        }
        public void SetHealth(int health, int maxHealth)
        {
            this._health = health;
            this.maxHealth.BaseValue = maxHealth;
        }
        public void ImmunityFrames(float time)
        {
            if (_immunityCoroutine != null) { return; }
            _immunityCoroutine = StartCoroutine(ImmunityTime());
            IEnumerator ImmunityTime()
            {
                float remainingTime = time;
                bodySprite.color = new Color(bodySprite.color.r, bodySprite.color.g, bodySprite.color.b, 0.5f);
                _damageImmune = true;
                while (remainingTime >= 0f)
                {
                    yield return new WaitForSeconds(0.1f);
                    bodySprite.color = new Color(bodySprite.color.r, bodySprite.color.g, bodySprite.color.b, 1);
                    remainingTime -= 0.1f;
                    if (remainingTime < 0) { break; }
                    yield return new WaitForSeconds(0.1f);
                    bodySprite.color = new Color(bodySprite.color.r, bodySprite.color.g, bodySprite.color.b, 0.5f);
                    remainingTime -= 0.1f;
                }
                bodySprite.color = new Color(bodySprite.color.r, bodySprite.color.g, bodySprite.color.b, 1);
                _damageImmune = false;
                _immunityCoroutine = null;

            }
        }
        public void AddCollisionDamageImmunityTime(float time)
        {
            if (_collisionDamageImmunityTime <= 0f || _collisionDamageImmunityTime < time) { _collisionDamageImmunityTime = time; }
        }
        #endregion

        public void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") && !CollisionDamageImmune)
            {
                if (_rb.velocity.magnitude > collisionDamageThreshold)
                    TakeDamage(new DamageInfo() { damage = Mathf.RoundToInt(MaxHealth / 5f), armorPenetration = 5 });
            }
        }
        private void Awake()
        {
            SetHealth(_health, _health);
            _rb = GetComponent<Rigidbody2D>();
        }
        private void FixedUpdate()
        {
            if (_collisionDamageImmunityTime >= 0f) { _collisionDamageImmunityTime -= Time.fixedDeltaTime; }
        }
    }
    [System.Serializable]
    public struct DamageReport
    {
        public int damageTaken;
        public int damageBlocked;
        public bool isKill;
        public bool isCrit;
    }
    [System.Serializable]
    public struct DamageInfo
    {
        public int damage;
        public int armorPenetration;
        public int critChance;
        public int critDamage; //At 0, crit damage does 150% damage and increases to this stat are additive.

        public DamageInfo(int dmg, int ap, int crit, int critDmg)
        {
            damage = dmg;
            armorPenetration = ap;
            critChance = crit;
            critDamage = critDmg;
        }
    }
}