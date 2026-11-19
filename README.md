[GridGame_README.md](https://github.com/user-attachments/files/35059011/GridGame_README.md)
# NEON DETONATOR（GridGame）

**立体グリッド上で移動・ジャンプ・落下を駆使し、爆弾で戦う3Dアクションゲームです。** 爆発の危険を予測して行動する敵AIを中心に実装しています。

- [プレイ動画](https://youtu.be/u76-sm23hyg)
- [ポートフォリオ](https://global-diagnostic-240.notion.site/6ebbcae3a9fb476ba20aa0e5ad399064)

## 開発環境

- Unity `6000.3.10f1` / C# / URP `17.3.0`
- Input System `1.18.0` / Unity Test Framework `1.6.0`

## 主要な設計・実装

### 1. 目的に応じた経路探索の使い分け

| 目的 | アルゴリズム | 判断対象 |
| --- | --- | --- |
| 爆発からの退避 | BFS | 安全セルまでの経路 |
| 攻撃可能位置への移動 | A* | 爆風がプレイヤーに届く位置 |
| アイテム取得 | Dijkstra | 移動・ジャンプ・落下の所要時間 |

[GridPathfindingSystem.cs](Assets/Scripts/AI/Pathfinding/GridPathfindingSystem.cs)では経路計算と移動実行を分け、ステップごとに座標と移動・ジャンプ・落下・ブロック設置後ジャンプなどの行動種別を保持します。

**制約**：BFS退避探索はステップ数を基準とし、行動の所要時間が異なる場合、厳密な最短時間経路を保証しません。

### 2. 爆発時刻の危険予測

[GridDangerMap.cs](Assets/Scripts/AI/DangerMap/GridDangerMap.cs)は爆弾の範囲と残り時間からセルごとの危険時刻を計算します。連鎖爆発による時刻の前倒しや、仮想的に設置した爆弾も考慮し、設置前の安全判断に使っています。

### 3. 敵AIの意思決定

[EnemyBrain.cs](Assets/Scripts/AI/Behavior/EnemyBrain.cs)が危険回避・攻撃位置への移動・爆弾設置・アイテム取得などを切り替えます。危険マップと経路探索を別クラスに委譲し、行動判断の責務を分離しています。逃走できない攻撃位置を一時的に除外する処理や、往復移動への対策もあります。

```text
Grid / Bomb情報 → GridDangerMap（危険時刻）
                           ↓
EnemyBrain（行動目的） → GridPathfindingSystem（経路探索）
                           ↓
                    Movement / Bomb処理
```

## テストコード・検証範囲

`Assets/Scripts/Tests/EditMode`に次のNUnitテストコードがあります。

| ファイル | テスト対象 |
| --- | --- |
| [GridUtilityTests.cs](Assets/Scripts/Tests/EditMode/GridUtilityTests.cs) | 座標変換、負の座標、グリッド境界 |
| [ExplosionSystemTests.cs](Assets/Scripts/Tests/EditMode/ExplosionSystemTests.cs) | 爆風範囲、障害物、端の処理 |
| [GridPathfindingSystemTests.cs](Assets/Scripts/Tests/EditMode/GridPathfindingSystemTests.cs) | BFS退避、安全セル、危険セル、行き止まり、落下 |

**注**：これはテストコードの存在と対象範囲を示したものであり、今回Unity Test Runnerでの実行成功を確認したものではありません。A*やDijkstra、連鎖爆発時刻予測の全面的なテスト完了も意味しません。

### 今後の改善

- A* / Dijkstra / 連鎖爆発の境界条件のテストを追加
- 実グリッド規模で探索ノード数、処理時間、GC Allocを計測
- A* / Dijkstraの候補ノード選択は現在線形走査。必要な場合にPriority Queueの導入を検討

## 開き方

1. Unity HubでUnity `6000.3.10f1`を使用してプロジェクトを開きます。
2. Package Managerの依存関係を確認します。
3. [SampleScene.unity](Assets/Scenes/SampleScene.unity)を開いてPlayします。

※全環境でのビルド・起動成功を保証するものではありません。

## この作品で伝えたいこと

経路探索アルゴリズムをゲーム固有の立体移動や爆発時刻の予測と組み合わせ、敵AIの意思決定に応用したことです。設計上の制約や、未検証の範囲も明示しています。
