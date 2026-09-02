using UnityEngine;

public class GridBomberGameMode : MonoBehaviour
{
    [SerializeField] private StageGenerator _stageGenerator;
    [SerializeField] private CharacterSpawner _characterSpawner;

    /// <summary>ステージ生成後にPlayerを開始地点へ生成します。</summary>
    private void Start()
    {
        _stageGenerator.GenerateStage();
        _characterSpawner.SpawnPlayer(_stageGenerator.PlayerSpawnPosition);
    }
}
