using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// メッセージウィンドウの左右にキャラクターアイコンを出し、会話しているように見せる。
/// 話しているキャラは明るく表示して跳ねさせ、聞いている側は暗く少し小さくする。
/// 左右の割り当ては、メッセージの指定(MessageData.iconSide)が無ければ会話の流れで自動で決める:
///   ・すでにどちらかに出ているキャラは同じ側
///   ・空いている側(左→右の順)に新しいキャラを出す
///   ・両方埋まっていれば、直前に話していない側を入れ替える
/// 割り当ては ResetSides を呼ぶまで覚えておく(Timelineのクリップごとに窓が閉じても同じ側に出すため)。
/// アイコンの出入り(外から滑り込む・外へ出ていく)もここで動かす。
///
/// アイコンは2種類:
///   ・普段(ステージ中に流れる案内): 窓の中の小さいアイコン(MessagePanel/IconField)
///   ・会話(イベント・はなす): 窓の後ろに大きく出すアイコン(ConversationIcons/Large*IconField)
/// 会話用は Prefab 上で枠(RectMask2D)とアイコンの位置・大きさをそのまま調整できる。
/// 調整する時は ConversationIcons を表示にして見る(実行時はこの部品が表示にする)。
/// 実行時に動かすのはアイコンの位置(出入り・跳ねる・聞き手の沈み)・拡縮(聞き手)・色だけで、
/// Prefab で置いたアイコンの位置・大きさが「話している時」の見た目になる
/// </summary>
public class MessageSpeakerIcons : MonoBehaviour {
    public enum eSide {
        None = -1,
        Left = 0,
        Right = 1,
    }

    // 普段用・会話用それぞれのアイコン1枚分
    [System.Serializable]
    private class IconView {
        public RectTransform field;   // アイコンの見える範囲(RectMask2D)
        public Image icon;

        [System.NonSerialized] public Vector2 basePos;       // 出ている時の位置
        [System.NonSerialized] public float baseScale = 1f; // 話している時の大きさ
        [System.NonSerialized] public float slideOffset;     // 外に隠れている時の横のずれ
    }

    [System.Serializable]
    private class Slot {
        public IconView normal;       // 普段の小さいアイコン
        public IconView conversation; // 会話で大きく出すアイコン
        public float normalSlideOffset; // 普段のアイコンが外に隠れている時の横のずれ(左は負、右は正)
        public bool flipX;            // 左右反転する(右側のキャラを内側へ向けるため)
        public RectTransform nameAnchor; // 会話中にキャラ名を出す位置の目印(ConversationIcons の下。Prefabで動かして調整する)

        [System.NonSerialized] public string characterKey;   // 出ているキャラ(空なら誰もいない)
        [System.NonSerialized] public Sprite sprite;
        [System.NonSerialized] public float slideRate;       // 0:外に隠れている 1:出ている
        [System.NonSerialized] public float targetSlide;
        [System.NonSerialized] public float slideDelay;
        [System.NonSerialized] public float focus;           // 0:聞いている 1:話している
        [System.NonSerialized] public float bounce;          // 跳ねている高さ
        [System.NonSerialized] public Coroutine bounceCoroutine;
    }

    [SerializeField] private Slot _left;
    [SerializeField] private Slot _right;
    [SerializeField] private GameObject _conversationRoot;       // 会話用アイコンの親(Prefabでは調整時以外は非表示にしておく)
    [SerializeField] private float _slideTime = 0.17f;            // 出入りの時間(秒)
    [SerializeField] private float _slideInDelay = 0.08f;         // 窓が開いてからアイコンが出始めるまで(秒)
    [SerializeField] private float _focusSpeed = 12f;             // 話す・聞くの切り替えの速さ
    [SerializeField] private Color _listenerColor = new Color(0.55f, 0.55f, 0.6f, 1f); // 聞いている側の色
    [SerializeField] private float _listenerScale = 0.94f;        // 聞いている側の大きさ
    [SerializeField] private float _listenerDrop = 20f;           // 聞いている側を下げる量
    [SerializeField] private float _bounceHeight = 20f;           // 跳ねる高さ
    [SerializeField] private float _bounceDuration = 0.2f;        // 跳ねる時間(秒)

