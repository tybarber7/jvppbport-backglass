using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace CabinetBackglass
{
    static class BackglassArt
    {
        static readonly string[] Areas = { "Forest", "Forest", "Plains", "Plains", "Ocean", "Ocean",
            "Cave", "Cave", "Safari Zone", "Volcano", "Lake", "Wilderness", "Ruins", "Ruins" };
        static readonly int[] Biomes = { 0, 0, 1, 1, 2, 2, 3, 3, 7, 4, 5, 8, 6, 6 };
        static readonly string[] Balls = { "Poke Ball", "Great Ball", "Ultra Ball", "Master Ball" };
        static readonly int[,] Palettes = {
            {0x233f65,0x408c8a,0x83c896,0xf7de97,0x357863,0x245546,0x173b3d},
            {0x655674,0xc98276,0xf3b785,0xffe19b,0x8a7964,0x706b50,0x425946},
            {0x243d79,0x3d83aa,0x79c3c5,0xffe7ab,0x3c90ac,0x305e91,0x273c6b},
            {0x29304f,0x534674,0x8d638e,0xe8b5bf,0x6f5b89,0x4c4369,0x302e53},
            {0x633659,0xa65263,0xf18e67,0xffd589,0x95515c,0x603c57,0x453047},
            {0x385c80,0x61a3a4,0xb8ce9c,0xffe1a2,0x649d92,0x427e89,0x34536f},
            {0x595273,0x9b7487,0xd6a395,0xffdb9c,0x987c7a,0x796175,0x51465f},
            {0x52426d,0xac7358,0xe8bd65,0xffe694,0x9a794c,0x787d47,0x495238},
            {0x292c5c,0x556c8b,0x9897be,0xe4dbfb,0x625c89,0x505878,0x343d5c}
        };
        const int Ink = 0x262425, Cream = 0xe7e8eb, Gold = 0xefcb3f, Ruby = 0xea492c,
            Sapphire = 0x4475ae, Cyan = 0x65cce7, Deep = 0x17223c, White = 0xffffff;
        const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789&.,:/-'";
        static readonly string[] Glyphs = {
            "0E111F11111111","1E111E1111111E","0F10101010100F","1E11111111111E","1F101E1010101F","1F101E10101010",
            "0F10101711110F","1111111F111111","0E04040404040E","0702020202120C","11121418141211","1010101010101F",
            "111B1515111111","11191513111111","0E11111111110E","1E11111E101010","0E11111115120D","1E11111E141211",
            "0F10100E01011E","1F040404040404","1111111111110E","11111111110A04","11111115151B11","11110A040A1111",
            "11110A04040404","1F01020408101F","0E11111111110E","040C040404040E","0E11010204081F","1E01010601011E",
            "02060A121F0202","1F101E0101011E","0E10101E11110E","1F010204080808","0E11110E11110E","0E11110F01010E",
            "0C12140815120D","00000000000C0C","00000000000604","000C0C000C0C00","01010204081010","0000001F000000","0C0C0800000000"
        };

        internal static void Render(Graphics target, Size size, DisplayState view, BackglassMotion motion = null, AreaPortraits portraits = null, PartyIcons partyIcons = null)
        {
            target.Clear(ColorOf(Ink));
            if (size.Width < 1 || size.Height < 1) return;
            using (var pixels = new Bitmap(400, 225))
            {
                using (var g = Graphics.FromImage(pixels))
                {
                    g.Clear(ColorOf(Deep));
                    for (int y = 1; y < 225; y += 4) Rect(g, 0, y, 400, 1, 0x203258);
                    Draw(g, view, motion, portraits, partyIcons);
                }
                float scale = Math.Min(size.Width / 400f, size.Height / 225f);
                int w = (int)(400 * scale), h = (int)(225 * scale);
                target.InterpolationMode = InterpolationMode.NearestNeighbor;
                target.PixelOffsetMode = PixelOffsetMode.Half;
                target.DrawImage(pixels, new Rectangle((size.Width-w)/2,(size.Height-h)/2,w,h));
            }
        }

        static void Draw(Graphics g, DisplayState view, BackglassMotion motion, AreaPortraits portraits, PartyIcons partyIcons)
        {
            var frame = view.Frame;
            Bitmap portrait = frame == null || portraits == null ? null : portraits.PanoramaAt(frame.Area);
            int biome = frame == null || frame.Area >= Areas.Length ? 0 : Biomes[frame.Area];
            Rect(g, 3, 3, 394, 218, 0x14151d);
            Frame(g, 5, 5, 390, 142, Sapphire, Ink);
            Rect(g, 9, 9, 382, 2, Gold);
            Rect(g, 10, 12, 380, 108, 0x16294f);

            if (portrait == null)
            {
                var artwork = g.Save();
                g.TranslateTransform(10, 13);
                g.ScaleTransform(380f / 212, 66f / 74);
                Landscape(g, 0, 0, 212, 76, biome, motion == null ? 0 : motion.Time);
                g.Restore(artwork);
            }
            else DrawPanorama(g, portrait, motion, biome);
            if (motion != null && motion.Travel < 1)
            {
                int sweep = (int)(motion.Travel * 400);
                using (var wash = new SolidBrush(Color.FromArgb((int)(140 * (1-motion.Travel)), ColorOf(Sapphire))))
                    g.FillRectangle(wash, 10+sweep, 16, Math.Max(0, 380-sweep), 63);
                if (sweep < 379) Rect(g, 10+sweep, 16, 1, 63, Cyan);
            }

            Split(g, 10, 12, 380, 3, Ruby, Sapphire);
            Rect(g, 10, 15, 380, 1, Cream);
            int titleWidth = portrait == null ? 238 : 127;
            int titleHeight = portrait == null ? 29 : 16;
            Rect(g, 19, 22, titleWidth, titleHeight, 0x192d53);
            Rect(g, 17, 20, titleWidth, titleHeight, 0x142950);
            Split(g, 17, 20, titleWidth, 1, Ruby, Cyan);
            Rect(g, 17, 19+titleHeight, titleWidth, 1, 0x142d63);
            DrawBallIcon(g, 21, 20 + (titleHeight-11)/2);
            Text(g, "PINBALL FIELD GUIDE", 34, portrait == null ? 29 : 25, portrait == null ? 2 : 1, White, titleWidth-18);
            if (frame != null)
            {
                Bitmap badge = TableBadge(frame.Area);
                if (badge != null) g.DrawImageUnscaled(badge, 383-badge.Width, 21);
            }
            else if (view.Status != "PREVIEW")
            {
                string status = view.Status == "WAITING FOR JVPPBPORT" ? "WAITING" : view.Status;
                Rect(g, 297, 21, 85, 15, 0x142950);
                Right(g, status, 377, 25, 1, White, 76);
            }
            Split(g, 10, 79, 380, 40, 0x62231f, 0x223d65);
            Split(g, 10, 79, 380, 2, Ruby, Cyan);
            Text(g, "TRAVEL AREA", 19, 84, 1, White, 185);
            Text(g, frame == null ? "READY TO PLAY" : Areas[frame.Area], 19, 95, frame == null ? 2 : 3, White, 206);
            if (frame != null) Right(g, frame.Caught + " CAUGHT / EVOLVED", 380, 104, 1, White, 161);
            Rect(g, 10, 119, 380, 23, White);
            Rect(g, 10, 119, 380, 1, Cream);
            if (view.Recent.Length == 0)
            {
                Text(g, view.HistoryIncomplete ? "NEW CATCHES WILL APPEAR HERE."
                    : frame == null ? "START A GAME TO LINK THE BACKGLASS." : "YOUR FIRST CATCH WILL APPEAR HERE.",
                    19, 127, 1, Ink, 358);
            }
            for (int i = 0; i < view.Recent.Length; i++)
            {
                int x = 16 + i * 74;
                double catchProgress = i == 0 && motion != null ? motion.Catch : 1;
                int card = catchProgress < 1 ? Mix(Gold, Cream, (int)(100*catchProgress), 100) : Cream;
                Rect(g, x, 123, 70, 15, card);
                if (catchProgress < 1)
                {
                    Rect(g, x, 122, Math.Max(1, 70-(int)(70*catchProgress)), 1, Ruby);
                    Diamond(g, x+65, 125, (int)(3*(1-catchProgress)), 0xffffff);
                }
                Rect(g, x, 138, 70, 1, Sapphire);
                bool hasIcon = partyIcons != null && partyIcons.Draw(g, view.Recent[i],
                    new Rectangle(x+2,122,15,16), motion == null ? 0 : motion.Time);
                if (!hasIcon) Diamond(g, x+5, 130, 2, Ruby);
                int textOffset = hasIcon ? 18 : 10;
                Text(g, view.Recent[i], x+textOffset, 127, 1, Ink, 69-textOffset);
            }

            Frame(g, 5, 150, 390, 67, Sapphire, 0x151721);
            string ballType = frame != null && frame.BallType < Balls.Length ? Balls[frame.BallType] : "";
            string timer = frame != null && frame.Seconds > 0
                ? string.Format("{0}:{1:00} LEFT", frame.Seconds/60, frame.Seconds%60) : "NO TIMER";
            StatPanel(g, 12, "BALLS LEFT", frame == null ? "-" : frame.Balls.ToString(), ballType, Ruby, 0x62231f);
            StatPanel(g, 303, "COINS", frame == null ? "-" : frame.Coins.ToString(), timer, Sapphire, 0x223d65);
            Frame(g, 102, 156, 196, 54, Cream, 0x180f10);
            Rect(g, 107, 161, 186, 44, 0x241715);
            using (var grid = new SolidBrush(ColorOf(0x38231e)))
                for (int y = 162; y < 205; y += 2)
                    for (int x = 108; x < 293; x += 2)
                        g.FillRectangle(grid, x, y, 1, 1);
            Center(g, "SCORE", 200, 165, 1, 0xffb45b, 100);
            Score(g, frame == null ? "-" : (motion == null ? frame.Score : motion.Score).ToString("N0", CultureInfo.InvariantCulture));
            Rect(g, 111, 198, 178, 1, 0x5c3729);
            if (view.Status == "PREVIEW")
                Text(g, "SAMPLE GAMEPLAY / ESC TO CLOSE", 10, 218, 1, Cream, 375);
            else if (view.HistoryIncomplete)
                Text(g, "CATCHES OBSERVED SINCE LAUNCH", 10, 218, 1, Cream, 375);
        }

        static readonly Bitmap RubyBadge = CreateTableBadge("RUBY", Ruby, 0x692219);
        static readonly Bitmap SapphireBadge = CreateTableBadge("SAPPHIRE", 0x579de8, 0x183879);

        internal static Bitmap TableBadge(int area)
        {
            switch (area)
            {
                case 0: case 2: case 4: case 6: case 8: case 9: case 12:
                    return RubyBadge;
                case 1: case 3: case 5: case 7: case 10: case 11: case 13:
                    return SapphireBadge;
                default:
                    return null;
            }
        }

        static Bitmap CreateTableBadge(string name, int rim, int shadow)
        {
            var face = new System.Collections.Generic.List<Rectangle>();
            for (int index = 0; index < name.Length; index++)
            {
                string rows = Glyphs[Alphabet.IndexOf(name[index])];
                for (int row = 0; row < 7; row++)
                {
                    int bits = int.Parse(rows.Substring(row*2,2), NumberStyles.HexNumber);
                    for (int col = 0; col < 5; col++)
                        if ((bits & (16>>col)) != 0)
                            face.Add(new Rectangle(3 + index*12 + col*2 + (6-row)/2, 3+row*2, 2, 2));
                }
            }

            var tile = new Bitmap(9 + name.Length*12, 22);
            using (var g = Graphics.FromImage(tile))
            using (var outer = new SolidBrush(ColorOf(0x13192d)))
            using (var lower = new SolidBrush(ColorOf(shadow)))
            using (var border = new SolidBrush(ColorOf(rim)))
            using (var white = new SolidBrush(ColorOf(White)))
            using (var cool = new SolidBrush(ColorOf(Cream)))
            {
                foreach (Rectangle pixel in face)
                    g.FillRectangle(outer, pixel.X-2,pixel.Y-2,6,8);
                foreach (Rectangle pixel in face)
                    g.FillRectangle(lower, pixel.X-1,pixel.Y+1,4,4);
                foreach (Rectangle pixel in face)
                    g.FillRectangle(border, pixel.X-1,pixel.Y-1,4,4);
                foreach (Rectangle pixel in face)
                    g.FillRectangle(pixel.Y < 12 ? white : cool, pixel);
            }
            return tile;
        }

        static void DrawBallIcon(Graphics g, int x, int y)
        {
            Rect(g, x+3, y, 5, 11, Ink);
            Rect(g, x+1, y+2, 9, 7, Ink);
            Rect(g, x, y+4, 11, 3, Ink);
            Rect(g, x+3, y+1, 5, 4, Ruby);
            Rect(g, x+2, y+3, 7, 2, Ruby);
            Rect(g, x+3, y+2, 2, 1, 0xff9078);
            Rect(g, x+2, y+6, 7, 2, White);
            Rect(g, x+3, y+8, 5, 2, Cream);
            Rect(g, x+4, y+4, 3, 3, Ink);
            Rect(g, x+5, y+5, 1, 1, White);
        }

        static void DrawPanorama(Graphics g, Bitmap panorama, BackglassMotion motion, int biome)
        {
            var state = g.Save();
            g.SetClip(new Rectangle(10,16,380,63));
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(panorama, new Rectangle(10,16,380,63));
            if (motion != null)
            {
                double time = motion.Time;
                int light = Palettes[biome,3];
                for (int i = 0; i < 16; i++)
                {
                    double pulse = Math.Sin(time * 2.2 + 2*i);
                    if (pulse < .38) continue;
                    int x = 15 + (int)((i*71 + time*(5+i%3)) % 370);
                    int y = 23 + (int)((i*19 + time*(biome == 4 ? 13 : 4)) % 51);
                    if (biome == 2 || biome == 5)
                    {
                        if (y > 45) Rect(g, x, y, pulse > .8 ? 6 : 3, 1, Cyan);
                    }
                    else Diamond(g, x, y, pulse > .85 ? 1 : 0, biome == 4 ? Gold : light);
                }
            }
            g.Restore(state);
        }
        static void Split(Graphics g, int x, int y, int w, int h, int left, int right)
        {
            for (int row = 0; row < h; row++)
            {
                int seam = w/2 + h/2 - row;
                Rect(g, x, y+row, seam, 1, left);
                Rect(g, x+seam, y+row, w-seam, 1, right);
            }
        }

        static void StatPanel(Graphics g, int x, string label, string value, string detail, int frame, int fill)
        {
            Frame(g, x, 156, 85, 54, frame, fill);
            Center(g, label, x+42, 163, 1, White, 76);
            Center(g, value, x+42, 172, 3, White, 76);
            Rect(g, x+7, 195, 71, 1, frame);
            Center(g, detail, x+42, 198, 1, White, 75);
        }

        static void Center(Graphics g, string value, int x, int y, int scale, int color, int max)
        {
            int width = Math.Min(max, value.Length * TextStride(value, scale, max, false) - scale);
            Text(g, value, x-width/2, y, scale, color, max);
        }
        static void Score(Graphics g, string value)
        {
            const int pitch = 3;
            int stride = Math.Min(18, 178 / Math.Max(1, value.Length));
            int dotWidth = stride >= 15 ? 2 : 1;
            int x = (400 - ((value.Length-1)*stride + 4*stride/6 + dotWidth))/2;
            const int y = 176;
            foreach (char ch in value)
            {
                int index = Alphabet.IndexOf(ch);
                if (index >= 0)
                    for (int row = 0; row < 7; row++)
                        for (int col = 0; col < 5; col++)
                        {
                            int bits = int.Parse(Glyphs[index].Substring(row*2,2), NumberStyles.HexNumber);
                            if ((bits & (16>>col)) == 0) continue;
                            Rect(g, x + col*stride/6, y + row*pitch, dotWidth, pitch-1, 0xffa339);
                        }
                x += stride;
            }
        }

        static void Landscape(Graphics g, int x, int y, int w, int h, int biome, double time)
        {
            var clip = g.Save();
            g.SetClip(new Rectangle(x,y,w,h));
            g.TranslateTransform(x,y);
            for (int row = 0; row < h; row += 4)
                Rect(g, 0, row, w, 4, Mix(Palettes[biome,0],Palettes[biome,2], Math.Min(row,68),68));
            int sun = Palettes[biome,3];
            for (int r=-15;r<=15;r++)
            {
                int radius = (int)Math.Sqrt(225-r*r);
                Rect(g, 164-radius, 30+r, radius*2+1, 1, r > 4 ? Mix(sun, Palettes[biome,2],1,4) : sun);
            }
            var rng = new Random(701 + biome);
            for (int i=0;i<19;i++)
            {
                int cx=rng.Next(4,w-13), cy=rng.Next(6,44);
                int span = rng.Next(2,11);
                int drift = ((int)(time * (biome == 3 ? 2 : 5)) + cx) % (w+span) - span;
                Rect(g,drift,cy,span,1,Mix(Palettes[biome,2],sun,1,3));
            }
            int far=Palettes[biome,4], middle=Palettes[biome,5], near=Palettes[biome,6];
            if (biome == 2 || biome == 5)
            {
                for (int layer=0;layer<5;layer++)
                {
                    int water=Mix(far,near,layer,5);
                    Rect(g,0,45+layer*7,w,8,water);
                    for(int i=0;i<16;i++)
                        Rect(g,(rng.Next(0,w)+(int)(time*(7+layer*3)))%w,46+layer*7+rng.Next(0,5),rng.Next(2,15),1,
                            Mix(water,Palettes[biome,2],1,3));
                }
                if (biome == 5)
                {
                    Peak(g, 28, 51, 44, 22, far, middle);
                    Peak(g, 64, 53, 53, 19, far, middle);
                }
            }
            else
            {
                if (biome == 4)
                {
                    Peak(g, 121, 71, 123, 49, far, near);
                    Rect(g, 116, 22, 11, 3, sun);
                    Rect(g, 121, 25, 4, 10, Ruby);
                    Rect(g, 123, 33, 5, 3, Gold);
                }
                for (int i=0;i<7;i++)
                    Peak(g, i*33+1, 70, rng.Next(40,77), rng.Next(14,36), far, middle);
                if (biome == 4)
                {
                    Rect(g, 120, 25, 4, 11, Ruby);
                    Rect(g, 122, 34, 4, 4, Gold);
                }
                if (biome == 0)
                    for (int i=0;i<28;i++)
                    {
                        int px=i*8, t=rng.Next(9,30);
                        Peak(g,px,72,10,t,near,middle);
                        Rect(g,px,70,2,5,near);
                    }
                if (biome == 3)
                    for (int i=0;i<25;i++)
                    {
                        int px=i*9;
                        Rect(g,px,0,8,5,near);
                        Rect(g,px+2,5,4,rng.Next(2,16),near);
                    }
                if (biome == 7)
                    for (int i=0;i<5;i++)
                    {
                        int px=22+i*46, top=53+rng.Next(0,8);
                        Rect(g,px,top,3,19,near);
                        Rect(g,px-10,top-3,24,3,near);
                        Rect(g,px-7,top-6,18,3,near);
                        Rect(g,px-3,top-8,10,2,middle);
                    }
                if (biome == 8)
                    for (int i=0;i<6;i++)
                    {
                        int px=9+i*39, tall=rng.Next(13,29);
                        Peak(g,px,72,rng.Next(15,34),tall,near,middle);
                        Rect(g,px-2,72-tall,3,4,Palettes[biome,2]);
                    }
                if (biome == 6)
                    for (int i=0;i<8;i++)
                    {
                        int px=13+i*27, top=rng.Next(42,63);
                        Rect(g,px,top,9,72-top,near);
                        Rect(g,px-2,top,13,3,middle);
                        Rect(g,px+3,top+5,1,9,far);
                    }
            }
            Rect(g,0,71,w,32,near);
            for (int i=0;i<26;i++)
                Rect(g,rng.Next(0,w),rng.Next(72,94),rng.Next(1,6),1,Mix(near,middle,1,3));
            Atmosphere(g, w, biome, time, sun);
            g.Restore(clip);
        }

        static void Atmosphere(Graphics g, int width, int biome, double time, int light)
        {
            for (int i = 0; i < 11; i++)
            {
                int x = (int)(i * 47 + time * (2 + i % 3)) % width;
                int y = 45 + (i * 11 + (int)(Math.Sin(time * 1.6 + i) * 4) + 66) % 23;
                double flicker = Math.Sin(time * 2.8 + i * 1.9);
                if (flicker <= .28) continue;
                int pigment = biome == 4 ? (i % 2 == 0 ? Ruby : Gold) : light;
                Rect(g, x, y, 1, 1, pigment);
                if (flicker > .9 && i % 3 == 0)
                {
                    Rect(g, x-1, y, 3, 1, pigment);
                    Rect(g, x, y-1, 1, 3, pigment);
                }
            }
        }

        static void Peak(Graphics g, int x, int baseline, int width, int height, int light, int dark)
        {
            for (int row=0;row<height;row++)
            {
                int half=width*(row+2)/(2*height);
                Rect(g,x-half,baseline-height+row,half*2,1,light);
                Rect(g,x,baseline-height+row,half,1,dark);
            }
        }

        static int Mix(int a, int b, int step, int count)
        {
            int result=0;
            for (int shift=0;shift<=16;shift+=8) result |=
                (((a>>shift&255)*(count-step)+(b>>shift&255)*step)/count)<<shift;
            return result;
        }

        static void Frame(Graphics g, int x, int y, int w, int h, int border, int inner)
        {
            Rect(g,x+2,y+3,w,h,Ink);
            Rect(g,x,y+2,w,h-4,Ink);
            Rect(g,x+2,y,w-4,h,Ink);
            Rect(g,x+2,y+2,w-4,h-4,border);
            Rect(g,x+4,y+4,w-8,h-8,inner);
            Rect(g,x+5,y+2,w-10,1,Mix(border,0xffffff,1,3));
        }

        static void Diamond(Graphics g, int x, int y, int radius, int c)
        {
            for(int r=-radius;r<=radius;r++)
            {
                int span=radius-Math.Abs(r);
                Rect(g,x-span,y+r,span*2+1,1,c);
            }
        }

        static void Text(Graphics g, string text, int x, int y, int scale, int color, int max, bool compact = false)
        {
            int stride=TextStride(text, scale, max, compact);
            if (scale==2 && text.Length*stride-scale>max) { scale=1; stride=6; }
            while (text.Length*stride-scale>max) text=text.Substring(0,text.Length-1);
            using (var brush=new SolidBrush(ColorOf(color)))
            foreach(var letter in text.ToUpperInvariant())
            {
                int index=Alphabet.IndexOf(letter);
                if (index>=0)
                    for(int row=0;row<7;row++)
                    {
                        int bits=int.Parse(Glyphs[index].Substring(row*2,2), NumberStyles.HexNumber);
                        for (int col=0;col<5;col++)
                            if ((bits & (16>>col))!=0)
                            {
                                int left=compact ? (col*(stride-1))/5 : col*scale;
                                int right=compact ? ((col+1)*(stride-1))/5 : (col+1)*scale;
                                g.FillRectangle(brush,x+left,y+row*scale,right-left,scale);
                            }
                    }
                x+=stride;
            }
        }

        static int TextStride(string value, int scale, int width, bool compact)
        {
            if (compact) return Math.Max(6, Math.Min(9, (width + scale) / Math.Max(1, value.Length)));
            return scale == 1 && value.Length * 6 - 1 > width ? 5 : 6 * scale;
        }

        static void Right(Graphics g, string value, int right, int y, int scale, int color, int max, bool compact = false)
        {
            int width=value.Length*TextStride(value, scale, max, compact)-scale;
            if (width>max && scale==2) { scale=1; compact=false; width=value.Length*6-1; }
            Text(g,value,right-Math.Min(width,max),y,scale,color,max,compact);
        }

        static Color ColorOf(int rgb) { return Color.FromArgb(rgb >> 16 & 255,rgb >> 8 & 255,rgb & 255); }

        static void Rect(Graphics g, int x, int y, int w, int h, int c)
        {
            using(var brush=new SolidBrush(ColorOf(c))) g.FillRectangle(brush,x,y,w,h);
        }
    }
}
