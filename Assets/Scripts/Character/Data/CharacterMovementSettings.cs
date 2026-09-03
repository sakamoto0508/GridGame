using UnityEngine;

/// <summary>Character共通の移動・ジャンプ・落下時間を設定します。</summary>
[CreateAssetMenu(fileName = "CharacterMovementSettings", menuName = "3D Grid Bomber/Settings/Character Movement")]
public class CharacterMovementSettings : ScriptableObject
{
    [Header("Move")]
    [SerializeField, Min(0f)] private float _moveDuration = 0.15f;

    [Header("Jump")]
    [SerializeField, Min(0f)] private float _jumpUpDuration = 0.15f;
    [SerializeField, Min(0f)] private float _airTime = 0.15f;
    [SerializeField, Min(0f)] private float _fallDuration = 0.15f;
    [SerializeField, Min(0f)] private float _jumpArcHeight = 0.5f;

    public float MoveDuration => _moveDuration;
    public float JumpUpDuration => _jumpUpDuration;
    public float AirTime => _airTime;
    public float FallDuration => _fallDuration;
    public float JumpArcHeight => _jumpArcHeight;
}
