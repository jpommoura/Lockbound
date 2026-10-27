using Godot;

public partial class PauseMenu : CanvasLayer
{
    Control root;
    Button firstButton;

    public override void _Ready()
    {
        // Always = continua funcionando mesmo com o jogo pausado
        ProcessMode = ProcessModeEnum.Always;
        Layer = 10;

        root = new Control { Visible = false };
        AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var dim = new ColorRect { Color = new Color(0, 0, 0, 0.6f) };
        root.AddChild(dim);
        dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var center = new CenterContainer();
        root.AddChild(center);
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 12);
        center.AddChild(box);

        var title = new Label
        {
            Text = "PAUSADO",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 36);
        box.AddChild(title);

        firstButton = AddButton(box, "Continuar", CloseMenu);

        AddButton(box, "Reiniciar fase", () =>
        {
            CloseMenu();
            GetTree().ReloadCurrentScene();
        });

        AddButton(box, "Mapa de fases", () =>
        {
            CloseMenu();
            GetTree().ChangeSceneToFile("res://LevelSelect.tscn");
        });

        AddButton(box, "Menu principal", () =>
        {
            CloseMenu();
            GetTree().ChangeSceneToFile("res://Title.tscn");
        });

        AddButton(box, "Sair", () => GetTree().Quit());
    }

    Button AddButton(Container parent, string text, System.Action action)
    {
        var b = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(280, 52)
        };
        b.AddThemeFontSizeOverride("font_size", 22);
        b.Pressed += action;
        parent.AddChild(b);
        return b;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
        {
            if (GetTree().Paused) CloseMenu();
            else OpenMenu();

            GetViewport().SetInputAsHandled();
        }
    }

    void OpenMenu()
    {
        root.Visible = true;
        GetTree().Paused = true;
        firstButton.GrabFocus();
    }

    void CloseMenu()
    {
        root.Visible = false;
        GetTree().Paused = false;
    }
}
