using UnityEngine;

/// <summary>PlayerとEnemyに共通するキャラクターの基底クラスです。</summary>
[RequireComponent(typeof(LifeComponent))]
public abstract class CharacterBase : MonoBehaviour
{
    /// <summary>このCharacterが生存中ならtrueです。</summary>
    public bool IsAlive => _lifeComponent != null && _lifeComponent.IsAlive;

    private LifeComponent _lifeComponent;

    protected virtual void Awake()
    {
        _lifeComponent = GetComponent<LifeComponent>();
    }

    /// <summary>指定された原因で、このCharacterの死亡処理を要求します。</summary>
    public bool Kill(DeathCause cause)
    {
        if (_lifeComponent == null)
        {
            Debug.LogError("CharacterにLifeComponentがありません。", this);
            return false;
        }

        return _lifeComponent.Kill(cause);
    }
}
