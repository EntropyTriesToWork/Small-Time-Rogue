using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SmallTimeRogue.Enemy
{
    public class Bomber : Enemy
    {
        public ThrownBomb bomb;
        public DamageInfo damageInfo;
        public float bombFuseTime;
        public SpriteRenderer bodySprite;

        private float _remainingFuseTime;
        private bool _preparingToBlow;

        #region Messages
        public override void Start()
        {
            base.Start();
            _preparingToBlow = false;
            _remainingFuseTime = bombFuseTime;
        }
        public override void Update()
        {
            if (target == null) { return; }

            if (GameManager.Instance.CurrentGameState == GameState.Normal)
            {
                if (_preparingToBlow)
                {
                    _remainingFuseTime -= Time.deltaTime;

                    bodySprite.color = Color.Lerp(bomb.nearingExplodeStateColor, bomb.normalStateColor, _remainingFuseTime / bombFuseTime);

                    if (_remainingFuseTime <= 0f)
                    {
                        DropCoins();
                        DropBomb(0);
                        _hc.DieSilently();
                        gameObject.SetActive(false);
                    }
                }
            }
        }
        #endregion
        public void DropBomb(float fuseTime)
        {
            _preparingToBlow = false;
            ThrownBomb go = Instantiate(bomb, transform.position, Quaternion.identity);
            go.damageInfo = damageInfo;
            go.fuseTime = fuseTime;
            go.GetComponent<Rigidbody2D>().AddForce(Vector2.up * 10f, ForceMode2D.Impulse);
        }
        public void StartFuse()
        {
            if (_preparingToBlow == true) { return; }
            _preparingToBlow = true;
            _remainingFuseTime = bombFuseTime;
        }
        #region States
        public override void Chase()
        {
            _rb.velocity += ((transform.position.x <= target.position.x ? Vector2.right : Vector2.left) * _baseStats.moveSpeed * Time.fixedDeltaTime);
            if (IsWallBlocking) { Jump(); }
        }
        public override void Attack()
        {
            StartFuse();
            _baseStats.moveSpeed = _baseStats.moveSpeed * 1.5f;
        }
        public override void Death(DamageReport report)
        {
            if (_preparingToBlow)
            {
                DropBomb(_remainingFuseTime);
            }
            else { DropBomb(bombFuseTime); }
            base.Death(report);
        }
        #endregion
    }
}