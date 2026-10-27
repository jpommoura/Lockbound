using Godot;

public partial class Title : Control
{
    Label prompt;
    double t = 0;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // Fundo provisório (depois trocamos por uma imagem 2D / pixel art)
        var bg = new ColorRect { Color = new Color(0.45f, 0.70f, 0.90f) };
        AddChild(bg);
        bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var center = new CenterContainer();
        AddChild(center);
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 40);
        center.AddChild(box);

        var title = new Label
        {
            Text = "LOCKBOUND",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 72);
        box.AddChild(title);

        prompt = new Label
        {
            Text = "Pressione ENTER para começar",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        prompt.AddThemeFontSizeOverride("font_size", 28);
        box.AddChild(prompt);
    }

    public override void _Process(double delta)
    {
        // Faz o texto piscar suavemente
        t += delta;
        prompt.Modulate = new Color(1, 1, 1, 0.5f + 0.5f * Mathf.Abs(Mathf.Sin((float)t * 2f)));
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
