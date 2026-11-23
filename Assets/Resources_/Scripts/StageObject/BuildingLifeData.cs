using UnityEngine;

/// <summary>
/// 背景の建物の絵の「窓」と「屋上」の位置(StageAmbienceが窓の明かりと煙を付ける場所)。
/// 位置はスプライトのピボットを原点としたローカル座標(ユニット)。エディタで絵を解析して作る。
/// </summary>
[CreateAssetMenu(menuName = "Lulu/Building Life Data", fileName = "BuildingLifeData")]
public class BuildingLifeData : ScriptableObject {
    [Tooltip("対象の絵。このスプライトを表示している建物にだけ付ける")]
    public Sprite sprite;
    [Tooltip("明かりを点ける窓(ローカル座標の矩形)")]
    public Rect[] windows = new Rect[0];
    [Tooltip("煙を上げる屋上の位置(ローカル座標)")]
    public Vector2[] smokePoints = new Vector2[0];
}
