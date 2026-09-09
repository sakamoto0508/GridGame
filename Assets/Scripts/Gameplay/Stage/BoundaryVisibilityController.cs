using System.Collections.Generic;
using UnityEngine;

/// <summary>描画用Cameraの向きから手前の外壁を隠します。Cinemachine更新後に判定します。</summary>
[DefaultExecutionOrder(10000)]
public class BoundaryVisibilityController : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private BoundaryViewSettings _settings;
    private BoundaryViewSettings _runtimeSettings;
    private GridManager _grid;
    private readonly List<BoundaryBlockView> _views = new();
    private readonly List<LineRenderer> _lines = new();
    // -1=外枠、-2=床、0=-X壁、1=+X壁、2=-Z壁、3=+Z壁。
    private readonly List<int> _lineFaces = new();
    private Material _lineMaterial;

    public void Init(GridManager grid)
    {
        _grid = grid;
        if (_settings == null)
        {
            _runtimeSettings = ScriptableObject.CreateInstance<BoundaryViewSettings>();
            _settings = _runtimeSettings;
        }
        if (_lines.Count == 0) CreateLines();
    }

    public void Register(Block block, Vector3Int position)
    {
        BoundaryBlockView view = block.GetComponent<BoundaryBlockView>();
        if (view == null) view = block.gameObject.AddComponent<BoundaryBlockView>();
        view.Init(_grid, block, position);
        if (!_views.Contains(view)) _views.Add(view);
    }

    private void LateUpdate()
    {
        if (_grid == null || _settings == null) return;
        Camera camera = _camera != null ? _camera : Camera.main;
        if (camera == null) return;
        // 位置ではなく実際の視線を使うので、Player追従中も安定します。
        Vector3 near = -camera.transform.forward;
        near.y = 0f;
        near.Normalize();
        Vector3Int size = _grid.Size;
        float threshold = _settings.DirectionThreshold;
        for (int i = _views.Count - 1; i >= 0; i--)
        {
            BoundaryBlockView view = _views[i];
            if (view == null || !view.IsRegistered)
            {
                if (view != null) view.Restore();
                _views.RemoveAt(i);
                continue;
            }
            Vector3Int p = view.Position;
            // 床は常に残す。天井は常に隠す。角の列はどちらかの面が手前なら隠す。
            bool hidden = p.y == size.y || (p.y >= 0 &&
                ((p.x == -1 && near.x < -threshold) || (p.x == size.x && near.x > threshold) ||
                 (p.z == -1 && near.z < -threshold) || (p.z == size.z && near.z > threshold)));
            view.SetHidden(hidden);
        }
        float width = CellSize * _settings.LineWidthInCells;
        if (_lineMaterial != null) _lineMaterial.color = _settings.LineColor;
        for (int i = 0; i < _lines.Count; i++)
        {
            int face = _lineFaces[i];
            bool hidden = (face == 0 && near.x < -threshold) || (face == 1 && near.x > threshold) ||
                          (face == 2 && near.z < -threshold) || (face == 3 && near.z > threshold);
            _lines[i].enabled = face == -1 ? _settings.ShowOutline :
                face == -2 ? _settings.ShowFloorGrid : _settings.ShowWallGrid && !hidden;
            _lines[i].widthMultiplier = width;
        }
    }

    private float CellSize => Vector3.Distance(_grid.GetWorldPosition(Vector3Int.zero),
        _grid.GetWorldPosition(Vector3Int.right));

    private void CreateLines()
    {
        // Resources内の専用Shaderを使い、ビルド時のShader除去を避けます。
        Shader shader = Resources.Load<Shader>("BoundaryOutline");
        if (shader == null)
        {
            Debug.LogWarning("BoundaryOutline Shaderがないため境界線は生成しません。", this);
            return;
        }
        _lineMaterial = new Material(shader);
        float cell = CellSize;
        Vector3 min = _grid.GetWorldPosition(Vector3Int.zero) - Vector3.one * (cell * 0.5f);
        Vector3 max = _grid.GetWorldPosition(_grid.Size) - Vector3.one * (cell * 0.5f);
        Vector3[] corners = new Vector3[8];
        for (int i = 0; i < 8; i++)
            corners[i] = new Vector3((i & 1) == 0 ? min.x : max.x,
                (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
        for (int i = 0; i < 8; i++)
            for (int axis = 1; axis <= 4; axis *= 2)
                if ((i & axis) == 0) AddLine(corners[i], corners[i | axis]);
        // 床表面よりわずかに上に置き、床とのちらつきを防ぎます。
        float y = min.y + cell * 0.01f;
        for (int x = 1; x < _grid.Size.x; x++)
            AddLine(new Vector3(min.x + x * cell, y, min.z), new Vector3(min.x + x * cell, y, max.z), -2);
        for (int z = 1; z < _grid.Size.z; z++)
            AddLine(new Vector3(min.x, y, min.z + z * cell), new Vector3(max.x, y, min.z + z * cell), -2);

        // 側壁の内側表面へ縦横の線を配置。壁と同じ面番号で描画を切り替えます。
        float inset = cell * 0.01f;
        for (int face = 0; face < 4; face++)
        {
            bool xWall = face < 2;
            float plane = face == 0 ? min.x + inset : face == 1 ? max.x - inset :
                face == 2 ? min.z + inset : max.z - inset;
            for (int height = 1; height < _grid.Size.y; height++)
            {
                float level = min.y + height * cell;
                AddLine(xWall ? new Vector3(plane, level, min.z) : new Vector3(min.x, level, plane),
                    xWall ? new Vector3(plane, level, max.z) : new Vector3(max.x, level, plane), face);
            }
            int count = xWall ? _grid.Size.z : _grid.Size.x;
            for (int column = 1; column < count; column++)
            {
                float along = (xWall ? min.z : min.x) + column * cell;
                AddLine(xWall ? new Vector3(plane, min.y, along) : new Vector3(along, min.y, plane),
                    xWall ? new Vector3(plane, max.y, along) : new Vector3(along, max.y, plane), face);
            }
        }
    }

    private void AddLine(Vector3 from, Vector3 to, int face = -1)
    {
        GameObject lineObject = new GameObject("Boundary Guide");
        lineObject.transform.SetParent(transform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = _lineMaterial;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        _lines.Add(line);
        _lineFaces.Add(face);
    }

    private void OnDisable()
    {
        foreach (BoundaryBlockView view in _views) if (view != null) view.Restore();
        foreach (LineRenderer line in _lines) if (line != null) line.enabled = false;
    }

    private void OnDestroy()
    {
        if (_lineMaterial != null) Destroy(_lineMaterial);
        if (_runtimeSettings != null) Destroy(_runtimeSettings);
        foreach (LineRenderer line in _lines) if (line != null) Destroy(line.gameObject);
    }
}
