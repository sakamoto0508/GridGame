# 3D Grid Bomber — Codex作業ログ兼仕様書

最終更新: 2026-09-11

## Cyberpunk Cube 1のFlowシェーダーURP移植（2026-09-11）

- 原因: インポートされたFlow_OnlyEmission_TransparentがBuilt-in用Surface Shaderで、URPの自動変換対象外。
- Rendering/Shaders/FlowOnlyEmissionTransparentURP.shaderを追加。元のプロパティ名・UVスクロール・Ramp色変化・Sin点滅・Simplex/画像ノイズを維持。元アセットは未変更。
- Editor/FlowShaderURPConverter.csに `Tools/NEON DETONATOR/Assets/Convert Flow Materials to URP` を追加。旧Shaderの.matだけを設定保持で差し替え、保存。確認ダイアログとUndo対応。
- 調査時の対象はCyberpunk Cube 1/Art/Materials/CT_  fukong01.matの1つ。実際の差し替えはUnityでメニュー実行が必要。
- 装飾用途のためShadowCasterは省略し、最終Alphaを0～1に制限。発光のBloomはカメラ側で必要。
- 新規Editorコードを一時targetsで含めたC#ビルド成功（警告0/エラー0）。targetsは削除。UnityでのShaderコンパイル・描画は未確認。
- 手順: Docs/FLOW_SHADER_URP.md。

## 屋上・都市背景（2026-09-11）

- StageGenerator.StartからRooftopBackgroundControllerを初期化。難易度選択中から暗い屋上台座・シアン帯・四辺のビル・シアン/緑の窓を表示する。
- 台座1Mesh＋都市4Meshへ窓も結合。ColliderやGrid登録を作らず、専用System.Randomでゲーム乱数に影響させない。
- MainCameraの向きに応じて手前側の都市を隠す。切り替えは即時で、フェードは未実装。
- RooftopBackgroundSettingsで数・寸法・色・点灯率を調整。未指定は既定値を使用。既存のCreate Default Settings AssetsにEnvironmentカテゴリのSO生成を追加した。SceneへのSO割り当てはUnity上で行う。
- Resources/NeonCityBackground.shaderはライト非依存の頂点カラー描画。既存のライト・Skybox・UI画像は変更しない。HDR色のにじみには既存カメラ側のBloomが必要。
- 設定手順: Docs/ROOFTOP_BACKGROUND_SETUP.md。
- 新規ファイルを一時targetsで含めたC#ビルド成功（警告0/エラー0）。一時targetsは削除済み。Unity上のShaderコンパイル・実描画・Q/E視認性は未確認。

## 難易度選択色・発光サイズ・メニュー入力修正（2026-09-11）

- 旧動作はUIフォーカス移動のみでは難易度が変わらなかった。専用InputActionsによりA/Dで難易度変更と緑表示、W/SでSTARTとのフォーカス移動、Enterで開始に変更。マウスも維持。
- 設定画面中のみEventSystem.sendNavigationEventsを退避/無効化して二重入力を防ぎ、試合開始/OnDisable/OnDestroy時に復元。
- Theme参照のフォールバックとSprite Override解除で選択色を確実に反映。
- 発光の子RectTransformのScaleリセット不足/固定余白を修正。sourceのPPUとSpriteサイズ差、9スライスの圧縮率から位置/寸法を計算。Glow Paddingは廃止。
- 保存済みSampleSceneは旧UI参照。適用/保存はUnityで必要。ビルド警告0/エラー0、Playでの操作/描画は未確認。

## Overlay UIの発光（2026-09-11）

- SceneのCanvasがOverlayであることを確認。Bloomのために描画方式を変えず、白いぼかし枠Spriteを着色して重ねる方式を実装。
- GlowRing.png追加（288×160、境界40px、16px余白、中心透明）。生成元Editor/BuildNeonGlow.ps1。
- NeonUIGlow追加。通常/強調/フォーカス/無効時の強さと広がりはTheme SO。Pointer/Select/Styleイベントのみで更新し毎フレーム監視なし。
- UI適用メニューが既存パネルへImageをEditor配置。Raycast無効、文字の背面、結果CanvasGroupのフェードを継承。通常文字はぼかさない。
- C#ビルド警告0/エラー0。PNGアルファの減衰確認済み。Scene反映とPlay Modeでの確認は未実施。

## Tools整理・UI PNG作成（2026-09-11）

- 旧テーマ適用/専用修復メニューを削除し、Tools/NEON DETONATOR/UIのApply Layout and SpritesとRefresh Stylesへ統合。Item/Explosion/Default Settings生成はAssetsサブメニューへ移動して維持。
- UI/Art/NeonへオリジナルPNG8種を実ファイルで作成。透明角/9スライス境界24px。生成元はEditor/BuildNeonSprites.ps1、用途は同フォルダREADME。
- 新規適用は標準Imageへ移行。独自Graphicは既存Sceneの移行互換用にのみ残す。SOのSprite参照を使って難易度/START/枠を切替。手動差替済みSpriteは維持。
- PNGの寸法/透明を確認、選択ボタン画像を目視確認。新規Importerを含むC#ビルドは警告0/エラー0。Unityでのメニュー実行/シーン反映は未実施。

## CanvasRenderer例外と開始画面の修正（2026-09-11）

- ユーザーからNeonDifficultyHARDのMissingComponentException報告。UGUIソースでImageはCanvasRenderer必須、GraphicはRectTransformのみ必須であることを確認。独自Panelの必須指定不足/自動Renderer追加依存を修正。
- CyberpunkPanelGraphicにRequireComponent(CanvasRenderer)。Editor生成はRendererを先にUndo管理下で追加。Repair NEON UI Renderersで既存PanelのRenderer欠落と破棄済みキャッシュを修復（Graphic再作成時も設定/Selectable参照保持）。配置適用時にも修復。
- 添付画像に合わせ、開始画面を1040×860・左上タイトル/右上操作説明/中央難易度横並び/下STARTへ変更。タイトルはNEON DETONATORを維持。選択ボタンの緑塗りを廃止し緑枠へ。
- 背景SpriteとTintをSOに追加（素材は未設定）。旧カラム区切りは非表示で保持。
- ビルド警告0/エラー0、主要UI領域の範囲/非重複を計算確認。Scene未変更、Unityで修復/適用/保存/実行確認が必要。

## 横並び難易度選択・分割HUD（2026-09-11）

