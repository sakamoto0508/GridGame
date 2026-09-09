# 試合設定画面の配置手順

UIは動的生成しません。以下をUnityのHierarchyに事前配置してください。
GameModeは通常起動時に試合を自動開始しなくなったため、配置が済むまではWaitingのままです。

## 推奨Hierarchy

```text
Canvas                         GameHud、MatchSetupUIをここへ付ける
├─ SetupPanel
│  ├─ Title                    TMP Text（例: 3D Grid Bomber）
│  ├─ DifficultyDropdown       TMP Dropdown
│  ├─ StartButton              Button（表示: START）
│  ├─ ControlsText             TMP Text
│  └─ ErrorText                TMP Text
├─ PlayingPanel
│  ├─ AliveCountText           既存の人数表示
│  └─ PlayerStatusText         既存の能力表示
└─ ResultPanel                 既存Panel + CanvasGroup
   ├─ ResultText
   ├─ RestartButton            表示: もう一度遊ぶ
   └─ ReturnToSetupButton      Button（表示: 設定へ戻る）
EventSystem                    InputSystemUIInputModule
```

GameHud/MatchSetupUIは常時有効なCanvas側に付けてください。
SetupPanel/PlayingPanel/ResultPanel自身やその子には付けません。状態変更でPanelを隠してもイベント受信を継続するためです。

## Inspectorの接続

MatchSetupUIへ以下を設定します。

| フィールド | 指定先 |
|---|---|
| Game Mode | SceneのGridBomberGameMode |
| Game State | SceneのGridBomberGameState |
| Setup Panel | SetupPanel |
| Playing Panel | PlayingPanel |
| Difficulty Dropdown | DifficultyDropdown |
| Start Button | StartButton |
| Controls Text | ControlsText |
| Error Text | ErrorText |
| Settings | MatchSetupSettings（省略時は既定文言） |

- DifficultyDropdownのOptionsは**Easy / Normal / Hardの順に3項目**を設定します。
- GameHudのReturn To Setup Buttonへ新しい戻るButton、Game ModeへGameModeを設定します。
- GameModeのGame Hudへ既存GameHudを指定します（1個なら自動検索も可能）。
- Result PanelにはCanvasGroupを事前追加し、GameHudのResult Canvas Groupへ指定してください。
- **Button OnClickやDropdown OnValueChangedへの手動イベント登録は不要です。** ComponentがOnEnableで購読し、OnDisableで解除します。重複した手動登録がある場合は削除してください。
- EventSystemのUI操作はPlayer生成前にも必要です。SceneにInputSystemUIInputModuleを持つEventSystemを用意します。
- Sceneを保存し、Build ProfilesのScene Listへ追加します。結果ボタンは同じSceneを再読み込みします。

## 文言設定

Create > 3D Grid Bomber > Settings > Match SetupからSOを作成します。
Tools > 3D Grid Bomber > Create Default Settings Assetsでも作成できます。
Controls Text/Start Failed Textを変更できます。レイアウト・ボタン名・フォントはUIのInspectorで調整します。

現在の既定説明はInput Actionsのキーボード設定に合わせています。
移動=WASD/矢印、ジャンプ=Space、Block=F、Bomb=R、Camera=Q/E。
リバインド設定を自動解析する機能はないため、Bindingを変えたら説明文も修正してください。

## 動作

- 初回: 設定画面でWaiting。開始を押したら選択難易度で生成しPlayingへ。
- 難易度の既定はGameModeのEnemy Difficulty。Scene再読込時は前回選択を引き継ぎます。
- もう一度遊ぶ: 同じ難易度でSceneを再読込し、設定画面を飛ばして開始。
- 設定へ戻る: Sceneを再読込し、設定画面で待機。難易度を変更して再開可能。
- 引継ぎ情報は一時的な静的データ。SOやセーブファイルには書き込みません。EditorのPlay開始時にはリセットします。
- 連打はGameMode側でもガード。生成途中で失敗した場合はError TextとConsoleを確認し、設定を直してSceneを再ロードしてください。

## 確認項目

1. 初回に設定画面が出て、まだPlayer/Enemy/終盤イベントが動かない。
2. Easy/Normal/Hardの各選択で、Enemy初期化ログのDifficultyが一致する。
3. Startを連打してもPlayer/Enemyが二重に出現しない。
4. 開始後にSetupPanelが隠れ、能力HUDが表示・更新される。
5. 勝敗後の再戦では同じ難易度ですぐ開始する。
6. 設定へ戻ると盤面・予告がリセットされ、難易度を変えて再開できる。
7. 戻る/再戦の繰り返しでボタンイベントが重複しない。

C#コンパイルは確認済み。Scene内のUI配置・接続とPlay Mode確認は未実施です。
