# 3D Grid Bomber クラス図

現在実装されている主要クラスの関係を示す。`--|>`は継承、`*--`は所有、`-->`は参照・利用を表す。

## 全体構造

```mermaid
classDiagram
    class GridBomberGameMode {
        -StageGenerator stageGenerator
        -CharacterSpawner characterSpawner
        -GridBomberGameState gameState
        -EnemyDifficulty enemyDifficulty
        +Start()
    }

    class GridBomberGameState {
        +MatchState State
        +CharacterBase Winner
        +int AliveCharacterCount
        +StartMatch(characters)
    }

    class StageGenerator {
        +Vector3Int PlayerSpawnPosition
        +Vector3Int EnemySpawnPosition
        +GenerateStage()
    }

    class CharacterSpawner {
        +SpawnPlayer(position)
        +SpawnTestEnemy(position, player, difficulty, settings)
    }

    class GridManager {
        -GridCell cells
        +Vector3Int Size
        +Contains(position)
        +CanCharacterEnter(position)
        +CanJumpUp(position, direction)
        +TryRegisterCharacter(position, character)
        +TryRegisterBlock(position, block)
        +TryRegisterBomb(position, bomb)
        +GetBlock(position)
        +GetBomb(position)
        +GetCharacter(position)
    }

    class GridCell {
        +Vector3Int Position
        +Block Block
        +Bomb Bomb
        +Item Item
        +CharacterBase Character
        +bool IsReserved
    }

    class CharacterBase {
        <<abstract>>
        +bool IsAlive
        +Kill(cause)
    }

    class PlayerCharacter
    class EnemyCharacter
    class PlayerController {
        +Init(camera)
    }
    class EnemyBrain {
        +EnemyAIState CurrentState
        +Init(grid, player, difficulty, settings)
    }

    class MovementComponent {
        +Vector3Int CurrentGridPosition
        +CharacterMoveState State
        +TryMove(direction)
        +TryJump(direction)
        +TryFace(direction)
    }

    class BombComponent {
        +int CurrentBombCount
        +TryPlaceBomb()
    }

    class BlockPlacementComponent {
        +TryPlaceBlock()
    }

    class LifeComponent {
        +bool IsAlive
        +Kill(owner, cause)
    }

    class Bomb {
        +Vector3Int GridPosition
        +int ExplosionPower
        +float RemainingFuseTime
        +Explode()
    }

    class Block {
        +BlockType Type
        +Vector3Int GridPosition
        +BlockBreak()
    }

    class ExplosionSystem {
        <<static>>
        +CalculateExplosionCells(grid, origin, power)
        +GenerateExplosion(grid, origin, power)
    }

    class GridGravitySystem {
        <<static>>
        +TryGetFallDestination()
        +TryGetBombFallDestination()
        +TryGetBlockFallDestination()
    }

    class ExplosionView {
        +Show(grid, cells)
    }

    GridBomberGameMode --> StageGenerator : generates stage
    GridBomberGameMode --> CharacterSpawner : spawns characters
    GridBomberGameMode --> GridBomberGameState : starts match
    StageGenerator --> GridManager
    CharacterSpawner --> GridManager
    GridManager *-- GridCell

    CharacterBase <|-- PlayerCharacter
    CharacterBase <|-- EnemyCharacter
    PlayerCharacter --> PlayerController
    EnemyCharacter --> EnemyBrain
    CharacterBase --> MovementComponent
    CharacterBase --> BombComponent
    CharacterBase --> BlockPlacementComponent
    CharacterBase --> LifeComponent

    PlayerController --> MovementComponent
    PlayerController --> BombComponent
    PlayerController --> BlockPlacementComponent
    EnemyBrain --> MovementComponent
    EnemyBrain --> BombComponent
    EnemyBrain --> BlockPlacementComponent

    MovementComponent --> GridManager
    MovementComponent --> GridGravitySystem
    BombComponent --> Bomb : creates
    BlockPlacementComponent --> Block : creates
    Bomb --> ExplosionSystem
    Bomb --> GridGravitySystem
    Bomb --> ExplosionView
    Block --> GridGravitySystem
    ExplosionSystem --> GridManager
    GridBomberGameState --> CharacterBase : observes deaths
```

