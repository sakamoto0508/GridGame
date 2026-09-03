using UnityEngine;

/// <summary>
/// CharacterによるBomb設置と、同時設置数の管理を担当します。
/// Player入力とEnemy AIの両方から利用できます。
/// </summary>
[RequireComponent(typeof(MovementComponent))]
public class BombComponent : MonoBehaviour
{
    /// <summary>現在盤面に残っている、このCharacterのBomb数です。</summary>
    public int CurrentBombCount => _currentBombCount;

    /// <summary>同時に設置できる最大Bomb数です。</summary>
    public int MaxBombCount => _settings != null ? _settings.MaxBombCount : 0;

    public float FuseTime => _settings != null ? _settings.FuseTime : 0f;
    public int ExplosionPower => _settings != null ? _settings.ExplosionPower : 0;

    [SerializeField] private BombSettings _settings;

    private GridManager _gridManager;
    private MovementComponent _movement;
    private CharacterBase _owner;
    private Bomb _bombPrefab;
    private int _currentBombCount;

    private void Awake()
    {
        _movement = GetComponent<MovementComponent>();
        _owner = GetComponent<CharacterBase>();
    }

    /// <summary>SpawnerからGridManagerと設置用Bomb Prefabを受け取ります。</summary>
    public void Init(GridManager gridManager, Bomb bombPrefab)
    {
        _gridManager = gridManager;
        _bombPrefab = bombPrefab;

        if (_gridManager == null)
            Debug.LogError("BombComponentの初期化に失敗しました: GridManagerが未設定です。", this);

        if (_bombPrefab == null)
            Debug.LogError("BombComponentの初期化に失敗しました: Bomb Prefabが未設定です。", this);

        if (_settings == null)
            Debug.LogError("BombComponentのBomb Settingsが未設定です。", this);
    }

    /// <summary>Characterが現在占有しているセルへのBomb設置を試みます。</summary>
    public bool TryPlaceBomb()
    {
        if (_gridManager == null || _movement == null || _owner == null ||
            _bombPrefab == null || _settings == null)
        {
            Debug.LogWarning("Bombを設置できません: BombComponentの参照が不足しています。", this);
            return false;
        }

        if (_currentBombCount >= _settings.MaxBombCount)
        {
            Debug.LogWarning(
                $"Bombを設置できません: 最大同時設置数 {_settings.MaxBombCount} に達しています。",
                this);
            return false;
        }

        Vector3Int position = _movement.CurrentGridPosition;

        if (!_gridManager.CanPlaceBomb(position, out string failureReason))
        {
            Debug.LogWarning($"Bombを設置できません: {failureReason}", this);
            return false;
        }

        Bomb bomb = Instantiate(
            _bombPrefab,
            _gridManager.GetWorldPosition(position),
            Quaternion.identity);

        if (!_gridManager.TryRegisterBomb(position, bomb))
        {
            Destroy(bomb.gameObject);
            Debug.LogWarning($"Bombのセル {position} への登録に失敗しました。", this);
            return false;
        }

        _currentBombCount++;
        bomb.Exploded += HandleBombExploded;
        bomb.Init(_gridManager, position, _owner, _settings);
        return true;
    }

    /// <summary>Bomb消滅時に設置数を戻し、イベント購読を解除します。</summary>
    private void HandleBombExploded(Bomb bomb)
    {
        bomb.Exploded -= HandleBombExploded;
        _currentBombCount = Mathf.Max(0, _currentBombCount - 1);
    }
}
