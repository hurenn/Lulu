using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 地形への自動装飾の設定。どこに・何を・どれくらいの密度で置くかをルールとして持つ。
/// 実際の配置はエディタ拡張 TerrainDecorator(Editor側)がシーンに焼き込む。実行時の処理は無い。
/// </summary>
[CreateAssetMenu(menuName = "Lulu/Terrain Decoration Profile", fileName = "TerrainDecorationProfile")]
public class TerrainDecorationProfile : ScriptableObject {
    /// <summary>装飾を置く位置の種類</summary>
    public enum ePlacement {
        Top,        // 地面の上(地形の真上の空きマス)
        Ceiling,    // 天井の下(地形の真下の空きマス)
        WallLeft,   // 壁の左側(地形の左隣の空きマス。壁の左面に貼り付く)
        WallRight,  // 壁の右側(地形の右隣の空きマス。壁の右面に貼り付く)
        Inner,      // 地形の内部(地形マスそのものに重ねる)
    }

    [Serializable]
    public class Variant {
        public TileBase tile;
        [Min(0f)] public float weight = 1f;
    }

    [Serializable]
    public class Rule {
        public string name = "Rule";
        public bool enabled = true;
        public ePlacement placement = ePlacement.Top;

        [Tooltip("この地形タイルの上にだけ置く。空なら全ての地形タイルが対象")]
        public List<TileBase> targetTerrainTiles = new List<TileBase>();

        [Tooltip("置く装飾。重みに応じてランダムに選ばれる")]
        public List<Variant> variants = new List<Variant>();

        [Tooltip("候補マスに置く確率")]
        [Range(0f, 1f)] public float density = 0.3f;

        [Tooltip("同じ配置先(Top/Ceiling等)の装飾どうしの最低間隔(マス)。0で隣接可")]
        [Min(0)] public int minSpacing = 1;

        [Tooltip("Top/Ceiling: 平らな面がこのマス数以上続く所だけに置く(細い足場を除外)。Wall: 壁の高さ")]
        [Min(1)] public int minSurfaceLength = 3;

        [Tooltip("Top/Ceiling/Wall: 面の端からこのマス数は置かない")]
        [Min(0)] public int edgeMargin = 1;

        [Tooltip("Top/Ceiling/Wall: 装飾の向きにこのマス数の空きが必要(背の高い装飾用)")]
        [Min(1)] public int clearance = 1;

        [Tooltip("Inner: 地形の表面からこのマス数以上内側だけに置く")]
        [Min(1)] public int innerDepth = 2;
    }

    [Tooltip("乱数シード。シーン名と組み合わせるので、同じシードでもステージごとに配置は変わる")]
    public int seed = 12345;

    [Tooltip("装飾の対象にする地形タイル。空ならシーン内の地形Tilemapのタイル全て")]
    public List<TileBase> terrainTiles = new List<TileBase>();

    [Tooltip("この名前のTilemapは地形として扱わない(扉など動く物)。周囲の装飾も避ける")]
    public List<string> excludedTilemapNames = new List<string> { "Door" };

    [Tooltip("除外Tilemap(扉など)の周りで装飾を置かない距離(マス)")]
    [Min(0)] public int excludedTilemapMargin = 2;

    [Tooltip("装飾Tilemapの描画順。地形Tilemapの最大のOrder in Layerにこの値を足す")]
    public int sortingOrderOffset = 1;

    public List<Rule> rules = new List<Rule>();
}
