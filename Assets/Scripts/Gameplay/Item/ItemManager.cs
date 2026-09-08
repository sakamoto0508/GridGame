using System.Collections.Generic;
using UnityEngine;

/// <summary>試合中だけ上空からItemを定期生成します。</summary>
public class ItemManager : MonoBehaviour
{
    [SerializeField] private ItemDropSettings _settings;
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private GridBomberGameState _gameState;
    private float _elapsed;

    private void Start()
    {
        if (_gridManager == null) _gridManager = FindFirstObjectByType<GridManager>();
        if (_gameState == null) _gameState = FindFirstObjectByType<GridBomberGameState>();
        if (_settings == null || _gridManager == null || _gameState == null)
        {
            Debug.LogError("ItemManagerにSettings、GridManager、GameStateが必要です。", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (_gameState.State != MatchState.Playing) { _elapsed = 0f; return; }
        _elapsed += Time.deltaTime;
        if (_elapsed < _settings.DropInterval) return;
        _elapsed = 0f;
        TryDropItem();
    }

    /// <summary>最上段の配置可能セルから均等に抽選します。空きがなければ次回まで待ちます。</summary>
    public bool TryDropItem()
    {
        if (_settings == null || _gridManager == null || _gameState == null ||
            _gameState.State != MatchState.Playing ||
            _gridManager.CountItems() >= _settings.MaxItemCount) return false;
        List<Item> prefabs = new List<Item>();
        if (_settings.Prefabs != null)
            foreach (Item prefab in _settings.Prefabs)
                if (prefab != null) prefabs.Add(prefab);
        if (prefabs.Count == 0) return false;
        List<Vector3Int> candidates = new List<Vector3Int>();
        Vector3Int size = _gridManager.Size;
        for (int x = 0; x < size.x; x++)
        for (int z = 0; z < size.z; z++)
        {
            Vector3Int cell = new Vector3Int(x, size.y - 1, z);
            if (_gridManager.CanPlaceItem(cell)) candidates.Add(cell);
        }
        if (candidates.Count == 0) return false;
        Vector3Int position = candidates[Random.Range(0, candidates.Count)];
        Item item = GridObjectPool.For(_gridManager).Rent(prefabs[Random.Range(0, prefabs.Count)],
            _gridManager.GetWorldPosition(position), Quaternion.identity);
        if (item.Init(_gridManager, position)) return true;
        item.Despawn();
        return false;
    }
}
