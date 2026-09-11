# 水色・黄色・緑のサイバーパンクUI

## 選択色・発光の整列・WASD入力の修正

- A/D（左右矢印）で難易度そのものを切り替え、即座に緑表示。端では停止する。
- W/S（上下矢印）で難易度欄/STARTへフォーカス移動。
- Enter/テンキーEnterで現在の難易度を使って開始（難易度欄にフォーカスしたままでも開始可能）。
- マウスによる難易度選択/STARTクリックも維持。
- MatchSetupUIのMenu Move/Menu SubmitはInputAction。Bindingが空ならWASD/矢印/Enterを設定。Inspectorから差替可能。
- 設定画面中のみEventSystemのNavigationを一時停止し、UI Moduleとの二重処理を防止。試合開始/無効化時に元へ戻す。
- 選択色はMatchSetupUIのTheme未指定でも各StyleのThemeを使用し、Sprite Overrideが選択画像を上書きしないようにした。
- 発光ImageのScale/回転を正規化。Spriteのサイズ差とPPU、9スライス縮小から余白を自動計算し、元の枠に一致させる。手動Glow Paddingは廃止。

Play停止中にApply Layout and Spritesを再実行してSceneを保存。保存済みSampleSceneは旧Dropdown参照のままだったため、未保存の編集内容と混同しないこと。
ビルド警告0/エラー0。Play ModeでA→D→S→W→Enter、マウス選択、異なる解像度での発光位置、結果画面の通常UIナビゲーション復帰を確認すること。

## UIの発光

保存SceneのCanvasはScreen Space Overlay。そのままではカメラ側のBloomの対象にならないため、ぼかした白い枠Spriteを着色して重ねる方式を採用。
実際のHDR発光や周囲を照らすLightではなく、UI内で光がにじんで見える表現。
Bloomで作る場合はScreen Space Camera/World Spaceへの変更に加え、対応Material・HDR・ポストプロセス・描画順の調整が必要なので、今回Canvasは変更していない。

### 反映

1. Play停止中に `Tools > NEON DETONATOR > UI > Apply Layout and Sprites` を再実行。
2. Sceneを保存。
3. ThemeのUI Glowを調整したら `Refresh Styles` でレイアウトを維持して反映。

各パネル/ボタン/キー枠にNeonGlowというImageと、親にNeonUIGlowをEditorで配置する。追加はEditorのみで、Play中のUI生成はしない。
通常枠の上、文字より下に発光Imageを表示。中央透明でRaycast Targetは無効。CanvasGroupによる結果フェードにも従う。
HOW TO PLAYの非表示背景には発光を出さない。文字にはぼかしを付けず可読性を維持する。

| Theme設定 | 既定値 | 効果 |
|---|---|---|
| Enable Glow | ON | 発光全体の有効/無効 |
| Glow Sprite | GlowRing | 白い発光用Sprite。色はPrimary/Success等から取得 |
| Glow Opacity | 0.30 | 通常パネル・枠 |
| Emphasized Glow Opacity | 0.85 | START/選択中難易度など |
| Hover Glow Opacity | 0.65 | マウスを重ねた時/キーボードフォーカス |
| Disabled Glow Opacity | 0.08 | 操作不能ボタン |

NeonUIGlowはPointer/SelectイベントとStyle変更から更新。Update/LateUpdateでの監視はない。
独自コードでButton.interactableだけを変更した直後に発光も更新したい場合は、そのObjectのNeonUIGlow.Refresh()を呼ぶ。
同梱GlowRing.pngは288×160、境界40px、中央と外端は透明。白背景のビューアーでは見えにくいが、Unity上で着色する素材。

### 確認項目

- 通常は水色の控えめな光、選択中難易度/STARTは強めの緑の光。
- マウス離脱後もキーボードフォーカス/選択がある場合は強調を維持。
- 選択を変えると旧難易度の緑色も解除される。
- STARTクリック、結果のフェード、再戦が従来どおり動く。
- 再適用してもNeonGlowは各Objectにつき1個。
- 発光が強いときはOpacityを下げる。余白は自動計算のため調整不要。

