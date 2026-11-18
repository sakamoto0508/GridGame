using UnityEngine;

/// <summary>試合設定画面の文言。UIのレイアウトやObjectはScene/Prefab側で用意します。</summary>
[CreateAssetMenu(menuName = "3D Grid Bomber/Settings/Match Setup", fileName = "MatchSetupSettings")]
public class MatchSetupSettings : ScriptableObject
{
    // 画面に表示する文言です（ゲーム内UIは英語表記で統一しています）。
    [SerializeField, TextArea(5, 10)] private string _controlsText =
        "Move: WASD / Arrow Keys\n" +
        "Jump: Space\n" +
        "Place Block: F\n" +
        "Place Bomb: R\n" +
        "Camera: Q / E";

    [SerializeField] private string _startFailedText = "Could not start. Check Console and reload the scene.";

    public string ControlsText => _controlsText;
    public string StartFailedText => _startFailedText;
}
