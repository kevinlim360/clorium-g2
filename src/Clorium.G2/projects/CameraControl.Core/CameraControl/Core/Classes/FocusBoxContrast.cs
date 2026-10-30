using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using CameraControl.Devices.Classes;

namespace CameraControl.Core.Classes;

public sealed class FocusBoxContrast
{
    public double X { get; private set; }
    public double Y { get; private set; }
    public double Width { get; private set; }
    public double Height { get; private set; }
    public static FocusBoxContrast From(LiveViewData frame)
    {
        if (frame == null || frame.ImageWidth <= 0 || frame.ImageHeight <= 0 || frame.FocusFrameXSize <= 0 || frame.FocusFrameYSize <= 0)
            throw new InvalidOperationException("라이브뷰에서 포커스 박스를 선택해 주세요.");
        return new FocusBoxContrast { X = (double)frame.FocusX / frame.ImageWidth, Y = (double)frame.FocusY / frame.ImageHeight,
            Width = (double)frame.FocusFrameXSize / frame.ImageWidth, Height = (double)frame.FocusFrameYSize / frame.ImageHeight };
    }
    public static FocusBoxContrast At(LiveViewData frame,int x,int y)
    {
        var box=From(frame);
        box.X=Math.Max(0,Math.Min(1,(double)x/frame.ImageWidth));
        box.Y=Math.Max(0,Math.Min(1,(double)y/frame.ImageHeight));
        return box;
    }
    public double Measure(LiveViewData frame)
    {
        if (frame?.ImageData == null || frame.ImageData.Length == 0) throw new InvalidOperationException("라이브뷰 이미지가 없습니다.");
        using (var stream = new MemoryStream(frame.ImageData, frame.ImageDataPosition, frame.ImageData.Length - frame.ImageDataPosition))
        using (var source = new Bitmap(stream))
        using (var crop = new Bitmap(128,128,PixelFormat.Format24bppRgb))
        {
            int left = Math.Max(0, (int)((X - Width / 2) * source.Width)), top = Math.Max(0, (int)((Y - Height / 2) * source.Height));
            int width = Math.Min(source.Width - left, Math.Max(3,(int)(Width * source.Width)));
            int height = Math.Min(source.Height - top, Math.Max(3,(int)(Height * source.Height)));
            if (width < 3 || height < 3) throw new InvalidOperationException("포커스 박스가 이미지 밖에 있습니다.");
            using (var g = Graphics.FromImage(crop)) g.DrawImage(source,new Rectangle(0,0,128,128),new Rectangle(left,top,width,height),GraphicsUnit.Pixel);
            var bits = crop.LockBits(new Rectangle(0,0,128,128),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
            var bytes = new byte[bits.Stride * 128];
            try { Marshal.Copy(bits.Scan0,bytes,0,bytes.Length); } finally { crop.UnlockBits(bits); }
            var gray = new double[128*128];
            for (int y=0;y<128;y++) for (int x=0;x<128;x++) { int p=y*bits.Stride+x*3; gray[y*128+x]=(bytes[p]+2.0*bytes[p+1]+bytes[p+2])/4; }
            double sum=0,squared=0; int count=0;
            for(int y=1;y<127;y++) for(int x=1;x<127;x++) { int p=y*128+x; double v=4*gray[p]-gray[p-1]-gray[p+1]-gray[p-128]-gray[p+128]; sum+=v; squared+=v*v; count++; }
            return squared/count - (sum/count)*(sum/count);
        }
    }
}
