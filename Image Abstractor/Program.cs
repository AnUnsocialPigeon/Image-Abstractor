using ImageMagick;
using System.Diagnostics;
using System.Drawing;

int ObjectCount = 500;
int GenCount = 50;
int KillCount = 40;
int RefiningRounds = 10;
string dir_Origional = "origional.png";
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

Image Origional = Image.FromFile(dir_Origional);
Bitmap B_Origional = new(Origional);
Image i2 = Image.FromHbitmap(new Bitmap(Origional.Width, Origional.Height).GetHbitmap());

Graphics i2Graphics = Graphics.FromImage(i2);
i2Graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 15, 15, 15)), new Rectangle(0, 0, B_Origional.Width, B_Origional.Height));

// AI Stuff
ImgObj[] Objs = new ImgObj[GenCount];
int MaxWidth = Origional.Width;
int MaxHeight = Origional.Height;
int MaxRadius = 400;
int MinRadius = 3;
int ComparisonSamples = 200;
int ColourSamples = 20;
int MinAlpha = 190;
int RadiusRange = 80;
int PointRange = 50;
int AlphaRange = 20;

MagickImageCollection gif = new MagickImageCollection();


for (int DrawnObjects = 0; DrawnObjects < ObjectCount; DrawnObjects++) {
    //Parallel.For(0, ObjectCount, (DrawnObjects) => {
    Console.WriteLine($"Drawing {DrawnObjects + 1}");
    // Init. Gen.
    int r;
    Point p;
    Color c;
    for (int i = 0; i < GenCount; i++) {
        r = rnd.Next(MinRadius, MaxRadius);
        p = new(rnd.Next(MaxWidth), rnd.Next(MaxHeight));
        c = AverageColour(p, r);
        c = Color.FromArgb(rnd.Next(MinAlpha, 255), c.R, c.G, c.B);

        Objs[i] = new ImgObj(p, r, c, rnd.NextDouble() > 0.5);
    }

    for (int ObjRound = 0; ObjRound < RefiningRounds; ObjRound++) {
        //_ = Parallel.For(0, RefiningRounds, (ObjRound) => {
        // Score Calc for each 
        Parallel.ForEach(Objs.Where(x => x.Score == -1), (obj) => {
            Image i3;
            lock (i2) i3 = (Image)i2.Clone();
            Graphics g = Graphics.FromImage(i3);
            if (obj.Circle) DrawCircle(ref g, obj.Center, obj.Radius, obj.Colour);
            else DrawSquare(ref g, obj.Center, obj.Radius, obj.Colour);
            obj.Score = ScoreImage(i3, Origional, ComparisonSamples);
        });


        // Order
        Objs = Objs.OrderBy(x => x.Score).Reverse().ToArray();

        // Update
        int remainder = GenCount - KillCount;

        Parallel.For(0, remainder, (offset) => {
            //for (int offset = 0; offset < remainder; offset++) {
            ImgObj o = Objs[offset];
            for (int child = 0; child < KillCount / remainder; child++) {
                lock (rnd) {
                    r = rnd.Next(
                    o.Radius - RadiusRange < MinRadius ? MinRadius : o.Radius - RadiusRange,
                    o.Radius + RadiusRange > MaxRadius ? MaxRadius : o.Radius + RadiusRange);

                    p = new(
                        rnd.Next(o.Center.X - PointRange < 0 ? 0 : o.Center.X - PointRange,
                            o.Center.X + PointRange > MaxWidth ? MaxWidth : o.Center.X + PointRange),
                        rnd.Next(o.Center.Y - PointRange < 0 ? 0 : o.Center.Y - PointRange,
                            o.Center.Y + PointRange > MaxHeight ? MaxHeight : o.Center.Y + PointRange));

                    c = Color.FromArgb(rnd.Next(o.Colour.A - AlphaRange < MinAlpha ? MinAlpha : o.Colour.A - AlphaRange,
                            o.Colour.A + AlphaRange > 255 ? 255 : o.Colour.A + AlphaRange), o.Colour.R, o.Colour.G, o.Colour.B);
                }
                Objs[remainder + offset + child] = new ImgObj(p, r, c, o.Circle);
            }
        });
    }

    if (Objs[0].Circle) DrawCircle(ref i2Graphics, Objs[0].Center, Objs[0].Radius, Objs[0].Colour);
    else DrawSquare(ref i2Graphics, Objs[0].Center, Objs[0].Radius, Objs[0].Colour);
    i2Graphics.Save();

    gif.Add(new MagickImage(ImageToByte(i2)));
    gif[gif.Count - 1].AnimationDelay = 1;
    Console.WriteLine($"Drawn: {DrawnObjects + 1} / {ObjectCount}");

}

