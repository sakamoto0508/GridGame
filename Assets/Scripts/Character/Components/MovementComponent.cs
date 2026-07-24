using UnityEngine;

/// <summary>
/// キャラクターの移動を管理するコンポーネント
/// </summary>
public class MovementComponent : MonoBehaviour
{
    [SerializeField] private GridManager _gridManager;
    private CharacterBase _character;
    private Vector3Int _currentGridPosition;

    public Vector3Int CurrentGridPosition => _currentGridPosition;

    private void Awake()
    {
        _character = GetComponent<CharacterBase>();
    }

    private void Start()
    {
        // キャラクターの初期位置をグリッド座標に変換して登録する
        _currentGridPosition = _gridManager.GetGridPosition(transform.position);

        if (!_gridManager.TryRegisterCharacter(_currentGridPosition,_character))
        {
            Debug.LogError("キャラクターの初期登録に失敗しました。", this);
            enabled = false;
        }
    }

    /// <summary>
    /// 指定された方向に移動を試みる。移動可能な場合は移動し、trueを返す。移動不可能な場合はfalseを返す。
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public bool TryMove(Vector3Int direction)
    {
        Vector3Int destination = _currentGridPosition + direction;

        if (!_gridManager.TryMoveCharacter(_currentGridPosition,destination,_character))
        {
            return false;
        }

        _currentGridPosition = destination;
        transform.position = _gridManager.GetWorldPosition(destination);

        return true;
    }

    /// <summary>
    /// 指定されたグリッド座標に移動を開始する。ワールド座標に変換してtransform
    /// </summary>
    /// <param name="destination"></param>
    private void BeginMove(Vector3Int destination)
    {
        transform.position = _gridManager.GetWorldPosition(destination);
    }
}
