# Itemの設定と確認

実装対象はBombPower（爆風距離）とBombCount（同時設置数）。SOは共有設定、InventoryComponentのボーナスはCharacterごとの実行状態。

## Unityでの設定

1. スクリプトのインポート・コンパイル完了後、`Tools > 3D Grid Bomber > Create Item Assets`を実行。
2. `Assets/Settings/Item`に効果別ItemSettingsとItemDropSettings、`Assets/Prefabs/Item`に2つのPrefabが生成される。球は爆風、立方体は同時設置数。既存Assetは保持する。
3. Sceneに空GameObject `ItemManager`を作り、ItemManager Componentを追加。
4. SettingsへItemDropSettings、Grid ManagerへSceneのGridManager、Game StateへGridBomberGameStateを割り当てる。Sceneに各1個しかなければ後者2つは自動検索も可能。
5. Player/EnemyへInventoryComponentを追加することを推奨。既存Prefabでも初回取得時に補完される。

## ルール

- Playing中のみ、既定6秒ごとに最上段の空きセルへ生成。最大5個（手動配置分も数える）。種類は均等抽選。
- Itemは1セルずつ落ち、セルに到着すると生存Characterの論理位置と照合して取得する。Characterを押し潰さない。
- ItemはCharacter・Bombと同居可能。Block・予約・他Itemに落下を妨げられる。床がなくても最下段で止まる。
- 爆風または後から同じセルに入ったBlockで消滅。登録解除は二重実行に耐える。
- 取得効果は通常+1、種類ごとにボーナス上限8。上限時も消費する。
- 爆風距離は設置時点でBombへコピーする。設置済みBombは後から強化されない。
- AIの仮想Bomb予測もBombComponentの強化済み性能を使う。Enemyは安全な着地済みItemを狙うCollectItem行動に対応。詳細は`ENEMY_ITEM_AI.md`。
- 移動速度・貫通・能力HUDは次の段階。

## Play Mode確認

1. Prefabを床の上の空きセルへ手動配置し、Playerで取得。ConsoleのItem取得ログと次に置くBombの射程を確認。
2. Bomb設置後に爆風Itemを取得。先に置いたBombの射程は変わらず、次のBombだけ伸びる。
3. 所持数Item取得後は2個同時設置できる。爆発後は枠が戻る。
4. Playerが取得してもEnemyの性能は変わらない。Enemy自身が取得するとEnemyだけが強化される。
5. 落下Itemを受け取っても死亡しない。爆風に当たったItemは消える。
6. 盤面5個で定期生成が止まり、取得・破壊後に再開する。試合終了後は新規生成しない。

取得判定はItemの見た目のセル到着とCharacterの論理セルを使うため、Characterの移動アニメーション完了より先に取得する場合がある。