SaveImage(i2, "Changed.png");
gif.Optimize();
gif.Write("ChangedGif.gif");

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

int ScoreImage(Image i1, Image i2, int samples) {
    Bitmap b1 = new(i1);
    Bitmap b2;
    lock (i2) b2 = new(i2);

    int px, py;
    int score = 0;
    Color p1, p2;

    for (int i = 0; i < samples; i++) {
        px = rnd.Next(i1.Width);
        py = rnd.Next(i1.Height);
        p1 = b1.GetPixel(px, py);
        p2 = b2.GetPixel(px, py);

        score += Math.Abs((p1.R - p2.R) + (p1.G - p2.G) + (p1.B - p2.B));
    }
    return score;
}

void SaveImage(Image image, string FilePath) => image.Save(FilePath);

Color AverageColour(Point p, int r) {
    int A = 0, R = 0, G = 0, B = 0, x, y, sub = 0;
    for (int i = 0; i < ColourSamples; i++) {
        (x, y) = GetRandomPointInCircle(r);
        x += p.X;
        y += p.Y;

        x = Math.Abs(x);
        y = Math.Abs(y);

        if (x < MaxWidth && y < MaxHeight) {
            Color c;
            lock (B_Origional) c = B_Origional.GetPixel(x, y);
            A += c.A;
            R += c.R;
            G += c.G;
            B += c.B;
            continue;
        }
        sub++;
    }

    if (sub == ColourSamples) {
        Color c;
        lock (B_Origional) c = B_Origional.GetPixel(p.X, p.Y);
        return c;
    }
    return Color.FromArgb(A / (ColourSamples - sub), R / (ColourSamples - sub), G / (ColourSamples - sub), B / (ColourSamples - sub));
}

(int, int) GetRandomPointInCircle(int r) {
    double theta = rnd.NextDouble() * 2 * Math.PI;
    double rad = rnd.NextDouble() + rnd.NextDouble();
    if (rad >= 1) rad = 2 - rad;
    return ((int)(r * rad * Math.Cos(theta)), (int)(r * rad * Math.Sin(theta)));
}

void DrawCircle(ref Graphics GraphicsObj, Point Center, int Radius, Color Colour) {
    Brush b = new SolidBrush(Colour);
    Rectangle r = new Rectangle(Center.X - (Radius / 2), Center.Y - (Radius / 2), Radius, Radius);

    lock (GraphicsObj) GraphicsObj.FillEllipse(b, r);
}

void DrawSquare(ref Graphics GraphicsObj, Point Center, int Height, Color Colour) {
    Brush b = new SolidBrush(Colour);
    Rectangle r = new(Center.X - (Height / 2), Center.Y - (Height / 2), Height, Height);
    lock (GraphicsObj) GraphicsObj.FillRectangle(b, r);
}

public class ImgObj {
    public Point Center { get; set; }
    public int Radius { get; set; }
    public Color Colour { get; set; }
    public int Score { get; set; } = -1;
    public bool Circle { get; set; } = true;
    public ImgObj(Point c, int r, Color col, bool cir) {
        Center = c;
        Radius = r;
        Colour = col;
        Circle = cir;
    }
}