## Enemy AI内部

```mermaid
classDiagram
    class EnemyBrain {
        +EnemyAIState CurrentState
        -GridDangerMap dangerMap
        -List escapePath
        -Vector3Int previousPosition
        -ThinkAndAct()
        -TryEscapeDanger()
        -TryPlaceBombByChance()
        -TryPlaceUsefulChaseBlock()
        -RecoverFromStuck()
        -SetState(state)
    }

    class EnemyAIState {
        <<enumeration>>
        Idle
        Chase
        MoveToAttackPosition
        PlaceBomb
        Escape
    }

    class GridDangerMap {
        -Dictionary dangerTimes
        +Rebuild(grid)
        +RebuildWithVirtualBomb(grid, position, power, fuse)
        +IsDangerous(position)
        +TryGetDangerTime(position, seconds)
    }

    class GridPathfindingSystem {
        <<static>>
        +FindPathToNearestSafeCell(grid, dangerMap, start, durations)
    }

    class ExplosionSystem {
        <<static>>
        +CalculateAffectedCells(grid, origin, power)
    }

    class EnemyAISettings {
        +EnemyDifficultyValues Easy
        +EnemyDifficultyValues Normal
        +EnemyDifficultyValues Hard
        +GetValues(difficulty)
    }

    class EnemyDifficultyValues {
        +float ActionInterval
        +float MistakeChance
        +float BombPlaceChance
        +int DetectionRange
        +int BombDistance
        +int MaxSameCellDecisions
        +float ReconsiderPause
        +float EscapeSafeConfirmationTime
    }

    class GridManager
    class MovementComponent
    class BombComponent
    class BlockPlacementComponent

    EnemyBrain --> EnemyAIState
    EnemyBrain *-- GridDangerMap
    EnemyBrain --> GridPathfindingSystem
    EnemyBrain --> EnemyAISettings
    EnemyAISettings *-- EnemyDifficultyValues
    GridDangerMap --> ExplosionSystem : shares explosion rules
    GridDangerMap --> GridManager : scans bombs
    GridPathfindingSystem --> GridDangerMap : checks time hazards
    GridPathfindingSystem --> GridManager : checks traversability
    EnemyBrain --> MovementComponent : move and jump
    EnemyBrain --> BombComponent : simulate and place
    EnemyBrain --> BlockPlacementComponent : create a foothold
```

## Enemyの判断順

```text
DangerMap更新
  → 危険ならEscape
  → Escape解除待ちなら安全地点で待機
  → 同一行動を繰り返したらStuck回復
  → 攻撃可能ならPlaceBomb
  → 攻撃位置があればMoveToAttackPosition
  → Player検知中ならChase
  → それ以外はIdle
```

---

# 参照関係の詳細図

## 矢印の読み方

| 表記 | 意味 | 設定される場所 |
|---|---|---|
| `Inspector参照` | `[SerializeField]`で保持する参照 | SceneまたはPrefabのInspector |
| `Init注入` | 生成後に`Init()`から渡される参照 | `CharacterSpawner`など |
| `GetComponent` | 同じGameObjectから取得する参照 | 各Componentの`Awake()`など |
| `生成` | `Instantiate()`で生成する | Spawner、BombComponentなど |
| `購読` | C# eventを監視する | GameState、BombComponentなど |
| `static利用` | インスタンスを保持せず関数だけ呼ぶ | Utility／Systemクラス |

## 1. Sceneとゲーム開始時の参照