    private bool _isConversation;   // 会話用の大きいアイコンを使っているか
    private eSide _speakingSide = eSide.None;
    private eSide _lastSpokenSide = eSide.None;
    private bool _isShown;          // 窓が開いていてアイコンを出している状態か
    private bool _initialized;

    /// <summary>右側に誰かいるか(本文の幅を左右対称にするかの判定に使う)</summary>
    public bool HasRight => !string.IsNullOrEmpty(_right.characterKey);

    /// <summary>今話している側</summary>
    public eSide SpeakingSide => _speakingSide;

    private void Awake() {
        _Initialize();
    }

    private void _Initialize() {
        if (_initialized) return;
        _initialized = true;
        if (_conversationRoot != null) _conversationRoot.SetActive(true);
        foreach (var slot in _Slots()) {
            // 名前の目印は位置を読むだけなので、見本の表示は隠す
            if (slot.nameAnchor != null) slot.nameAnchor.gameObject.SetActive(false);
            var normal = slot.normal;
            if (normal.icon != null) {
                // 普段のアイコンは枠の中央(シーン側に以前のアニメで動いた位置が残っていても使わない)
                normal.basePos = Vector2.zero;
                normal.baseScale = 1f;
                normal.slideOffset = slot.normalSlideOffset;
            }
            var conv = slot.conversation;
            if (conv.icon != null) {
                // 会話用はPrefabで置いた位置・大きさを「話している時」の見た目にする
                var rt = conv.icon.rectTransform;
                conv.basePos = rt.anchoredPosition;
                conv.baseScale = Mathf.Abs(rt.localScale.y);
                // 枠の外まで隠れるずれ(枠の半分+アイコンの半分)。右側は右へ、左側は左へ隠す
                float fieldWidth = conv.field != null ? conv.field.rect.width : rt.rect.width;
                float hide = fieldWidth * 0.5f + rt.rect.width * conv.baseScale * 0.5f + Mathf.Abs(conv.basePos.x);
                conv.slideOffset = slot.normalSlideOffset < 0f ? -hide : hide;
                // 会話用は窓の後ろに大きく出すので、窓全体の半透明を受けないようにする
                if (conv.field != null) {
                    var group = conv.field.GetComponent<CanvasGroup>();
                    if (group == null) group = conv.field.gameObject.AddComponent<CanvasGroup>();
                    group.ignoreParentGroups = true;
                    group.blocksRaycasts = false;
                    group.interactable = false;
                }
            }
            slot.sprite = null;
            slot.slideRate = 0f;
            slot.targetSlide = 0f;
            _Apply(slot);
        }
    }

    private Slot[] _Slots() => new[] { _left, _right };
    private Slot _Get(eSide side) => side == eSide.Left ? _left : side == eSide.Right ? _right : null;
    private static eSide _Opposite(eSide side) => side == eSide.Left ? eSide.Right : eSide.Left;
    private IconView _Active(Slot slot) => _isConversation && slot.conversation.icon != null ? slot.conversation : slot.normal;

    /// <summary>
    /// 左右の割り当てを忘れる(次の会話を最初から並べ直す)
    /// </summary>
    public void ResetSides() {
        _Initialize();
        foreach (var slot in _Slots()) {
            slot.characterKey = null;
            slot.sprite = null;
            slot.targetSlide = 0f;
            slot.slideRate = 0f;
            _Apply(slot);
        }
        _speakingSide = eSide.None;
        _lastSpokenSide = eSide.None;
    }

    /// <summary>
    /// 会話中にキャラ名を出す位置の目印(無ければnull)
    /// </summary>
    public RectTransform GetNameAnchor(eSide side) {
        var slot = _Get(side);
        return slot != null ? slot.nameAnchor : null;
    }

