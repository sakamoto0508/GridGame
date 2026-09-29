using System;
using UnityEngine;

/// <summary>Characterが死亡した原因を表します。</summary>
public enum DeathCause
{
    Explosion,
    FallingBlock
}

/// <summary>Characterの生存状態と、一度だけ実行される死亡処理を管理します。</summary>
[RequireComponent(typeof(MovementComponent))]
public class LifeComponent : MonoBehaviour
{
    /// <summary>死亡時にCharacterと死亡原因を通知します。</summary>
    public event Action<CharacterBase, DeathCause> Died;

    /// <summary>カメラ等の共通演出向け通知。勝敗判定用のDiedより先に通知します。</summary>
    public static event Action<CharacterBase, DeathCause> AnyCharacterDied;

    // Domain Reloadを無効にしたEditorでも、前の試合の購読を保持しません。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetNotifications() => AnyCharacterDied = null;

    /// <summary>現在生存中ならtrueです。</summary>
    public bool IsAlive { get; private set; } = true;

    private CharacterBase _character;
    private MovementComponent _movement;
    [SerializeField] private CharacterDeathVisualSettings _deathVisualSettings;

    private void Awake()
    {
        _character = GetComponent<CharacterBase>();
        _movement = GetComponent<MovementComponent>();
    }

    /// <summary>
    /// Characterを死亡させ、占有中のGridCellから登録解除します。
    /// 二重に呼ばれた場合は何もせずfalseを返します。
    /// </summary>
    public bool Kill(DeathCause cause)
    {
        if (!IsAlive)
            return false;

        IsAlive = false;
        // 勝敗イベントでCharacterが無効化される前に、独立した見た目を生成します。
        try { CharacterDeathBurst.Spawn(_character, transform, _deathVisualSettings, cause); }
        catch (Exception error) { Debug.LogException(error, this); } // 表示の失敗で死亡処理を中断しない。
        AudioManager.PlayAt(SoundId.CharacterDeath, transform.position);
        try { AnyCharacterDied?.Invoke(_character, cause); }
        catch (Exception error) { Debug.LogException(error, this); }

        if (_movement != null)
            _movement.UnregisterFromGrid();

        Debug.Log($"Character died: Name={name}, Cause={cause}", this);
        Died?.Invoke(_character, cause);

        // 破片は独立Objectなので、本人を非表示にしても演出は継続します。
        gameObject.SetActive(false);
        return true;
    }
}