- MatchSetupUIにEasy/Normal/Hard ButtonとTheme参照を追加。新参照が揃えば旧Dropdownより優先し、クリックで選択、標準UI左右ナビゲーション/Submitに対応。選択状態を緑で維持。
- 設定中は操作説明を有効化。EditorメニューでHOW TO PLAYカードを生成し既存ControlsTextを移動・プレビューする。
- GameHudに人数/爆風/所持上限/設置数/座標の個別Textを追加。既存Actionから更新し、毎フレーム監視なし。旧Textは保持して非表示。
- Editor適用メニューで左上の大きな人数、右上のラベル/数値行、下部キー枠、中央透明の外周枠を配置。Undoと再適用に対応。
- Sceneへの反映はApply NEON DETONATOR Layoutの再実行待ち。C#ビルド警告0・エラー0。Unityでの表示/選択操作は未確認。

## タイトル決定・NEON DETONATOR配置（2026-09-11）

- ゲームタイトルを「NEON DETONATOR（ネオン・デトネーター）」に決定。SOへ英字/日本語/短縮表示の文言とタイトルサイズを追加。
- Apply NEON DETONATOR Layoutメニューを追加。既存SOの配色/Fontを保持し、横長2カラムの設定画面、少し大きな結果画面、控えめなHUDタイトルへ再配置する。
- 既存のCyberpunkTitle/各参照を再利用し、操作説明と選択操作を左右に分離。Object生成はEditorのみ。
- C#ビルド確認。Scene反映と画面確認はUnityメニュー実行待ち。タイトル変更はUI対象でありProjectSettingsの製品名やコード名は変更していない。

## 水色・黄色・緑のサイバーパンクUI（2026-09-11）

- 画像サンプルの方向性に合わせ、UIテーマSO、角落としパネルGraphic、事前配置UIへのStyle設定を追加。
- EditorのApply Cyberpunk UI Themeメニューで既存MatchSetupUI/GameHudの参照から適用。中央設定/結果、左右HUD、説明/操作ヒントを編集時に配置。実行時UI生成なし。Undo/再適用に対応。
- Refresh Cyberpunk UI Stylesでレイアウトを維持した色/Fontの反映。SOに配色/フォント/初期配置値を集約。既定Settings生成にも追加。
- 難易度Dropdown、イベント更新、勝敗フェード、既存ゲームロジックは維持。フォントダウンロード/音源/3Dモデル/ライト/爆風配色は未変更。
- 手順と検証項目はDocs/CYBERPUNK_UI_SETUP.md。C#ビルド警告0・エラー0。Unityでのメニュー実行と見た目の確認は未実施で、Sceneはまだ変更していない。

## オーディオ管理（2026-09-10）

- Audio/AudioManagerとAudio/Data/GameAudioSettingsを追加。Sceneに1個をInspector配置し、SOで音声候補・音量・音程・2D/3D・距離・同時数・最短間隔を指定する。
- 固定数AudioSourceを再利用。SE全体/音別の上限超過時は新規要求をスキップ。発音元の死亡/Pool返却後も音は継続。Scene再読込では破棄する。
- Bomb設置/爆発、Block設置、Item取得、Character死亡へ成功時の呼出しを接続。爆発音はセル単位ではなくBomb単位。UiConfirm/Win/Loseは識別子のみで未接続。
- BGMループ再生/差替/停止、音量変更API、任意AudioMixer出力に対応。実行時の音量操作はSOを変更しない。UI/音素材/Scene配置は自動生成しない。
- 既定Settings生成メニューにAudioを追加。設定・テスト手順はDocs/AUDIO_SETUP.md。
- C#ビルド警告0・エラー0。音声未設定のため実際の発音確認は未実施。

## 試合設定画面（2026-09-10）

- GameModeの起動時自動開始を廃止。公開StartMatch(difficulty)で選択難易度を受け取り、1回だけ生成する。通常起動はWaiting。
- MatchSetupUI/MatchSetupSettings追加。事前配置したTMP Dropdown（Easy/Normal/Hard）、開始Button、操作説明/エラーText、設定/プレイPanelをInspectorで参照。ボタン・選択変更・試合状態をイベント購読する。
- GameHudに設定へ戻るButtonを追加。再戦は同難易度でScene再読込→即開始、設定復帰はScene再読込→Waiting。MatchLaunchRequestが一度きりの引継ぎを保持し、Play開始時にクリアする。SOは書き換えない。
- C#ビルドは警告/エラー0。Scene UIは未配置・未接続。`Docs/MATCH_SETUP_UI.md`に階層/接続/試験手順を記載。UIを配置するまでは自動開始しない点に注意。

## HUDのイベント化・事前配置方針（2026-09-10）

- UIは原則Scene/Prefabに事前配置しInspectorから参照する方針に変更。GameHudのPanel/TMP動的生成と結果CanvasGroupの自動追加を削除。Player Status TextとResult PanelのCanvasGroupをUnityで用意すること（Scene未変更）。
- GameHud.LateUpdateを削除。BombComponent.StatsChangedとMovementComponent.GridPositionChanged（event System.Action）で表示を更新。Bind/OnEnableで購読と初期反映、再Bind/OnDisable/OnDestroyで解除する。
- BombComponentは設置・爆発後に通知し、InventoryComponent.Changedも中継する。既存PrefabでInventoryがない場合はBombComponentがAwakeで補完し、初回取得の通知を取りこぼさない。
- Movementの全座標更新をSetGridPositionへ統一。移動・ジャンプ・落下で実際に論理座標が変わった場合のみ通知。
- GameHudSettingsから自動レイアウト用の位置/サイズ/色/余白を削除。表示書式と結果演出時間はSO、レイアウトや文字色・サイズはScene内のRectTransform/TMP/Imageで調整する。
- 検証項目: Item取得・Bomb設置/連鎖爆発・段差移動の表示、HUD無効化中の変化が再有効化時に反映されること、二重購読がないこと。Play Modeは未確認。

## Player能力・座標HUD（2026-09-10）

