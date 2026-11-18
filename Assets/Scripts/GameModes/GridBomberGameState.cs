using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>現在の試合進行状態です。</summary>
public enum MatchState
{
    Waiting,
    Playing,
    Finished
}

/// <summary>試合状態、生存Character、勝者を保持します。</summary>
public class GridBomberGameState : MonoBehaviour
{
    /// <summary>試合開始に必要な最低キャラクター数です。</summary>
    private const int MinimumCharactersToStart = 2;

    public event Action<MatchState> StateChanged;
    public event Action<CharacterBase> MatchFinished;
    public event Action<int> AliveCharacterCountChanged;

    public MatchState State { get; private set; } = MatchState.Waiting;
    public CharacterBase Winner { get; private set; }
    public int AliveCharacterCount => _aliveCharacters.Count;

    private readonly List<CharacterBase> _aliveCharacters = new List<CharacterBase>();

    /// <summary>生成済みCharacterを登録して試合を開始します。最低2体必要です。</summary>
    public bool StartMatch(params CharacterBase[] characters)
    {
        if (State != MatchState.Waiting)
        {
            Debug.LogWarning($"試合を開始できません: 現在状態={State}", this);
            return false;
        }

        if (characters == null)
            return false;

        for (int i = 0; i < characters.Length; i++)
            RegisterCharacter(characters[i]);

        if (_aliveCharacters.Count < MinimumCharactersToStart)
        {
            Debug.LogError(
                $"試合開始に必要なキャラクター数が不足しています。登録数={_aliveCharacters.Count}",
                this);
            ClearRegisteredCharacters();
            return false;
        }

        SetState(MatchState.Playing);
        Debug.Log($"試合開始。参加キャラクター数={_aliveCharacters.Count}", this);
        return true;
    }

    /// <summary>生存Characterを重複なしで登録し、死亡Eventを購読します。</summary>
    private bool RegisterCharacter(CharacterBase character)
    {
        if (character == null || !character.IsAlive || _aliveCharacters.Contains(character))
            return false;

        LifeComponent lifeComponent = character.GetComponent<LifeComponent>();

        if (lifeComponent == null)
        {
            Debug.LogError($"Character '{character.name}' にLifeComponentがありません。", character);
            return false;
        }

        _aliveCharacters.Add(character);
        lifeComponent.Died += HandleCharacterDied;
        AliveCharacterCountChanged?.Invoke(_aliveCharacters.Count);
        return true;
    }

    /// <summary>死亡者を一覧から除外し、残り1体以下なら試合を終了します。</summary>
    private void HandleCharacterDied(CharacterBase character, DeathCause cause)
    {
        if (character == null || !_aliveCharacters.Remove(character))
            return;

        LifeComponent lifeComponent = character.GetComponent<LifeComponent>();

        if (lifeComponent != null)
            lifeComponent.Died -= HandleCharacterDied;

        AliveCharacterCountChanged?.Invoke(_aliveCharacters.Count);

        Debug.Log(
            $"キャラクターが脱落しました。名前={character.name}、原因={cause}、残り={_aliveCharacters.Count}体",
            this);

        if (State == MatchState.Playing && _aliveCharacters.Count <= 1)
            FinishMatch();
    }

    /// <summary>最後の生存者を勝者として試合を終了します。全滅時は引き分けです。</summary>
    private void FinishMatch()
    {
        Winner = _aliveCharacters.Count == 1 ? _aliveCharacters[0] : null;
        SetState(MatchState.Finished);

        Debug.Log(
            Winner != null ? $"試合終了。勝者={Winner.name}" : "試合終了。引き分けです。",
            this);
        MatchFinished?.Invoke(Winner);
    }

    /// <summary>状態を更新し、変更Eventを通知します。</summary>
    private void SetState(MatchState state)
    {
        State = state;
        StateChanged?.Invoke(State);
    }

    /// <summary>購読中の死亡Eventをすべて解除します。</summary>
    private void ClearRegisteredCharacters()
    {
        for (int i = 0; i < _aliveCharacters.Count; i++)
        {
            CharacterBase character = _aliveCharacters[i];

            if (character == null)
                continue;

            LifeComponent lifeComponent = character.GetComponent<LifeComponent>();

            if (lifeComponent != null)
                lifeComponent.Died -= HandleCharacterDied;
        }

        _aliveCharacters.Clear();
        AliveCharacterCountChanged?.Invoke(0);
    }

    private void OnDestroy()
    {
        ClearRegisteredCharacters();
    }
}
