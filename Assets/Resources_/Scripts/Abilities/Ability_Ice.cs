using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class Ability_Ice : Ability_Base {

    private int _attackStep = 0; // 0:未攻撃, 1:1段目, 2:2段目, 3:3段目

    private float _currentComboTime = 0f;
    // コンボ攻撃のクールタイム
    private float _currentComboCoolTime = 0f;

    // 長押し判定時間
    private float _longPressThreshold = 0.3f;
    private float _pressHoldTime = 0f;

    // 長押し実行済みフラグ
    private bool _isHoldExecuted = false;
    // 攻撃方向ロックフラグ
    private bool _isAttackDirectionLocked = false;
    private bool _isEffectRight = true; // 攻撃エフェクトを出す向き(攻撃方向がロックされている間は更新しない)

    // ヒットストップ管理
    [SerializeField] private LocalTimePause _localTimeController;
    
    [SerializeField] private GameObject _slash1;
    [SerializeField] private GameObject _slash2;
    [SerializeField] private GameObject _slash3;
    [SerializeField] private GameObject _lockonSlash;
    private IEnumerator _separateRoutine = null;

    [Header("必殺技:最初の一撃(正面へ飛ばし、敵に当たったら連続攻撃へ移る)")]
    [SerializeField] private GameObject _specialFirstAttackPrefab;
    [SerializeField] private float _specialFirstAttackSpeed = 30.0f;
    [SerializeField] private float _specialFirstAttackTime = 0.5f;   // 飛んでいる時間(この間に敵に当たらなければ失敗)
    [SerializeField] private int _specialFirstAttackDamage = 2;

    [Header("必殺技:連続攻撃(プレイヤーと同じ姿のため、序盤は同時に1体しか出さない)")]
    [SerializeField] private GameObject _specialAttackPrefab;
    [SerializeField] private float _specialAttackDuration = 3.0f;      // 連続攻撃を生成し続ける時間
    [SerializeField] private int _specialAttackHitCount = 30;          // 連続攻撃でダメージを与える回数
    [SerializeField] private int _specialAttackDamage = 2;
    [SerializeField] private float _specialAttackIntervalRate = 0.1f;  // 最後の生成間隔÷最初の生成間隔(小さいほど後半の生成が速くなる)
    [SerializeField] private float _specialAttackOverlapTime = 1.5f;   // この時間を超えたら、同時に2体以上表示してよい
    [SerializeField] private float _specialAttackDistance = 9.0f;      // 敵の中心から出現位置までの距離(反対側の同じ距離まで通過する)
    [SerializeField] private float _specialAttackDashTime = 0.12f;     // 出現位置から反対側まで通過する時間
    [SerializeField] private float _specialAttackAngleRange = 35.0f;   // 通過する向きの、水平からの角度のばらつき
    [SerializeField] private float _specialAttackHitStopTime = 0.04f;  // 敵のヒットストップ

    [Header("必殺技:最後の一撃")]
    [SerializeField] private float _specialFinishChargeTime = 2.0f;    // 力を溜める時間
    [SerializeField] private float _specialFinishDistance = 10.0f;     // 敵の中心から力を溜める位置までの距離
    [SerializeField] private float _specialFinishDashTime = 0.15f;
    [SerializeField] private int _specialFinishDamage = 30;
    [SerializeField] private Color _specialFinishChargeColor = new Color(0.55f, 0.85f, 1.0f, 1.0f);
    [SerializeField] private Sprite _specialFinishImage;               // 最後の一撃が当たった瞬間に表示する一枚絵
    [SerializeField] private float _specialFinishImageTime = 2.0f;

    // 必殺技の演出中に作ったもの(能力が途中で破棄された時に片付ける)
    private Enemy_Base _specialTarget = null;
    private GameObject _specialFinishImageObj = null;
    private bool _isSpecialTimeStopped = false;
    private readonly System.Collections.Generic.List<GameObject> _specialObjects = new System.Collections.Generic.List<GameObject>();

    protected override void _Update() {
        // タイマー減少
        if (_attackStep > 0) {
            _currentComboTime -= Time.deltaTime;
            if (_currentComboTime <= 0f) {
                _attackStep = 0; // コンボリセット
            }
        }
        // コンボタイマー
        if (_currentComboCoolTime > 0f) {
            _currentComboCoolTime -= Time.deltaTime;
        }
    }

    public override eAbilityResult ExecuteSimple() {
        // オーバーヒート中は使用不可
        if (_cannotUse) {
            UpdatePartnerTransform(); // 位置更新
            Instantiate(_warpAnimationPrefab, transform.position, Quaternion.identity); // 召喚エフェクト再生
            return eAbilityResult.None;
        }
        // 切り離し攻撃終了
        _EndSeparate();

        // 必殺技チャージ停止
        _StopSpecialCharge();

        // 必殺技発動
        if (_isSpecialCharged && !_motorStates.isWarpDashing && !_motorStates.isSliding) {
            _UseSpecial();
            return eAbilityResult.IceSpecial;
        }

        // ロックオン攻撃判定
        if (LockonManager.Instance.HasTarget) {
            if (!_charaParam.isOverheat) {
                // ロックオン中の攻撃

                // ロックオン対象の方向を向く
                var lockon = LockonManager.Instance;
                Vector3 to_target = lockon.targetTransform.position - _playerTransform.position;
                to_target.y = 0; // 水平成分のみ

                // ワープチェッカー取得
                WarpChecker warp_checker = null;
                if (to_target.x > 0) {
                    warp_checker = lockon.GetTargetWarpPos(WarpControl.eWarpDirection.Left);
                } else if (to_target.x < 0) {
                    warp_checker = lockon.GetTargetWarpPos(WarpControl.eWarpDirection.Right);
                }

                // ロックオン攻撃実行
                if (warp_checker != null) {
                    _LockonSlash(warp_checker, lockon.targetTransform.position);
                    return eAbilityResult.IceLockonSlash;
                }
            } else {
                // オーバーヒート中は使用不可
                UpdatePartnerTransform(); // 位置更新
                Instantiate(_warpAnimationPrefab, transform.position, Quaternion.identity); // 召喚エフェクト再生
            }
        }

        // コンボ攻撃判定
        var attack_result = _ComboSlash();
        // 切り離し攻撃判定
        // if(attack_result == eAbilityResult.None) attack_result = _SeparateSlash();

        // 攻撃実行
        if (attack_result != eAbilityResult.None) {
            _AppearCheck(eAbilityType.Ice);
            return attack_result;
        }

        return eAbilityResult.None;
    }

    /// <summary>
    /// ロックオン攻撃実行
    /// </summary>
    private void _LockonSlash(WarpChecker warp_checker, Vector3 target_pos) {
        if (_lockonSlash == null) {
            Debug.Log("ロックオン攻撃エフェクトが見つかりません");
            return;
        }
        Debug.Log("Lockon Slash");

        IEnumerator attack_routine() {
            yield return _warpControl?.TargetWarp(warp_checker);

            _OnceAttack(target_pos, warp_checker, eAbilityType.LockonSlash);
        }
        StartCoroutine(attack_routine());
    }

    /// <summary>
    /// コンボ攻撃実行
    /// </summary>
    private eAbilityResult _ComboSlash() {

        // クールタイム中は実行不可
        if (_currentComboCoolTime > 0f) {
            return eAbilityResult.None;
        }

        if (_attackStep == 0) {
            // 1段目
            Debug.Log("Slash 1");
            StartCoroutine(_UpdateTransformEasing(_slash1, "Node_Attack1"));
            _SetComboStep(1);
            return eAbilityResult.IceSlash1;            // 実行結果返却
        } else if (_attackStep == 1) {
            // 2段目
            Debug.Log("Slash 2");
            StartCoroutine(_UpdateTransformEasing(_slash2, "Node_Attack2"));
            _SetComboStep(2);
            return eAbilityResult.IceSlash2;            // 実行結果返却
        } else if (_attackStep == 2) {
            // 3段目
            Debug.Log("Slash 3");
            StartCoroutine(_UpdateTransformEasing(_slash3, "Node_Attack3"));
            _SetComboStep(0); // コンボリセット
            return eAbilityResult.IceSlash3;            // 実行結果返却
        }

        return eAbilityResult.None;
    }

    /// <summary>
    /// コンボ段階更新
    /// </summary>
    private void _SetComboStep(int step) {
        _attackStep = step;
        _currentComboTime = _param.comboReceptionTime;    // 次のコンボ受付時間
        _currentComboCoolTime = step == 0 ? _param.comboCoolTime : _param.comboIntervalTime;     // クールタイムセット
    }

    private IEnumerator _UpdateTransformEasing(GameObject effect, string anim_name) {
        if (_isAppearing) {
            // イージングで位置を更新
            float elapsedTime = 0f;
            Vector3 startPos = transform.position;
            Vector3 targetPos = _playerTransform.position + new Vector3(
                _localPosition.x * (_isRight ? -1 : 1),
                _localPosition.y,
                _localPosition.z);
            while (elapsedTime < _param.moveDuration) {
                transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / _param.moveDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        
        UpdatePartnerTransform();   // キャラクター位置を更新

        var effect_obj = _SpawnAttackEffect(effect);  // エフェクト生成
        if (effect_obj == null) yield break;
        var damage_zone = effect_obj.GetComponentInChildren<DamageZone>();
        damage_zone?.AddHitStopTarget(_localTimeController); // ヒットストップ対象に追加

        _anim?.Play(anim_name, 0, 0.0f);   // アニメーション再生
    }

    /// <summary>
    /// 切り離し連続攻撃
    /// </summary>
    private IEnumerator _SeparateRoutine() {
        _anim?.SetBool("SeparateEnd", false);
        _anim?.Play("Node_SeparateAttack", 0, 0.0f);   // アニメーション再生
        _isHoldExecuted = true;
        _isAttackDirectionLocked = true; // 攻撃方向ロック
        _canForceReturn = false;      // 強制帰還無効化
        _attackStep = 0; // コンボリセット
        UpdatePartnerTransform();   // キャラクター位置を更新

        float separate_time = 5.5f;
        float current_time = 0f;
        float attack_delay = 0.3f; // 攻撃エフェクト発生までの遅延時間
        float current_delay = 0f;
        _ResetReturnTimer(separate_time); // 帰還タイマーリセット
        while (current_time < separate_time) {
            current_time += Time.deltaTime;
            if (current_delay < attack_delay) {
                current_delay += Time.deltaTime;
            } else {
                // 一定時間ごとに攻撃エフェクト発生
                current_delay = 0f;
                _SpawnAttackEffect(_slash1);  // エフェクト生成
            }
            yield return null;
        }

        yield return new WaitForSeconds(separate_time);
        _anim?.SetBool("SeparateEnd", true);
    }

    // 切り離し攻撃終了
    private void _EndSeparate() {
        // 実行中の連続攻撃コルーチンがあれば停止
        if (_separateRoutine != null) {
            StopCoroutine(_separateRoutine);
            _separateRoutine = null;
        }
        _isAttackDirectionLocked = false;
        _canForceReturn = true;      // 強制帰還有効化

        _anim?.SetBool("SeparateEnd", true);
    }

    public override eAbilityResult ExecuteLong() {
        return eAbilityResult.None;
        // 切り離し
        if (!_isHoldExecuted) {
            _pressHoldTime += Time.deltaTime;
            if (_pressHoldTime >= _longPressThreshold) {
                _EndSeparate();

                Debug.Log("Ice Separate");
                _separateRoutine = _SeparateRoutine();
                StartCoroutine(_separateRoutine);
                return eAbilityResult.IceSeparate;
            }
        }
    }

    public override eAbilityResult ExecuteRelease() {
        _pressHoldTime = 0f;
        _isHoldExecuted = false;
        return eAbilityResult.None;
    }

    /// <summary>
    /// 自動攻撃実行
    /// </summary>
    public void ExecuteAutoAttack(WarpChecker warp_checker, Vector3 enemy_pos) {
        _AutoSlash(warp_checker, enemy_pos);
    }

    /// <summary>
    /// 自動攻撃
    /// </summary>
    private void _AutoSlash(WarpChecker warp_checker, Vector3 enemy_pos) {
        if (_lockonSlash == null) {
            Debug.Log("ロックオン攻撃エフェクトが見つかりません");
            return;
        }
        Debug.Log("Lockon Slash");

        _OnceAttack(enemy_pos, warp_checker, eAbilityType.AutoSlash);
    }

    /// <summary>
    /// 一撃攻撃
    /// </summary>
    /// <param name="target_pos">敵の位置</param>
    private void _OnceAttack(Vector3 target_pos, WarpChecker warp_checker, eAbilityType ability_type) {
        if(_warpControl == null || warp_checker == null) {
            Debug.Log("ワープコントロールまたはワープチェッカーが見つかりません");
            return;
        }

        // フラッシュ
        Color flash_color = Color.white;
        flash_color.a = 0.5f;
        var flash = ScreenFlash.Instance;
        flash?.Flash(color: flash_color);

        // MP消費
        _AppearCheck(ability_type, force_appear: true);

        // キャラクター位置を更新
        UpdatePartnerTransform(warp_checker.transform.position);
        var is_right = target_pos.x > transform.position.x;
        _isRight = is_right;

        // エフェクトの向きを調整して生成
        _UpdateEffectDirection();
        _SpawnAttackEffect(_lockonSlash);
        // アニメーション再生
        _anim?.Play("Node_Attack2", 0, 0.0f);

        // コンボ1段目をスキップ
        _SetComboStep(1);
    }

    public override void UpdateParameter(bool is_right, Transform chara_transform, CommonParameter param, CharacterParameter_Player chara_param, WarpControl warp_control, MotorStates motor_states) {
        base.UpdateParameter(is_right, chara_transform, param, chara_param, warp_control, motor_states);
        // 向きに応じて攻撃エフェクトを出す向きを更新
        _UpdateEffectDirection();
    }

    /// <summary>
    /// 攻撃エフェクトを出す向きを更新する(攻撃方向がロックされている間は前の向きのまま)
    /// </summary>
    private void _UpdateEffectDirection() {
        if (!_isAttackDirectionLocked) {
            _isEffectRight = _isRight;
        }
    }

    /// <summary>
    /// 攻撃エフェクトを生成し、向きを合わせる
    /// (プレハブ本体の向きを書き換えるとプレハブに差分が出るため、生成したものの向きを変える)
    /// </summary>
    private GameObject _SpawnAttackEffect(GameObject effect) {
        if (effect == null) {
            Debug.Log("攻撃エフェクトが登録されていません");
            return null;
        }
        var effect_obj = Instantiate(effect, transform.position, Quaternion.identity);
        var scale = effect_obj.transform.localScale;
        scale.x = _isEffectRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        effect_obj.transform.localScale = scale;
        return effect_obj;
    }

    public override void OnWarp() {
        // 即座に帰還
        if (_isAppearing) {
            _ResetReturnTimer(0.01f);
        }
    }

    protected override void _ForceReturn() {
        base._ForceReturn();
        // 切り離し攻撃終了
        _EndSeparate();
    }

    #region 必殺技

    protected override void _OnSpecialCutInFinished(PlayableDirector obj) {
        base._OnSpecialCutInFinished(obj);
        StartCoroutine(_SpecialAttack());
    }

    /// <summary>
    /// 必殺技攻撃
    /// 最初の一撃を正面へ飛ばし、敵に当たったら姿を消して連続攻撃→力を溜めた最後の一撃→敵の横に姿を現す
    /// 演出が終わるまで、敵のHPは1より減らない
    /// </summary>
    private IEnumerator _SpecialAttack() {
        Enemy_Base target = null;
        yield return _SpecialFirstAttack(hit => target = hit);
        if (target == null) {
            // 敵に当たらなければ失敗してそのまま終了
            _onEndSpecialAttack?.Invoke();
            yield break;
        }
        _specialTarget = target;

        // プレイヤーの姿を消す
        var player = _playerTransform != null ? _playerTransform.GetComponent<Player_Character>() : null;
        if (player != null) {
            _SpawnWarpEffect(player.transform.position);
            player.SetSpecialHidden();
        }
        Vector2 last_center = _GetTargetCenter(target);
        // 最後の一撃は、プレイヤーがいた側から突撃する
        bool is_from_left = _playerTransform == null || _playerTransform.position.x <= last_center.x;

        yield return _SpecialRushAttack(target);

        bool is_dash_right = is_from_left;
        if (_IsValidTarget(target)) {
            yield return _SpecialFinishAttack(target, is_from_left);
            last_center = _GetTargetCenter(target);
        }

        // 突撃した先(敵の向こう側)のワープチェッカーの位置にプレイヤーを表示
        if (player != null) {
            Vector2 appear_pos = last_center;
            if (_IsValidTarget(target)) {
                var side = is_dash_right ? WarpControl.eWarpDirection.Right : WarpControl.eWarpDirection.Left;
                var checker = target.GetWarpChecker(side, true);
                var point = checker != null ? checker.GetWarpPoint() : null;
                if (point.HasValue) appear_pos = point.Value;
            }
            player.AppearFromSpecial(appear_pos, appear_pos.x < last_center.x);
        }
        _onEndSpecialAttack?.Invoke();

        // 最後の一撃でHPが1だった時は、演出が終わってから倒す
        _specialTarget = null;
        if (target != null) {
            target.SetMinHP(0);
            if (!target.isDead && target.currentHP == 1) {
                target.Damage(1, Vector2.zero, 0f, 0f, false, true);
            }
        }
    }

    /// <summary>
    /// 最初の一撃: プレイヤーから正面へ飛ばし、最初に当たった敵を返す(当たらなければnull)
    /// </summary>
    private IEnumerator _SpecialFirstAttack(System.Action<Enemy_Base> on_hit) {
        if (_specialFirstAttackPrefab == null || _playerTransform == null) yield break;

        bool is_right = _isRight;
        var obj = Instantiate(_specialFirstAttackPrefab, _playerTransform.position, Quaternion.identity);
        _specialObjects.Add(obj);
        _SetFacing(obj.transform, is_right ? Vector2.right : Vector2.left);

        // 当たり判定はDamageZoneのコライダーの範囲を使い、ダメージはここで与える(敵の被弾無敵に関係なく当てるため)
        var zone = obj.GetComponentInChildren<DamageZone>(true);
        var box = zone != null ? zone.GetComponent<BoxCollider2D>() : null;
        Vector3 local_center = Vector3.zero;
        Vector2 hit_size = Vector2.one;
        if (box != null) {
            local_center = obj.transform.InverseTransformPoint(box.transform.TransformPoint(box.offset));
            var scale = box.transform.lossyScale;
            hit_size = new Vector2(box.size.x * Mathf.Abs(scale.x), box.size.y * Mathf.Abs(scale.y));
        }
        _DisableDamageZone(obj);

        int enemy_mask = LayerMask.GetMask("Enemy");
        Vector3 velocity = (is_right ? Vector3.right : Vector3.left) * _specialFirstAttackSpeed;
        Enemy_Base hit = null;
        float time = 0f;
        while (time < _specialFirstAttackTime && hit == null) {
            obj.transform.position += velocity * Time.deltaTime;
            Vector2 center = obj.transform.TransformPoint(local_center);
            float nearest = float.MaxValue;
            foreach (var col in Physics2D.OverlapBoxAll(center, hit_size, 0f, enemy_mask)) {
                var enemy = col.GetComponentInParent<Enemy_Base>();
                if (!_IsValidTarget(enemy) || enemy.IsEventInvincible) continue;
                // シールド等で防がれる相手には当たらない
                if (enemy is IDamageSourceBlocker blocker && blocker.IsBlockedDamageSource(null)) continue;
                float distance = Mathf.Abs(enemy.transform.position.x - _playerTransform.position.x);
                if (distance < nearest) {
                    nearest = distance;
                    hit = enemy;
                }
            }
            time += Time.deltaTime;
            yield return null;
        }

        if (hit != null) {
            // 演出が終わるまで倒れないようにしてからダメージを与える
            hit.SetMinHP(1);
            _SpecialDamage(hit, _specialFirstAttackDamage, zone);
            CinemachineManager.Instance?.ShakeCamera(0.3f, 0.1f);
        }
        _SpawnWarpEffect(obj.transform.position);
        _DestroySpecialObject(obj);
        on_hit(hit);
    }

    /// <summary>
    /// 連続攻撃: 敵の周囲から敵を通過する攻撃を生成し続ける(後半になるほど生成が速くなる)
    /// </summary>
    private IEnumerator _SpecialRushAttack(Enemy_Base target) {
        if (_specialAttackPrefab == null || _specialAttackHitCount <= 0) yield break;

        // 生成間隔は等比で短くし、合計が連続攻撃の時間になるようにする
        int count = _specialAttackHitCount;
        float ratio = count > 1 ? Mathf.Pow(Mathf.Max(_specialAttackIntervalRate, 0.001f), 1f / (count - 1)) : 1f;
        float first_interval = Mathf.Approximately(ratio, 1f)
            ? _specialAttackDuration / count
            : _specialAttackDuration * (1f - ratio) / (1f - Mathf.Pow(ratio, count));

        int spawned = 0;
        int alive = 0;
        float time = 0f;
        float next_spawn_time = 0f;
        float interval = first_interval;
        bool is_from_left = Random.value < 0.5f;
        while (spawned < count && _IsValidTarget(target)) {
            // 序盤はプレイヤーと同じ姿が2体以上見えないように、前の攻撃が消えるまで待つ
            bool can_overlap = time > _specialAttackOverlapTime;
            if (time >= next_spawn_time && (can_overlap || alive == 0)) {
                // 左右から交互に、角度をばらつかせて通過させる
                float angle = Random.Range(-_specialAttackAngleRange, _specialAttackAngleRange) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(angle) * (is_from_left ? 1f : -1f), Mathf.Sin(angle));
                is_from_left = !is_from_left;

                alive++;
                StartCoroutine(_SpecialDash(target, dir, _specialAttackDistance, _specialAttackDashTime, () => alive--));
                spawned++;
                next_spawn_time = Mathf.Max(next_spawn_time, time) + interval;
                interval *= ratio;
            }
            time += Time.deltaTime;
            yield return null;
        }
        // 最後の攻撃が通過し終わるまで待つ
        while (alive > 0) yield return null;
    }

    /// <summary>
    /// 敵の中心を通過する攻撃を1回行う(ワープエフェクトと同時に出現し、通過した瞬間にダメージを与え、ワープエフェクトを出しながら消える)
    /// </summary>
    private IEnumerator _SpecialDash(Enemy_Base target, Vector2 dir, float distance, float dash_time, System.Action on_end) {
        Vector2 center = _GetTargetCenter(target);
        Vector2 start = center - dir * distance;
        Vector2 end = center + dir * distance;

        var obj = Instantiate(_specialAttackPrefab, start, Quaternion.identity);
        _specialObjects.Add(obj);
        var zone = obj.GetComponentInChildren<DamageZone>(true);
        _DisableDamageZone(obj);
        _SetFacing(obj.transform, dir);
        _SpawnWarpEffect(start);

        bool is_hit = false;
        float time = 0f;
        while (time < dash_time) {
            time += Time.deltaTime;
            float rate = Mathf.Clamp01(time / dash_time);
            obj.transform.position = Vector2.Lerp(start, end, rate);
            if (!is_hit && rate >= 0.5f) {
                is_hit = true;
                _SpecialDamage(target, _specialAttackDamage, zone);
            }
            yield return null;
        }

        _SpawnWarpEffect(end);
        _DestroySpecialObject(obj);
        on_end?.Invoke();
    }

    /// <summary>
    /// 最後の一撃: 力を溜めてから突撃し、当たった瞬間に一枚絵を表示する
    /// </summary>
    private IEnumerator _SpecialFinishAttack(Enemy_Base target, bool is_from_left) {
        if (_specialAttackPrefab == null) yield break;

        Vector2 dir = is_from_left ? Vector2.right : Vector2.left;
        Vector2 center = _GetTargetCenter(target);
        Vector2 start = center - dir * _specialFinishDistance;

        var obj = Instantiate(_specialAttackPrefab, start, Quaternion.identity);
        _specialObjects.Add(obj);
        var zone = obj.GetComponentInChildren<DamageZone>(true);
        _DisableDamageZone(obj);
        _SetFacing(obj.transform, dir);
        _SpawnWarpEffect(start);

        var renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        var lights = obj.GetComponentsInChildren<Light2D>();
        var light_intensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) light_intensities[i] = lights[i].intensity;

        // 力を溜める(震えながら色が明滅し、光が強くなっていく)
        float time = 0f;
        float next_shake_time = 0f;
        while (time < _specialFinishChargeTime) {
            time += Time.deltaTime;
            float rate = Mathf.Clamp01(time / _specialFinishChargeTime);
            obj.transform.position = start + Random.insideUnitCircle * Mathf.Lerp(0.02f, 0.2f, rate);
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * Mathf.Lerp(8f, 40f, rate));
            var color = Color.Lerp(Color.white, _specialFinishChargeColor, pulse * rate);
            foreach (var rend in renderers) rend.color = color;
            for (int i = 0; i < lights.Length; i++) lights[i].intensity = light_intensities[i] * Mathf.Lerp(1f, 3f, rate);
            if (time >= next_shake_time) {
                CinemachineManager.Instance?.ShakeCamera(Mathf.Lerp(0.03f, 0.2f, rate), 0.1f);
                next_shake_time = time + 0.2f;
            }
            yield return null;
        }
        foreach (var rend in renderers) rend.color = Color.white;

        // 突撃
        if (_IsValidTarget(target)) center = _GetTargetCenter(target);
        start = obj.transform.position;
        Vector2 end = center + dir * _specialFinishDistance;
        time = 0f;
        while (time < _specialFinishDashTime) {
            time += Time.deltaTime;
            float rate = Mathf.Clamp01(time / _specialFinishDashTime);
            obj.transform.position = Vector2.Lerp(start, end, rate);
            if (rate >= 0.5f) break; // 敵の中心に届いた瞬間に当たる
            yield return null;
        }

        // 攻撃がヒットする瞬間に一枚絵を表示する(表示している間は時間を止める)
        _SpecialDamage(target, _specialFinishDamage, zone);
        CinemachineManager.Instance?.ShakeCamera(1.0f, 0.3f);
        ScreenFlash.Instance?.Flash(0.2f, Color.white);
        yield return _ShowSpecialFinishImage();

        _SpawnWarpEffect(obj.transform.position);
        _DestroySpecialObject(obj);
    }

    /// <summary>
    /// 一枚絵を表示する(表示している間は時間を止め、ポーズも開けないようにする)
    /// </summary>
    private IEnumerator _ShowSpecialFinishImage() {
        if (_specialFinishImage == null || _specialFinishImageTime <= 0f) yield break;

        var pause_ui = GetPauseUI();
        if (pause_ui != null) pause_ui.canOpen = false;
        _isSpecialTimeStopped = true;
        Pause_UI.SetTimeScale(0f);

        _specialFinishImageObj = _CreateSpecialFinishImage(out var group, out var image_rect);
        const float fade_in = 0.1f;
        const float fade_out = 0.25f;
        float time = 0f;
        while (time < _specialFinishImageTime) {
            time += Time.unscaledDeltaTime;
            group.alpha = Mathf.Min(Mathf.Clamp01(time / fade_in), Mathf.Clamp01((_specialFinishImageTime - time) / fade_out));
            // 表示した瞬間に少し大きく出て、元の大きさに戻る
            float pop = 1f - Mathf.Clamp01(time / 0.2f);
            image_rect.localScale = Vector3.one * (1f + 0.15f * pop * pop);
            yield return null;
        }
        Destroy(_specialFinishImageObj);
        _specialFinishImageObj = null;

        _isSpecialTimeStopped = false;
        Pause_UI.SetTimeScale(1f);
        if (pause_ui != null) pause_ui.canOpen = true;
    }

    /// <summary>
    /// 一枚絵を表示する画面を作る(画面全体を薄暗くし、その上に一枚絵を画面の高さに合わせて表示する)
    /// </summary>
    private GameObject _CreateSpecialFinishImage(out CanvasGroup group, out RectTransform image_rect) {
        var root = new GameObject("IceSpecialFinishImage", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(root.transform, false);
        _Stretch(background.GetComponent<RectTransform>());
        var background_image = background.GetComponent<Image>();
        background_image.color = new Color(0.05f, 0.12f, 0.25f, 0.55f);
        background_image.raycastTarget = false;

        var picture = new GameObject("Image", typeof(RectTransform), typeof(Image));
        picture.transform.SetParent(root.transform, false);
        image_rect = picture.GetComponent<RectTransform>();
        _Stretch(image_rect);
        var image = picture.GetComponent<Image>();
        image.sprite = _specialFinishImage;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return root;
    }

    private static void _Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 必殺技のダメージを与える(被弾無敵に関係なく当て、吹き飛ばさない)
    /// </summary>
    private void _SpecialDamage(Enemy_Base target, int damage, DamageZone zone) {
        if (!_IsValidTarget(target)) return;
        if (!target.Damage(damage, Vector2.zero, 0f, 0f, false, true)) return;
        var center = _GetTargetCenter(target);
        if (zone != null) zone.PlayHitReaction(center);
        var time_pause = target.GetComponent<LocalTimePause>();
        if (time_pause != null && _specialAttackHitStopTime > 0f) time_pause.StartPause(_specialAttackHitStopTime);
    }

    /// <summary>
    /// 進む向きに合わせて向ける(画像は左向き。右へ進む時は左右反転し、上下の傾きは回転で付ける)
    /// </summary>
    private static void _SetFacing(Transform obj, Vector2 dir) {
        var scale = obj.localScale;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (dir.x >= 0f) {
            scale.x = -Mathf.Abs(scale.x);
        } else {
            scale.x = Mathf.Abs(scale.x);
            angle -= 180f;
        }
        obj.localScale = scale;
        obj.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// 演出用に当たり判定を無効にする(ダメージは_SpecialDamageで与える)
    /// </summary>
    private static void _DisableDamageZone(GameObject obj) {
        foreach (var damage_zone in obj.GetComponentsInChildren<DamageZone>(true)) {
            if (damage_zone.gameObject != obj) {
                damage_zone.gameObject.SetActive(false);
            } else {
                foreach (var col in damage_zone.GetComponents<Collider2D>()) col.enabled = false;
                damage_zone.enabled = false;
            }
        }
    }

    private static bool _IsValidTarget(Enemy_Base target) {
        return target != null && !target.isDead;
    }

    private static Vector2 _GetTargetCenter(Enemy_Base target) {
        if (target == null) return Vector2.zero;
        var col = target.GetComponent<Collider2D>();
        return col != null && col.enabled ? (Vector2)col.bounds.center : (Vector2)target.transform.position;
    }

    private void _SpawnWarpEffect(Vector3 position) {
        if (_warpAnimationPrefab != null) Instantiate(_warpAnimationPrefab, position, Quaternion.identity);
    }

    private void _DestroySpecialObject(GameObject obj) {
        _specialObjects.Remove(obj);
        if (obj != null) Destroy(obj);
    }

    // 必殺技の途中で能力が破棄された時に、止めた時間や敵のHPの下限を元に戻す
    private void OnDestroy() {
        if (_isSpecialTimeStopped) {
            Pause_UI.SetTimeScale(1f);
            var pause_ui = GetPauseUI();
            if (pause_ui != null) pause_ui.canOpen = true;
        }
        if (_specialFinishImageObj != null) Destroy(_specialFinishImageObj);
        foreach (var obj in _specialObjects) {
            if (obj != null) Destroy(obj);
        }
        if (_specialTarget != null) _specialTarget.SetMinHP(0);
    }

    #endregion
}
