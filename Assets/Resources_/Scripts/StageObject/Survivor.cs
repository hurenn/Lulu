using System.Collections;
using UnityEngine;

// 救出対象1人分。プレイヤーが触れたらRescueMissionへ伝え、チェックポイントと同様に復帰位置を保存して姿を消す
public class Survivor : StageObject_Base {
    [SerializeField] private RescueMission _mission; // 未設定の場合はシーン中から自動取得する
    [SerializeField] private string _rescuedStateName = "Rescued"; // 救出時に再生するAnimatorのステート名（見つからない場合は即座に消える）

    protected bool _isRescued = false;

    // 救出済みか（リトライ時、Start前に参照されても判定できるようCheckpointManagerの記録も見る）
    public bool IsRescued => _isRescued || CheckpointManager.Instance.IsRescuedSurvivor(SurvivorId);

    // 救出時にRescueMissionの救出メッセージを出すか（イベントキャラは出さない）
    protected virtual bool _ShowsRescueMessage => true;

    // リトライ時に救出済みか判定するためのID（同名のSurvivorが並ぶため、名前ではなく兄弟順のパスで識別する）
    // プレイ中に前の兄弟がDestroyされると兄弟順がずれるため、読み込み直後（Awake）の値を保持して使う
    private string _survivorId = null;
    public string SurvivorId {
        get {
            if (_survivorId == null) _survivorId = _CalcSurvivorId();
            return _survivorId;
        }
    }

    private string _CalcSurvivorId() {
        string id = transform.GetSiblingIndex().ToString();
        for (var parent = transform.parent; parent != null; parent = parent.parent) {
            id = parent.GetSiblingIndex() + "/" + id;
        }
        return id;
    }

    private void Awake() {
        _survivorId = _CalcSurvivorId();
        if (_mission == null) _mission = FindAnyObjectByType<RescueMission>();
    }

    private void Start() {
        // リトライ時：救出済みなら最初から消しておく（人数はRescueMissionが復元する）
        if (CheckpointManager.Instance.IsRescuedSurvivor(SurvivorId)) {
            _isRescued = true;
            gameObject.SetActive(false);
        }
    }

    protected override void _HitPlayer(Player_Character player) {
        _Rescue(player);
    }

    /// <summary>
    /// 救出処理（救出カウント・チェックポイント保存・姿を消す）
    /// </summary>
    protected void _Rescue(Player_Character player) {
        if (_isRescued) return;
        if (_mission == null) {
            Debug.LogError("Survivor: RescueMissionがシーン中に見つかりません");
            return;
        }
        _isRescued = true;

        // チェックポイントと同様の処理（リトライ時はこの場所から復帰）
        var checkpoint_manager = CheckpointManager.Instance;
        checkpoint_manager.SaveRescuedSurvivor(SurvivorId, _ShowsRescueMessage);
        checkpoint_manager.SaveCheckpoint(transform.position, player.GetParameterSnapshot());
        player.SaveAbilitySlot(); // 能力保存

        _mission.OnSurvivorRescued(_ShowsRescueMessage);

        _Disappear();
    }

    /// <summary>
    /// 救出アニメーションを流して姿を消す（アニメーションが無ければ即座に消す）
    /// </summary>
    private void _Disappear() {
        foreach (var col in GetComponents<Collider2D>()) {
            col.enabled = false;
        }

        if (_animator == null || _animator.runtimeAnimatorController == null
            || !_animator.HasState(0, Animator.StringToHash(_rescuedStateName))) {
            gameObject.SetActive(false);
            return;
        }
        StartCoroutine(_DisappearCo());
    }

    private IEnumerator _DisappearCo() {
        _animator.enabled = true;
        _animator.Play(_rescuedStateName, 0, 0f);
        yield return null; // Play()の反映は次フレーム

        // 救出ステートの再生が終わるまで待つ
        while (true) {
            var state = _animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(_rescuedStateName) || state.normalizedTime >= 1f) {
                break;
            }
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
