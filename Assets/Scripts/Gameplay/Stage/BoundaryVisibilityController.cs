using System.Collections.Generic;
using UnityEngine;

/// <summary>描画用Cameraの向きから手前の外壁を隠します。Cinemachine更新後に判定します。</summary>
[DefaultExecutionOrder(10000)] // Cinemachineのカメラ更新より後に実行するため、大きな値にしています。
public class BoundaryVisibilityController : MonoBehaviour
{
    // 面の表面から少し内側へずらして、線と面のちらつきを防ぎます。
    private const float GridLineInsetInCells = 0.01f;

    /// <summary>セル中心からセル端までの距離（セル単位）です。</summary>
    private const float HalfCell = 0.5f;

    private const string OutlineShaderName = "BoundaryOutline";

    // 境界線がどの面に属するかを表す番号です。壁の番号はループで使うため0〜3の連番にしています。
    private const int FaceOutline = -1;
    private const int FaceFloor = -2;
    private const int FaceWallMinX = 0;
    private const int FaceWallMaxX = 1;
    private const int FaceWallMinZ = 2;
    private const int FaceWallMaxZ = 3;
    private const int WallFaceCount = 4;

    [SerializeField] private Camera _camera;
    [SerializeField] private BoundaryViewSettings _settings;

    private readonly List<BoundaryBlockView> _views = new();
    private readonly List<LineRenderer> _lines = new();

    // 各線が属する面番号（FaceOutline / FaceFloor / FaceWall〜）。
    private readonly List<int> _lineFaces = new();

    private BoundaryViewSettings _runtimeSettings;
    private GridManager _grid;
    private Material _lineMaterial;

    private float CellSize => _grid.CellSize;

    public void Init(GridManager grid)
    {
        _grid = grid;

        if (_settings == null)
        {
            _runtimeSettings = ScriptableObject.CreateInstance<BoundaryViewSettings>();
            _settings = _runtimeSettings;
        }

        if (_lines.Count == 0)
            CreateLines();
    }

    public void Register(Block block, Vector3Int position)
    {
        BoundaryBlockView view = block.GetComponent<BoundaryBlockView>();
        if (view == null)
            view = block.gameObject.AddComponent<BoundaryBlockView>();

        view.Init(_grid, block, position);

        if (!_views.Contains(view))
            _views.Add(view);
    }

    private void LateUpdate()
    {
        if (_grid == null || _settings == null)
            return;

        Camera camera = _camera != null ? _camera : Camera.main;
        if (camera == null)
            return;

        // 位置ではなく実際の視線を使うので、Player追従中も安定します。
        // nearは「カメラ側（手前）」を指す水平方向です。
        Vector3 near = -camera.transform.forward;
        near.y = 0f;
        near.Normalize();

        Vector3Int size = _grid.Size;
        float threshold = _settings.DirectionThreshold;

        UpdateBlockVisibility(size, near, threshold);
        UpdateLineVisibility(near, threshold);
    }

    /// <summary>外殻Blockのうち、天井と手前の壁を隠します。</summary>
    private void UpdateBlockVisibility(Vector3Int size, Vector3 near, float threshold)
    {
        for (int i = _views.Count - 1; i >= 0; i--)
        {
            BoundaryBlockView view = _views[i];

            // 登録が外れたBlockは表示を戻して管理対象から外します。
            if (view == null || !view.IsRegistered)
            {
                if (view != null)
                    view.Restore();

                _views.RemoveAt(i);
                continue;
            }

            Vector3Int p = view.Position;

            // 床は常に残す。天井は常に隠す。角の列はどちらかの面が手前なら隠す。
            bool isCeiling = p.y == size.y;
            bool isNearWall =
                (p.x == -1 && near.x < -threshold) ||
                (p.x == size.x && near.x > threshold) ||
                (p.z == -1 && near.z < -threshold) ||
                (p.z == size.z && near.z > threshold);

            bool hidden = isCeiling || (p.y >= 0 && isNearWall);
            view.SetHidden(hidden);
        }
    }

    /// <summary>境界線の表示ON/OFFと太さを更新します。手前の壁の線は隠します。</summary>
    private void UpdateLineVisibility(Vector3 near, float threshold)
    {
        float width = CellSize * _settings.LineWidthInCells;

        if (_lineMaterial != null)
            _lineMaterial.color = _settings.LineColor;

        for (int i = 0; i < _lines.Count; i++)
        {
            int face = _lineFaces[i];

            bool hidden =
                (face == FaceWallMinX && near.x < -threshold) ||
                (face == FaceWallMaxX && near.x > threshold) ||
                (face == FaceWallMinZ && near.z < -threshold) ||
                (face == FaceWallMaxZ && near.z > threshold);

            if (face == FaceOutline)
                _lines[i].enabled = _settings.ShowOutline;
            else if (face == FaceFloor)
                _lines[i].enabled = _settings.ShowFloorGrid;
            else
                _lines[i].enabled = _settings.ShowWallGrid && !hidden;

            _lines[i].widthMultiplier = width;
        }
    }

