using UnityEngine;

[CreateAssetMenu(fileName = "BuildConfig", menuName = "Lulu/BuildConfig", order = 0)]
public class BuildConfig : ScriptableObject
{
    public enum eBuildType
    {
        Product,    // 製品版
        Demo,      // 体験版
        Exhibition, // 展示版
        Test,       // テスト版
    }

    [System.Flags]
    public enum eBuildTypeMask
    {
        None = 0,
        Product = 1 << (int)eBuildType.Product,
        Demo = 1 << (int)eBuildType.Demo,
        Exhibition = 1 << (int)eBuildType.Exhibition,
        Test = 1 << (int)eBuildType.Test,
    }

    private const string _resourcePath = "BuildConfig";

    private static BuildConfig _instance;
    public static BuildConfig Instance {
        get {
            if (_instance == null) {
                _instance = Resources.Load<BuildConfig>(_resourcePath);
            }
            return _instance;
        }
    }

    [SerializeField] private eBuildType _buildType = eBuildType.Product;
    public eBuildType BuildType => _buildType;

    /// <summary>現在のビルドタイプがマスクに含まれるか判定</summary>
    public bool Matches(eBuildTypeMask mask) {
        return (mask & (eBuildTypeMask)(1 << (int)_buildType)) != 0;
    }
}
