# ロボットモデルとアニメーション

1. UnityでPlayを停止し、コンパイル完了を待つ。
2. `Tools > NEON DETONATOR > Assets > Apply Robot Characters` を実行して適用する。
3. Player.prefab/TestEnemyPrefab.prefabが更新される。元のPrefabはAssets/Scripts/Generated/RobotCharactersへバックアップする。通常Undoではなくバックアップから復旧する。
4. Playして向き・歩行・ジャンプ・着地・ボム/ブロック設置を確認する。

既存の入力/AI/Collider/Movementの設定は維持する。適用済みなら再実行してもモデルを上書きしない。

## モデル

明るいグレーの頭・胴体・短い手足、正面のバイザー。Playerは水色、Enemyは黄色とアンテナ。
ゲーム本体とは別のRobot Visual以下に配置。各MeshをPrefab内で編集できる。
スキニングしたFBXではなくUnity標準プリミティブで組んだ初期モデル。

## 動作

RobotCharacterViewがMovementComponentのState/FacingDirectionと実移動量を読み取り、Visualの向きと関節姿勢だけを補間する。
待機の微小上下動、歩行の手足振り/傾き、ジャンプの足畳み、落下姿勢、着地のつぶれ、設置成功時の腕の突き出しを実装。
Animator/AnimationClip/RootMotionは使わない。ゲーム側の移動や入力を遅らせないため、ジャンプ前の待ち時間も追加しない。
死亡は従来の立方体破片演出をそのまま使用する。

RobotVisualSettingsで回転/姿勢補間速度、歩幅、振り幅、着地や設置の演出時間を調整できる。
ボム/ブロック設置は成功時だけ通知されるBombPlaced/BlockPlacedイベントを購読し、無効化時に解除する。

## 確認状況

C#ビルド成功（警告0/エラー0）。UnityでのPrefab適用・実描画・操作中の動作は未確認。特にカメラ距離での識別性、足と床の位置、空中設置時の姿勢を確認すること。
