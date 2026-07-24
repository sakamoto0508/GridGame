using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの入力を受け取り、キャラクターの移動を制御するクラス
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField] private MovementComponent _movementComponent;
    [SerializeField] private Camera _camera;

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        Vector2 input = context.ReadValue<Vector2>();
        Vector3Int direction = ConvertToGridDirection(input);

        if (direction != Vector3Int.zero)
        {
            _movementComponent.TryMove(direction);
        }
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
        if (Mathf.Abs(desiredDirection.x) >
            Mathf.Abs(desiredDirection.z))
        {
            return new Vector3Int(
                desiredDirection.x > 0f ? 1 : -1,
                0,
                0);
        }

        return new Vector3Int(
            0,
            0,
            desiredDirection.z > 0f ? 1 : -1);
    }
}