    private void CreateLines()
    {
        // Resources内の専用Shaderを使い、ビルド時のShader除去を避けます。
        Shader shader = Resources.Load<Shader>(OutlineShaderName);
        if (shader == null)
        {
            Debug.LogWarning("BoundaryOutline Shaderがないため境界線は生成しません。", this);
            return;
        }

        _lineMaterial = new Material(shader);

        // フィールド全体を囲む直方体の最小・最大座標（セル中心から半セル外側）
        float cell = CellSize;
        Vector3 min = _grid.GetWorldPosition(Vector3Int.zero) - Vector3.one * (cell * HalfCell);
        Vector3 max = _grid.GetWorldPosition(_grid.Size) - Vector3.one * (cell * HalfCell);

        CreateOutlineLines(min, max);
        CreateFloorGridLines(min, max, cell);
        CreateWallGridLines(min, max, cell);
    }

    /// <summary>直方体の12辺を外枠として追加します。</summary>
    private void CreateOutlineLines(Vector3 min, Vector3 max)
    {
        Vector3[] corners = new Vector3[CubeCorner.Count];
        for (int i = 0; i < CubeCorner.Count; i++)
        {
            corners[i] = new Vector3(
                CubeCorner.IsPositive(i, CubeCorner.BitX) ? max.x : min.x,
                CubeCorner.IsPositive(i, CubeCorner.BitY) ? max.y : min.y,
                CubeCorner.IsPositive(i, CubeCorner.BitZ) ? max.z : min.z);
        }

        // 各頂点から、各軸の正方向に隣接する頂点へ線を引くと12辺になります。
        for (int i = 0; i < CubeCorner.Count; i++)
        {
            for (int axis = CubeCorner.BitX; axis <= CubeCorner.BitZ; axis *= 2)
            {
                if ((i & axis) == 0)
                    AddLine(corners[i], corners[i | axis]);
            }
        }
    }

    /// <summary>床のセル境界線を追加します。</summary>
    private void CreateFloorGridLines(Vector3 min, Vector3 max, float cell)
    {
        // 床表面よりわずかに上に置き、床とのちらつきを防ぎます。
        float y = min.y + cell * GridLineInsetInCells;

        for (int x = 1; x < _grid.Size.x; x++)
        {
            float lineX = min.x + x * cell;
            AddLine(new Vector3(lineX, y, min.z), new Vector3(lineX, y, max.z), FaceFloor);
        }

        for (int z = 1; z < _grid.Size.z; z++)
        {
            float lineZ = min.z + z * cell;
            AddLine(new Vector3(min.x, y, lineZ), new Vector3(max.x, y, lineZ), FaceFloor);
        }
    }

    /// <summary>側壁の内側表面へ縦横の線を配置。壁と同じ面番号で描画を切り替えます。</summary>
    private void CreateWallGridLines(Vector3 min, Vector3 max, float cell)
    {
        float inset = cell * GridLineInsetInCells;

        for (int face = 0; face < WallFaceCount; face++)
        {
            // X方向に垂直な壁（-X/+X）か、Z方向に垂直な壁（-Z/+Z）か
            bool xWall = face == FaceWallMinX || face == FaceWallMaxX;

            float plane;
            if (face == FaceWallMinX)
                plane = min.x + inset;
            else if (face == FaceWallMaxX)
                plane = max.x - inset;
            else if (face == FaceWallMinZ)
                plane = min.z + inset;
            else
                plane = max.z - inset;

            // 横線（高さごと）
            for (int height = 1; height < _grid.Size.y; height++)
            {
                float level = min.y + height * cell;
                Vector3 from = xWall ? new Vector3(plane, level, min.z) : new Vector3(min.x, level, plane);
                Vector3 to = xWall ? new Vector3(plane, level, max.z) : new Vector3(max.x, level, plane);
                AddLine(from, to, face);
            }

            // 縦線（列ごと）
            int count = xWall ? _grid.Size.z : _grid.Size.x;
            for (int column = 1; column < count; column++)
            {
                float along = (xWall ? min.z : min.x) + column * cell;
                Vector3 from = xWall ? new Vector3(plane, min.y, along) : new Vector3(along, min.y, plane);
                Vector3 to = xWall ? new Vector3(plane, max.y, along) : new Vector3(along, max.y, plane);
                AddLine(from, to, face);
            }
        }
    }

    private void AddLine(Vector3 from, Vector3 to, int face = FaceOutline)
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
        foreach (BoundaryBlockView view in _views)
        {
            if (view != null)
                view.Restore();
        }

        foreach (LineRenderer line in _lines)
        {
            if (line != null)
                line.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (_lineMaterial != null)
            Destroy(_lineMaterial);

        if (_runtimeSettings != null)
            Destroy(_runtimeSettings);

        foreach (LineRenderer line in _lines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }
    }
}
