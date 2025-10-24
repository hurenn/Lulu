using UnityEngine;

[CreateAssetMenu(fileName = "ExParameter", menuName = "Lulu/ExStatus", order = 1)]
public class ExParameter : ScriptableObject {
    [Header("行動インターバル")]
    public float ActionInterval = 3.0f;

    [Header("攻撃の速さ")]
    public float ShootTime = 1.0f;
    public float RainShootTime = 0.8f;
    public float BurstTime = 1.5f;
    public float ThreeShootTime = 0.8f;
    public float JumpShootWait = 0.1f;
    public float JumpShootTime = 0.2f;
    public float SpecialShootTime = 5.0f;

    [Header("レーザーから爆発までの時間")]
    public float ShootExplosionTime = 1.0f;

    [Header("行動の重み")]
    public int ShootWeight = 15;
    public int RainShootWeight = 40;
    public int BurstWeight = 15;
    public int ThreeShootWeight = 15;
    public int JumpShootWeight = 10;

    [Header("攻撃インターバル")]
    public float ShootInterval = 0.2f;
}