using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace CabinetBackglass
{
    sealed class PartyIcons : IDisposable
    {
        sealed class Icon
        {
            internal Bitmap Image;
            internal Rectangle Bounds;
        }

        readonly string directory;
        readonly Dictionary<string,Icon> cache = new Dictionary<string,Icon>(StringComparer.Ordinal);

        internal PartyIcons(string directory) { this.directory = directory; }

        internal bool Draw(Graphics g, string species, Rectangle target, double time)
        {
            Icon icon;
            if (!cache.TryGetValue(species, out icon))
            {
                icon = Load(species);
                cache.Add(species, icon);
            }
            if (icon == null) return false;

            Rectangle frame = icon.Bounds;
            frame.Y += ((int)(time * 4) % 2) * 32;
            float scale = Math.Min((float)target.Width / frame.Width, (float)target.Height / frame.Height);
            int width = Math.Max(1, (int)(frame.Width*scale));
            int height = Math.Max(1, (int)(frame.Height*scale));
            var destination = new Rectangle(target.X+(target.Width-width)/2, target.Y+(target.Height-height)/2, width, height);
            var saved = g.Save();
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(icon.Image, destination, frame, GraphicsUnit.Pixel);
            g.Restore(saved);
            return true;
        }

        Icon Load(string species)
        {
            string file = Path.Combine(directory,species.ToLowerInvariant()+".png");
            if (!File.Exists(file)) return null;
            try
            {
                using (var stream = File.OpenRead(file))
                using (var png = Image.FromStream(stream, false, true))
                {
                    if (png.Width != 32 || png.Height != 64) return null;
                    var bitmap = new Bitmap(png);
                    int left=32, top=32, right=-1, bottom=-1;
                    for(int y=0; y<64; y++)
                        for(int x=0; x<32; x++)
                        {
                            if(bitmap.GetPixel(x,y).A==0) continue;
                            left=Math.Min(left,x);
                            top=Math.Min(top,y%32);
                            right=Math.Max(right,x);
                            bottom=Math.Max(bottom,y%32);
                        }
                    if (right < left)
                    {
                        bitmap.Dispose();
                        return null;
                    }
                    return new Icon {
                        Image = bitmap,
                        Bounds = Rectangle.FromLTRB(left,top,right+1,bottom+1)
                    };
                }
            }
            catch (IOException) { return null; }
            catch (ArgumentException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }

        public void Dispose()
        {
            foreach (Icon icon in cache.Values)
                if (icon != null) icon.Image.Dispose();
        }
    }
}