C#ビルド警告0/エラー0。PNGの中央/外端透明と枠からのアルファ減衰を確認。Unityでの実際の発光・操作・フェード確認は未実施。

## 最新: Sprite版とメニュー整理

現在使うUIメニューは次の2つ（以下の過去手順にある旧メニューは削除済み）。

1. `Tools > NEON DETONATOR > UI > Apply Layout and Sprites`
2. `Tools > NEON DETONATOR > UI > Refresh Styles`

Play停止中に1を実行し、Sceneを保存する。Renderer修復、標準Imageへの移行、PNGの9スライスImport、SO割当、配置をまとめて行う。
2は配置を変えず、文字色/Font/SOに指定したSpriteを再反映する。

旧Apply Cyberpunk UI Theme/専用Repairメニューを削除。再生成用のItem/Explosion/Default Settingsは `Tools > NEON DETONATOR > Assets` に整理して残した。
旧CyberpunkPanelGraphicクラスは未移行SceneのMissing Script防止用として残しているが、新しい配置では標準Imageを使用する。

PNGは `Assets/Scripts/UI/Art/Neon` に8種類を作成済み。角は透明、サイズ256×128、境界24px。各用途は同フォルダのREADME参照。
色は画像に含むため、パネル/ボタン配色はSpriteを差し替える。ThemeのCornerCut/BorderWidthは旧独自Graphic向けで、Spriteの形状は変更しない。
背景盤面イラスト/ロゴの文字画像は未作成。タイトル・説明・数値はTMPを維持する。

確認: メニュー適用後に各Panel/ButtonのImage TypeがSliced、Source Imageが設定済みであること。難易度選択でButtonSelectedへ切替、STARTで開始、結果/再戦、キー枠、外周中央の透明、再適用時の非重複を確認する。

## CanvasRenderer修正・添付見本の配置に変更

旧版の独自CyberpunkPanelGraphicにはCanvasRendererのRequireComponentがなく、新しいボタン生成時のRenderer追加をUGUI側に任せていた。
RequireComponentとEditor側の明示生成を追加。既存の欠落/破棄済みRendererキャッシュも、独自Graphicの設定を維持して修復する。

1. Playを停止してコンパイルを待つ。
2. `Tools > 3D Grid Bomber > Repair NEON UI Renderers`で既存UIを修復。
3. `Tools > 3D Grid Bomber > Apply NEON DETONATOR Layout`を実行して配置を更新（こちらにも修復処理を含む）。
4. Sceneを保存し、ConsoleをClearして再生。MouseをHARDなどに重ねて例外が出ないこと、クリック/左右移動/決定/STARTを確認。

新配置は1040×860。左上に白い英字タイトルと日本語名、右上に操作説明、中央に難易度ラベルと横並び3ボタン、下に大きな緑のSTART。
以前の左右カラム区切りとカテゴリ見出しは非表示。選択中の難易度は暗い背景と緑の枠/文字に変更。
テーマのSetup Backgroundへ任意の盤面画像Spriteを指定可能。Setup Background Tintで暗さを調整して再適用する。
見本全体（文字/ボタン入り）を背景にするとUIが二重になるため、背景だけの画像を使用する。未指定時は暗いPanel背景。

C#ビルドは警告0・エラー0。主要領域の比率計算は範囲内/重なりなしを確認。Unityでの修復メニュー実行・描画・Raycastの実行確認は未実施。

## 横並び難易度・サンプル風HUD（追加）

最新の `Apply NEON DETONATOR Layout` を再実行すると、以下を配置/接続する。