```mermaid
classDiagram
    class GridBomberGameMode {
        -StageGenerator _stageGenerator
        -CharacterSpawner _characterSpawner
        -GridBomberGameState _gameState
        -EnemyAISettings _enemyAISettings
        -EnemyDifficulty _enemyDifficulty
        -Start()
    }
    class StageGenerator {
        -GridManager _gridManager
        -StageSettings _settings
        +GenerateStage()
    }
    class CharacterSpawner {
        -GridManager _gridManager
        -CharacterPrefabSettings _settings
        -Camera _gameCamera
        +SpawnPlayer()
        +SpawnTestEnemy()
    }
    class GridBomberGameState {
        -List~CharacterBase~ _aliveCharacters
        +StartMatch()
    }
    class GridManager {
        -GridSettings _settings
        -GridCell[][][] _cells
    }
    class GameHud {
        -GridBomberGameState _gameState
        -GameHudSettings _settings
    }
    class Camera
    class EnemyAISettings
    class StageSettings
    class CharacterPrefabSettings
    class GridSettings

    GridBomberGameMode --> StageGenerator : Inspector参照
    GridBomberGameMode --> CharacterSpawner : Inspector参照
    GridBomberGameMode --> GridBomberGameState : Inspector参照
    GridBomberGameMode --> EnemyAISettings : Inspector参照
    StageGenerator --> GridManager : Inspector参照
    StageGenerator --> StageSettings : Inspector参照
    CharacterSpawner --> GridManager : Inspector参照
    CharacterSpawner --> CharacterPrefabSettings : Inspector参照
    CharacterSpawner --> Camera : Inspector参照
    GridManager --> GridSettings : Inspector参照
    GridManager *-- "1..*" GridCell : 実行時に生成・所有
    GameHud --> GridBomberGameState : Inspector参照・Event購読
```

開始順は`GameMode.Start()`から、`StageGenerator.GenerateStage()`、Character生成、`GameState.StartMatch()`の順になる。

## 2. Character Prefab内部とInit参照

```mermaid
classDiagram
    class CharacterSpawner
    class CharacterBase {
        <<abstract>>
        -LifeComponent _lifeComponent
        +bool IsAlive
        +Kill(cause)
    }
    class PlayerCharacter
    class EnemyCharacter
    class PlayerController {
        -MovementComponent _movement
        -BombComponent _bombComponent
        -BlockPlacementComponent _blockPlacement
        -Camera _camera
    }
    class EnemyBrain {
        -EnemyCharacter _enemy
        -CharacterBase _player
        -MovementComponent _movement
        -BombComponent _bombComponent
        -BlockPlacementComponent _blockPlacement
        -GridManager _gridManager
    }
    class MovementComponent {
        -CharacterBase _character
        -GridManager _gridManager
        -CharacterMovementSettings _settings
    }
    class BombComponent {
        -CharacterBase _owner
        -MovementComponent _movement
        -GridManager _gridManager
        -BombSettings _settings
        -Bomb _bombPrefab
    }
    class BlockPlacementComponent {
        -MovementComponent _movement
        -GridManager _gridManager
        -Block _blockPrefab
    }
    class LifeComponent {
        -MovementComponent _movement
        +event Died
    }
    class Camera
    class GridManager
    class CharacterMovementSettings
    class BombSettings

    CharacterBase <|-- PlayerCharacter
    CharacterBase <|-- EnemyCharacter
    CharacterBase --> LifeComponent : GetComponent

    PlayerCharacter *-- PlayerController : Prefab Component
    PlayerCharacter *-- MovementComponent : Prefab Component
    PlayerCharacter *-- BombComponent : Prefab Component
    PlayerCharacter *-- BlockPlacementComponent : Prefab Component
    EnemyCharacter *-- EnemyBrain : Prefab Component
    EnemyCharacter *-- MovementComponent : Prefab Component
    EnemyCharacter *-- BombComponent : Prefab Component
    EnemyCharacter *-- BlockPlacementComponent : Prefab Component

    PlayerController --> MovementComponent : GetComponent
    PlayerController --> BombComponent : GetComponent
    PlayerController --> BlockPlacementComponent : GetComponent
    PlayerController --> Camera : Init注入
    EnemyBrain --> MovementComponent : GetComponent
    EnemyBrain --> BombComponent : GetComponent
    EnemyBrain --> BlockPlacementComponent : GetComponent
    EnemyBrain --> CharacterBase : Init注入 Player
    EnemyBrain --> GridManager : Init注入

    MovementComponent --> CharacterBase : GetComponent
    MovementComponent --> CharacterMovementSettings : Inspector参照
    MovementComponent --> GridManager : Init注入
    BombComponent --> CharacterBase : GetComponent
    BombComponent --> MovementComponent : GetComponent
    BombComponent --> BombSettings : Inspector参照
    BombComponent --> GridManager : Init注入
    BlockPlacementComponent --> MovementComponent : GetComponent
    BlockPlacementComponent --> GridManager : Init注入
    LifeComponent --> MovementComponent : GetComponent

    CharacterSpawner --> PlayerCharacter : 生成
    CharacterSpawner --> EnemyCharacter : 生成
    CharacterSpawner --> MovementComponent : Init注入 Gridと開始座標
    CharacterSpawner --> BombComponent : Init注入 GridとBomb Prefab
    CharacterSpawner --> BlockPlacementComponent : Init注入 GridとBlock Prefab
    CharacterSpawner --> EnemyBrain : Init注入 Playerと難易度
```

