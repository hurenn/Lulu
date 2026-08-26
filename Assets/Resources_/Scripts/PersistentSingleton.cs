using UnityEngine;

/// <summary>
/// シーンをまたいで保持される（DontDestroyOnLoadで永続化される）シングルトンの基底クラス。
/// 経験値やチェックポイント情報など、ゲーム進行状態を保持するマネージャーに使用する。
/// </summary>
public abstract class PersistentSingleton<T> : MonoBehaviour where T : PersistentSingleton<T> {
    private static T _instance;

    public static T Instance {
        get {
            if (_instance == null) {
                _instance = FindAnyObjectByType<T>();
                if (_instance == null) {
                    var obj = new GameObject(typeof(T).Name);
                    _instance = obj.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    private void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }
        _instance = (T)this;
        // DontDestroyOnLoadはルートオブジェクトにしか使えない。
        // シーンに手動配置された子オブジェクトの場合はそのシーンの寿命に従う（元の挙動と同じ）
        if (transform.parent == null) {
            DontDestroyOnLoad(gameObject);
        }
        OnSingletonAwake();
    }

    private void OnDestroy() {
        if (_instance == this) {
            _instance = null;
        }
        OnSingletonDestroy();
    }

    /// <summary>
    /// このインスタンスが正規のシングルトンとして確定した際に呼ばれる（重複破棄される場合は呼ばれない）
    /// </summary>
    protected virtual void OnSingletonAwake() { }

    /// <summary>
    /// 破棄時に呼ばれる
    /// </summary>
    protected virtual void OnSingletonDestroy() { }

    /// <summary>
    /// 既存のインスタンスを破棄し、新規インスタンスを即座に生成する
    /// </summary>
    protected static void ForceRecreate() {
        if (_instance != null) {
            Destroy(_instance.gameObject);
            _instance = null;
        }
        // Destroy()は次フレーム末まで実際の破棄が遅延するため、
        // Instanceゲッター経由(FindAnyObjectByType)だと破棄予定の古いインスタンスを再度拾ってしまう。
        // ここでは必ず新規GameObjectを作成する
        var obj = new GameObject(typeof(T).Name);
        _instance = obj.AddComponent<T>();
    }
}
