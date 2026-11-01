using System.Collections.Generic;
using Godot;

public partial class Title : Control
{
    // A arte é desenhada em 384x216 "pixels" e ampliada para a janela
    const float ArtW = 384f, ArtH = 216f;

    // Quanto tempo cada período do dia dura (segundos). Menor = dia passa mais rápido.
    const double PhaseSeconds = 12.0;

    // Posição do arco do sol/lua (em pixels da arte)
    const float CenterX = 192f, HorizonY = 140f, RadiusX = 170f, RadiusY = 120f;

    // Períodos: 0 = dia, 1 = entardecer, 2 = noite, 3 = amanhecer
    static readonly Color[] SkyTop = { C(60, 140, 225), C(60, 55, 120), C(6, 8, 26), C(85, 100, 185) };
    static readonly Color[] SkyMid = { C(120, 190, 245), C(190, 100, 120), C(16, 22, 58), C(225, 150, 170) };
    static readonly Color[] SkyBot = { C(190, 225, 250), C(255, 170, 90), C(34, 44, 92), C(255, 205, 150) };
    static readonly Color[] CastleTint = { new(1f, 1f, 1f), new(1f, 0.72f, 0.62f), new(0.28f, 0.32f, 0.55f), new(1f, 0.82f, 0.78f) };
    static readonly Color[] FarTint = { new(1f, 1f, 1f), new(0.9f, 0.6f, 0.7f), new(0.22f, 0.26f, 0.45f), new(0.85f, 0.72f, 0.85f) };
    static readonly Color[] CloudTint = { new(1f, 1f, 1f), new(1f, 0.7f, 0.55f), new(0.30f, 0.34f, 0.55f), new(1f, 0.78f, 0.72f) };
    static readonly float[] StarsAmount = { 0f, 0.25f, 1f, 0.2f };
    static readonly float[] LightsAmount = { 0f, 0.8f, 1f, 0.5f };

    static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

    // Cenário
    Gradient skyGrad;
    Sprite2D sun, moon, far, lava, mid, castle, lights, ship;
    float shipX = 60f;
    readonly List<Sprite2D> clouds = new();
    readonly List<float> cloudX = new(), cloudY = new(), cloudSpeed = new();
    readonly List<ColorRect> stars = new();
    readonly List<Vector2> starPos = new();
    readonly List<float> starPhase = new();
    readonly List<ColorRect> flies = new();
    readonly List<Vector2> flyBase = new();
    readonly List<float> flyPhase = new();

    float s = 3f;                 // ampliação da arte
    Vector2 origin = Vector2.Zero; // canto da arte na tela
    Vector2 lastSize = Vector2.Zero;

    double clock = 0.1;           // hora do dia (0..4, e vai dando voltas)
    double jumpTarget = -1;       // quando clica no sol/lua: para onde o relógio corre
    double blink = 0;
    Label prompt;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // Céu: degradê que muda de cor
        skyGrad = new Gradient();
        skyGrad.Offsets = new float[] { 0f, 0.55f, 1f };
        skyGrad.Colors = new Color[] { SkyTop[0], SkyMid[0], SkyBot[0] };

