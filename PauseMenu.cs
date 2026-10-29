using System.Collections.Generic;
using Godot;

public partial class PauseMenu : CanvasLayer
{
    const string DefaultInfo = "Clique numa tecla e aperte a nova.   Esc: cancelar   Delete: limpar";

    Control root;
    VBoxContainer mainBox;
    VBoxContainer controlsBox;
    Button firstButton;
    Button firstKeyButton;
    Label infoLabel;

    readonly Dictionary<(string action, int slot), Button> keyButtons = new();
    string capAction;
    int capSlot = -1;   // -1 = não está esperando tecla

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

        BuildMainMenu(center);
        BuildControlsMenu(center);
    }

    // ---------- Menu principal do pause ----------
    void BuildMainMenu(Control parent)
    {
        mainBox = new VBoxContainer();
        mainBox.AddThemeConstantOverride("separation", 12);
        parent.AddChild(mainBox);

        var title = new Label
        {
            Text = "PAUSADO",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 36);
        mainBox.AddChild(title);

        firstButton = AddButton(mainBox, "Continuar", CloseMenu);

        AddButton(mainBox, "Reiniciar fase", () =>
        {
            CloseMenu();
            GetTree().ReloadCurrentScene();
        });

        AddButton(mainBox, "Controles", ShowControls);

        AddButton(mainBox, "Mapa de fases", () =>
        {
            CloseMenu();
            GetTree().ChangeSceneToFile("res://LevelSelect.tscn");
        });

        AddButton(mainBox, "Menu principal", () =>
        {
            CloseMenu();
            GetTree().ChangeSceneToFile("res://Title.tscn");
        });

        AddButton(mainBox, "Sair", () => GetTree().Quit());
    }

    // ---------- Tela de controles ----------
    void BuildControlsMenu(Control parent)
    {
        controlsBox = new VBoxContainer { Visible = false };
        controlsBox.AddThemeConstantOverride("separation", 12);
        parent.AddChild(controlsBox);

        var title = new Label
        {
            Text = "CONTROLES",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 36);
        controlsBox.AddChild(title);

        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 16);
        grid.AddThemeConstantOverride("v_separation", 8);
        controlsBox.AddChild(grid);

        // cabeçalho
        grid.AddChild(new Label());
        foreach (var h in new[] { "Tecla 1", "Tecla 2" })
        {
            var head = new Label { Text = h, HorizontalAlignment = HorizontalAlignment.Center };
            head.AddThemeFontSizeOverride("font_size", 18);
            grid.AddChild(head);
        }

        // uma linha por comando
        foreach (var action in GameControls.Actions)
        {
            var name = new Label
            {
                Text = GameControls.Names[action],
                CustomMinimumSize = new Vector2(180, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            name.AddThemeFontSizeOverride("font_size", 22);
            grid.AddChild(name);

            for (int s = 0; s < 2; s++)
            {
                string a = action;
                int slot = s;

                var b = new Button { CustomMinimumSize = new Vector2(150, 44) };
                b.AddThemeFontSizeOverride("font_size", 20);
                b.Pressed += () => StartCapture(a, slot);
                grid.AddChild(b);

                keyButtons[(a, slot)] = b;
                firstKeyButton ??= b;
            }
        }

        infoLabel = new Label
        {
            Text = DefaultInfo,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        infoLabel.AddThemeFontSizeOverride("font_size", 18);
        controlsBox.AddChild(infoLabel);

        AddButton(controlsBox, "Restaurar padrão", () =>
        {
            CancelCapture();
            GameControls.ResetToDefault();
            RefreshKeyButtons();
            infoLabel.Text = "Controles restaurados para o padrão.";
        });

        AddButton(controlsBox, "Voltar", ShowMain);
    }

    void RefreshKeyButtons()
    {
        foreach (var kv in keyButtons)
            kv.Value.Text = GameControls.KeyName(GameControls.Get(kv.Key.action, kv.Key.slot));
    }

    void StartCapture(string action, int slot)
    {
        RefreshKeyButtons();   // desfaz um "Pressione..." anterior, se houver
        capAction = action;
        capSlot = slot;
        keyButtons[(action, slot)].Text = "Pressione...";
        infoLabel.Text = $"{GameControls.Names[action]}: aperte a nova tecla (Esc cancela, Delete limpa)";
    }

    void CancelCapture()
    {
        capSlot = -1;
        capAction = null;
        RefreshKeyButtons();
        if (infoLabel != null) infoLabel.Text = DefaultInfo;
    }

    // _Input roda antes de tudo: assim a tecla escolhida não aciona botões nem fecha o pause
    public override void _Input(InputEvent @event)
    {
        if (capSlot < 0) return;
        if (@event is not InputEventKey k || !k.Pressed || k.Echo) return;

        GetViewport().SetInputAsHandled();

        string action = capAction;
        int slot = capSlot;
        string message = "";
        bool ok = true;

        if (k.Keycode == Key.Escape)
        {
            CancelCapture();
            return;
        }
        else if (k.Keycode == Key.Delete || k.Keycode == Key.Backspace)
        {
            ok = GameControls.Clear(action, slot, out message);
        }
        else
        {
            ok = GameControls.TrySet(action, slot, k.Keycode, out message);
        }

        capSlot = -1;
        capAction = null;
        RefreshKeyButtons();
        infoLabel.Text = message != "" ? message : DefaultInfo;
    }

    // ---------- Geral ----------
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
        // W/S também navegam pelos botões do menu principal (as setas já funcionam sozinhas)
        if (root.Visible && @event is InputEventKey nav && nav.Pressed && (nav.Keycode == Key.W || nav.Keycode == Key.S))
        {
            MoveFocus(nav.Keycode == Key.S);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
        {
            if (controlsBox.Visible) ShowMain();            // Esc na tela de controles = voltar
            else if (GetTree().Paused) CloseMenu();
            else OpenMenu();

            GetViewport().SetInputAsHandled();
        }
    }

    // W/S também navegam pelos botões (as setas já funcionam sozinhas)
    void MoveFocus(bool next)
    {
        var owner = GetViewport().GuiGetFocusOwner();
        if (owner == null)
        {
            (controlsBox.Visible ? firstKeyButton : firstButton).GrabFocus();
            return;
        }

        var target = next ? owner.FindNextValidFocus() : owner.FindPrevValidFocus();
        target?.GrabFocus();
    }

    void ShowControls()
    {
        mainBox.Visible = false;
        controlsBox.Visible = true;
        CancelCapture();
        firstKeyButton.GrabFocus();
    }

    void ShowMain()
    {
        CancelCapture();
        controlsBox.Visible = false;
        mainBox.Visible = true;
        firstButton.GrabFocus();
    }

    void OpenMenu()
    {
        ShowMain();
        root.Visible = true;
        GetTree().Paused = true;
        firstButton.GrabFocus();
    }

    void CloseMenu()
    {
        CancelCapture();
        root.Visible = false;
        GetTree().Paused = false;
    }
}
