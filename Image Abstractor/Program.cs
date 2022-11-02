using Image_Abstractor;
//using ImageMagick;
using System.Diagnostics;
using System.Drawing;

int ObjectCount = 5000;
int GenCount = 100;
int KillCount = 97;
int RefiningRounds = 3;
string dir_Origional = args.Length == 0 ? "origional.png" : args[0];
string ans;

do {
    Console.WriteLine("(D)efault, or (C)ustom settings?");
    Console.Write("D/C: ");
    ans = Console.ReadLine().ToUpper();
    Console.Clear();
} while (ans != "D" && ans != "C");
if (ans == "C") {
    // Custom settings
    Console.Write("File Location: ");
    dir_Origional = Console.ReadLine();

    Console.Write("Population per Generation: ");
    string t = Console.ReadLine();
    if (!int.TryParse(t, out GenCount)) {
        GenCount = 150;
    }

    Console.Write("Amount killed per Generation: ");
    t = Console.ReadLine();
    if (!int.TryParse(t, out KillCount) || KillCount <= GenCount) {
        GenCount = 150;
        KillCount = 100;
    }
}


if (!OperatingSystem.IsWindows()) return;
Random rnd = new();

Image Original = Image.FromFile(dir_Origional);

Console.WriteLine("Copying image to buffer");
DirectBitmap B_Origional = new(Image.FromFile(dir_Origional));
Image i2 = Image.FromHbitmap(new Bitmap(Original.Width, Original.Height).GetHbitmap());
Bitmap bi2 = new(i2);
Console.WriteLine("Done");

Graphics i2Graphics = Graphics.FromImage(i2);
i2Graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 15, 15, 15)), new Rectangle(0, 0, B_Origional.Width, B_Origional.Height));

// AI Stuff
ImgObj[] Objs = new ImgObj[GenCount];
for (int x = 0; x < Objs.Length; x++) Objs[x] = new();
int MaxWidth = Original.Width;
int MaxHeight = Original.Height;
int StartMaxRadius = 85;    
int MaxRadius = StartMaxRadius;
int MinRadius = 2;
int RadiusDecay = 70;
int RadiusDecayPower = 4;
int ComparisonSamples = 300;
int ObjSamples = 10;
int ColourSamples = 20;
int MaxAlpha = 255;
int MinAlpha = 180;
int RadiusRange = 15;
int PointRange = 15;
int AlphaRange = 10;

DateTime start = DateTime.Now;

Rectangle _rect = new();

int lastScore = int.MaxValue;

//MagickImageCollection gif = new MagickImageCollection();

int minr, maxr, minp_X, maxp_X, minp_Y, maxp_Y, r;
Point p = new();
Color c;
for (int DrawnObjects = 0; DrawnObjects < ObjectCount; DrawnObjects++) {
    //Parallel.For(0, ObjectCount, (DrawnObjects) => {
    //Console.WriteLine($"Drawing {DrawnObjects + 1}");
    // Init. Gen.
    for (int i = 0; i < GenCount; i++) {
        r = rnd.Next(MinRadius, MaxRadius);
        p = new(rnd.Next(MaxWidth), rnd.Next(MaxHeight));
        c = AverageColour(p, r, true);
        //c = Color.FromArgb(255, c.R, c.G, c.B);
        Objs[i].Center = p;
        Objs[i].Radius = r;
        Objs[i].Colour = c;
        //rnd.NextDouble() > 0.5
    }

    for (int ObjRound = 0; ObjRound < RefiningRounds; ObjRound++) {
        // Score Calc for each 
        foreach (var obj in Objs.Where(x => x.Score == -1))
            obj.Score = ScoreImage(obj, ComparisonSamples);

        // Order
        Objs = Objs.OrderBy(x => x.Score).ToArray();

        // Update
        int remainder = GenCount - KillCount;

        for (int offset = 0; offset < remainder; offset++) {

            ImgObj o = Objs[offset];
            for (int child = 0; child < KillCount / remainder; child++) {
                minr = o.Radius - RadiusRange;
                maxr = o.Radius + RadiusRange;

                minp_X = o.Center.X - PointRange;
                maxp_X = o.Center.X + PointRange;
                minp_Y = o.Center.Y - PointRange;
                maxp_Y = o.Center.Y + PointRange;

                r = rnd.Next(minr < MinRadius ? MinRadius : minr, maxr > MaxRadius ? MaxRadius : maxr);

                p.X = rnd.Next(minp_X < 0 ? 0 : minp_X, maxp_X > MaxWidth ? MaxWidth : maxp_X);
                p.Y = rnd.Next(minp_Y < 0 ? 0 : minp_Y, maxp_Y > MaxHeight ? MaxHeight : maxp_Y);


                Objs[remainder + offset + child] = new ImgObj(p, r, AverageColour(p, r, false, o.Alpha));
            }
        }
    }

    if (Objs[0].Score <= lastScore) {
        DrawCircle(ref i2Graphics, Objs[0].Center, Objs[0].Radius, Objs[0].Colour);
        lastScore = Objs[0].Score;

        bi2 = new(i2);
        _ = UpdateDisplay(DrawnObjects, true);
        continue;
    }
    _ = UpdateDisplay(DrawnObjects, false);
}

