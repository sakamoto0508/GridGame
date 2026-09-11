# UIの流れる発光

## 表示

既存のNeonUIGlowを拡張したため、現在の発光付きUIはPlayし直すだけで適用されます。3D用Block Lit FlowをUIへ割り当てる必要はありません。

UI自体にNeonUIGlowと子Imageがない場合のみ、従来の `Tools > NEON DETONATOR > UI > Apply Layout and Sprites` で作成します。この操作は配置を変更するため、既にUIがあるなら再実行は不要です。

## CyberpunkUIThemeの設定

- Enable Glow: 従来の発光を含む全体のON/OFF。
- Enable Flow Glow: 流れる光のON/OFF。OFFにすると従来の静的Glowに戻る。
- Glow Flow Speed: 周回速度。既定0.25で約4秒/周、負値で逆回転、0で停止。
- Glow Flow Strength: 光の帯の強さ。
- Glow Flow Width: 光の帯の広さ。
- Glow Opacity / Emphasized Glow Opacity: 通常/選択時の透明度。難易度の選択色は既存のStyleから引き継ぐ。

Play中にSOを変更した場合は、対象CyberpunkUIStyleのApply ThemeかPlayし直しで反映します。SOの毎フレーム監視はしません。

## 仕様

- Resources/NeonUIFlow.shaderはuGUI専用。文字/ボタン本体ではなく、既存のGlow子Imageだけに割り当てる。
- 9-slice/AtlasのUVと独立したローカル座標で光を周回させる。ぼかしSpriteのAlphaを使用し、中央の透明部分は塗りつぶさない。
- 頂点色、CanvasGroup Alpha、Stencil Mask、RectMask2Dのクリップ/Softnessに対応するコードを実装。
- Overlay対応でBloom不要。HDRのにじみではなく、ぼかしたSpriteと流れる強弱による発光表現。
- Imageごとに専用Materialを持ち、無効化/破棄時に解放して元のMaterialに戻す。共有Material Assetは変更しない。個別MaterialのためUIのバッチ数が増える可能性あり。
- 非操作状態のボタンは光の移動による強弱を無効化し、従来のDisabled Glow Opacityを適用。
- アニメーションはGPUの_Timeを使用。Time.timeScale=0では停止。Update/LateUpdateでのUI更新は追加しない。

## Unityでの確認事項

1. 難易度を変更すると選択枠が緑になり、枠の上を光が巡回する。
2. STARTやHUDの文字と本体画像が変化していない。
3. 勝敗表示でGlowも一緒にフェードする。
4. 解像度変更後も発光レイヤーの大きさが合う。
5. Mask/RectMask2Dを使用する箇所でクリップされる。

実描画・Shaderコンパイル・上記操作はこの環境では未確認です。
