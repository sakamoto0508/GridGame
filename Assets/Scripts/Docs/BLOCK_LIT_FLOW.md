# 不透明Block＋流れる発光

## 適用

1. Unityのインポート完了を待ち、Playを停止する。
2. `Tools > NEON DETONATOR > Assets > Apply Block Lit Flow` を実行し「適用」を押す。
3. 既存の `Assets/Material/BreakBlockMaterial.mat` と `UnbreakBlockMaterial.mat` が更新される。Prefabの付け直しは不要。
4. Playして、破壊可能Blockは緑、破壊不可Blockは水色の発光ラインになっているか確認する。

初回の元マテリアルは `Assets/Scripts/Rendering/MaterialBackups` に保存される。直後のUndoにも対応。適用済みのマテリアルは再実行で設定を上書きしない。

## 調整

Shader名は `NEON DETONATOR/URP/Block Lit Flow`。別のマテリアルでも直接選択して使える。

- Body Color / Base Texture: 本体の色と画像。Alphaは無視し常に不透明。
- Metallic / Smoothness: 金属感と表面の滑らかさ。
- EmissionColor / EmissionColorIntensity: 発光色と強度。
- Use Built-in Neon Lines: ONならテクスチャ不要の枠線。Neon Line Widthで幅、Neon Pulse Speedで流れる速度を変更。
- TillingXY: UVタイル数。Cubeの既存UVを利用するため、別モデルではUV配置に応じた模様になる。
- 任意の発光画像を使う場合: Use Built-in Neon LinesをOFFにし、TextureEmissionへ画像を設定。SpeedXYで画像をスクロール。
- UseAlphaGradient/UseSinOrTexture/Noise類: 本体の透過ではなく、発光だけの明滅に作用する。

## 仕様と制約

- 1つのMeshRenderer・1つのMaterialで本体と発光を描画。装飾用の子メッシュは不要。
- URPのPBRライティング、メイン/追加ライト、影、深度、深度法線Passを実装。本体Alphaは常に1。
- 不透明なので内部のBlockを透かして見ることはできない。外殻のカメラ側非表示は既存BoundaryBlockViewが引き続き担当する。
- 既存のBlockOutlineViewは変更しない。線が太く感じる場合はBlockSettingsのShow Outline/幅を調整。
- GPU Instancingを有効化。ゲーム側のBlock登録・落下・プール・破壊処理は変更しない。
- 動的ブロック向けで、ライトマップ・法線マップ・モーションベクトルは未対応。発光は周囲を照らすLightではなく、にじみには別途Bloomが必要。
- この環境ではUnityのメニュー適用・Shader描画テストは未実行。C#ビルドはShaderコンパイルの代わりにはならない。
