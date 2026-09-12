# ネオン電子音SE

このプロジェクト用に波形合成した効果音です。外部の録音・音楽・ゲーム音声のサンプルは使用していません。
44.1kHz、16bit PCM、モノラルWAV。全ファイルのピークを約0.72に制限しています。

## Unityへの登録

1. Playを停止してインポートを待つ。
2. 使用中のGameAudioSettingsをProjectで選択する。
3. `Tools > NEON DETONATOR > Audio > Assign Generated SE` を実行する。
4. AudioManagerのSettingsがこのAssetを参照していることを確認してPlayする。

選択していない場合はAssets/Settings/Audio/GameAudioSettings.assetを使用します。
登録済みClipは維持し、空欄だけ補完します。BGM・Mixer・全体音量は変更しません。
すでに別のSEが入っている項目を変更したい場合は、対応するWAVをSoundsのClipsへ手動で割り当ててください。

| ファイル | 用途 |
|---|---|
| BombPlace.wav | ボム設置 |
| Explosion.wav | 短い低音の爆発 |
| BlockPlace.wav | ブロック設置 |
| ItemCollect.wav | アイテム取得の上昇音 |
| CharacterDeath.wav | デジタルな下降音 |
| UiSelect.wav | 難易度変更 |
| UiConfirm.wav | 試合開始成功 |
| Win.wav | 勝利 |
| Lose.wav | 敗北 |

引き分けは勝敗ジングルを鳴らしません。移動・ジャンプ・独立したブロック破壊音は今回の対象外です。
勝敗音はGameHudの結果表示待機とフェードイン完了後に鳴ります。途中で結果表示を中断した場合は鳴りません。リトライ/戻るボタンには新規SEを接続していません。

## 調整と再生成

各音量はGameAudioSettings.Sounds.Volumeから調整します。最初は小さな音量で試聴し、BGMと複数爆発を重ねて調整してください。
再生成スクリプトはEditor/GenerateNeonSfx.ps1。PowerShell 7から実行できます。既存WAVを上書きしない設計です。
波形ヘッダー・データ長・ピーク値は検証済み。実機での聴感やBGMとの音量バランスは未確認です。