- GameModeが生成したPlayerをGameHud.BindPlayerへ注入。Game Hud未指定時はSceneのGameHudを検索。
- 爆風距離(ExplosionPower)、同時設置上限(MaxBombCount)、現在の設置数(CurrentBombCount)、グリッド座標(CurrentGridPosition)を表示。値が変わった場合だけ文字列を更新し、アイテム効果・設置・爆発・移動を反映。
- Player Status Text未指定時は既存Canvas右上に半透明のパネルとTMPテキストを生成。レイキャストは無効、結果UIの背面。既存の生存人数・結果フェードは維持。
- GameHudSettingsのPlayer Statusで文言/サイズ/位置/文字色/背景色/余白を設定。書式は{0}=射程、{1}=上限、{2}=設置中、{3}/{4}/{5}=X/Y/Z。既定はフォント互換性を考慮した英語表記。
- 座標は表示Transformではなく論理セル。移動開始時に先のセルへ切り替わる場合がある。死亡後も最終座標と残存Bomb数を参照する。表示対象が破棄されたら空欄にする。
- C#コンパイル成功（警告/エラー0）。Play Modeで初期値、Item取得後の増加、設置/連鎖爆発後のカウント、移動/ジャンプ/落下の座標、死亡/リスタートを確認すること。画面での配置・読みやすさは未確認。

## 天井Spot Light（2026-09-10）

- StageGeneratorがStageLightingControllerを追加し、LightingRoot配下に天井内側から下向きのSpot Lightを生成。既定2×2灯・影なし。既存Directional Lightは変更していない。
- Block Prefabや天井Rendererとは別管理なので、落下BlockにLightが付いたり、天井非表示で照明が消えることはない。
- StageLightingSettingsで有効/無効・灯数・色・強度・角度・天井からの距離・照射距離・影を調整。Create > 3D Grid Bomber > Settings > Stage Lightingから生成し、StageGeneratorのLighting Settingsへ指定。既定Settings生成メニューにも追加。
- フィールド寸法に合わせた等間隔配置。再生成時は既存灯を再利用。実行中にSOを調整した場合はControllerのContext Menu > Refresh Lightingで反映できる。
- Play Modeで明るさ、4方向からの段差視認性、影の負荷を確認すること。次の候補は現在の爆風距離/同時設置数を表示する能力HUD。

## 終盤落下イベント（2026-09-09）

- EndPhaseSettings/EndPhaseManager追加。GameModeからGridManagerへ自動初期化。既定60秒開始→2秒の列予告→破壊不可Block落下→着地後3秒待機。
- 天井内側の最上段へプールから生成。予告中のBlock/Bomb/予約による生成位置の閉塞はキャンセル。最上段Characterの押し潰しも追加。
- GridDangerMapに予告/落下列の危険情報を合流。仮想Bombによる逃走確認にも反映。全高を生成時刻から危険扱いする保守的な予測。
- 試合終了・無効化で予告を消去し、生成中/落下中のイベントBlockを返却。着地済みは残す。
- 設定・仕様・試験手順は`Docs/END_PHASE.md`。C#コンパイル成功（警告/エラー0）、Play Mode未確認。

## EnemyのItem取得（2026-09-09）

- CollectItem状態を追加。逃走と進行中の設置→ジャンプ完了を優先し、その次に安全な着地済みItemを狙う。
- GridPathfindingSystemにDijkstraによるItem経路探索と保持経路の再検証を追加。歩行/既存段差ジャンプ/降下を扱い、Block設置は行わない。予測危険セルは避ける保守的な経路。
- 目標を固定して消失/危険化/到達不能/タイムアウトで解除、待ち時間後に再探索。強化上限品は対象外。Item.SpawnVersionでプール再出現を区別。
- EnemyAISettingsに難易度別の取得有効設定・探索間隔・再試行待ち・計画制限時間を追加。DetectionRangeをItem検知にも利用。
- 行き詰まり後の待機中も危険判定を止めないように修正。手順は`Docs/ENEMY_ITEM_AI.md`。C#ビルド確認済み、Play Mode未確認。

## 側壁グリッド・破壊可能Block枠線（2026-09-09）

- BoundaryVisibilityControllerに側壁4面の縦横グリッド線を追加。面番号で線を管理し、手前の非表示壁に属する線も非表示にする。外枠12辺は従来どおり別制御。
- BoundaryViewSettings.ShowWallGridで側面線を表示切替。色・幅は既存のLineColor/LineWidthInCellsを共用。
- Block.InitializeでBreakableにBlockOutlineViewを自動追加。1セルの12辺を子LineRendererで描画し、落下時も追従。1本の経路で全辺を通り、一部の辺は重複する。
- BlockSettingsのShowOutline/OutlineColor/OutlineWidthInCellsで表示を調整。線・Materialは初回だけ生成してプール再利用時に再設定。返却時は非表示にし、Materialは最終破棄時に解放する。
- C#ビルド: 警告/エラー0。Unity上での描画は未確認。側壁のE/Q切替、線と面の重なり、Blockの落下・破壊・再設置を確認すること。

## 外殻の視認性改善（2026-09-09）

- StageGeneratorがBoundaryVisibilityControllerを自動追加し、外殻だけをBoundaryBlockViewへ登録する。天井は常時非表示、床は常時表示。描画Cameraの視線方向から手前の壁を隠し、斜め視点では2面を隠す。
- Renderer.forceRenderingOffだけを変更する。BlockのGameObject、Collider、グリッド登録は維持し、非表示の壁も移動制限として機能する。プール返却時は元の描画状態を復元。
- 内部領域を囲う12本の枠線と床グリッド線をLineRendererで追加。専用ShaderはResources/BoundaryOutline.shader。奥の壁や内部Blockの素材は変更していない。
- Cameraは未指定ならCamera.main。別の描画Cameraを使う場合はStageGeneratorのObjectにBoundaryVisibilityControllerを事前追加してCameraへ指定する。
- 表示/線色/線幅はBoundaryViewSettingsで調整。Create > 3D Grid Bomber > Settings > Boundary Viewで作成し、事前追加したControllerへ設定。未指定時は実行時SOの既定値を使用。
- C#コンパイルは警告/エラー0。Shaderと実際の描画はUnity Play Modeで未確認。確認項目: 4方向と回転途中、天井の非表示、床グリッド、非表示壁への移動不可、Block返却/再利用後の描画復元。

## フィールド外殻（2026-09-09）

