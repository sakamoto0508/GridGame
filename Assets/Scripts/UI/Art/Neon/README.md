# NEON DETONATOR UI Sprites

このプロジェクト用に図形から作成したオリジナルPNG。外部の画像/フォントは使用していません。
文字は画像へ焼き込まず、既存TMPで表示します。

| PNG | 用途 |
|---|---|
| PanelCyan | 基本パネル、HUD |
| PanelGreen | 結果パネル |
| ButtonNeutral | 未選択の難易度/補助ボタン |
| ButtonSelected | 選択中の難易度、RETRY |
| ButtonPrimary | START（緑の塗りつぶし） |
| FrameCyan | 外周枠（中央透明） |
| KeycapCyan | キー操作の枠 |
| PanelWarning | 警告パネル（予備、現状のエラーは文字のみ） |
| GlowRing | 発光用の白い枠。Themeで着色して通常Spriteの上へ重ねる |

- 256×128、RGBA、透明な角、9スライス境界は四辺24px。
- Unity ImageのSource Imageへ設定し、TypeをSlicedにする。Image Colorは白。
- Sprite ImportはSingle/Full Rect/PPU100/MipMapなし/Clamp。
- UI適用メニューがImport設定とSOへの割当を行う。SOに手動割当済みのSpriteは上書きしない。
- PNGに枠と背景色を含むため、配色変更はSprite差替えか再生成で行う。Themeの色は文字/選択時Tint/装飾線に引き続き適用。
- 再生成元はEditor/BuildNeonSprites.ps1。実行するとこのフォルダの8PNGを上書きする。
- 特殊なShaderや独自Graphicは不要。背景の盤面イラストは含まない。

GlowRingだけ288×160、境界40px（既存Panelの外側に16px余白を追加）。中央透明。
生成元はEditor/BuildNeonGlow.ps1（Windows PowerShell 5.1で実行）。通常の8PNGは再生成せず、GlowRingのみを出力する。
