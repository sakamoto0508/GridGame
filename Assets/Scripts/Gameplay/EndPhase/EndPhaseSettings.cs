using UnityEngine;

/// <summary>終盤イベントの時間・生成物・予告表示をまとめた設定です。</summary>
[CreateAssetMenu(menuName = "3D Grid Bomber/Settings/End Phase", fileName = "EndPhaseSettings")]
public class EndPhaseSettings : ScriptableObject
{
    [SerializeField] private bool _enabled = true;
    [SerializeField, Min(0f)] private float _startAfterSeconds = 60f;
    [SerializeField, Min(0.1f)] private float _warningSeconds = 2f;
    [Tooltip("前のBlockが着地してから次の予告までの秒数。")]
    [SerializeField, Min(0.1f)] private float _dropInterval = 3f;
    [Tooltip("空欄ならStageのUnbreakable Block Prefabを使います。")]
    [SerializeField] private Block _blockPrefab;
    [SerializeField] private Color _warningColor = new Color(1f, 0.15f, 0.05f, 1f);
    [SerializeField, Min(0.001f)] private float _lineWidthInCells = 0.06f;
    public bool Enabled => _enabled;
    public float StartAfterSeconds => Mathf.Max(0f, _startAfterSeconds);
    public float WarningSeconds => Mathf.Max(0.1f, _warningSeconds);
    public float DropInterval => Mathf.Max(0.1f, _dropInterval);
    public Block BlockPrefab => _blockPrefab;
    public Color WarningColor => _warningColor;
    public float LineWidthInCells => Mathf.Max(0.001f, _lineWidthInCells);
}
