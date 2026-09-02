using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの入力を受け取り、キャラクターの移動を制御するクラス
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField] private MovementComponent _movementComponent;
    [SerializeField] private BlockPlacementComponent _blockPlacementComponent;
    private Camera _camera;
    private Vector2 _moveInput;

    /// <summary>Spawnerからゲーム用Cameraを受け取ります。</summary>
    public void Init(Camera gameCamera)
    {
        _camera= gameCamera;
    }

    /// <summary>
    /// 移動入力を保存し、入力開始時にカメラ基準の水平移動を要求します。
    /// 保存した入力は方向付きジャンプにも利用します。
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();

        if (!context.performed)
            return;

        Vector3Int direction = ConvertToGridDirection(_moveInput);

        if (direction != Vector3Int.zero)
        {
            _movementComponent.TryMove(direction);
        }
    }

    /// <summary>
    /// 現在押されている方向でジャンプします。方向入力がなければその場ジャンプになります。
    /// </summary>
    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.started)
            return;

        Vector3Int direction = ConvertToGridDirection(_moveInput);
        _movementComponent.TryJump(direction);
    }

    /// <summary>地上では正面、ジャンプ中では直下へのBlock配置を要求します。</summary>
    public void OnPlaceBlock(InputAction.CallbackContext context)
    {
        if (!context.started)
            return;

        if (_blockPlacementComponent == null)
        {
            Debug.LogWarning(
                "Block配置入力を受け取りましたが、PlayerControllerのBlock Placement Componentが未設定です。",
                this);
            return;
        }

        _blockPlacementComponent.TryPlaceBlock();
    }

    /// <summary>
    /// 入力された2Dベクトルをグリッド方向に変換する。入力が正の値の場合は1、負の値の場合は-1、0の場合は0に変換される。
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private Vector3Int ConvertToGridDirection(Vector2 input)
    {
        if (input.sqrMagnitude < 0.01f || _camera == null)
            return Vector3Int.zero;

        Transform cameraTransform = _camera.transform;

        // カメラの傾きを除き、地面上の方向にする
        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;

        Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        // 入力をカメラ基準のワールド方向へ変換
        Vector3 desiredDirection = cameraRight * input.x + cameraForward * input.y;

        // 斜め移動を防ぎ、X/Zの強い方向へ丸める
        if (Mathf.Abs(desiredDirection.x) > Mathf.Abs(desiredDirection.z))
        {
            return new Vector3Int(desiredDirection.x > 0f ? 1 : -1, 0, 0);
        }

        return new Vector3Int(0, 0, desiredDirection.z > 0f ? 1 : -1);
    }
}
