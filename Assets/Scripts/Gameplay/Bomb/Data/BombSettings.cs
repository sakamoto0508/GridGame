using UnityEngine;

/// <summary>Bombの所持数、Fuse、爆風、落下速度を設定します。</summary>
[CreateAssetMenu(fileName = "BombSettings", menuName = "3D Grid Bomber/Settings/Bomb")]
public class BombSettings : ScriptableObject
{
    [SerializeField, Min(1)] private int _maxBombCount = 1;
    [SerializeField, Min(0f)] private float _fuseTime = 3f;
    [SerializeField, Min(1)] private int _explosionPower = 1;
    [SerializeField, Min(0f)] private float _fallDurationPerCell = 0.12f;

    public int MaxBombCount => _maxBombCount;
    public float FuseTime => _fuseTime;
    public int ExplosionPower => _explosionPower;
    public float FallDurationPerCell => _fallDurationPerCell;
}
