using System;
using ImageMagick;

namespace trimtest
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var img = new MagickImage(args[0]))
            {
                // 左上原点の色で透過
                var pixels = img.GetPixels();
                var basePixel = pixels.GetPixel(0, 0);
                img.Transparent(basePixel.ToColor());

                img.Alpha(AlphaOption.Set);

                img.Write(args[1]);
            }

            using (var img = new MagickImage(args[1]))
            {
                img.Trim();
                img.RePage();

                img.Write(args[1]);
            }
        }
    }
}
