# 電子音を合成して16bit・44.1kHz・モノラルのWAVとして保存します。
# 外部の録音素材は使用しません。既存ファイルは上書きしません。
$neonOutput = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Audio/Clips/Neon'))
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Text;
public static class NeonSfxSynthesis
{
    const int Rate = 44100;
    static double Sin(double phase) { return Math.Sin(2 * Math.PI * phase); }
    // 周波数を直線的に変える電子音。位相を積分して不連続なノイズを避けます。
    static double Chirp(double t, double start, double end, double length)
    { return Sin(start*t + (end-start)*t*t/(2*length)); }
    static double Note(double t, double frequency, double length)
    {
        if (t < 0 || t >= length) return 0;
        return (Sin(t*frequency) + 0.18*Sin(t*frequency*2)) * Math.Min(1,t/0.006) * Math.Pow(1-t/length,1.4);
    }
    public static void Generate(string folder)
    {
        Directory.CreateDirectory(folder);
        string[] names = { "BombPlace", "Explosion", "BlockPlace", "ItemCollect", "CharacterDeath", "UiConfirm", "Win", "Lose", "UiSelect", "CameraRotate" };
        double[] lengths = { .16, .48, .14, .38, .55, .19, .85, .75, .075, .24 };
        for (int kind=0; kind<names.Length; kind++)
        {
            string path=Path.Combine(folder,names[kind]+".wav");
            if(File.Exists(path)) { Console.WriteLine("既存ファイルを維持: "+path); continue; }
            double duration=lengths[kind];
            double[] samples=new double[(int)(Rate*duration)];
            Random random=new Random(9041+kind);
            double filteredNoise=0, peak=0;
            for(int i=0;i<samples.Length;i++)
            {
                double t=(double)i/Rate;
                double noise=random.NextDouble()*2-1;
                filteredNoise += .12*(noise-filteredNoise);
                double value=0;
                switch(kind)
                {
                    case 0: value=.65*Chirp(t,650,200,duration)*Math.Exp(-24*t)+noise*.25*Math.Exp(-130*t); break;
                    case 1: value=1.5*filteredNoise*Math.Exp(-9*t)+.7*Chirp(t,115,32,duration)*Math.Exp(-12*t)+noise*.15*Math.Exp(-65*t); break;
                    case 2: value=(Sin(t*240)+.4*Sin(t*670))*Math.Exp(-40*t)+noise*.3*Math.Exp(-95*t); break;
                    case 3: value=Note(t,660,.15)+Note(t-.09,880,.16)+Note(t-.18,1320,.2); break;
                    case 4: value=.65*Chirp(t,620,65,duration)*Math.Exp(-5*t)*(0.65+.35*Sin(t*24))+filteredNoise*.8*Math.Exp(-9*t); break;
                    case 5: value=Note(t,740,.12)+Note(t-.065,1110,.125); break;
                    case 6: value=Note(t,523,.24)+Note(t-.16,659,.25)+Note(t-.32,784,.28)+Note(t-.48,1047,.37); break;
                    case 7: value=Note(t,440,.27)+Note(t-.19,349,.28)+Note(t-.39,220,.36); break;
                    case 8: value=Chirp(t,1100,850,duration)*Math.Exp(-65*t); break;
                    // 回転に合わせた短い電子的な風切り音。末尾に小さな確定音を重ねます。
                    case 9: value=(.45*filteredNoise+.22*Chirp(t,420,1200,duration))*Math.Sin(Math.PI*t/duration)*Math.Exp(-4*t)+.18*Note(t-.17,950,.07); break;
                }
                // 両端を短くフェードし、再生開始・終了のクリックを防ぎます。
                value *= Math.Min(1,t/.003)*Math.Min(1,(duration-t)/.012);
                samples[i]=value;
            }
            // 直流成分を除き、ピークに余裕を持たせて正規化します。
            double mean=0;
            foreach(double value in samples) mean+=value/samples.Length;
            for(int i=0;i<samples.Length;i++) { samples[i]-=mean; peak=Math.Max(peak,Math.Abs(samples[i])); }
            using(BinaryWriter writer=new BinaryWriter(new FileStream(path,FileMode.CreateNew)))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36+samples.Length*2);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(Rate); writer.Write(Rate*2);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length*2);
                foreach(double value in samples) writer.Write((short)Math.Round(value/Math.Max(peak,.001)*.72*32767));
            }
            Console.WriteLine(names[kind]+": "+duration+"秒 / "+samples.Length+"サンプル");
        }
    }
}
'@
[NeonSfxSynthesis]::Generate($neonOutput)