        var sky = new TextureRect
        {
            Texture = new GradientTexture2D
            {
                Gradient = skyGrad,
                Fill = GradientTexture2D.FillEnum.Linear,
                FillFrom = new Vector2(0, 0),
                FillTo = new Vector2(0, 1),
                Width = 4,
                Height = 128
            },
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(sky);
        sky.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var rng = new System.Random(7);

        // Estrelas
        for (int i = 0; i < 70; i++)
        {
            var r = new ColorRect { Color = new Color(1f, 1f, 0.95f), MouseFilter = Control.MouseFilterEnum.Ignore };
            AddChild(r);
            stars.Add(r);
            starPos.Add(new Vector2(rng.Next(0, 384), rng.Next(0, 115)));
            starPhase.Add((float)(rng.NextDouble() * 6.28));
        }

        // Sol e lua
        sun = MakeSprite("sun.png", true);
        moon = MakeSprite("moon.png", true);

        // Nuvens (cada uma com altura e velocidade diferentes)
        string[] cloudFiles = { "cloud_2.png", "cloud_1.png", "cloud_3.png", "cloud_1.png", "cloud_2.png" };
        float[] ys = { 22, 48, 70, 34, 86 };
        float[] speeds = { 5, 8, 3.5f, 6, 4 };
        for (int i = 0; i < cloudFiles.Length; i++)
        {
            clouds.Add(MakeSprite(cloudFiles[i], true));
            cloudX.Add(i * 80f);
            cloudY.Add(ys[i]);
            cloudSpeed.Add(speeds[i]);
        }

        // Montanhas, castelo na colina e janelas acesas
        ship = MakeSprite("ship.png", true);          // nave cruzando o céu
        far = MakeSprite("far.png", false);           // montanhas, deserto, templo, vulcão
        lava = MakeSprite("lava.png", false);         // lava brilhante do vulcão
        mid = MakeSprite("mid.png", false);           // cidade, catedral, floresta, lago
        castle = MakeSprite("front.png", false);      // castelo, colina, caminho, cerca
        lights = MakeSprite("lights.png", false);     // janelas acesas

        // Vaga-lumes
        for (int i = 0; i < 16; i++)
        {
            var r = new ColorRect { Color = new Color(1f, 0.95f, 0.5f), MouseFilter = Control.MouseFilterEnum.Ignore };
            AddChild(r);
            flies.Add(r);
            flyBase.Add(new Vector2(rng.Next(30, 360), rng.Next(150, 205)));
            flyPhase.Add((float)(rng.NextDouble() * 6.28));
        }

        // Textos por cima de tudo
        var title = new Label { Text = "LOCKBOUND", HorizontalAlignment = HorizontalAlignment.Center };
        StyleText(title, 84, new Color(1f, 0.93f, 0.7f), 14);
        AddChild(title);
        title.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        title.OffsetTop = 30;
        title.OffsetBottom = 140;

        prompt = new Label { Text = "Pressione ENTER para começar", HorizontalAlignment = HorizontalAlignment.Center };
        StyleText(prompt, 28, Colors.White, 10);
        AddChild(prompt);
        prompt.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        prompt.OffsetTop = -110;
        prompt.OffsetBottom = -60;

        var hint = new Label { Text = "Clique no sol ou na lua para mudar a hora", HorizontalAlignment = HorizontalAlignment.Center };
        StyleText(hint, 16, new Color(1f, 1f, 1f, 0.75f), 8);
        AddChild(hint);
        hint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        hint.OffsetTop = -44;
        hint.OffsetBottom = -14;

        Layout();
        Resized += Layout;
    }

    Sprite2D MakeSprite(string file, bool centered)
    {
        var sp = new Sprite2D
        {
            Texture = GD.Load<Texture2D>($"res://art/title/{file}"),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Centered = centered
        };
        AddChild(sp);
        return sp;
    }