`PlayerController`と`EnemyBrain`は判断元だけが異なり、実際の行動は共通Componentへ依頼する。

## 3. Bomb、Block、Explosion、Gravity

```mermaid
classDiagram
    class BombComponent {
        -Bomb _bombPrefab
        -BombSettings _settings
        +TryPlaceBomb()
    }
    class Bomb {
        -GridManager _gridManager
        -BombSettings _settings
        -CharacterBase Owner
        -ExplosionView _explosionView
        +event Exploded
        +Init()
        +Explode()
    }
    class BlockPlacementComponent {
        -Block _blockPrefab
        +TryPlaceBlock()
    }
    class Block {
        -GridManager _gridManager
        -BlockSettings _settings
        +Initialize()
        +BlockBreak()
    }
    class ExplosionView {
        -ExplosionVisualSettings _settings
        +Show()
    }
    class ExplosionSystem {
        <<static>>
        +CalculateExplosionCells()
        +GenerateExplosion()
    }
    class GridGravitySystem {
        <<static>>
        +TryGetBombFallDestination()
        +TryGetBlockFallDestination()
        +TryGetFallDestination()
    }
    class GridManager
    class CharacterBase
    class ExplosionEffect
    class BombSettings
    class BlockSettings
    class ExplosionVisualSettings

    BombComponent --> Bomb : Instantiate・Init
    BombComponent --> BombSettings : Inspector参照
    BombComponent --> Bomb : Exploded Event購読
    Bomb --> GridManager : Init注入
    Bomb --> CharacterBase : Init注入 Owner
    Bomb --> BombSettings : Init注入
    Bomb --> ExplosionView : GetComponent
    Bomb --> ExplosionSystem : static利用
    Bomb --> GridGravitySystem : static利用
    ExplosionSystem --> GridManager : セル照会・効果適用
    ExplosionView --> ExplosionVisualSettings : Inspector参照
    ExplosionView --> ExplosionEffect : Instantiate

    BlockPlacementComponent --> Block : Instantiate・Initialize
    Block --> GridManager : Initialize注入
    Block --> BlockSettings : Inspector参照
    Block --> GridGravitySystem : static利用
    GridGravitySystem --> GridManager : セル照会・移動
```

## 4. Enemy AIの参照とデータの流れ

