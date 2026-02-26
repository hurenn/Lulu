using UnityEngine;

public class Enemy_Sainome : Enemy_Base
{
    [Header("AI Parameters")]
    [SerializeField] private float _walkSpeed = 2.0f;
    [SerializeField] private float _patrolDistance = 5.0f;
    [SerializeField] private bool _enablePatrol = true;

    // パトロールの初期位置
    private Vector3 _startPosition;

    protected override void _Setup() {
        base._Setup();
        _startPosition = transform.position;
    }

    protected override void _UpdateSpecials() {
        base._UpdateSpecials();

        if (_isDead || _damageReactionTimer > 0) {
            // 移動速度を0にしてアニメーションも停止
            Vector2 velocity = _rb.linearVelocity;
            velocity.x = 0;
            _rb.linearVelocity = velocity;
            return;
        }

        AI_Walk();
    }

    /// <summary>
    /// 歩行AI
    /// </summary>
    protected void AI_Walk() {
        if (!_enablePatrol) {
            return;
        }

        // 壁に接触したら方向転換
        if ((_isRight && _isTouchingRight) || (!_isRight && _isTouchingLeft)) {
            _TurnAround();
        }

        // 崖に到達したら方向転換（前方の地面チェック）
        if (!_CheckGroundAhead()) {
            _TurnAround();
        }

        // パトロール範囲を超えたら方向転換
        if (_patrolDistance > 0f) {
            float distanceFromStart = transform.position.x - _startPosition.x;
            if ((_isRight && distanceFromStart > _patrolDistance) || 
                (!_isRight && distanceFromStart < -_patrolDistance)) {
                _TurnAround();
            }
        }

        // 移動処理
        _Walk();
    }

    /// <summary>
    /// 歩行処理
    /// </summary>
    private void _Walk() {
        Vector2 velocity = _rb.linearVelocity;
        velocity.x = _isRight ? _walkSpeed : -_walkSpeed;
        _rb.linearVelocity = velocity;

        // 歩行アニメーション再生
        if (_anim != null) {
            _anim.SetBool("Walk", true);
        }
    }

    /// <summary>
    /// 方向転換
    /// </summary>
    private void _TurnAround() {
        _isRight = !_isRight;
    }

    /// <summary>
    /// 前方の地面をチェック
    /// </summary>
    /// <returns>地面があればtrue</returns>
    private bool _CheckGroundAhead() {
        if (_param == null) {
            return true;
        }

        // キャラクターのサイズを取得
        Vector2 characterSize = GetCharacterSize();

        // チェック位置を計算（前方少し先の地面）
        float checkDistance = characterSize.x * 0.6f;
        Vector2 checkPosition = transform.position;
        checkPosition.x += _isRight ? checkDistance : -checkDistance;
        checkPosition.y -= characterSize.y * 0.5f + _param.groundCheckHeight;

        // 地面チェック
        Vector2 boxSize = new Vector2(_param.groundCheckHeight, _param.groundCheckHeight);
        Collider2D hit = Physics2D.OverlapBox(checkPosition, boxSize, 0f, _groundLayer);

        return hit != null;
    }

    private void OnDrawGizmosSelected() {
        // パトロール範囲を可視化
        if (_enablePatrol && _patrolDistance > 0f) {
            Gizmos.color = Color.yellow;
            Vector3 startPos = Application.isPlaying ? _startPosition : transform.position;
            Vector3 leftBound = startPos + Vector3.left * _patrolDistance;
            Vector3 rightBound = startPos + Vector3.right * _patrolDistance;
            
            Gizmos.DrawLine(leftBound + Vector3.up, leftBound + Vector3.down);
            Gizmos.DrawLine(rightBound + Vector3.up, rightBound + Vector3.down);
            Gizmos.DrawLine(leftBound, rightBound);
        }

        // 前方地面チェック位置を可視化
        if (Application.isPlaying && _param != null) {
            Vector2 characterSize = GetCharacterSize();
            float checkDistance = characterSize.x * 0.6f;
            Vector2 checkPosition = transform.position;
            checkPosition.x += _isRight ? checkDistance : -checkDistance;
            checkPosition.y -= characterSize.y * 0.5f + _param.groundCheckHeight;

            Gizmos.color = _CheckGroundAhead() ? Color.green : Color.red;
            Vector2 boxSize = new Vector2(_param.groundCheckHeight, _param.groundCheckHeight);
            Gizmos.DrawWireCube(checkPosition, boxSize);
        }
    }
}
