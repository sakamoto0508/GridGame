# Item・Blockのオブジェクトプール

## 使い方

追加のInspector設定は不要です。既存のPrefabとSettingsをそのまま使用します。
StageGenerator、BlockPlacementComponent、ItemManagerが、GridManager上に自動追加される
GridObjectPoolから個体を借ります。同じPrefabは生成元が違っても同じプールを使います。
Bombや爆発エフェクトは今回の対象外です。

## ライフサイクル

- Rent: 待機個体があれば再利用、なければInstantiate。状態・位置・回転・スケールをリセットして有効化。
- 生成元: BlockはTryRegisterBlock → Initialize、ItemはInitで登録する。
- 取得・爆風・配置失敗: グリッド登録を解除し、非アクティブにして返却する。
- Blockの落下Awaitableは世代番号を照合する。返却して即座に借り直しても古い落下処理は終了する。
- 待機個体はGridManager配下の`Pooled Objects (Inactive)`に格納。通常の生成・返却ではDestroyしない。
- プールはScene内だけで保持し、Scene終了で一括破棄。事前生成や固定容量は設けず、Prefabごとのピーク需要まで増える方式。
- プール外で手動生成した個体は返却先がないためDestroyする。新しい生成処理でもプールを利用すること。

明示的に消すときは`Item.Despawn()` / `Block.Despawn()`を使用してください。
`SetActive(false)`だけでは登録解除はされますが、プールの待機リストには戻りません。
破壊不能Blockの爆風耐性を守る通常の破壊判定には、引き続き`BlockBreak()`を使います。

## Play Mode確認

1. Itemを取得・爆風で除去し、次の同種出現時に待機個体が再利用されることをHierarchyで確認。
2. 同じBlock Prefabを設置 → 爆破 → 再設置し、再設置後も破壊・落下できることを確認。
3. Blockを落下中に爆破し、すぐ別セルへ同じPrefabを設置する。旧落下先へ瞬間移動しないことを確認。
4. Itemの取得効果が1回だけ適用され、返却後にCountItemsが減ることを確認。
5. 上下に積んだBlockの下段を爆破し、上段の落下・押し潰しが維持されることを確認。
6. Sceneを再ロードし、前の試合の個体やグリッド占有が残らないことを確認。

コンパイル検証とPlay Mode検証は別です。上記の視覚・挙動確認はUnity上で行ってください。
