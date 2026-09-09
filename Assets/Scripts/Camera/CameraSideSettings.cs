using UnityEngine;

/// <summary>辺から隣の辺へカメラを切り替えるときの表示設定です。</summary>
[CreateAssetMenu(menuName = "3D Grid Bomber/Settings/Camera Side", fileName = "CameraSideSettings")]
public class CameraSideSettings : ScriptableObject
{
    [Tooltip("視点切替にかける秒数。0なら即座に切り替えます。")]
    [Min(0f)] [SerializeField] private float _transitionDuration = 0.4f;
    public float TransitionDuration => Mathf.Max(0f, _transitionDuration);
}
