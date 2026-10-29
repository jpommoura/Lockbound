using Godot;

// Menu que aparece quando o jogador termina a fase
public partial class FinishMenu : CanvasLayer
{
    public string Info = "";
    public bool HasNext = true;

    Button firstButton;

    public override void _Ready()
    {
        Layer = 11;

        var root = new Control();
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
            Text = HasNext ? "FASE COMPLETA!" : "VOCÊ TERMINOU O JOGO!",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 36);
        box.AddChild(title);

        var info = new Label
        {
            Text = Info,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        info.AddThemeFontSizeOverride("font_size", 22);
        box.AddChild(info);

        if (HasNext)
        {
            firstButton = AddButton(box, "Próxima fase", () =>
            {
                Maze.level += 1;
                GetTree().ReloadCurrentScene();
            });
        }

        var restart = AddButton(box, "Reiniciar fase", () => GetTree().ReloadCurrentScene());
        var map = AddButton(box, "Mapa de fases", () => GetTree().ChangeSceneToFile("res://LevelSelect.tscn"));
        AddButton(box, "Tela inicial", () => GetTree().ChangeSceneToFile("res://Title.tscn"));

        // Na última fase não existe "próxima": o foco começa em "Mapa de fases"
        if (firstButton == null) firstButton = map;
        firstButton.GrabFocus();
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

    // W/S também navegam pelos botões (as setas já funcionam sozinhas)
    void MoveFocus(bool next)
    {
        var owner = GetViewport().GuiGetFocusOwner();
        if (owner == null)
        {
            firstButton.GrabFocus();
            return;
        }

        var target = next ? owner.FindNextValidFocus() : owner.FindPrevValidFocus();
        target?.GrabFocus();
    }

    // Esc aqui não abre o menu de pause por cima
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey nav && nav.Pressed && (nav.Keycode == Key.W || nav.Keycode == Key.S))
        {
            MoveFocus(nav.Keycode == Key.S);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
        {
            GetViewport().SetInputAsHandled();
        }
    }
}
