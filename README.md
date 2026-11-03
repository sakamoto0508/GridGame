# NEON DETONATOR（GridGame）

**Unity / C#で制作した、立体グリッドと爆弾を使うアクションゲーム**です。グリッド上での移動・爆発と、危険を避けて行動する敵AIを実装しています。

- [プレイ動画（YouTube）](https://youtu.be/u76-sm23hyg)
- [ポートフォリオ](https://global-diagnostic-240.notion.site/6ebbcae3a9fb476ba20aa0e5ad399064)

## 開発環境

- **Unity**：`6000.3.10f1`
- **言語**：C#
- **レンダリング**：URP `17.3.0`
- **パッケージ**：Input System `1.18.0`、Unity Test Framework `1.6.0` ほか

## 技術的なこだわり

### 1. 用途別の経路探索

敵AIでは目的に応じて探索アルゴリズムを使い分けています。

| 目的 | 採用した方法 | 判断対象 |
| --- | --- | --- |
| 爆発からの退避 | BFS | 安全なセルへの経路 |
| 攻撃位置への接近 | A* | 爆弾を置ける位置への移動 |
| アイテム取得 | Dijkstra | 移動・ジャンプ・落下の所要時間を考慮した経路 |

[GridPathfindingSystem.cs](Assets/Scripts/AI/Pathfinding/GridPathfindingSystem.cs)では、経路を求める処理と実際に移動する処理を分離しています。経路のステップは位置だけでなく、移動・ジャンプ・落下などの行動種別も保持します。

**設計上の制約**：退避用BFSはステップ数を基準に探索するため、移動とジャンプの所要時間が異なる状況では厳密な最短時間経路を保証しません。

### 2. 爆発時刻の危険予測

[GridDangerMap.cs](Assets/Scripts/AI/DangerMap/GridDangerMap.cs)は、爆弾の爆発範囲と残り時間からセルごとの危険時刻を構築します。爆弾間の連鎖爆発による時刻の前倒しや、仮想的に設置した爆弾も考慮し、敵が安全な移動先を判断するために利用しています。

### 3. 敵の意思決定

[EnemyBrain.cs](Assets/Scripts/AI/Behavior/EnemyBrain.cs)は、危険回避・攻撃位置への移動・爆弾設置・アイテム取得などの目的を切り替えます。経路探索は`GridPathfindingSystem`、危険予測は`GridDangerMap`に委譲しています。

```text
Grid / Bomb情報 → GridDangerMap（危険時刻）
                           ↓
EnemyBrain（行動目的） → GridPathfindingSystem（経路）
                           ↓
                    Movement / Bomb処理
```

## プロジェクトの開き方

1. リポジトリをクローンし、Unity Hubから`6000.3.10f1`で開きます。
2. Package Managerの依存解決を確認します。
3. `Assets/Scenes/SampleScene.unity`を開いてPlayします。このシーンがBuild Settingsに登録されています。

> 外部アセットやローカル設定によっては追加作業が必要です。全環境での実行成功を保証するものではありません。

## 検証・今後の改善

- 探索の到達可能性、危険セルの回避、境界条件を自動テストで確認する予定です。
- 現在の[GridUtilityTests.cs](Assets/Scripts/Tests/EditMode/GridUtilityTests.cs)は`Assert.Pass()`のみのプレースホルダーであり、ロジックの正確性を検証するテストではありません。
- A*／Dijkstraの候補ノード選択は線形走査を使用しています。実際のグリッド規模で処理時間・GC Allocを測定し、必要に応じて優先度付きキューなどを検討します。

## この作品で伝えたいこと

アルゴリズムを単に実装するだけでなく、ゲーム内の行動制約と危険予測を組み合わせ、敵の意思決定に応用しました。
