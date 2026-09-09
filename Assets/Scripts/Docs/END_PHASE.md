# 終盤落下イベント

## Unity設定

追加設定なしでもGameModeがGridManagerにEndPhaseManagerを追加します。
既定は60秒後に開始、2秒予告、着地後3秒で次の予告です。
生成物はStageのUnbreakable Block Prefab。天井の内側（Y=Size.y-1）から落とします。

調整するにはCreate > 3D Grid Bomber > Settings > End PhaseからSOを作成し、
GridBomberGameModeのEnd Phase Settingsへ指定してください。
Tools > 3D Grid Bomber > Create Default Settings AssetsでもSOを作成できます。

- Enabled: 終盤イベントの有効/無効。
- Start After Seconds: Playingになってから終盤開始までのゲーム時間。
- Warning Seconds: 予告時間。
- Drop Interval: 着地後から次の予告まで。予告・落下時間とは別。
- Block Prefab: 任意の上書き。BlockSettingsがUnbreakableである必要があります。
- Warning Color / Line Width In Cells: 赤い列マーカーの表示設定。
- 落下時間は使用PrefabのBlockSettings.FallDurationで調整します。

## 仕様

- 空いている最上段の列を均等抽選。予告中は列全体を枠線で表示します。
- 1回につき1個だけ落下。予告/落下が終わるまで次は始めません。
- 生成位置がBlock/Bomb/予約で塞がれた場合はキャンセルし、別列へ無予告で生成しません。
- 最上段のCharacterは生成時、それより下のCharacterは既存の落下通過判定で押し潰します。
- Blockはプールから取得し、着地後は破壊不可Blockとして残ります。盤面の使用可能空間が減ります。
- 全列が最上段まで埋まった場合は、間隔を空けて再確認します。
- Waiting/Finishedでは時計・予告・生成を進めません。Finishedになったらマーカーを消し、
  生成処理中/落下中のイベントBlockを返却して追加の押し潰しを防ぎます。着地済みは残します。
- 無効化時も進行中の予告・落下を解除します。試合の再開は従来どおりScene再ロードです。
- 予告Shaderがない場合は、見えない予告で落下させずエラーログで停止します。

## AI

GridDangerMapはBomb危険情報にEndPhaseManagerの予告を合流します。
予告列は全高を危険として扱い、危険時刻は生成時刻（落下中は0秒）にします。
実際の各高さへの到達より早く見積もる保守的な予測です。
途中のBlockが破壊された場合にも逃げ遅れにくい代わりに、障害物の下も避けます。
仮想Bombを使う設置前の逃走可否判定にも含まれます。

## Play Modeテスト（未実施）

1. Start After Secondsを5にして試合開始。開始前には落下せず、5秒後に赤い列予告が出る。
2. Warning Seconds後に同じ列へ落ちる。天井自体は移動しない。
3. 予告列から出れば生存し、列内の通過地点に残れば死亡する。
4. 最上段にCharacterがいる場合も予告後に押し潰される。
5. Enemyが予告列から逃走し、その列のItem取得を中断する。
6. 予告中に別のBombで試合終了。マーカーが消え、その後生成しない。
7. 落下中に試合終了。イベントBlockが返却され、勝者が後から押し潰されない。
8. 着地済みのイベントBlockは爆風で壊れない。着地後に次の予告へ進む。
9. Time.timeScale=0では予告・落下・時計が停止し、再開後に続行する。

C#コンパイルは確認済みです。予告の視認性・AIの回避・試合終了のタイミングはUnity上で確認してください。