- 難易度はEASY / NORMAL / HARDの横並びButton。選択中は緑で塗り、他は暗い背景と控えめな枠。
- クリックで選択。キーボードはUIの左右入力でフォーカス移動、Submit（UI設定に従う）で確定。下でSTART、上で難易度に戻る。
- 初期選択と再戦/設定復帰時の選択はGameModeの難易度と同期する。試合開始後は選択不可。
- 操作説明をHOW TO PLAYカードに常時表示（設定画面中）。文言はMatchSetupSettings.ControlsText。説明はEditorでもプレビュー可能。
- 旧Dropdownは非表示で保持。新しいButton参照が全て揃うと新UIを優先する。Button未設定の旧SceneではDropdownで従来どおり動く。
- 左上HUDはALIVEラベルと大きな2桁の人数。右上はRANGE / LIMIT / PLACED / GRIDのラベルと右揃えの値。
- 下部にキー枠と操作名、画面外周に中央透明の細い水色枠。UIはゲーム画面を覆わず、装飾のRaycastも無効。
- GameHudの新しいAlive/Power/Limit/Placed/Position Value TextはEditorメニューで接続。旧Textは非表示で保持。更新は従来のActionのみ。

確認項目: 3難易度すべてで開始、設定へ戻った際の選択維持、左右フォーカスと決定、操作説明の表示、取得時のRANGE/LIMIT、設置/爆発時のPLACED、移動/落下時のGRID、死亡時のALIVE、結果フェードと再戦。
新UIの配置/参照反映にはメニューの再実行とScene保存が必要。C#ビルドは警告0・エラー0、Unityでの描画/操作確認は未実施。

## NEON DETONATOR版（2026-09-11）

- 新タイトルは「NEON DETONATOR（ネオン・デトネーター）」。英字2行のロゴ風見出しと日本語サブタイトルを配置。
- `Tools > 3D Grid Bomber > Apply NEON DETONATOR Layout`を実行すると、既存SOの色/Fontを維持して新しいサイズ設定を適用する。
- 設定画面は1160×700、左にタイトル/操作説明、右に難易度/開始/エラー。Panel内は比率による配置で領域を分離する。
- 結果は720×420、上に小さなゲーム名、中央に勝敗、下に再戦/戻る。HUD上部にも小さくゲーム名を表示。
- タイトル文言はSOのTitle/Japanese Title/Compact Titleで編集可能。変更後は配置メニューを再実行する（Refreshは色/Fontのみ）。
- 日本語サブタイトルには日本語対応のTMP Fontが必要。Body Fontが未指定なら既存Fontを使うため、文字が□になる場合はFontを設定する。
- 配置・サイズを手動調整した後は通常のRefreshを使用。NEONの配置メニューを再実行するとプリセットサイズに戻る。
- Sceneへの適用/保存はUnity Editorで実行する。ウィンドウの製品名やプロジェクト名は変更していない。

## 適用手順

1. Unityのコンパイルが終わるまで待つ。
2. Playを停止し、SampleSceneを開く。現在のSceneを保存しておく。
3. `Tools > 3D Grid Bomber > Apply Cyberpunk UI Theme`を実行する。
4. 変更内容の確認ダイアログで「適用」を選ぶ。
5. Gameビューで確認し、Sceneを保存する。

開いているアクティブSceneのMatchSetupUIとGameHudがそれぞれ1個であり、各Panel/Text/Button/Dropdown参照が揃っている必要がある。
Setup/Playing/Result Panelは同じCanvasに属していること。
適用直後は元のActive状態を維持する。Sceneで見た目を確認するときはSetupPanel/ResultPanelのActiveを一時的に切り替える。
Play時は既存のMatchSetupUI/GameHudが状態に応じて表示を切り替える。

## 変更されるもの

