# Cyberpunk Cube 1のFlowシェーダーをURPへ移植

## 適用手順

1. Unityに戻り、インポート・コンパイル完了を待つ。Playは停止する。
2. `Tools > NEON DETONATOR > Assets > Convert Flow Materials to URP` を実行する。
3. 対象数を確認して「変換」を押す。マテリアルが保存される。
4. Shader欄が `NEON DETONATOR/URP/Flow Only Emission Transparent` になり、Consoleに変換完了が出ることを確認する。
5. Scene/Gameで表示とアニメーションを確認する。直後ならUndoで差し替えを戻せる（戻した後は保存する）。

調査時の対象は `Assets/Import/COMICOMI/Cyberpunk Cube 1/Art/Materials/CT_  fukong01.mat` の1つ。
独立した.matのうち旧シェーダー名が一致するものだけを変更する。他のピンクのシェーダーまでは変換しない。

## 維持する機能

- 同じプロパティ名でテクスチャ・色・スクロール速度などを継承。
- メイン画像のUVスクロール、Ramp画像による色の往復、Sin点滅。
- 元のSimplexノイズと任意ノイズ画像による透過・発光変化。
- 両面表示、通常のアルファブレンド、深度書き込みOFF。

URPのCore.hlslを使う頂点/フラグメントシェーダーへ変更。ASE専用Inspectorへの依存を外した。
原版シェーダーとその.metaは変更していない。新版は手書きコードで、ASEグラフを編集するものではない。

## 差分・確認範囲

- 装飾エフェクトとしてShadowCasterは省略したため影は投影しない。
- 最終アルファは0～1へ制限する。HDRの発光強度は制限しない。
- 発光のにじみにはHDR/Bloomが別途必要。
- C#のビルド確認とShaderのプロパティ照合は実施。Unity上のShaderコンパイル・描画とマテリアル差し替えは未実行。上記メニューで適用する。
- Importフォルダはgitignore対象なので、変換後のマテリアルはGitへ自動的には保存されない。配布・別環境移行時はアセット本体とマテリアルの扱いに注意。
