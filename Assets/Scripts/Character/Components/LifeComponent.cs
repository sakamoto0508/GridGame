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

    /// <summary>現在生存中ならtrueです。</summary>
    public bool IsAlive { get; private set; } = true;

    private CharacterBase _character;
    private MovementComponent _movement;

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

        if (_movement != null)
            _movement.UnregisterFromGrid();

        Debug.Log($"Character died: Name={name}, Cause={cause}", this);
        Died?.Invoke(_character, cause);

        // 勝敗処理と死亡演出を追加するまでは、死亡Characterを非表示・操作不能にします。
        gameObject.SetActive(false);
        return true;
    }
}
