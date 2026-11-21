using System;
using System.Drawing;
using System.IO;

namespace CabinetBackglass
{
    sealed class AreaPortraits : IDisposable
    {
        // The game reports field-specific area IDs. A panorama may be shared by habitat.
        static readonly string[] Names = {
            "Ruby Forest", "Sapphire Forest", "Ruby Plains", "Sapphire Plains",
            "Ruby Ocean", "Sapphire Ocean", "Ruby Cave", "Sapphire Cave",
            "Ruby Safari Zone", "Ruby Volcano", "Sapphire Lake", "Sapphire Wilderness",
            "Ruby Ruins", "Sapphire Ruins"
        };
        readonly Bitmap[] panoramas = new Bitmap[Names.Length];

        internal AreaPortraits(string directory)
        {
            for (int i = 0; i < Names.Length; i++)
            {
                string file = Path.Combine(directory, "Pinball " + Names[i] + ".png");
                if (!File.Exists(file))
                {
                    string habitat = Names[i].Substring(Names[i].IndexOf(' ') + 1);
                    file = Path.Combine(directory, "Pinball " + habitat + ".png");
                }
                if (!File.Exists(file)) continue;
                try
                {
                    using (var stream = File.OpenRead(file))
                    using (var png = Image.FromStream(stream, false, true))
                    {
                        if (png.Width > 0 && png.Height > 0 && png.Width <= 1024 && png.Height <= 1024)
                            panoramas[i] = new Bitmap(png);
                    }
                }
                catch (ArgumentException) { }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        internal Bitmap PanoramaAt(int area)
        {
            return area >= 0 && area < panoramas.Length ? panoramas[area] : null;
        }

        public void Dispose()
        {
            foreach (Bitmap panorama in panoramas)
                if (panorama != null) panorama.Dispose();
        }
    }
}