- ユーザー確認済み: 内部Size=(15,15,15)なら内部座標0～14、外殻は各軸-1/15、外寸17×17×17。厚さ1セルで床・四方の壁・天井を囲う。
- StageGeneratorは従来の内部Y=0床/1段の壁を廃止し、6面の外殻をプールから生成。角・辺は重複させない。内部の破壊可能Block生成はX/Z=0～Size-1を対象に変更し、開始安全地点の除外は維持。
- StageSettingsのFloorY/WallYは廃止。床の高さは-1に固定。既存のPlayer/Enemy開始位置・BreakableBlockY設定は維持するため、Y=1なら足場がないとY=0へ落ちる。
- GridManager.Containsは内部範囲のみ。外殻は専用DictionaryとTryRegisterBoundaryBlockで管理し、GetBlock/HasBlockでは外殻も参照できる。通常の移動・Item/Bomb/Block設置範囲は拡張しない。
- 外殻Blockは重力対象外。返却時に外殻登録と固定フラグも解除する。AIの足場判定・段差からの落下先探索はY=-1床を認識するように修正。
- 検証: コンパイルは警告/エラー0（未反映CameraSideSettingsを一時的にビルドへ追加）。座標生成アルゴリズムを15³/7³/2×3×4/1³で照合し、全外殻の網羅・重複なしを確認。15³の外殻は1,538個。
- Play Mode未確認: 床Y=-1上でのPlayer/Enemy移動、外殻の固定・爆風耐性、天井でのジャンプ停止、再利用後のBlock重力を確認すること。
- 天井・手前壁の描画は「外殻の視認性改善」で非表示対応済み。上空Itemは従来どおり内部最上段へ生成する。

## E/Qによる視点切替（2026-09-09）

- カメラ入力は1つのVector2 Actionへ統合。PlayerController.OnCameraChangeが値をGridCameraSideController.HandleInputへ渡し、カメラ側でXの正負を判断する。Yは未使用。同方向の継続入力は1回のみ、canceled時のゼロ入力で解除する。
- UnityではCameraChange（Value/Vector2、Interactionsなし）の2D Vector Compositeを使い、Right=E、Left=Q、Up/Downは空欄。PlayerInputのUnity EventsからOnCameraChangeに接続する。今回Asset/Prefabは変更していない。

- PlayerControllerがE/Q入力を受け、GridCameraSideControllerに90度の視点切替を要求。Eは右隣の辺側（初期-Z側なら+X側）、Qは逆方向。長押しでは連続回転しない。
- CharacterSpawnerが設定済みCinemachineCameraに切替Componentを自動追加・注入。Player追従は維持し、FollowOffsetと仮想Cameraの回転を初期値から4方向に変更する。フィールド中心の周回ではなくPlayer追従視点の向き切替。
- FollowのBinding ModeはWorld Space。高さ・距離・傾きを維持し、円弧上を既定0.4秒で滑らかに切り替える。CameraSideSettingsのTransitionDurationで調整し、0なら即時。切替中の追加入力にも現在の表示角度から対応する。通常追従のDampingは維持。
- CameraSideSettingsはCreate > 3D Grid Bomber > Settings > Camera Sideから作成し、CinemachineCameraにGridCameraSideControllerを事前追加してSettingsへ設定する。未指定でも実行時SOの既定値を利用。
- Rotation Control=Noneを推奨。Game Cameraは実際の描画用Cameraを維持し、移動入力は表示されたカメラ方向で計算する。
- SampleSceneにはCinemachine Cameraが2つあるため、Spawnerに指定した方が出力されるよう、不要な方を無効化するかPriorityを設定する。Scene自体は変更していない。
- Play Mode確認: E×4で一周、E→Qで元に戻る、長押し・同時押し、切替後の方向入力、ジャンプ・落下後も追従。

## Item・Blockプール化（2026-09-08）

- GridObjectPoolを導入。GridManagerごと・Prefabごとに共有し、StageGenerator、BlockPlacementComponent、ItemManagerの生成をRentへ変更。
- PooledGridObjectをItem/Blockの共通基底とし、取得・破壊・生成失敗時に登録解除して返却。SO設定と既存Prefabは継続利用できる。
- Itemの取得・落下状態をリセット。Blockは世代番号で返却前のAwaitableを無効化し、再利用後の個体に古い処理が触れないようにした。
- Scene終了でプールも破棄する。Bomb/爆発演出は対象外。使い方と確認項目は`Docs/OBJECT_POOL.md`。

## Camera方針変更（2026-09-08）

- Cinemachineを使用する方針のため、独自のPlayerCameraFollow、CameraFollowSettingsと対応meta、Spawnerでの追従初期化、専用SO生成処理を削除。
- CharacterSpawnerのGame CameraとPlayerControllerへの注入は、カメラ基準入力に必要なので維持。Camera未指定時はCamera.mainを利用する。
- 今回は不要要素の削除のみ。生成PlayerをCinemachineの追従対象へ割り当てる処理やScene設定は未実装。

## Item実装（2026-09-08）

- BombPower/BombCountの2種類を実装。ItemSettingsとItemDropSettingsで調整する。
- GridCell/GridManagerへItem登録・移動・解除APIを追加。Itemは1セルずつ落下し、到着セルのCharacterへ効果を与える。
- InventoryComponentがCharacterごとのボーナスを保持。SOを変更しない。BombComponentとAIの仮想予測は強化済み性能を参照する。
- Bombは設置時の射程を固定。爆風中のItemは破壊される。
- ItemManagerはPlaying中のみ定期出現・盤面上限を管理。
- EditorのCreate Item Assetsメニューで既存設定を保持しつつテストPrefab/設定を生成可能。
- Unity設定・仕様・確認手順は`Docs/ITEM_SETUP.md`。CollectItem AI、速度/貫通Item、能力HUDは未実装。

このファイルは、別のCodexチャットや別の開発者が現在の状態から作業を再開するための引き継ぎ資料である。
コードを変更した際は「現在の実装状況」「既知の課題」「次に実装する項目」も更新すること。

## 1. プロジェクト概要

- エンジン: Unity
- ジャンル: 3Dグリッド対戦アクション
- 仮タイトル: **3D Grid Bomber**
- 基本コンセプト: 3次元空間で戦うボンバーマン
- 基本勝利条件: 最後まで生き残る、または敵を全滅させる
- 基本敗北条件: 爆風または落下Blockの下敷きで死亡する

### Unity上の座標規約

企画書原案ではZを高さとしていたが、Unity実装では次に統一している。

```text
X: 左右
Y: 高さ
Z: 前後
```

論理グリッド座標には`Vector3Int`、表示上のワールド座標には`Vector3`を使用する。
`CellSize`はGrid→World変換時にだけ使用し、論理座標へ掛けない。

