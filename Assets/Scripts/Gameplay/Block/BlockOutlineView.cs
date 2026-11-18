using UnityEngine;

/// <summary>1セルの破壊可能Blockの12辺を描画。子Objectなので落下の表示位置にも追従します。</summary>
public class BlockOutlineView : MonoBehaviour
{
    private const string OutlineShaderName = "BoundaryOutline";

    [Tooltip("枠線を置く位置（セル幅に対する中心からの距離）。0.5より少し大きくし、面とのちらつきを防ぎます。")]
    [SerializeField] private float _outlineHalfSizeInCells = 0.505f;

    private LineRenderer _line;
    private Material _material;
    private BlockSettings _settings;
    private float _cellSize;

    public void Init(GridManager grid, BlockSettings settings)
    {
        _settings = settings;
        _cellSize = grid.CellSize;

        if (_line == null)
        {
            Shader shader = Resources.Load<Shader>(OutlineShaderName);
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
            _line.positionCount = CubeCorner.EdgePath.Length;
        }

        float half = _cellSize * _outlineHalfSizeInCells;

        for (int i = 0; i < CubeCorner.EdgePath.Length; i++)
        {
            Vector3 offset = CubeCorner.Sign(CubeCorner.EdgePath[i]) * half;
            _line.SetPosition(i, transform.InverseTransformVector(offset));
        }

        Refresh();
    }

    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        if (_line == null || _settings == null)
            return;

        _line.enabled = _settings.ShowOutline;
        _line.widthMultiplier = _cellSize * _settings.OutlineWidthInCells;
        _material.color = _settings.OutlineColor;
    }

    private void OnDisable()
    {
        // プール内では描画しない。再貸出後のInitで再設定します。
        if (_line != null)
            _line.enabled = false;

        _settings = null;
    }

    private void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }
}
