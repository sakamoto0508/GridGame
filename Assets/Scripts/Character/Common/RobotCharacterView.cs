using UnityEngine;

/// <summary>パーツ回転式の簡易アニメーション。Visual以下のみを動かし、Root Motionは使いません。</summary>
public class RobotCharacterView : MonoBehaviour
{
    [SerializeField] private RobotVisualSettings _settings;
    [SerializeField] private Transform _facing;
    [SerializeField] private Transform _pose;
    [SerializeField] private Transform _leftArm, _rightArm, _leftLeg, _rightLeg;
    private MovementComponent _movement;
    private BombComponent _bomb;
    private BlockPlacementComponent _block;
    private CharacterMoveState _previousState;
    private Vector3 _previousPosition;
    private float _walkPhase, _idleTime, _placeRemaining, _landRemaining;

    private void Awake()
    {
        _movement = GetComponent<MovementComponent>();
        _bomb = GetComponent<BombComponent>();
        _block = GetComponent<BlockPlacementComponent>();
    }
    private void OnEnable()
    {
        _previousPosition = transform.position;
        _previousState = _movement != null ? _movement.State : CharacterMoveState.Grounded;
        _walkPhase = _idleTime = _placeRemaining = _landRemaining = 0;
        if (_bomb != null) _bomb.BombPlaced += OnPlaced;
        if (_block != null) _block.BlockPlaced += OnPlaced;
    }
    private void OnDisable()
    {
        if (_bomb != null) _bomb.BombPlaced -= OnPlaced;
        if (_block != null) _block.BlockPlaced -= OnPlaced;
    }
    private void OnPlaced() { if (_settings != null) _placeRemaining = _settings.PlacementDuration; }

    private void LateUpdate()
    {
        if (_movement == null || _settings == null || _facing == null || _pose == null) return;
        float dt = Time.deltaTime;
        if (dt <= 0) return;
        CharacterMoveState state = _movement.State;
        bool airborne = state == CharacterMoveState.Jumping || state == CharacterMoveState.Falling;
        bool wasAirborne = _previousState == CharacterMoveState.Jumping || _previousState == CharacterMoveState.Falling;
        if (wasAirborne && !airborne) _landRemaining = _settings.LandingDuration;
        Vector3 delta = transform.position - _previousPosition;
        delta.y = 0;
        bool walking = !airborne && delta.sqrMagnitude > 0.000001f;
        // 実移動距離で足踏み位相を進めるため、速度アップ時も足の回転が追従します。
        if (walking) _walkPhase += delta.magnitude / Mathf.Max(0.1f, _settings.StrideLength) * Mathf.PI * 2;
        _idleTime += dt;
        Vector3 direction = (Vector3)_movement.FacingDirection;
        if (direction.sqrMagnitude > 0)
            _facing.rotation = Quaternion.Slerp(_facing.rotation, Quaternion.LookRotation(direction), 1 - Mathf.Exp(-_settings.TurnSpeed * dt));
        float gait = walking ? Mathf.Sin(_walkPhase) * _settings.WalkSwing : 0;
        float armLeft = -gait, armRight = gait, legLeft = gait, legRight = -gait;
        float lean = walking ? _settings.WalkLean : 0;
        if (airborne)
        {
            armLeft = armRight = state == CharacterMoveState.Jumping ? -55 : -100;
            legLeft = legRight = state == CharacterMoveState.Jumping ? 35 : 12;
            lean = state == CharacterMoveState.Jumping ? -8 : 5;
        }
        if (_placeRemaining > 0)
        {
            float progress = 1 - _placeRemaining / Mathf.Max(0.05f, _settings.PlacementDuration);
            // 成功通知を受けた時だけ短く腕を突き出す。次の入力をブロックしません。
            armLeft = armRight = -75 * Mathf.Sin(progress * Mathf.PI);
            _placeRemaining = Mathf.Max(0, _placeRemaining - dt);
        }
        float squash = _landRemaining > 0 ? _settings.LandingSquash * _landRemaining / Mathf.Max(0.05f, _settings.LandingDuration) : 0;
        _landRemaining = Mathf.Max(0, _landRemaining - dt);
        _pose.localScale = new Vector3(1 + squash * 0.4f, 1 - squash, 1 + squash * 0.4f);
        _pose.localPosition = new Vector3(0, !airborne && !walking ? Mathf.Sin(_idleTime * _settings.IdleFrequency * Mathf.PI * 2) * _settings.IdleBob : 0, 0);
        float blend = 1 - Mathf.Exp(-_settings.PoseSpeed * dt);
        _pose.localRotation = Quaternion.Slerp(_pose.localRotation, Quaternion.Euler(lean, 0, 0), blend);
        Pose(_leftArm, armLeft, blend); Pose(_rightArm, armRight, blend);
        Pose(_leftLeg, legLeft, blend); Pose(_rightLeg, legRight, blend);
        _previousPosition = transform.position;
        _previousState = state;
    }
    private static void Pose(Transform joint, float angle, float blend)
    {
        if (joint != null) joint.localRotation = Quaternion.Slerp(joint.localRotation, Quaternion.Euler(angle, 0, 0), blend);
    }
}