## 2. ゲーム仕様

### フィールド

- 既定サイズは7×7×7
- 完全な3次元グリッド
- 各セルはカテゴリ別に以下を保持できる
  - Block
  - Bomb
  - Item
  - Character
  - 予約状態
- 1セルに単一の汎用`Occupant`を置く設計にはしない

現在の試作ステージは次の構成である。

```text
y = 0: 床Block
y = 1: Player、外周壁、通路、破壊可能Block
```

`StageGenerator`は以下を生成する。

1. y=0の固定床
2. y=1の外周壁
3. 内側セルの破壊可能Block
4. Player開始地点と、その右・前の脱出用セルは空ける

ランダム配置は`_randomSeed`で再現可能にしている。

### Player操作

- 水平4方向へ1セル単位で移動する
- 入力方向はCameraから見たX/Z方向へ変換する
- 斜め入力はワールドX/Zの強い軸へ丸める
- Yは高さ専用
- PlayerとEnemyは同じ`MovementComponent`を使用する想定

#### 通常移動

- 移動先セルが空いている場合だけ移動できる
- 移動開始時に論理セルのCharacter占有を移動先へ更新する
- 表示位置はUnity `Awaitable`で補間する
- 移動中は新しい移動・ジャンプ要求を受け付けない
- 最後に入力された水平4方向を向きとして保存する

#### その場ジャンプ

方向入力なしでJumpを押すと、現在セルの1セル上へジャンプする。

```text
ジャンプ前       ジャンプ中
y=2 空           y=2 Player
y=1 Player       y=1 空
y=0 Block        y=0 Block
```

- 上のセルが空いている場合だけ開始可能
- ジャンプ中、元のセルへBlockを配置できる
- Blockを置かなければ元のセルへ戻る
- Blockを置けばPlayerは上のセルに残り、そのBlock上へ着地する
- Block配置可能時間は主に`MovementComponent._airTime`で調整する

#### 方向付き段差ジャンプ

移動方向を押しながらJumpを押すと、方向先の1段高いBlock上へジャンプする。

例:

```text
現在Playerセル    (1, 1, 1)
入力方向          (1, 0, 0)
対象Blockセル     (2, 1, 1)
着地Playerセル    (2, 2, 1)
```

成功条件:

- 入力がX/Z平面の単位4方向
- 方向先の同じ高さにBlockがある
- Block上のセルがグリッド内
- Block上のセルにBlockまたはCharacterがなく、予約されていない
- Characterが移動中・ジャンプ中・落下中ではない

表示は放物線状に補間する。

#### Characterの重力

- Player直下に足場がなければ真下へ落下する
- 真下を探索し、最初の障害物の1セル上へ着地する
- 高所から空セルへ水平移動した場合も落下する
- 待機中に足場Blockが失われた場合も`Update()`で検出して落下する
- 落下開始時に着地点を論理的に確保する
- 落下中は入力を受け付けない
- グリッド最下部より下には落ちない

### Block

種類:

- `Breakable`
- `Unbreakable`

用途:

- 通路封鎖
- 爆風防御
- 足場作成

配置ルール:

- 地上ではCharacterの正面セルへ置く
- その場ジャンプ中はCharacter直下へ置く
- Block、Bomb、Characterまたは予約があるセルには置けない
- 配置失敗時はConsoleへ具体的な理由を出す

重力:

- Block生成・初期化時に直下を探索する
- 足場がなければ最初の障害物の1セル上まで落下する
- 落下前にGridCell上のBlock登録を着地点へ移動する
- 表示は`Awaitable`で補間する

将来仕様:

- 爆風で破壊可能Blockを破壊する
- 落下BlockがCharacterへ当たったら即死させる
- 支えているBlockが後から破壊された場合、上のBlockも再度落下させる

### Bomb

- PlayerはBombを設置できる
- 同時設置可能数を持つ
- Bombは重力で落下する
- 一定時間後に爆発する
- 誘爆による連鎖爆発がある
- 爆発はUnity座標の±X、±Y、±Zの6方向へ伸びる
- 爆風距離はBomb Powerで増える
- 爆風はCharacter/Enemyを即死させる
- 爆風は破壊可能Blockを破壊する
- 破壊不能Blockで爆風は停止する
- 破壊可能Blockは破壊され、そのセルで爆風を停止する想定
- 将来アイテムでBlock貫通数を増やせる
- 爆発判定はActor/Effect生成より先に、影響GridCellを計算する
- 連鎖爆発は直接再帰ではなくキューで処理する方針

現在の実装:

- BombはBlockを継承しない独立クラス
- Characterの現在セルへ設置する
- CharacterとBombは同じセルに存在できる
- Block、既存Bomb、予約セルとは重複できない
- 最大同時設置数、Fuse秒数、Explosion Powerを`BombComponent`が保持する
- Fuseは落下中も進む
- 足場がなければBomb専用の重力判定で落下する
- Fuse終了時に二重実行を防いでGrid登録を解除する
- 消滅時に設置者の現在Bomb数を戻す
- 現段階の爆発はConsoleログとBomb消滅までで、爆風は未実装

### Item（未実装仕様）

- 初期配置ではなく上空から落下する
- Playerが取得すると強化される
- 候補:
  - 爆風距離+1
  - 同時設置Bomb数+1
  - 移動速度上昇
  - Jump強化
  - Block貫通数+1

### Enemy AI（未実装仕様）

- Playerと同じ移動、ジャンプ、Block設置、Bomb設置能力を使う
- Playerへ接近する
- 爆風を回避する
- Itemを取得する
- Blockを配置する
- 難易度はEasy / Normal / Hardから選択可能にする
- 将来、セルごとの将来危険時間を持つDanger Mapを使用する

### 終盤フェーズ（未実装仕様）

- 一定時間経過後に開始する
- 上空からランダムにBlockが落下する
- 落下地点のCharacterは即死する
- 安全地帯を徐々に減らし、試合の長期化を防ぐ

## 3. 現在のアーキテクチャ

主要な責務分離:

