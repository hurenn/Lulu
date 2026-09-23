using UnityEngine;

// 草むら1つ分。潜み状態を表示し、プレイヤーが近づいたら逃げるアニメを再生する
public class BrushwoodHidingSpot : MonoBehaviour {
    [SerializeField] private Animator _anim;

    private bool _isHiding = false;
    public event System.Action OnFled; // 逃げた時に呼ばれる(次の草むらへの切り替え用)

    private void Awake() {
        if (_anim == null) _anim = GetComponent<Animator>();
        _anim.enabled = true;
    }

    // このスポットを潜み状態にする
    public void ShowHiding() {
        _isHiding = true;
        _anim.SetTrigger("View");
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (!_isHiding || !collision.CompareTag("Player")) return;
        _isHiding = false;
        _anim.SetTrigger("Hide");
        OnFled?.Invoke();
    }
}
