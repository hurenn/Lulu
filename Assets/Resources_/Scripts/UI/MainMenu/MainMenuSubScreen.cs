using UnityEngine;

/// <summary>
/// メインメニューの項目から開く画面(ステージセレクト・よみもの など)の共通部分。
/// MainMenuManagerが部屋のズームとクロスフェードを終えてからOpenを呼び、
/// 画面側は戻る操作をされたらCloseしてonCancelを呼ぶ(MainMenuManagerが逆再生で部屋へ戻す)
/// </summary>
public abstract class MainMenuSubScreen : MonoBehaviour {
    /// <summary>
    /// 開く
    /// </summary>
    /// <param name="onCancel">戻る操作をされた時の処理</param>
    public abstract void Open(System.Action onCancel);

    /// <summary>
    /// 閉じる
    /// </summary>
    public abstract void Close();
}