```text
GridBomberGameMode
├── StageGenerator.GenerateStage()
├── CharacterSpawner.SpawnPlayer()
├── CharacterSpawner.SpawnTestEnemy()
├── GridBomberGameState.StartMatch()
└── Enemy Difficulty選択（Easy / Normal / Hard）

PlayerController
├── Camera基準入力の変換
├── MovementComponentへ移動・ジャンプ要求
├── BlockPlacementComponentへ配置要求
└── BombComponentへ設置要求

EnemyBrain
├── 難易度別の行動間隔・判断ミス・Bomb確率・検知距離
├── Player検知外ではランダム徘徊
├── Player検知中は距離が縮む方向を優先
├── 近距離でBomb設置
└── 全方向が塞がれた場合にBomb設置を試行

MovementComponent
├── 通常移動
├── その場ジャンプ
├── 段差ジャンプ
├── Character落下
├── 論理座標
└── 向き・移動状態

GridManager
├── GridCell生成と保持
├── 座標変換の窓口
├── Character/Block登録・解除・移動
├── 移動可能判定
├── 段差ジャンプ判定
└── Block配置判定と失敗理由

GridGravitySystem
└── 真下の着地セル計算
```

### Managerへ集めない方針

- 移動ルール: `MovementComponent`
- 重力の着地探索: `GridGravitySystem`
- 爆発探索: `ExplosionSystem`
- 経路探索: `GridPathfindingSystem`
- ステージ生成: `StageGenerator`
- 入力: `PlayerController`
- Block配置: `BlockPlacementComponent`

## 4. 実装済みファイル

### 実装が進んでいるもの

- `Core/Manager/GridManager.cs`
- `Core/Grid/GridUtility.cs`
- `Core/Gravity/GridGravitySystem.cs`
- `Cell/GridCell.cs`
- `Character/Components/MovementComponent.cs`
- `Character/Components/BlockPlacementComponent.cs`
- `Character/Player/PlayerController.cs`
- `Character/Player/CharacterSpawner.cs`
- `Character/Player/PlayerCharacter.cs`
- `Gameplay/Block/Block.cs`
- `Gameplay/Stage/StageGenerator.cs`
- `GameModes/GridBomberGameMode.cs`
- `GameModes/GridBomberGameState.cs`
- `UI/GameHud.cs`
- `Gameplay/Bomb/Bomb.cs`
- `Character/Components/BombComponent.cs`
- `AI/Behavior/EnemyBrain.cs`
- `AI/Behavior/EnemyDifficulty.cs`

### 現在ほぼ空の土台

- `Gameplay/Explosion/ExplosionSystem.cs`
- `Gameplay/Item/Item.cs`
- `Character/Components/InventoryComponent.cs`
- `AI/Pathfinding/GridPathfindingSystem.cs`
- `AI/DangerMap/GridDangerMap.cs`
- `GameModes/EndPhase/EndPhaseController.cs`

## 5. Unity Editor設定

### Scene

想定Hierarchy:

```text
GridManager
StageGenerator
CharacterSpawner
GridBomberGameMode
Main Camera
```

PlayerはSceneへ事前配置せず、Play開始後に`CharacterSpawner`が1体生成する。

`GridBomberGameMode`:

- Stage Generatorを設定
- Character Spawnerを設定
- Game Stateを設定
- Enemy DifficultyをEasy / Normal / Hardから選択

`StageGenerator`:

- Grid Managerを設定
- Unbreakable Block Prefabを設定
- Breakable Block Prefabを設定
- Player Spawn Position既定値は(1,1,1)
- Enemy Spawn Position既定値は(5,1,5)

`CharacterSpawner`:

- Grid Managerを設定
- Player Prefabを設定
- Test Enemy Prefabを設定
- Game Cameraを設定
- Placeable Block Prefabを設定

### Test Enemy Prefab

最低限必要なComponent:

```text
EnemyCharacter
MovementComponent
LifeComponent
BombComponent
EnemyBrain
```

Playerと区別できる色やMeshを設定する。BombComponentとEnemyBrainは同じ共通Movementを使用する。

### GameHud

Canvasへ`GameHud`を追加し、以下を設定する:

- Game State: Scene上の`GridBomberGameState`
- Alive Count Text: 生存人数表示用のTextMeshProUGUI
- Result Panel: 試合終了時だけ表示するPanel
- Result Text: YOU WIN / YOU LOSE / DRAW表示用のTextMeshProUGUI
- Restart Button: Scene再読み込み用Button
- Result Canvas Group: Result PanelのCanvasGroup（未設定なら実行時に自動追加）
- Result Delay: 死亡から表示開始までの秒数（既定0.75秒）
- Fade Duration: 透明から完全表示までの秒数（既定0.5秒）

Restart ButtonのOnClickはコードで購読するため、InspectorのOn Clickへ重ねて登録しない。

### Player Prefab

必要Component:

```text
PlayerCharacter
MovementComponent
PlayerController
BlockPlacementComponent
BombComponent
PlayerInput
Collider / Visual
```

`PlayerController`:

- Movement Componentを同じPlayerのComponentへ設定
- Block Placement Componentを同じPlayerのComponentへ設定
- Bomb Componentを同じPlayerのComponentへ設定

`PlayerInput`:

- Actions: `InputSystem_Actions`
- Default Action Map: `Player`
- Behavior: `Invoke Unity Events`
- Move → `PlayerController.OnMove`
- Jump → `PlayerController.OnJump`
- PlaceBlock → `PlayerController.OnPlaceBlock`
- PlaceBomb → `PlayerController.OnPlaceBomb`

現在の入力Action:

- Move: Vector2（WASD等）
- Jump: Button
- PlaceBlock: Button（現状Eキー）
- PlaceBomb: Button（新規作成して任意キーへ割り当てる）

## 6. 重要な実装判断

### GridManagerとGridUtility

- 座標計算本体は`GridUtility`へ置く
- `GridManager`は現在のSize、CellSize、Transform原点を補う窓口を提供する
- 呼び出し側はGridManagerの`GetWorldPosition()`などを使う

### Characterの登録

- `TryRegisterCharacter`: Grid外から初めて登場させる初期配置・Spawn用
- `TryMoveCharacter`: GridCell間の通常移動・ジャンプ・落下用
- `TryUnregisterCharacter`: 死亡・退場用

### 論理位置と表示位置

- 移動・ジャンプ・落下開始時に論理セルを先に確保する
- Transformは`Awaitable`で後から補間する
- これにより、アニメーション中に他Characterが同じ着地点へ入ることを防ぐ

### 入力と移動の分離

- `PlayerController`は入力だけを解釈する
- `MovementComponent`は入力デバイスを知らない
- 将来Enemy AIも同じ`MovementComponent`を呼ぶ

