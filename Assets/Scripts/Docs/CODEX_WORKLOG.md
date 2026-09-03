# 3D Grid Bomber — Codex作業ログ兼仕様書

最終更新: 2026-09-03

このファイルは、別のCodexチャットや別の開発者が現在の状態から作業を再開するための引き継ぎ資料である。
コードを変更した際は「現在の実装状況」「既知の課題」「次に実装する項目」も更新すること。

## 1. プロジェクト概要

- エンジン: Unity
- ジャンル: 3Dグリッド対戦アクション
- 仮タイトル: **3D Grid Bomber**
- 基本コンセプト: 3次元空間で戦うボンバーマン
- 基本勝利条件: 最後まで生き残る、または敵を全滅させる
- 基本敗北条件: 爆風または落下Blockの下敷きで死亡する

### Unity上の座標規約

企画書原案ではZを高さとしていたが、Unity実装では次に統一している。

```text
X: 左右
Y: 高さ
Z: 前後
```

論理グリッド座標には`Vector3Int`、表示上のワールド座標には`Vector3`を使用する。
`CellSize`はGrid→World変換時にだけ使用し、論理座標へ掛けない。

## 2. ゲーム仕様

### フィールド

- 既定サイズは7×7×7
- 完全な3次元グリッド
- 各セルはカテゴリ別に以下を保持できる
  - Block
  - Bomb
  - Item
  - Character
  - 予約状態
- 1セルに単一の汎用`Occupant`を置く設計にはしない

現在の試作ステージは次の構成である。

```text
y = 0: 床Block
y = 1: Player、外周壁、通路、破壊可能Block
```

`StageGenerator`は以下を生成する。

1. y=0の固定床
2. y=1の外周壁
3. 内側セルの破壊可能Block
4. Player開始地点と、その右・前の脱出用セルは空ける

ランダム配置は`_randomSeed`で再現可能にしている。

### Player操作

- 水平4方向へ1セル単位で移動する
- 入力方向はCameraから見たX/Z方向へ変換する
- 斜め入力はワールドX/Zの強い軸へ丸める
- Yは高さ専用
- PlayerとEnemyは同じ`MovementComponent`を使用する想定

#### 通常移動

- 移動先セルが空いている場合だけ移動できる
- 移動開始時に論理セルのCharacter占有を移動先へ更新する
- 表示位置はUnity `Awaitable`で補間する
- 移動中は新しい移動・ジャンプ要求を受け付けない
- 最後に入力された水平4方向を向きとして保存する

#### その場ジャンプ

方向入力なしでJumpを押すと、現在セルの1セル上へジャンプする。

```text
ジャンプ前       ジャンプ中
y=2 空           y=2 Player
y=1 Player       y=1 空
y=0 Block        y=0 Block
```

- 上のセルが空いている場合だけ開始可能
- ジャンプ中、元のセルへBlockを配置できる
- Blockを置かなければ元のセルへ戻る
- Blockを置けばPlayerは上のセルに残り、そのBlock上へ着地する
- Block配置可能時間は主に`MovementComponent._airTime`で調整する

#### 方向付き段差ジャンプ

移動方向を押しながらJumpを押すと、方向先の1段高いBlock上へジャンプする。

例:

```text
現在Playerセル    (1, 1, 1)
入力方向          (1, 0, 0)
対象Blockセル     (2, 1, 1)
着地Playerセル    (2, 2, 1)
```

成功条件:

- 入力がX/Z平面の単位4方向
- 方向先の同じ高さにBlockがある
- Block上のセルがグリッド内
- Block上のセルにBlockまたはCharacterがなく、予約されていない
- Characterが移動中・ジャンプ中・落下中ではない

表示は放物線状に補間する。

#### Characterの重力

- Player直下に足場がなければ真下へ落下する
- 真下を探索し、最初の障害物の1セル上へ着地する
- 高所から空セルへ水平移動した場合も落下する
- 待機中に足場Blockが失われた場合も`Update()`で検出して落下する
- 落下開始時に着地点を論理的に確保する
- 落下中は入力を受け付けない
- グリッド最下部より下には落ちない

### Block

種類:

- `Breakable`
- `Unbreakable`

用途:

- 通路封鎖
- 爆風防御
- 足場作成

配置ルール:

- 地上ではCharacterの正面セルへ置く
- その場ジャンプ中はCharacter直下へ置く
- Block、Bomb、Characterまたは予約があるセルには置けない
- 配置失敗時はConsoleへ具体的な理由を出す

重力:

- Block生成・初期化時に直下を探索する
- 足場がなければ最初の障害物の1セル上まで落下する
- 落下前にGridCell上のBlock登録を着地点へ移動する
- 表示は`Awaitable`で補間する

