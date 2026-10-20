using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 水のゼルクスのボス戦イベント
///   ・ボス戦に入った直後(ボス戦開始のTimelineの再生開始時)に、炎と光の能力を持った状態にする(氷の能力は持たない)
///   ・最初に真っすぐ撃たれた巨大な弾がNode_Ep3Eventの目の前まで来たら、Node_Ep3Eventを表示して巨大な弾を自動で跳ね返す
///   ・HPが2/3を切ったら、会話イベント(Timeline)を再生する。その間ボスは射撃しない
///   ・会話イベントの後、氷の能力を取得し、敵の弾を跳ね返せるようになる
/// (ダウンして少し後に必殺技を使えるようにする処理は、Enemy_ZelxWater側で行う)
/// </summary>
public class ZelxWaterBattleEvent : MonoBehaviour {
    [SerializeField] private Enemy_ZelxWater _boss;
    [SerializeField] private PlayableDirector _battleStartDirector; // ボス戦開始のTimeline(再生が始まったら能力を整える)
    [SerializeField] private eAbilityType[] _startAbilities = { eAbilityType.Fire, eAbilityType.Light }; // ボス戦に入った直後に持っている能力
    [SerializeField] private eAbilityType _joinAbility = eAbilityType.Ice; // 会話イベントの後に取得する能力(ボス戦に入った時点では持たない)

    [Header("仲間が巨大な弾を跳ね返す")]
    [SerializeField] private GameObject _nodeEvent;           // 巨大な弾を跳ね返す仲間(Node_Ep3Event)。最初は非表示
    [SerializeField] private float _nodeReflectDistance = 2.0f; // 巨大な弾が、仲間の斬撃の位置からこの距離(横方向)まで来たら跳ね返す

    [Header("会話イベント")]
    [SerializeField] private PlayableDirector _talkDirector;   // HPが2/3を切った時に再生する会話イベント
    [SerializeField] private int _talkPhase = 1;              // ボスのHPの段階がこの値に達したら会話イベントを始める(1: HP2/3以下)

    private ZelxFireBullet _watchingBullet; // 仲間が跳ね返すのを待っている巨大な弾
    private bool _isNodeReflected = false;  // 仲間が巨大な弾を跳ね返したか(1回だけ)
    private bool _isTalkStarted = false;
    private Transform _nodeSlashPoint;       // 仲間の斬撃の当たり判定の位置

    private void Start() {
        if (_boss != null) {
            _boss.OnGiantBulletFired += _OnGiantBulletFired;
            _boss.OnPhaseChanged += _OnPhaseChanged;
        }
        if (_battleStartDirector != null) _battleStartDirector.played += _OnBattleStart;
        if (_nodeEvent != null) {
            var damage_zone = _nodeEvent.GetComponentInChildren<DamageZone>(true);
            _nodeSlashPoint = damage_zone != null ? damage_zone.transform : _nodeEvent.transform;
        }
    }

    private void OnDestroy() {
        if (_boss != null) {
            _boss.OnGiantBulletFired -= _OnGiantBulletFired;
            _boss.OnPhaseChanged -= _OnPhaseChanged;
        }
        if (_battleStartDirector != null) _battleStartDirector.played -= _OnBattleStart;
        if (_talkDirector != null) _talkDirector.stopped -= _OnTalkEnded;
    }

    // ボス戦に入った直後: 炎と光の能力を持たせ、後で取得する能力(氷)は外しておく
    private void _OnBattleStart(PlayableDirector director) {
        var player = PlayerCharacterManager.Current as Player_Character;
        if (player == null) return;
        var join_slot = player.GetCurrentSlot(_joinAbility);
        if (join_slot.HasValue) player.RemoveAbility(join_slot.Value);
        foreach (var ability in _startAbilities) {
            _GiveAbility(player, ability);
        }
    }

    private void _OnGiantBulletFired(ZelxFireBullet bullet) {
        if (_isNodeReflected || _nodeEvent == null) return;
        _watchingBullet = bullet;
    }

    private void Update() {
        _UpdateNodeReflect();
    }

    // 巨大な弾が仲間の目の前まで来たら、仲間を表示して跳ね返す
    private void _UpdateNodeReflect() {
        if (_watchingBullet == null) return;
        // 届く前に消えた場合は、次に撃たれた巨大な弾を待つ
        if (_watchingBullet.IsReleased || _watchingBullet.IsReflected) {
            _watchingBullet = null;
            return;
        }
        if (Mathf.Abs(_watchingBullet.transform.position.x - _nodeSlashPoint.position.x) > _nodeReflectDistance) return;

        _isNodeReflected = true;
        _nodeEvent.SetActive(true);
        // ボスは上下に漂っているため、跳ね返した弾はボスへ向けて飛ばし、確実に当ててダウンさせる
        _watchingBullet.HitBySlash(_nodeEvent.GetComponentInChildren<SlashAttackInfo>(true), true);
        _watchingBullet = null;
    }

    // HPが2/3を切ったら会話イベントを始める。その間ボスは射撃しない
    private void _OnPhaseChanged(int phase) {
        if (_isTalkStarted || phase < _talkPhase) return;
        _isTalkStarted = true;
        _watchingBullet = null;
        _boss.SetAttackPaused(true);
        if (_talkDirector == null) {
            _OnTalkEnded(null);
            return;
        }
        _talkDirector.stopped += _OnTalkEnded;
        _talkDirector.gameObject.SetActive(true);
        _talkDirector.time = 0;
        _talkDirector.Play();
    }

    // 会話イベントの後: 氷の能力を取得し(仲間が加わるので、表示していた仲間は消す)、ボスの射撃を再開する
    private void _OnTalkEnded(PlayableDirector director) {
        if (_talkDirector != null) _talkDirector.stopped -= _OnTalkEnded;
        var player = PlayerCharacterManager.Current as Player_Character;
        if (player != null) _GiveAbility(player, _joinAbility);
        if (_nodeEvent != null) _nodeEvent.SetActive(false);
        if (_boss != null) _boss.SetAttackPaused(false);
    }

    // 能力を持っていなければ、割り当てられたボタンに装備する
    private static void _GiveAbility(Player_Character player, eAbilityType ability) {
        if (ability == eAbilityType.None || player.GetCurrentSlot(ability).HasValue) return;
        player.SetAbilitySlot(ability, PlayerParameter.Instance.GetAssignedSlot(ability));
    }
}