    void StyleText(Label l, int size, Color color, int outline)
    {
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", color);
        l.AddThemeColorOverride("font_outline_color", new Color(0.04f, 0.05f, 0.12f));
        l.AddThemeConstantOverride("outline_size", outline);
        l.MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    // Calcula a ampliação da arte para cobrir a janela, colada embaixo e centralizada
    void Layout()
    {
        s = Mathf.Max(Size.X / ArtW, Size.Y / ArtH);
        origin = new Vector2((Size.X - ArtW * s) / 2f, Size.Y - ArtH * s);

        foreach (var sp in new[] { far, lava, mid, castle, lights })
        {
            sp.Scale = new Vector2(s, s);
            sp.Position = origin;
        }

        foreach (var c in clouds) c.Scale = new Vector2(s, s);
        sun.Scale = moon.Scale = ship.Scale = new Vector2(s, s);

        foreach (var r in stars) r.Size = new Vector2(s, s);
        foreach (var r in flies) r.Size = new Vector2(s, s);
    }

    Vector2 ToScreen(Vector2 art) => origin + art * s;

    // Posição do sol (offset 0) ou da lua (offset 2, do lado oposto) no arco
    Vector2 BodyArt(double offset)
    {
        double ang = Mathf.Pi / 2.0 - (clock - 0.25 + offset) / 4.0 * Mathf.Tau;
        return new Vector2(CenterX + RadiusX * (float)Mathf.Cos(ang), HorizonY - RadiusY * (float)Mathf.Sin(ang));
    }

    static float Smooth(double a, double b, double x)
    {
        double t = Mathf.Clamp((x - a) / (b - a), 0.0, 1.0);
        return (float)(t * t * (3.0 - 2.0 * t));
    }

    // O sol/lua está acima da colina (dá para clicar)?
    bool OverMouse(Sprite2D body, Vector2 mouse)
    {
        float bodyArtY = (body.Position.Y - origin.Y) / s;
        return bodyArtY < 134f && body.Position.DistanceTo(mouse) <= 14f * s;
    }

    public override void _Process(double delta)
    {
        // Se a janela mudou de tamanho, reajusta a arte
        if (Size != lastSize)
        {
            lastSize = Size;
            Layout();
        }

        // ---- relógio ----
        if (jumpTarget >= 0)
        {
            clock = Mathf.Min(clock + 1.4 * delta, jumpTarget);
            if (clock >= jumpTarget) jumpTarget = -1;
        }
        else
        {
            clock += delta / PhaseSeconds;
        }

        double p = Mathf.PosMod(clock, 4.0);
        int i = (int)p;
        int j = (i + 1) % 4;
        float w = Smooth(0.55, 1.0, p - i);   // fica parado no período e depois muda

        // ---- céu e cores ----
        skyGrad.SetColor(0, SkyTop[i].Lerp(SkyTop[j], w));
        skyGrad.SetColor(1, SkyMid[i].Lerp(SkyMid[j], w));
        skyGrad.SetColor(2, SkyBot[i].Lerp(SkyBot[j], w));

        castle.Modulate = CastleTint[i].Lerp(CastleTint[j], w);
        Color farColor = FarTint[i].Lerp(FarTint[j], w);
        far.Modulate = farColor;
        mid.Modulate = farColor.Lerp(castle.Modulate, 0.55f);
        ship.Modulate = farColor.Lerp(Colors.White, 0.4f);
        // lava sempre brilha, mais forte à noite
        float glowAmt = Mathf.Lerp(StarsAmount[i], StarsAmount[j], w);
        lava.Modulate = new Color(1f, 1f, 1f, 0.8f + 0.2f * glowAmt) * (1f + 0.5f * glowAmt);
        Color cloudColor = CloudTint[i].Lerp(CloudTint[j], w);

        float starsAmt = Mathf.Lerp(StarsAmount[i], StarsAmount[j], w);
        float lightsAmt = Mathf.Lerp(LightsAmount[i], LightsAmount[j], w);
        lights.Modulate = new Color(1, 1, 1, lightsAmt);

        // ---- sol e lua ----
        Vector2 mouse = GetGlobalMousePosition();
        sun.Position = ToScreen(BodyArt(0));
        moon.Position = ToScreen(BodyArt(2));

        bool hover = OverMouse(sun, mouse) || OverMouse(moon, mouse);
        MouseDefaultCursorShape = hover ? Control.CursorShape.PointingHand : Control.CursorShape.Arrow;
        float bodyScale = hover ? s * 1.15f : s;
        sun.Scale = moon.Scale = new Vector2(bodyScale, bodyScale);

        // ---- nuvens ----
        for (int k = 0; k < clouds.Count; k++)
        {
            cloudX[k] -= cloudSpeed[k] * (float)delta;
            float width = clouds[k].Texture.GetWidth();
            if (cloudX[k] < -width) cloudX[k] = ArtW + 10f;

            clouds[k].Position = ToScreen(new Vector2(cloudX[k] + width / 2f, cloudY[k] + clouds[k].Texture.GetHeight() / 2f));
            clouds[k].Modulate = cloudColor;
        }

        // ---- nave ----
        shipX += 4f * (float)delta;
        if (shipX > ArtW + 20f) shipX = -20f;
        ship.Position = ToScreen(new Vector2(shipX, 40f + 3f * Mathf.Sin((float)clock * 2f)));

        // ---- estrelas piscando ----
        for (int k = 0; k < stars.Count; k++)
        {
            float twinkle = 0.55f + 0.45f * Mathf.Sin((float)clock * 9f + starPhase[k]);
            stars[k].Color = new Color(1f, 1f, 0.95f, starsAmt * twinkle);
            stars[k].Position = ToScreen(starPos[k]);
        }

        // ---- vaga-lumes ----
        float fliesAmt = lightsAmt;
        float t = (float)clock * 3f;
        for (int k = 0; k < flies.Count; k++)
        {
            Vector2 off = new Vector2(7f * Mathf.Sin(t * 0.5f + flyPhase[k]), 4f * Mathf.Sin(t * 0.7f + flyPhase[k] * 1.7f));
            float glow = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(t * 1.3f + flyPhase[k]));
            flies[k].Color = new Color(1f, 0.95f, 0.5f, fliesAmt * glow);
            flies[k].Position = ToScreen(flyBase[k] + off);
        }

        // ---- texto piscando ----
        blink += delta;
        prompt.Modulate = new Color(1, 1, 1, 0.5f + 0.5f * Mathf.Abs(Mathf.Sin((float)blink * 2f)));
    }

    public override void _Input(InputEvent @event)
    {
        // Clique no sol ou na lua: o dia avança para o próximo período
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            Vector2 mouse = GetGlobalMousePosition();
            if (OverMouse(sun, mouse) || OverMouse(moon, mouse))
            {
                jumpTarget = jumpTarget < 0 ? Mathf.Floor(clock) + 1 : jumpTarget + 1;
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo &&
            (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter))
        {
            // Começa sempre da fase 1, com o tempo zerado
            Maze.level = 1;
            Maze.totalTime = 0;
            GetTree().ChangeSceneToFile("res://LevelSelect.tscn");
        }
    }
}