    /// <summary>
    /// アイコンの画像名からキャラを表す文字列を取り出す(最初の'_'まで。例: Lulu_SmileTalk → Lulu)
    /// </summary>
    public static string GetCharacterKey(Sprite icon) {
        if (icon == null) return string.Empty;
        return icon.name.Split('_')[0];
    }

    /// <summary>
    /// 会話用の大きいアイコンを使うか(イベント・はなす・話しかけた会話は会話用、ステージ中に流れる案内は普段用)
    /// </summary>
    public void SetConversation(bool conversation) {
        _Initialize();
        if (_isConversation == conversation) return;
        _isConversation = conversation;
        foreach (var slot in _Slots()) _Apply(slot);
    }

    /// <summary>
    /// 話すキャラを出す。どちら側に出したかを返す(キャラがいない語りの行はNone)
    /// </summary>
    public eSide ShowSpeaker(Sprite icon, eMessageIconSide requested) {
        _Initialize();
        string key = GetCharacterKey(icon);
        if (string.IsNullOrEmpty(key)) {
            // 誰も話していない(語り・空のアイコン): 両方とも聞いている側の見た目にする
            _speakingSide = eSide.None;
            return eSide.None;
        }

        eSide side = _DecideSide(key, requested);
        var slot = _Get(side);
        // 反対側に同じキャラがいれば外す(左右の指定で移動した時)
        var other = _Get(_Opposite(side));
        if (other.characterKey == key) {
            other.characterKey = null;
            other.targetSlide = 0f;
        }

        bool isNewCharacter = slot.characterKey != key;
        slot.characterKey = key;
        slot.sprite = icon;
        if (isNewCharacter && _isShown) {
            // 窓が開いたまま別のキャラに入れ替わる時は、外から滑り込ませ直す
            slot.slideRate = 0f;
            slot.slideDelay = 0f;
        }
        if (_isShown) slot.targetSlide = 1f;

        _speakingSide = side;
        _lastSpokenSide = side;
        _Apply(slot);
        return side;
    }

    /// <summary>
    /// 会話形式でないメッセージ(ステージ中に自動で流れる案内など): 今まで通り左だけにアイコンを出す。
    /// 右側は使わず、キャラのいない行(語り)は左のアイコンも消す
    /// </summary>
    public eSide ShowSingle(Sprite icon) {
        _Initialize();
        _right.characterKey = null;
        _right.targetSlide = 0f;
        if (string.IsNullOrEmpty(GetCharacterKey(icon))) {
            _left.characterKey = null;
            _left.targetSlide = 0f;
            _speakingSide = eSide.None;
            return eSide.None;
        }
        return ShowSpeaker(icon, eMessageIconSide.Left);
    }

    private eSide _DecideSide(string key, eMessageIconSide requested) {
        if (requested == eMessageIconSide.Left) return eSide.Left;
        if (requested == eMessageIconSide.Right) return eSide.Right;
        if (_left.characterKey == key) return eSide.Left;
        if (_right.characterKey == key) return eSide.Right;
        if (string.IsNullOrEmpty(_left.characterKey)) return eSide.Left;
        if (string.IsNullOrEmpty(_right.characterKey)) return eSide.Right;
        // 両方埋まっている: 直前に話していない側を入れ替える
        return _lastSpokenSide == eSide.None ? eSide.Right : _Opposite(_lastSpokenSide);
    }

    /// <summary>
    /// 窓が開いた: 出ているキャラを外から滑り込ませる
    /// </summary>
    public void SlideIn() {
        _Initialize();
        _isShown = true;
        foreach (var slot in _Slots()) {
            if (string.IsNullOrEmpty(slot.characterKey)) continue;
            slot.slideDelay = _slideInDelay;
            slot.targetSlide = 1f;
        }
    }