将来仕様:

- 爆風で破壊可能Blockを破壊する
- 落下BlockがCharacterへ当たったら即死させる
- 支えているBlockが後から破壊された場合、上のBlockも再度落下させる

### Bomb

- PlayerはBombを設置できる
- 同時設置可能数を持つ
- Bombは重力で落下する
- 一定時間後に爆発する
- 誘爆による連鎖爆発がある
- 爆発はUnity座標の±X、±Y、±Zの6方向へ伸びる
- 爆風距離はBomb Powerで増える
- 爆風はCharacter/Enemyを即死させる
- 爆風は破壊可能Blockを破壊する
- 破壊不能Blockで爆風は停止する
- 破壊可能Blockは破壊され、そのセルで爆風を停止する想定
- 将来アイテムでBlock貫通数を増やせる
- 爆発判定はActor/Effect生成より先に、影響GridCellを計算する
- 連鎖爆発は直接再帰ではなくキューで処理する方針

現在の実装:

- BombはBlockを継承しない独立クラス
- Characterの現在セルへ設置する
- CharacterとBombは同じセルに存在できる
- Block、既存Bomb、予約セルとは重複できない
- 最大同時設置数、Fuse秒数、Explosion Powerを`BombComponent`が保持する
- Fuseは落下中も進む
- 足場がなければBomb専用の重力判定で落下する
- Fuse終了時に二重実行を防いでGrid登録を解除する
- 消滅時に設置者の現在Bomb数を戻す
- 現段階の爆発はConsoleログとBomb消滅までで、爆風は未実装

### Item（未実装仕様）

- 初期配置ではなく上空から落下する
- Playerが取得すると強化される
- 候補:
  - 爆風距離+1
  - 同時設置Bomb数+1
  - 移動速度上昇
  - Jump強化
  - Block貫通数+1

### Enemy AI（未実装仕様）

- Playerと同じ移動、ジャンプ、Block設置、Bomb設置能力を使う
- Playerへ接近する
- 爆風を回避する
- Itemを取得する
- Blockを配置する
- 難易度はEasy / Normal / Hardから選択可能にする
- 将来、セルごとの将来危険時間を持つDanger Mapを使用する

### 終盤フェーズ（未実装仕様）

- 一定時間経過後に開始する
- 上空からランダムにBlockが落下する
- 落下地点のCharacterは即死する
- 安全地帯を徐々に減らし、試合の長期化を防ぐ

## 3. 現在のアーキテクチャ

主要な責務分離:

```text
GridBomberGameMode
├── StageGenerator.GenerateStage()
└── CharacterSpawner.SpawnPlayer()

PlayerController
├── Camera基準入力の変換
├── MovementComponentへ移動・ジャンプ要求
├── BlockPlacementComponentへ配置要求
└── BombComponentへ設置要求

MovementComponent
├── 通常移動
├── その場ジャンプ
├── 段差ジャンプ
├── Character落下
├── 論理座標
└── 向き・移動状態

GridManager
├── GridCell生成と保持
├── 座標変換の窓口
├── Character/Block登録・解除・移動
├── 移動可能判定
├── 段差ジャンプ判定
└── Block配置判定と失敗理由

GridGravitySystem
└── 真下の着地セル計算
```

### Managerへ集めない方針

- 移動ルール: `MovementComponent`
- 重力の着地探索: `GridGravitySystem`
- 爆発探索: `ExplosionSystem`
- 経路探索: `GridPathfindingSystem`
- ステージ生成: `StageGenerator`
- 入力: `PlayerController`
- Block配置: `BlockPlacementComponent`

## 4. 実装済みファイル

### 実装が進んでいるもの

- `Core/Manager/GridManager.cs`
- `Core/Grid/GridUtility.cs`
- `Core/Gravity/GridGravitySystem.cs`
- `Cell/GridCell.cs`
- `Character/Components/MovementComponent.cs`
- `Character/Components/BlockPlacementComponent.cs`
- `Character/Player/PlayerController.cs`
- `Character/Player/CharacterSpawner.cs`
- `Character/Player/PlayerCharacter.cs`
- `Gameplay/Block/Block.cs`
- `Gameplay/Stage/StageGenerator.cs`
- `GameModes/GridBomberGameMode.cs`
- `Gameplay/Bomb/Bomb.cs`
- `Character/Components/BombComponent.cs`

### 現在ほぼ空の土台

- `Gameplay/Explosion/ExplosionSystem.cs`
- `Gameplay/Item/Item.cs`
- `Character/Components/InventoryComponent.cs`
- `Character/Components/LifeComponent.cs`
- `AI/Behavior/EnemyBrain.cs`
- `AI/Pathfinding/GridPathfindingSystem.cs`
- `AI/DangerMap/GridDangerMap.cs`
- `GameModes/GridBomberGameState.cs`
- `GameModes/EndPhase/EndPhaseController.cs`
- `UI/GameHud.cs`