## 7. 配置失敗ログ

Blockを置けない場合、以下をConsoleへ出す実装になっている。

- GridManager未設定
- MovementComponent不足
- Placeable Block Prefab未設定
- PlayerControllerのBlockPlacementComponent未設定
- Player PrefabにBlockPlacementComponentがない
- Moving/Fallingなど配置不可の移動状態
- 対象セルがGrid範囲外
- 対象セルにBlockが存在
- 対象セルにBombが存在
- 対象セルにCharacterが存在
- 対象セルが予約済み
- Block生成後のGrid登録失敗

ログには対象座標、現在座標、向き、移動状態も含める。

## 8. 動作確認済み

ユーザーが確認済み:

- ステージBlock生成
- Camera基準のPlayer水平移動
- Blockによる移動阻止
- Player自動生成
- 移動補間
- その場ジャンプ・段差ジャンプの基礎
- 地上正面／ジャンプ中直下へのBlock配置
- Characterの自動落下
- 配置したBlockの落下
- BombのGrid登録、重力、Fuse、消滅、同時設置数管理（コード実装。Editor設定と動作確認待ち）
- ExplosionSystemによる±X・±Y・±Zの爆風セル計算とConsole出力
- 爆風範囲内にあるBreakable Blockの破壊
- Block破壊後、同じ列の上側Blockを下から順番に重力再判定
- 爆風セル内のBombを収集し、Fuseを待たずに連鎖爆発
- ExplosionViewが計算済み爆風セルへ見た目専用Effectを生成
- ExplosionEffectが設定時間後に自身を破棄
- 爆風セルを`Center`、`Middle`、`End`、`BlockedEnd`に分類し、6方向へ回転表示
- Editorメニューから爆風Materialと4種類のParticle Prefabを自動生成可能
- 落下Blockが通過する各セルをGrid基準で判定し、Characterを押し潰して死亡させる処理
- Player 1体と棒立ちTest Enemy 1体の生成・Grid登録
- Waiting / Playing / Finishedの試合状態と死亡時の勝者判定
- GameHudによる生存人数、YOU WIN / YOU LOSE / DRAWの表示
- Restart Buttonによる現在Sceneの再読み込み
- 死亡後にDelayを置き、Result PanelをCanvasGroupでフェードイン表示
- Easy / Normal / Hardを選択可能な簡易Enemy AI

## 9. 既知の課題・注意点

1. `Block`は生成時と自身の落下終了時に重力を再判定するが、静止後に下のBlockだけが破壊された場合を常時監視していない。Block破壊実装時に、上に積まれたBlockへ落下再評価を通知すること。
2. `GridGravitySystem.TryGetFallDestination()`は現在`GridManager.CanEnter()`を共用している。Bomb/Item実装時は、Character・Block・Bomb・Itemごとに通過/着地条件を分けること。
3. その場ジャンプ中、元セルを別Characterが確保する競合への完全な予約処理は未完成。将来は「ジャンプ元予約」と「本人によるBlock配置許可」を両立する予約情報を設計する。
4. `GridCell.IsReserved`は存在するが、予約の設定・解除APIはまだ未実装。
5. `OnDrawGizmos()`は空。必要ならGrid可視化を追加する。
6. EditModeテストはPlaceholderのみ。GridUtility、占有移動、重力着地探索のテストを追加する。
7. Unity Editor上のコンパイル・Inspector設定は変更後に必ず確認する。
8. `CharacterBase`には`LifeComponent`が必要。既存Player Prefabへ自動追加されない場合はInspectorから追加する。

## 10. 次に実装する項目

次はBombの6方向爆発セル計算を実装する。

推奨順:

1. Bomb Prefab、BombComponent、PlaceBomb InputをEditorで接続して動作確認

   - Scene上の`CharacterSpawner`へBomb Prefabを設定する
   - Player Prefabへ`BombComponent`を追加する
   - Player Prefabの`PlayerController`へ同じ`BombComponent`を設定する
   - Input Actionsへ`PlaceBomb` Actionを追加し、`PlayerInput`から`OnPlaceBomb`へ通知する
2. `ExplosionSystem`で±X/±Y/±Zの影響セルを純粋計算（実装済み）
3. Fuse終了後に`Bomb`から`ExplosionSystem`へ爆発要求（実装済み）
4. 破壊不能Blockで停止する（実装済み）
5. 破壊可能Blockを影響対象に含め、そのセルで停止する（実装済み）
6. 爆風セル計算をEditModeテストまたはScene上のConsoleで検証する
7. 爆風対象の破壊可能Blockを破壊する（実装済み）
8. 破壊したBlockより上のBlockを重力再判定する（実装済み）
9. Characterを検出して`LifeComponent`へ死亡要求を送る（実装済み）
10. Bombを検出して連鎖爆発キューへ追加する（実装済み）
11. 計算結果を使って爆風Effectを表示する（コード実装済み・Prefab設定待ち）

Explosion EffectのEditor設定:

自動生成する場合:

1. ProjectウィンドウでBomb Prefabを選択する
2. `Tools > 3D Grid Bomber > Create Explosion Effect Prefabs`を実行する
3. `Assets/Prefabs/Effects/Explosion`と`Assets/Materials/Effects/Explosion`を確認する
4. `ExplosionVisualSettings.asset`へ4種類のPrefabが登録される
5. Bomb Prefabを選択して実行した場合、ExplosionViewへSettings Assetも自動設定される

1. Center、Middle、End、必要ならBlockedEndの見た目用Prefabを作る
2. 各Prefabへ`ExplosionEffect`を追加する
3. Middle、End、BlockedEndはローカルZ+方向へ伸びる向きで作る
4. 判定はGrid側で完了しているため、死亡判定用Colliderは追加しない
5. Bomb Prefabへ`ExplosionView`を追加する
6. `ExplosionVisualSettings`へCenter、Middle、End、BlockedEnd Prefabを設定する
7. BlockedEnd Prefabは任意。未設定の場合はEnd Prefabを使用する
8. `ExplosionVisualSettings.Effect Duration`で表示秒数を調整する

最初のBomb完成条件:

```text
入力でBombを配置（実装済み）
→ GridCellへ登録（実装済み）
→ 足場がなければ落下（実装済み）
→ Fuse終了（実装済み）
→ 6方向の影響セルをConsoleで確認（コード実装済み・動作確認待ち）
→ BombをGridCellから解除（実装済み）
```