```mermaid
classDiagram
    class EnemyBrain {
        -EnemyAIState _currentState
        -GridDangerMap _dangerMap
        -List~Vector3Int~ _escapePath
        -EnemyDifficultyValues _difficultyValues
        -ThinkAndAct()
        -TryEscapeDanger()
        -CanBombHitPlayer()
        -TryPlaceBombByChance()
        -RecoverFromStuck()
    }
    class GridDangerMap {
        -Dictionary~Vector3Int,float~ _dangerTimes
        +Rebuild(grid)
        +RebuildWithVirtualBomb()
        +IsDangerous(position)
    }
    class GridPathfindingSystem {
        <<static>>
        +FindPathToNearestSafeCell()
    }
    class ExplosionSystem
    class GridManager
    class MovementComponent
    class BombComponent
    class BlockPlacementComponent
    class EnemyAISettings
    class EnemyDifficultyValues
    class Bomb

    EnemyBrain *-- GridDangerMap : 自身で生成・所有
    EnemyBrain --> GridPathfindingSystem : static利用
    EnemyBrain --> GridManager : 盤面状態を照会
    EnemyBrain --> MovementComponent : Move・Jump・Face要求
    EnemyBrain --> BombComponent : 仮想計算後に設置要求
    EnemyBrain --> BlockPlacementComponent : 足場設置要求
    EnemyBrain --> EnemyDifficultyValues : 難易度値を保持
    EnemyAISettings *-- EnemyDifficultyValues
    GridDangerMap --> GridManager : 全セルのBombを走査
    GridDangerMap --> Bomb : 位置・威力・Fuseを参照
    GridDangerMap --> ExplosionSystem : 爆風ルールを再利用
    GridPathfindingSystem --> GridManager : 進入・Jump可否を照会
    GridPathfindingSystem --> GridDangerMap : 到着時刻と危険時刻を比較
```

Enemy AIの処理データは次のように流れる。

```text
GridManager上のBomb
  → GridDangerMap
  → ExplosionSystemで爆風セル計算
  → セルごとの危険時刻
  → GridPathfindingSystemで安全経路計算
  → EnemyBrainが行動を選択
  → Movement / Bomb / BlockPlacement Componentへ要求
  → GridManager上の状態が更新される
```

## 5. ScriptableObjectの参照

```mermaid
classDiagram
    class GameConfig {
        <<ScriptableObject Catalog>>
    }
    class GridSettings
    class CharacterMovementSettings
    class CharacterPrefabSettings
    class BombSettings
    class BlockSettings
    class StageSettings
    class EnemyAISettings
    class ExplosionVisualSettings
    class GameHudSettings

    class GridManager
    class MovementComponent
    class CharacterSpawner
    class BombComponent
    class Bomb
    class Block
    class StageGenerator
    class GridBomberGameMode
    class ExplosionView
    class GameHud

    GameConfig --> GridSettings : カタログ参照
    GameConfig --> CharacterMovementSettings : カタログ参照
    GameConfig --> CharacterPrefabSettings : カタログ参照
    GameConfig --> BombSettings : カタログ参照
    GameConfig --> StageSettings : カタログ参照
    GameConfig --> EnemyAISettings : カタログ参照
    GameConfig --> ExplosionVisualSettings : カタログ参照
    GameConfig --> GameHudSettings : カタログ参照

    GridManager --> GridSettings : Inspector参照
    MovementComponent --> CharacterMovementSettings : Inspector参照
    CharacterSpawner --> CharacterPrefabSettings : Inspector参照
    BombComponent --> BombSettings : Inspector参照
    Bomb --> BombSettings : Init注入
    Block --> BlockSettings : Inspector参照
    StageGenerator --> StageSettings : Inspector参照
    GridBomberGameMode --> EnemyAISettings : Inspector参照
    ExplosionView --> ExplosionVisualSettings : Inspector参照
    GameHud --> GameHudSettings : Inspector参照
```

`GameConfig`は一覧用カタログであり、現在のRuntime Componentは`GameConfig`経由ではなく、必要なSettingsを直接参照している。
