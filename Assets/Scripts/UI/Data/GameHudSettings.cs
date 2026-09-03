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