## 5. Unity Editor設定

### Scene

想定Hierarchy:

```text
GridManager
StageGenerator
CharacterSpawner
GridBomberGameMode
Main Camera
```

PlayerはSceneへ事前配置せず、Play開始後に`CharacterSpawner`が1体生成する。

`GridBomberGameMode`:

- Stage Generatorを設定
- Character Spawnerを設定

`StageGenerator`:

- Grid Managerを設定
- Unbreakable Block Prefabを設定
- Breakable Block Prefabを設定
- Player Spawn Position既定値は(1,1,1)

`CharacterSpawner`:

- Grid Managerを設定
- Player Prefabを設定
- Game Cameraを設定
- Placeable Block Prefabを設定

### Player Prefab

必要Component:

```text
PlayerCharacter
MovementComponent
PlayerController
BlockPlacementComponent
BombComponent
PlayerInput
Collider / Visual
```

`PlayerController`:

- Movement Componentを同じPlayerのComponentへ設定
- Block Placement Componentを同じPlayerのComponentへ設定
- Bomb Componentを同じPlayerのComponentへ設定

`PlayerInput`:

- Actions: `InputSystem_Actions`
- Default Action Map: `Player`
- Behavior: `Invoke Unity Events`
- Move → `PlayerController.OnMove`
- Jump → `PlayerController.OnJump`
- PlaceBlock → `PlayerController.OnPlaceBlock`
- PlaceBomb → `PlayerController.OnPlaceBomb`

現在の入力Action:

- Move: Vector2（WASD等）
- Jump: Button
- PlaceBlock: Button（現状Eキー）
- PlaceBomb: Button（新規作成して任意キーへ割り当てる）

## 6. 重要な実装判断

### GridManagerとGridUtility

- 座標計算本体は`GridUtility`へ置く
- `GridManager`は現在のSize、CellSize、Transform原点を補う窓口を提供する
- 呼び出し側はGridManagerの`GetWorldPosition()`などを使う

### Characterの登録

- `TryRegisterCharacter`: Grid外から初めて登場させる初期配置・Spawn用
- `TryMoveCharacter`: GridCell間の通常移動・ジャンプ・落下用
- `TryUnregisterCharacter`: 死亡・退場用

### 論理位置と表示位置

- 移動・ジャンプ・落下開始時に論理セルを先に確保する
- Transformは`Awaitable`で後から補間する
- これにより、アニメーション中に他Characterが同じ着地点へ入ることを防ぐ

### 入力と移動の分離

- `PlayerController`は入力だけを解釈する
- `MovementComponent`は入力デバイスを知らない
- 将来Enemy AIも同じ`MovementComponent`を呼ぶ

## 7. 配置失敗ログ

Blockを置けない場合、以下をConsoleへ出す実装になっている。

- GridManager未設定
- MovementComponent不足
- Placeable Block Prefab未設定
- PlayerControllerのBlockPlacementComponent未設定
- Player PrefabにBlockPlacementComponentがない
- Moving/Fallingなど配置不可の移動状態
- 対象セルがGrid範囲外
- 対象セルにBlockが存在
- 対象セルにBombが存在
- 対象セルにCharacterが存在
- 対象セルが予約済み
- Block生成後のGrid登録失敗

ログには対象座標、現在座標、向き、移動状態も含める。

## 8. 動作確認済み

ユーザーが確認済み:

- ステージBlock生成
- Camera基準のPlayer水平移動
- Blockによる移動阻止
- Player自動生成
- 移動補間
- その場ジャンプ・段差ジャンプの基礎
- 地上正面／ジャンプ中直下へのBlock配置
- Characterの自動落下
- 配置したBlockの落下
- BombのGrid登録、重力、Fuse、消滅、同時設置数管理（コード実装。Editor設定と動作確認待ち）
- ExplosionSystemによる±X・±Y・±Zの爆風セル計算とConsole出力
- 爆風範囲内にあるBreakable Blockの破壊
- Block破壊後、同じ列の上側Blockを下から順番に重力再判定
- 爆風セル内のBombを収集し、Fuseを待たずに連鎖爆発
- ExplosionViewが計算済み爆風セルへ見た目専用Effectを生成
- ExplosionEffectが設定時間後に自身を破棄
- 爆風セルを`Center`、`Middle`、`End`、`BlockedEnd`に分類し、6方向へ回転表示
- Editorメニューから爆風Materialと4種類のParticle Prefabを自動生成可能
- 落下Blockが通過する各セルをGrid基準で判定し、Characterを押し潰して死亡させる処理

