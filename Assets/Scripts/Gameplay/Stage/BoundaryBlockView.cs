using UnityEngine;

/// <summary>外殻Blockの描画だけを抑制。プール返却時は元の描画状態へ戻します。</summary>
public class BoundaryBlockView : MonoBehaviour
{
    private Renderer[] _renderers;
    private bool[] _originalForceOff;
    private GridManager _grid;
    private Block _block;
    public Vector3Int Position { get; private set; }
    public bool IsRegistered => _grid != null && _block != null &&
        gameObject.activeInHierarchy && _grid.GetBlock(Position) == _block;

    public void Init(GridManager grid, Block block, Vector3Int position)
    {
        Restore();
        _grid = grid;
        _block = block;
        Position = position;
        _renderers = GetComponentsInChildren<Renderer>(true);
        _originalForceOff = new bool[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            _originalForceOff[i] = _renderers[i].forceRenderingOff;
    }

    public void SetHidden(bool hidden)
    {
        if (_renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null)
                _renderers[i].forceRenderingOff = hidden || _originalForceOff[i];
    }

    public void Restore() => SetHidden(false);

    private void OnDisable()
    {
        Restore();
        _grid = null;
        _block = null;
    }
}