演出、死亡、Block破壊、Item出現は影響セル計算が安定してから追加する。

## 11. 今後の大まかなロードマップ

```text
Grid/占有管理                    完了
Player移動                       完了
ジャンプ                         基礎完了
Character重力                    完了
Block配置・重力                  基礎完了
Bomb配置・重力・Fuse             コード実装済み／Editor確認待ち
6方向Explosion                   完了
Block破壊・Character死亡         完了
Item落下・取得                   未実装
Enemy AI                         基礎完了
Danger Map・経路探索             未実装
終盤Block落下                    未実装
勝敗・UI・爆風演出               基礎完了
Sound                            未実装
```

## 12. ScriptableObject設定

ゲーム調整値とPrefab参照はジャンル別Settings Assetへ移行済み。
Scene Object参照（GridManager、Camera、GameState、UI Object等）はScene側で設定する。

```text
Assets/Settings
├── Grid/GridSettings
├── Character/CharacterMovementSettings
├── Character/CharacterPrefabSettings
├── Bomb/BombSettings
├── Block/BreakableBlockSettings
├── Block/UnbreakableBlockSettings
├── Stage/StageSettings
├── Enemy/EnemyAISettings
├── Effects/ExplosionVisualSettings
├── UI/GameHudSettings
└── GameConfig
```

生成手順:

1. Unityの`Tools > 3D Grid Bomber > Create Default Settings Assets`を実行
2. 各Assetの値とPrefab参照を設定
3. SceneとPrefabの各Componentへ対応Assetを割り当てる

割り当て先:

- GridManager → GridSettings
- StageGenerator → StageSettings
- CharacterSpawner → CharacterPrefabSettings
- Player/EnemyのMovementComponent → CharacterMovementSettings
- Player/EnemyのBombComponent → BombSettings

## Enemy AI危険回避（2026-09-04）

- `GridDangerMap`を実装し、盤面上のBombを走査して爆発予定セルと残り時間を記録するようにした。
- 爆風範囲は`ExplosionSystem.CalculateAffectedCells()`を再利用し、実際の爆発判定との不一致を防いでいる。
- `GridPathfindingSystem`へ水平4方向の幅優先探索を実装した。
- Enemyは現在地が危険な場合、追跡・Bomb設置より先に最寄りの安全セルへ移動する。
- 経路探索は同じ高さの水平移動に加え、隣接する1段高いBlock上へのジャンプに対応。
- DangerMapはBomb同士の連鎖関係を反復計算し、誘爆によって前倒しされる爆発時刻を反映する。
- 経路探索は移動・ジャンプ時間とAIの行動間隔を考慮し、爆発前に通過できないセルを除外する。
- Bomb設置前に仮想BombをDangerMapへ追加し、安全セルへの経路がある場合だけ設置する。
- EnemyはPlayerへ爆風が届く移動候補を優先し、攻撃可能な位置からBomb設置を試みる。
- Enemyは直前にいたセルを記録し、ほかに候補がある場合は直前セルへの後退を最後に評価する。
- 危険回避中は決定した逃走経路を保持し、安全になるか経路が塞がれるまで同じ方針で進む。
- 安全セルへ脱出した後も、Bombが残っている間は通常追跡で爆風予定セルへ再進入しない。
- 通常行動で`A→B→A→B`となる2セル往復を検出し、往復先を数回分の思考時間だけ候補から外す。
- 往復抑制は通常追跡・徘徊だけに適用し、Escape中に必要な後退は妨げない。
- `EnemyAIState`（Idle / Chase / MoveToAttackPosition / PlaceBomb / Escape）を導入した。
- 生存判断を最優先し、Bomb設置成功後は即座にEscapeへ遷移する。
- 現在状態はEnemyBrainのInspectorで確認でき、状態変化はConsoleへ出力される。
- Chaseでは安全な通常移動に加え、既存Block上への1段ジャンプを選択できる。
- 通常移動と既存Blockでは近づけない場合、安定した足場上へBlockを設置し、次の行動でその上へジャンプする。
- Escapeでも通常経路がない場合、設置とジャンプが爆発時刻に間に合う場合だけBlock足場を作る。
- EnemyのBlockPlacementComponentはCharacterSpawnerから初期化され、古いPrefabで不足している場合は実行時に補完される。
- 同じセルでBomb設置失敗など同一判断を繰り返した回数を検出し、別方向への移動または短時間の再考待機を行う。
- 反復検出回数と待機時間はEnemyAISettingsの`Max Same Cell Decisions`、`Reconsider Pause`で難易度別に調整できる。
- Bomb設置後は、自分のBombが盤面から消えるまでEscape状態を保持する。安全セル到着直後に攻撃へ戻る状態振動を防止する。
- Escape解除には、現在地・水平隣接セル・ジャンプ着地候補が一定時間連続して安全であることを要求する。
- 安全確認時間はEnemyAISettingsの`Escape Safe Confirmation Time`で難易度別に調整できる。
- Chase／MoveToAttackPositionは局所マンハッタン評価から行動付きA*へ変更した。
- A*のゴールはPlayerセルではなく、Bombの爆風がPlayerへ届き、AIのBombDistance内にある攻撃可能セルとする。
- A*は通常移動、既存BlockへのJumpUp、経路中1回までのPlaceBlockAndJumpを比較し、行動列をEnemyBrainへ返す。
- EnemyBrainは攻撃経路を保持して1手ずつ実行し、Player移動・危険化・実行失敗時だけ再探索する。
- Chase用A*へ`MoveAndFall`を追加し、水平に踏み出した先に足場がなければ下の着地セルまでを1行動として探索する。
- MovementComponentの`TryMoveAndFall()`が論理上の着地セルを先に確保し、水平移動後に落下表示を連続実行する。
- 降下経路では落下列にある各セルの危険時刻も確認する。
- Breakable/Unbreakable/配置用Block → 対応するBlockSettings
- GridBomberGameMode → EnemyAISettings
- BombのExplosionView → ExplosionVisualSettings
- GameHud → GameHudSettings

既存Componentの数値・Prefab欄はSO参照へ置き換わるため、移行直後は必ず上記を再設定する。

検証:

- `Assembly-CSharp.csproj`: build成功、警告0、エラー0
- `Assembly-CSharp-Editor.csproj`: build成功、警告0、エラー0