DateTime end = DateTime.Now;

Console.WriteLine($"Time Elapsed: {(end - start).TotalSeconds}s");

SaveImage(i2, "Changed.png");
//SaveImage(B_Origional.Bitmap, "Changed2.png");
//gif.Optimize();
//gif.Write("ChangedGif.gif");

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Done!");

var processss = new Process();
processss.StartInfo = new ProcessStartInfo("Changed.png") {
    UseShellExecute = true
};
processss.Start();

byte[] ImageToByte(Image img) {
    ImageConverter converter = new ImageConverter();
    return (byte[])converter.ConvertTo(img, typeof(byte[]));
}

async Task UpdateDisplay(int DrawnObjects, bool success) {
    Console.Title = (DrawnObjects + 1) + "/" + ObjectCount;
    MaxRadius = StartMaxRadius - (int)(RadiusDecay * (Math.Pow(DrawnObjects, RadiusDecayPower) / Math.Pow(ObjectCount, RadiusDecayPower)));
    if (!success) Console.WriteLine($"Failure at {DrawnObjects + 1}");
}

int ScoreImage(ImgObj i1, int samples) {
    int px, py;
    int score = 0;
    Color p1 = new(), p2 = new();

    for (int i = 0; i < samples; i++) {
        px = rnd.Next(B_Origional.Width);
        py = rnd.Next(B_Origional.Height);
        score += GetPixelScore(i1, bi2, px, py, p1, p2);
    }
    return (128 * samples) - score;
}

int GetPixelScore(ImgObj i1, Bitmap bi2, int px, int py, Color p1, Color p2) {
    p1 = B_Origional.GetPixel(px, py);
    if (Math.Pow(px - i1.Center.X, 2) + Math.Pow(py - i1.Center.Y, 2) <= Math.Pow(i1.Radius, 2)) p2 = i1.Colour;
    else p2 = bi2.GetPixel(px, py);
    return Math.Abs(p1.R - p2.R) + Math.Abs(p1.G - p2.G) + Math.Abs(p1.B - p2.B);
}

void SaveImage(Image image, string FilePath) => image.Save(FilePath);

Color AverageColour(Point p, int r, bool randomAlpha, int alphaFromPoint = -1) {
    int R = 0, G = 0, B = 0, x, y, sub = 0;
    for (int i = 0; i < ColourSamples; i++) {
        (x, y) = GetRandomPointInCircle(r);
        x += p.X;
        y += p.Y;

        x = Math.Abs(x);
        y = Math.Abs(y);

        if (x < MaxWidth && y < MaxHeight) {
            Color c;
            c = B_Origional.GetPixel(x, y);
            R += c.R;
            G += c.G;
            B += c.B;
            continue;
        }
        sub++;
    }

    if (sub == ColourSamples) {
        c = B_Origional.GetPixel(p.X, p.Y);
        return c;
    }
    return Color.FromArgb(
        randomAlpha ? rnd.Next(MinAlpha, MaxAlpha) : alphaFromPoint == -1 ? MaxAlpha :
            (int)Math.Clamp(alphaFromPoint + (rnd.NextDouble() - 0.5f) * 2 * AlphaRange, MinAlpha, MaxAlpha),
        R / (ColourSamples - sub),
        G / (ColourSamples - sub),
        B / (ColourSamples - sub));
}

(int, int) GetRandomPointInCircle(int r) {
    var angle = rnd.NextDouble() * Math.PI * 2;
    var radius = rnd.NextDouble() * r;
    var x = radius * Math.Cos(angle);
    var y = radius * Math.Sin(angle);
    return ((int)x, (int)y);
}
void DrawCircle(ref Graphics GraphicsObj, Point Center, int Radius, Color Colour) {
    Brush b = new SolidBrush(Colour);
    _rect.X = Center.X - (Radius / 2);
    _rect.Y = Center.Y - (Radius / 2);
    _rect.Width = Radius;
    _rect.Height = Radius;
    GraphicsObj.FillEllipse(b, _rect);
}

public class ImgObj {
    public Point Center { get; set; } = new Point(0, 0);
    public int Radius { get; set; } = 5;
    public Color Colour { get; set; } = Color.White;
    public int Alpha { get; set; } = 255;
    public int Score { get; set; } = -1;
    //public bool Circle { get; set; } = true;
    public ImgObj() { }
    public ImgObj(Point c, int r, Color col) {
        Center = c;
        Radius = r;
        Colour = col;
        Alpha = Colour.A;
    }
}

public class Point {
    public int X { get; set; }
    public int Y { get; set; }
    public Point(int x, int y) {
        X = x;
        Y = y;
    }
    public Point() { }
}