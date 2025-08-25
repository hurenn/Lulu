using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class DamageZone : MonoBehaviour {
    [SerializeField] private float _damage = 1.0f;
    [SerializeField] private bool _isPlayersAttack = false;
    [SerializeField] private bool _isEnemysAttack = false;
    [SerializeField] private bool _isNeutralAttack = false;

    // ヒットしたら消えるかどうか
    [SerializeField] private bool _isHitDestroy = false;
    [SerializeField] private GameObject _parentObject = null;

    // 一度だけダメージを与えるかどうか
    [SerializeField] private bool _isOnceHit = true;
    private List<GameObject> _hitObjects = new List<GameObject>();

    // 連続ダメージ判定のディレイ時間
    [SerializeField] private float _delayTime = 0.5f;
    private float _currentDelayTimer = 0;
    // 攻撃可能かどうか
    private bool _isAttakable = true;

    // ダメージ判定の有効無効
    private bool _isEnable = true;

    // Update is called once per frame
    void Update() {
        // 連続ダメージ判定のディレイ処理
        if (_currentDelayTimer > 0 && _isAttakable == false) {
            _currentDelayTimer -= Time.deltaTime;
        }
        if (_currentDelayTimer < 0 && _isAttakable == false) {
            _isAttakable = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other) {
        if ((_isPlayersAttack && other.gameObject.layer == LayerMask.NameToLayer("Player")) ||
            (_isEnemysAttack && other.gameObject.layer == LayerMask.NameToLayer("Enemy"))) {
            return;
        }

        Character_Base character = other.GetComponent<Character_Base>();
        if (character == null) {
            return;
        }
        if (character.isInvincible || _hitObjects.Contains(other.gameObject)) {
            return;
        }

        if (_isOnceHit) {
            // ヒットした相手を記録しておく
            _hitObjects.Add(other.gameObject);
        }

        character.Damage(_damage);
        _currentDelayTimer = _delayTime;
        _isAttakable = false;
        if (_isHitDestroy && _parentObject != null) {
            Destroy(_parentObject);
        }
    }

    //private void OnTriggerStay2D(Collider2D Collider)//Trigger版ダメージ判定
    //{
    //    if (Stay == false) {
    //        /*
    //        if (HitDebug)
    //        {
    //            if (Collider.gameObject.layer.Equals(16))
    //                Debug.Log(Collider.gameObject.GetComponent<Enemy>().invinceTime + " >?" + Collider.gameObject.GetComponent<Enemy>().GetMaxInvince());
    //            else
    //                Debug.Log(Collider.gameObject.layer);
    //        }*/

    //        if (EnemysAtack == true || NeutralAtack == true)    //プレイヤーダメージ判定
    //            if (Collider.gameObject.tag.Contains("Player")) {
    //                if (HitDebug)
    //                    Debug.Log("TriggerHit Player");

    //                //プレイヤーダメージ
    //                PlayerHit(PlayerDamage, Collider.gameObject);
    //            }

    //        if (PlayersAtack == true || NeutralAtack == true)   //敵ダメージ判定
    //            if (Collider.gameObject.layer.Equals(16)    //レイヤーネーム「エネミー」
    //                && Collider.gameObject.GetComponent<Enemy>().invinceTime >= Collider.gameObject.GetComponent<Enemy>().GetMaxInvince()) {
    //                if (HitDebug)
    //                    Debug.Log("TriggerHit Enemy");

    //                //Collider.gameObject.GetComponent<Enemy>().HP -= EnemyDamage;

    //                //敵ダメージ
    //                Collider.GetComponent<Enemy>().Damage(EnemyDamage, 0);
    //                if (invinceTimeSpecific) {
    //                    Collider.gameObject.GetComponent<Enemy>().invinceTime = SpecialInvinceTime;
    //                } else {
    //                    Collider.gameObject.GetComponent<Enemy>().invinceTime = 0;
    //                }
    //                if (Disappear)
    //                    Destroy(this.gameObject);
    //            }
    //    }
    //}
    //private void OnCollisionStay2D(Collision2D Collider)    //Collision版ダメージ判定
    //{
    //    if (Stay == false) {
    //        if (EnemysAtack == true || NeutralAtack == true)
    //            if (Collider.gameObject.tag == "Player") {
    //                if (HitDebug)
    //                    Debug.Log("TriggerHit Player");
    //                PlayerHit(PlayerDamage, Collider.gameObject);
    //            }

    //        if (PlayersAtack == true || NeutralAtack == true)
    //            if (Collider.gameObject.layer.Equals(16)    //レイヤーネーム「エネミー」
    //                && Collider.gameObject.GetComponent<Enemy>().invinceTime >= Collider.gameObject.GetComponent<Enemy>().GetMaxInvince()) {
    //                if (HitDebug)
    //                    Debug.Log("TriggerHit Enemy");
    //                //Collider.gameObject.GetComponent<Enemy>().HP -= EnemyDamage;
    //                Collider.gameObject.GetComponent<Enemy>().Damage(EnemyDamage, 0);
    //                if (invinceTimeSpecific) {
    //                    Collider.gameObject.GetComponent<Enemy>().invinceTime = SpecialInvinceTime;
    //                } else {
    //                    Collider.gameObject.GetComponent<Enemy>().invinceTime = 0;
    //                }
    //                if (Disappear)
    //                    Destroy(this.gameObject);
    //            }
    //    }
    //}
    //void PlayerHit(int PlayerDamage, GameObject player) //プレイヤーダメージ処理
    //{
    //    if (Attakable)
    //        player.GetComponent<HPManager>().Damage(PlayerDamage);
    //}
}
