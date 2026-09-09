using UnityEngine;

/// <summary>1セルの破壊可能Blockの12辺を描画。子Objectなので落下の表示位置にも追従します。</summary>
public class BlockOutlineView : MonoBehaviour
{
    // 12辺を1本の連続線で通る経路。一部の辺は同じ位置を折り返します。
    private static readonly int[] EdgePath = { 0, 1, 3, 2, 0, 4, 5, 1, 5, 7, 3, 7, 6, 2, 6, 4 };
    private LineRenderer _line;
    private Material _material;
    private BlockSettings _settings;
    private float _cellSize;

    public void Init(GridManager grid, BlockSettings settings)
    {
        _settings = settings;
        _cellSize = Vector3.Distance(grid.GetWorldPosition(Vector3Int.zero), grid.GetWorldPosition(Vector3Int.right));
        if (_line == null)
        {
            Shader shader = Resources.Load<Shader>("BoundaryOutline");
            if (shader == null)
            {
                Debug.LogWarning("BoundaryOutline ShaderがないためBlockの枠線を生成できません。", this);
                return;
            }
            _material = new Material(shader);
            GameObject child = new GameObject("Block Outline");
            child.transform.SetParent(transform, false);
            _line = child.AddComponent<LineRenderer>();
            _line.sharedMaterial = _material;
            _line.useWorldSpace = false;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.positionCount = EdgePath.Length;
        }
        // セル表面のわずか外側へ配置し、面とのちらつきを軽減します。
        float half = _cellSize * 0.505f;
        for (int i = 0; i < EdgePath.Length; i++)
        {
            int corner = EdgePath[i];
            Vector3 offset = new Vector3((corner & 1) == 0 ? -half : half,
                (corner & 2) == 0 ? -half : half, (corner & 4) == 0 ? -half : half);
            _line.SetPosition(i, transform.InverseTransformVector(offset));
        }
        Refresh();
    }

    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        if (_line == null || _settings == null) return;
        _line.enabled = _settings.ShowOutline && _settings.Type == BlockType.Breakable;
        _line.widthMultiplier = _cellSize * _settings.OutlineWidthInCells;
        _material.color = _settings.OutlineColor;
    }

    private void OnDisable()
    {
        // プール内では描画しない。再貸出後のInitで再設定します。
        if (_line != null) _line.enabled = false;
        _settings = null;
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }
}