    /// <summary>
    /// 窓が閉じる: 全員を外へ出す(割り当ては覚えたまま)
    /// </summary>
    public void SlideOut() {
        _Initialize();
        _isShown = false;
        foreach (var slot in _Slots()) {
            slot.slideDelay = 0f;
            slot.targetSlide = 0f;
        }
    }

    /// <summary>
    /// 話している側のアイコンを一瞬跳ねさせる
    /// </summary>
    public void BounceSpeaker() {
        var slot = _Get(_speakingSide);
        if (slot == null || !isActiveAndEnabled) return;
        if (slot.bounceCoroutine != null) StopCoroutine(slot.bounceCoroutine);
        slot.bounceCoroutine = StartCoroutine(_BounceCo(slot));
    }

    private IEnumerator _BounceCo(Slot slot) {
        float elapsed = 0f;
        while (elapsed < _bounceDuration) {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _bounceDuration);
            slot.bounce = _bounceHeight * 4f * t * (1f - t); // 放物線状に跳ねる
            yield return null;
        }
        slot.bounce = 0f;
        slot.bounceCoroutine = null;
    }

    /// <summary>
    /// アイコンの範囲(プレイヤーと重なった時に窓を薄くする判定用)
    /// </summary>
    public bool ContainsScreenPoint(Vector2 screenPoint) {
        foreach (var slot in _Slots()) {
            var view = _Active(slot);
            if (view.icon == null || string.IsNullOrEmpty(slot.characterKey) || slot.slideRate <= 0f) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(view.icon.rectTransform, screenPoint, null)) return true;
        }
        return false;
    }

    // MessageViewerがUpdateでアイコンの透明度を変えた後に上書きするため、LateUpdateで動かす
    private void LateUpdate() {
        float dt = Time.unscaledDeltaTime;
        foreach (var slot in _Slots()) {
            // 出入り
            if (slot.slideDelay > 0f) {
                slot.slideDelay -= dt;
            } else if (_slideTime > 0f) {
                slot.slideRate = Mathf.MoveTowards(slot.slideRate, slot.targetSlide, dt / _slideTime);
            } else {
                slot.slideRate = slot.targetSlide;
            }
            // 話す・聞くの見た目
            bool speaking = _speakingSide != eSide.None && slot == _Get(_speakingSide);
            slot.focus = Mathf.Lerp(slot.focus, speaking ? 1f : 0f, 1f - Mathf.Exp(-_focusSpeed * dt));
            _Apply(slot);
        }
    }

    private void _Apply(Slot slot) {
        var active = _Active(slot);
        _ApplyView(slot, slot.normal, active == slot.normal, keepAlpha: true);
        _ApplyView(slot, slot.conversation, active == slot.conversation, keepAlpha: false);
    }

    // keepAlpha: 普段用はMessageViewerがプレイヤーとの重なりで透明度を変えるのでそのまま。会話用は不透明
    private void _ApplyView(Slot slot, IconView view, bool isActive, bool keepAlpha) {
        if (view.icon == null) return;
        bool visible = isActive && !string.IsNullOrEmpty(slot.characterKey) && slot.sprite != null;
        if (view.icon.enabled != visible) view.icon.enabled = visible;
        if (!visible) return;

        if (view.icon.sprite != slot.sprite) view.icon.sprite = slot.sprite;
        var rt = view.icon.rectTransform;
        float eased = 1f - Mathf.Pow(1f - slot.slideRate, 2f);
        float x = view.basePos.x + view.slideOffset * (1f - eased);
        float y = view.basePos.y + slot.bounce - _listenerDrop * (1f - slot.focus);
        rt.anchoredPosition = new Vector2(x, y);
        float scale = view.baseScale * Mathf.Lerp(_listenerScale, 1f, slot.focus);
        rt.localScale = new Vector3(slot.flipX ? -scale : scale, scale, 1f);
        // 色は明るさだけ変える
        var color = Color.Lerp(_listenerColor, Color.white, slot.focus);
        color.a = keepAlpha ? view.icon.color.a : 1f;
        view.icon.color = color;
    }
}
