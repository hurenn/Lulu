using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 水のゼルクスの弾幕1(プレイヤーの脇をかすめる弧を描く弾と、プレイヤーを狙う弾を、順不同・初速ばらばらで撃つ)の共通処理
/// ボス(Enemy_ZelxWater)とイベント用(Enemy_ZelxWater_Event)で使う
/// </summary>
public static class ZelxBarrage1 {
    /// <summary>
    /// 1発ごとの撃ち方の設定
    /// </summary>
    public struct Settings {
        public Vector2 speedRateRange;    // 初速の倍率の範囲
        public Vector2 missTurnRateRange; // かすめる弾の曲がる速さの範囲(度/秒。向きは左右ランダム)
        public Vector2 missDistanceRange; // かすめる弾が、プレイヤーからどれだけ横に外れた所を通るか
        public Vector2 lifetimeRange;     // 弾の寿命の範囲(撃ってからこの時間で爆発して消える)
    }

    /// <summary>
    /// これから撃つ弾の並びを作る(trueで狙う弾、falseでかすめる弾。順番はばらばら)
    /// </summary>
    public static void CreateQueue(List<bool> queue, int miss_count, int aim_count) {
        queue.Clear();
        for (int i = 0; i < miss_count; i++) queue.Add(false);
        for (int i = 0; i < aim_count; i++) queue.Add(true);
        for (int i = queue.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            (queue[i], queue[j]) = (queue[j], queue[i]);
        }
    }

    /// <summary>
    /// 弾を1発撃つ。狙う弾はプレイヤーへ真っすぐ、かすめる弾は弧を描いてプレイヤーの脇を通る
    /// </summary>
    /// <param name="fallback_direction">プレイヤーがいない時に撃つ向き</param>
    public static void Fire(ZelxFireBullet bullet, Vector2 origin, bool is_aim, Vector2 fallback_direction, Settings settings, Action<ZelxFireBullet> on_release, IDamageable owner) {
        float speed_rate = Random.Range(settings.speedRateRange.x, settings.speedRateRange.y);
        float lifetime = Random.Range(settings.lifetimeRange.x, settings.lifetimeRange.y);
        var target = PlayerCharacterManager.Current != null ? PlayerCharacterManager.Current.transform : null;
        if (target == null) {
            bullet.FireStraight(fallback_direction, speed_rate, on_release, owner);
            bullet.SetLifetime(lifetime);
            return;
        }

        Vector2 to_target = (Vector2)target.position - origin;
        if (is_aim) {
            bullet.FireStraight(to_target, speed_rate, on_release, owner);
            bullet.SetLifetime(lifetime);
            return;
        }

        // プレイヤーから横へ外した点を、円弧がちょうど通るように撃ち出す向きを決める
        // (円弧は、弦の向きから曲がる角度の合計の半分だけ手前に傾けて撃ち出すと、弦の端を通る)
        Vector2 side = new Vector2(-to_target.y, to_target.x).normalized * (Random.value < 0.5f ? -1f : 1f);
        Vector2 aim_pos = (Vector2)target.position + side * Random.Range(settings.missDistanceRange.x, settings.missDistanceRange.y);
        float turn_rate = Random.Range(settings.missTurnRateRange.x, settings.missTurnRateRange.y) * (Random.value < 0.5f ? -1f : 1f);
        Vector2 chord = aim_pos - origin;
        float total_turn = GetArcTotalTurn(chord.magnitude, bullet.BaseSpeed * speed_rate, Mathf.Abs(turn_rate));
        float launch_angle = (Mathf.Atan2(chord.y, chord.x) * Mathf.Rad2Deg - total_turn * 0.5f * Mathf.Sign(turn_rate)) * Mathf.Deg2Rad;
        bullet.FireArc(new Vector2(Mathf.Cos(launch_angle), Mathf.Sin(launch_angle)), speed_rate, turn_rate, on_release, owner, target);
        bullet.SetLifetime(lifetime);
    }

    /// <summary>
    /// 弦の長さ・弾の速さ・曲がる速さから、円弧を描いて弦の端に届くまでに曲がる角度の合計(度)を求める
    /// </summary>
    public static float GetArcTotalTurn(float chord_length, float speed, float turn_rate) {
        if (speed <= 0f) return 0f;
        float theta = turn_rate * chord_length / speed; // 最初は弦の長さで近似し、円弧の長さで求め直して収束させる
        for (int iter = 0; iter < 4; iter++) {
            float half_rad = Mathf.Clamp(theta, 1f, 170f) * 0.5f * Mathf.Deg2Rad;
            float arc_length = chord_length * half_rad / Mathf.Sin(half_rad);
            theta = turn_rate * arc_length / speed;
        }
        return Mathf.Min(theta, 170f);
    }
}
