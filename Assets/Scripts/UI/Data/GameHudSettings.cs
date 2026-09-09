using UnityEngine;

/// <summary>HUD文言と結果表示Animationの時間を設定します。</summary>
[CreateAssetMenu(fileName = "GameHudSettings", menuName = "3D Grid Bomber/Settings/Game HUD")]
public class GameHudSettings : ScriptableObject
{
    [Header("Text")]
    [SerializeField] private string _aliveFormat = "ALIVE: {0}";
    [SerializeField] private string _winText = "YOU WIN";
    [SerializeField] private string _loseText = "YOU LOSE";
    [SerializeField] private string _drawText = "DRAW";

    [Header("Player Status")]
    [Tooltip("{0}=爆風距離、{1}=同時設置上限、{2}=現在の設置数、{3}/{4}/{5}=グリッドX/Y/Z")]
    [SerializeField, TextArea(4, 6)] private string _playerStatusFormat =
        "RANGE: {0}\nBOMB LIMIT: {1}\nBOMBS PLACED: {2}\nGRID (X,Y,Z): ({3}, {4}, {5})";

    public string PlayerStatusFormat => _playerStatusFormat;

    [Header("Result Animation")]
    [SerializeField, Min(0f)] private float _resultDelay = 0.75f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.5f;

    public string AliveFormat => _aliveFormat;
    public string WinText => _winText;
    public string LoseText => _loseText;
    public string DrawText => _drawText;
    public float ResultDelay => _resultDelay;
    public float FadeDuration => _fadeDuration;
}
