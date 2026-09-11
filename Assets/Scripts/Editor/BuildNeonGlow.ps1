# Run with Windows PowerShell 5.1: powershell -NoProfile -File Editor/BuildNeonGlow.ps1
# Creates a white alpha-only glow ring. Tint is applied by Unity's UI Image.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
public static class NeonGlowTextureBuilder {
    public static void Save(string path) {
        // 16px padding outside the existing 256x128 panel. Border: 24+16=40px.
        var p = new PointF[] {new PointF(36,18),new PointF(252,18),new PointF(270,36),new PointF(270,124),
            new PointF(252,142),new PointF(36,142),new PointF(18,124),new PointF(18,36)};
        using (var image = new Bitmap(288,160)) {
            for(int y=0;y<160;y++) for(int x=0;x<288;x++) {
                double distance=double.MaxValue;
                for(int i=0;i<8;i++) {
                    PointF a=p[i], b=p[(i+1)%8];
                    double dx=b.X-a.X,dy=b.Y-a.Y;
                    double t=Math.Max(0,Math.Min(1,((x+.5-a.X)*dx+(y+.5-a.Y)*dy)/(dx*dx+dy*dy)));
                    double vx=x+.5-a.X-t*dx,vy=y+.5-a.Y-t*dy;
                    distance=Math.Min(distance,Math.Sqrt(vx*vx+vy*vy));
                }
                // A narrow luminous core plus a softer halo; center stays transparent.
                double alpha=.5*Math.Exp(-distance*distance/50)+.4*Math.Exp(-distance*distance/6.48);
                image.SetPixel(x,y,Color.FromArgb((int)Math.Round(255*alpha),255,255,255));
            }
            image.Save(path,ImageFormat.Png);
        }
    }
}
'@
$glowPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../UI/Art/Neon/GlowRing.png'))
[NeonGlowTextureBuilder]::Save($glowPath)
Write-Output $glowPath