## 9. 既知の課題・注意点

1. `Block`は生成時と自身の落下終了時に重力を再判定するが、静止後に下のBlockだけが破壊された場合を常時監視していない。Block破壊実装時に、上に積まれたBlockへ落下再評価を通知すること。
2. `GridGravitySystem.TryGetFallDestination()`は現在`GridManager.CanEnter()`を共用している。Bomb/Item実装時は、Character・Block・Bomb・Itemごとに通過/着地条件を分けること。
3. その場ジャンプ中、元セルを別Characterが確保する競合への完全な予約処理は未完成。将来は「ジャンプ元予約」と「本人によるBlock配置許可」を両立する予約情報を設計する。
4. `GridCell.IsReserved`は存在するが、予約の設定・解除APIはまだ未実装。
5. `OnDrawGizmos()`は空。必要ならGrid可視化を追加する。
6. EditModeテストはPlaceholderのみ。GridUtility、占有移動、重力着地探索のテストを追加する。
7. Unity Editor上のコンパイル・Inspector設定は変更後に必ず確認する。
8. `CharacterBase`には`LifeComponent`が必要。既存Player Prefabへ自動追加されない場合はInspectorから追加する。

## 10. 次に実装する項目

次はBombの6方向爆発セル計算を実装する。

推奨順:

1. Bomb Prefab、BombComponent、PlaceBomb InputをEditorで接続して動作確認

   - Scene上の`CharacterSpawner`へBomb Prefabを設定する
   - Player Prefabへ`BombComponent`を追加する
   - Player Prefabの`PlayerController`へ同じ`BombComponent`を設定する
   - Input Actionsへ`PlaceBomb` Actionを追加し、`PlayerInput`から`OnPlaceBomb`へ通知する
2. `ExplosionSystem`で±X/±Y/±Zの影響セルを純粋計算（実装済み）
3. Fuse終了後に`Bomb`から`ExplosionSystem`へ爆発要求（実装済み）
4. 破壊不能Blockで停止する（実装済み）
5. 破壊可能Blockを影響対象に含め、そのセルで停止する（実装済み）
6. 爆風セル計算をEditModeテストまたはScene上のConsoleで検証する
7. 爆風対象の破壊可能Blockを破壊する（実装済み）
8. 破壊したBlockより上のBlockを重力再判定する（実装済み）
9. Characterを検出して`LifeComponent`へ死亡要求を送る（実装済み）
10. Bombを検出して連鎖爆発キューへ追加する（実装済み）
11. 計算結果を使って爆風Effectを表示する（コード実装済み・Prefab設定待ち）

Explosion EffectのEditor設定:

自動生成する場合:

1. ProjectウィンドウでBomb Prefabを選択する
2. `Tools > 3D Grid Bomber > Create Explosion Effect Prefabs`を実行する
3. `Assets/Prefabs/Effects/Explosion`と`Assets/Materials/Effects/Explosion`を確認する
4. Bomb Prefabを選択して実行した場合、4種類の参照も自動設定される

1. Center、Middle、End、必要ならBlockedEndの見た目用Prefabを作る
2. 各Prefabへ`ExplosionEffect`を追加する
3. Middle、End、BlockedEndはローカルZ+方向へ伸びる向きで作る
4. 判定はGrid側で完了しているため、死亡判定用Colliderは追加しない
5. Bomb Prefabへ`ExplosionView`を追加する
6. `Center Prefab`、`Middle Prefab`、`End Prefab`へ各Prefabを設定する
7. `Blocked End Prefab`は任意。未設定の場合はEnd Prefabを使用する
8. `Effect Duration`で表示秒数を調整する

最初のBomb完成条件:

```text
入力でBombを配置（実装済み）
→ GridCellへ登録（実装済み）
→ 足場がなければ落下（実装済み）
→ Fuse終了（実装済み）
→ 6方向の影響セルをConsoleで確認（コード実装済み・動作確認待ち）
→ BombをGridCellから解除（実装済み）
```

演出、死亡、Block破壊、Item出現は影響セル計算が安定してから追加する。

## 11. 今後の大まかなロードマップ

```text
Grid/占有管理                    完了
Player移動                       完了
ジャンプ                         基礎完了
Character重力                    完了
Block配置・重力                  基礎完了
Bomb配置・重力・Fuse             コード実装済み／Editor確認待ち
6方向Explosion                   次
Block破壊・Character死亡         未実装
Item落下・取得                   未実装
Enemy AI・Danger Map             未実装
終盤Block落下                    未実装
勝敗・UI・演出・Sound            未実装
```