- CanvasScaler: 1920×1080基準のScale With Screen Size。
- SetupPanel: 中央の暗い角落としパネル、英字タイトル、難易度Dropdown、緑のSTARTボタン、説明文、黄色のエラー。
- PlayingPanel: 全画面の透明な親に変更。左上に水色の生存数カード、右上に能力/座標カード、下に操作ヒント。
- ResultPanel: 中央の角落としパネル、勝敗文字、緑のRETRYと水色の戻るボタン。
- 既存Panel/Button/Dropdownの背景ImageはCyberpunkPanelGraphicへ置換する。Button/Dropdown本体とイベントは維持。
- 装飾用のタイトル/カード/操作ヒントは編集時に生成され、Sceneに保存される。実行時にはUIを生成しない。
- 適用直後はCtrl+ZでScene変更を戻せる。新規SOアセットは残る。
- 何度実行しても同名の装飾Objectは再利用。ただし再適用はレイアウトをテーマの初期値に戻す。

## 変更しないもの

- 試合ルール、入力、難易度の対応順、HUDのActionによる更新、結果の遅延/フェード。
- 難易度の内部ルールは維持。表示は最新メニューで横並びButtonへ移行する。
- 3Dのキャラクター/ブロック/背景都市/ライト/爆風素材。
- 音声やBGM。GameAudioSettingsで別途指定する。
- 外部フォントの自動取得。既存Fontを維持し、任意で差し替える。

## テーマSO

`Assets/Settings/UI/CyberpunkUITheme.asset`が自動作成される。既存の場合はそのSOを使い、設定値を上書きしない。

| 項目 | 既定値/用途 |
|---|---|
| Primary | 水色 #70DFFF。HUD/基本枠 |
| Success | 緑 #6EF59A。開始/再戦/選択枠 |
| Warning | 黄色 #FFE45C。エラーなど注意文 |
| Panel | 暗い #0B1118、ほぼ不透明 |
| Text / Muted | 見出し/補助文 |
| Body Font | TMP Font Asset。日本語を含むため日本語対応フォント推奨 |
| Heading Font | 英字タイトル/結果用。未指定ならBody Font、それもなければ既存Font |
| Corner Cut / Border Width | パネルの角落とし量/枠幅 |
| Highlight Tint / Disabled Alpha | ボタンのフォーカス/無効状態 |
| Editorでの初期配置 | メニュー適用時のパネルサイズ・余白・文字サイズ |

色やフォントを変更したら `Tools > 3D Grid Bomber > Refresh Cyberpunk UI Styles` を実行する。
このメニューはレイアウトを変更しない。Play開始時やObject再有効化時にもスタイルは適用される。
レイアウトは配置済みRectTransformで自由に調整可能。
Themeを毎フレーム読み直す処理はない。Play中のSO変更を即時反映したい場合は対象CyberpunkUIStyleのContext Menu > Apply Theme。

## 実装メモ

- CyberpunkPanelGraphic: UGUIのメッシュで8頂点の角落とし形状と枠を描画。画像ダウンロードや特殊Shader不要。
- CyberpunkUIStyle: 色/Font/Selectableの見た目だけを担当。CanvasGroupによる結果フェードをそのまま利用。
- CyberpunkUIThemeInstaller: 既存UIのSerialized参照を使って適用。UIのゲームロジックは変更しない。
- 操作ヒントは現在のWASD/Space/F/R/Q/Eを記載。入力を変更したらTextも変更する。
- フォントは画像内の字形を抽出したものではない。Orbitronなどの実フォントを使う場合は別途TMP Font Assetを用意する。

## テスト

1. Editor適用後、Consoleにエラーがない。
2. 初回は設定画面が表示される。Dropdownのリストがパネルの前に開き、Easy/Normal/Hardを選べる。
3. STARTで盤面が生成され、左右のHUDが更新される。タイトル/設定は消える。
4. 勝敗後に結果パネルが遅延/フェード付きで出る。RETRY/戻るの両方が動く。
5. 1920×1080と1280×720でUIが画面外へ出ない。狭い縦長画面は別途レイアウト調整が必要。
6. 再適用しても装飾Objectが増殖しない。UndoでSceneの変更を戻せる。

C#ビルド確認済み。Unity Editor内のメニュー実行/描画/操作の検証は未実施。
