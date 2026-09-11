# 屋上・都市背景

## 表示する

1. Unityに戻ってスクリプトとShaderのインポートを待つ。
2. StageGeneratorのGrid Manager参照が設定されていることを確認する。
3. Playする。StageGenerator.Startが背景を生成するので、試合開始前から表示される。

Sceneの手作業による背景配置や追加Prefabは不要。背景設定が未指定でも既定値で表示する。

## 見た目を調整する

1. `Tools > NEON DETONATOR > Assets > Create Default Settings Assets` を実行。
2. `Assets/Settings/Environment/RooftopBackgroundSettings.asset` をStageGeneratorのBackground Settingsに割り当て、Sceneを保存。
3. Buildings Per Sideで四辺それぞれのビル数、City Gapで台座との距離、Building Height/Widthで高さ・幅を設定する。長さはセル単位。
4. Lit Window Chanceで点灯率、Window Brightnessで明るさ、Cyan/Greenで発光色を調整する。
5. 調整後はPlayし直す。Play中は追加されたRooftopBackgroundControllerのコンテキストメニュー `Rebuild Background (Play Mode)` でも再生成できる。

Hide Near Side Buildingsは既定でON。MainCameraタグの実カメラの向きを使って手前側のビルを非表示にする。Q/E回転中に表示が切り替わる方式で、フェードではない。

## 構造と注意

- 台座1Mesh、都市は四辺各1Mesh。窓も都市Meshに結合する。
- Collider、Block登録、AI探索への参加は一切ない。
- System.Randomを使い、ゲーム本体のUnityEngine.Randomの状態を変更しない。
- ライトに依存しない頂点カラーShaderをResourcesから読み込む。シアンの帯と窓はHDR色。にじむ発光にはカメラ側のHDR・Bloom設定が別途必要。
- Directional Light、スポットライト、カメラ、Skybox、UI背景画像は変更しない。UIの不透明な画像で覆われた部分は3D背景が見えない。
- 簡易的な3D都市背景であり、サンプル画像の写実的な都市を再現するものではない。空・霧・広告看板は含まない。
- UnityでのShader描画・実カメラでの視認性は要確認。通常のPlay終了で生成物を破棄する。
