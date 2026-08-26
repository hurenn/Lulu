using UnityEngine;

/// <summary>
/// シーンに紐づくシングルトンの基底クラス。DontDestroyOnLoadはせず、シーンごとに生成・破棄される。
/// カメラやロックオン対象など、そのシーン内のオブジェクトへの参照を持つマネージャーに使用する。
/// </summary>
public abstract class SceneSingleton<T> : MonoBehaviour where T : SceneSingleton<T> {
    protected static T _instance;
    public static T Instance => _instance;
    public static bool HasInstance => _instance != null;

    private void Awake() {
        if (_instance != null && _instance != this) {
            Debug.LogWarning($"{typeof(T).Name}が複数存在するため、二重生成分を削除します");
            Destroy(gameObject);
            return;
        }
        _instance = (T)this;
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
    /// インスタンスが無ければ新規生成する
    /// </summary>
    protected static T InstantiateIfMissing() {
        if (_instance == null) {
            var obj = new GameObject(typeof(T).Name);
            _instance = obj.AddComponent<T>();
        }
        return _instance;
    }
}
