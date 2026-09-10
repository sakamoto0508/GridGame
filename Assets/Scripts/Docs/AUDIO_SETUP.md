# オーディオ設定

## 初回設定

1. Unityの再生を停止する。
2. ProjectのAssets/SettingsにAudioフォルダを作る。
3. 右クリック > Create > 3D Grid Bomber > Settings > AudioでGameAudioSettingsを作る。
   Tools > 3D Grid Bomber > Create Default Settings Assetsでも作成可能（既存Assetは上書きしない）。
4. Hierarchyで空のGameObjectを作り、AudioManagerと命名する。常時有効なSceneルートに置く。
5. AudioManagerコンポーネントを追加し、Settingsに作成したSOを指定する。
6. Main CameraのAudioListenerが有効で、Scene内に有効なAudioListenerが1個だけあることを確認する。
7. SOのSoundsを展開し、各項目のClipsへ用意した音声Assetを設定する。
8. Sceneを保存して再生する。Scene/Prefabや音声素材は今回自動作成していない。

## 音の対応

| Id | 再生箇所 |
|---|---|
| BombPlace | Player/Enemyのボム設置成功時 |
| Explosion | ボム1個の爆発につき1回要求（上限制御あり） |
| BlockPlace | Player/Enemyのブロック設置成功時。ステージ生成では鳴らない |
| ItemCollect | Player/Enemyの取得時 |
| CharacterDeath | Player/Enemy死亡時 |
| UiConfirm / Win / Lose | 将来用。呼出しは未接続 |

Clipsが空の音は無音でスキップする。複数登録するとランダム選択し、空スロットは無視する。
同じIdを複数作った場合は警告し、最初の項目だけ使用する。

## 調整項目

- Master Volume: 全体音量。
- Sfx Volume / Music Volume: SE / BGM音量。実際のSE音量はMaster × Sfx × 各音Volume。
- Voice Count: SE全体の同時再生上限。起動時にこの数のAudioSourceを作り、以降使い回す。既定16。
- 各音のMax Voices: 同じIdの同時再生上限。既定3。
- 各音のCooldown: 同じIdの再生要求を受け付ける最短間隔。既定0.05秒。0なら間隔制限なし。
- 上限に達した場合、再生中の音を切らずに新規要求を捨てる。爆発と死亡など異なるIdのCooldownは独立。
- Pitch: 再生速度・音程。
- Spatial Blend: 0なら2Dで距離によらず聞こえる。まずは全音0で動作確認する。
- Spatial Blendを1にすると発生地点の3D音になる。Min/Max Distanceはワールド単位で、カメラ上のListenerまでの距離を考慮する。
- Sfx Output / Music Output: 任意のAudioMixerGroup。未設定でも再生できる。
- Default Music + Play Music On Start: 起動時のループBGM。設定画面から流れる。試合状態別の切替やクロスフェードは未実装。

設定値を確実に確認するには、Playを停止してSOを編集してから再生する。
音量APIは実行時の値のみを変更し、SOやセーブファイルには保存しない。

## コードからの利用

```csharp
// 位置不要の音は常に2D。音が鳴らせない場合はfalseを返す。
AudioManager.Play(SoundId.UiConfirm);

// ゲーム内の音。3DにするかどうかはSO側で決める。
AudioManager.PlayAt(SoundId.Explosion, transform.position);

// BGMの切替、音量変更。Manager未配置でも安全。
AudioManager.Instance?.PlayMusic(musicClip);
AudioManager.Instance?.SetMasterVolume(0.5f);
AudioManager.Instance?.SetSfxVolume(0.8f);
AudioManager.Instance?.SetMusicVolume(0.3f);
AudioManager.Instance?.StopAllSfx();
AudioManager.Instance?.StopMusic();
```

SetMasterVolume / SetSfxVolume / SetMusicVolumeはSliderのOn Value Changed(float)にも接続できる。
UIは自動生成しない。必要ならInspectorで配置する。

## ライフサイクル

- ManagerはSceneに1個。DontDestroyOnLoadは使わず、再戦や設定へ戻る際に古い音を破棄する。
- SEのSourceはManagerの子なので、発音元が死亡・破棄・Pool返却されても再生が続く。
- Manager無効化時はSE/BGMを停止する。再有効化だけではBGMは自動再開しない。
- 起動時にSettingsが未指定なら警告して無効化する。設定後はPlayをやり直す。
- AudioManager未配置でも、ゲーム処理はそのまま動作する。
- 音声選択は専用乱数を使用し、ステージやAIのUnity乱数列に影響しない。

## 確認項目

1. 5種類のSEが、それぞれ成功時に鳴る。設置失敗では鳴らない。
2. 爆風距離が伸びてもセル数に比例して爆発音が増えない。
3. 連鎖爆発でMax Voices/Cooldown/Voice Countの制限が働く。
4. 死亡音・取得音が発音元の非表示後も途切れない。
5. 音量APIが再生中のSE/BGMにも反映される。
6. 再戦/設定へ戻るを繰り返しても古い音やManagerが残らない。
7. Game ViewのMute Audioが無効で、AudioListenerが1個ある。

C#ビルドは警告0・エラー0。実際の発音・音量バランスは音声Asset設定後にPlay Modeで確認すること。
