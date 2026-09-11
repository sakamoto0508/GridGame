# 背景と対戦ブロックをシーンへ事前配置する

## 操作手順

1. Playを停止し、HierarchyでStageGeneratorのあるGameObjectを選択する。
2. `Tools > NEON DETONATOR > Stage > Bake Background Into Scene` を実行する。
3. `Tools > NEON DETONATOR > Stage > Bake Blocks Into Scene` を実行する。
4. Sceneを保存する。

背景はScene Rooftop Background、対戦ブロックはScene Stage Blocksに生成される。
再生成は手動編集を置き換えるため確認ダイアログを表示する。Sceneの変更はUndo可能。

## 対戦マップの編集

- Scene Stage Blocks内のBlock Prefabインスタンスを移動・複製・削除して編集する。
- 追加する場合もBlockコンポーネントとBlockSettingsを持つPrefabをこの子へ配置する。
- 位置はGridManagerの原点＋セルサイズ×整数座標に合わせる。親Rootは動かさず、各Blockを移動する。
- 内部座標は0～Size-1。外殻は各軸-1またはSizeで、Unbreakableのみ許可。
- Player/Enemyの開始セルは空ける。床を削除すれば足場もなくなる。開始位置の変更は従来どおりStageSettingsで行う。
- 非アクティブなBlockは試合の登録対象外。見づらい外壁や天井はHierarchyの「目」アイコンでSceneビューだけ隠す（GameObjectを無効化しない）。
- 試合開始時に全Blockを検証して登録し、その後に下から初期化する。足場がなければ既存の重力が適用される。
- Scene Blocks Rootが設定されている間はランダムブロックと外殻の自動生成は行わない。生成エラー時はCharacterを生成せず試合開始を中止する。
- Scene配置Blockは破壊時に通常Destroyする。試合中に新規生成するBlock/Itemは既存のPoolを引き続き使用する。リトライはScene再読込なので初期配置へ戻る。

## 背景の編集と高所からの見た目

- 背景Mesh/MaterialはAssets/Scripts/Generated/CityBackgroundsに永続化する。Sceneだけ保存してMeshが失われる問題を避ける。
- 保存された背景は再生時に再生成せず、Scene上のTransform編集を維持する。
- 都市は四辺単位の結合Mesh。個々のビルを独立したGameObjectとして動かす構成ではない。まず設定値を調整して再ベイクする。
- RooftopBackgroundSettingsのFit Skyline To Grid HeightをONにすると、遠景ビルの屋上をフィールド最高高度＋Skyline Height Above Gridより上へ配置する。
- Skyline Rows（既定2）とSkyline Row Spacing（既定18セル）で奥の高層ビル列を追加。奥の列は高さ・幅を増やす。
- 実際の建物寸法を固定する方式で、Playerの高さに合わせた伸縮は行わない。遠近による見かけのサイズ差自体は残る。
- BackgroundSettingsやグリッドサイズを変更したら背景を再ベイクする。再生成時に旧Mesh Assetは消さず残す。
- 保存済み背景はPlay ModeのRebuildでは作り直さない。上記Editorメニューを使用する。

## 自動生成に戻す

Scene Stage Blocksを削除してStageGeneratorのScene Blocks RootをNoneにする。
背景はScene Rooftop Backgroundを削除してRooftopBackgroundControllerのBaked RootをNoneにする。
参照だけを解除して古いRootをSceneに残すと、描画が重複するため注意する。

## 確認状況

C#ビルド成功（警告0/エラー0）。この環境ではUnityでのベイク操作、Scene再読込、破壊・落下・勝敗、高所/QE回転時の描画は未検証。
