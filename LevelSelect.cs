using Godot;

public partial class LevelSelect : Control
{
    // Coloque true para liberar todas as fases (útil para testar)
    const bool DevUnlockAll = false;

    int total = 0;
    int unlocked = 1;
    int selected = 1;
    string info = "";

    Vector2[] pos;
    const float Radius = 32f;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        while (Godot.FileAccess.FileExists($"res://maps/fase{total + 1}.txt"))
        {
            total++;
        }

        unlocked = DevUnlockAll ? total : Mathf.Clamp(Progress.Unlocked(), 1, System.Math.Max(total, 1));
        selected = unlocked;

        BuildLayout();
        Resized += BuildLayout;
    }

    // Posiciona os círculos em "cobrinha": uma linha da esquerda pra direita,
    // a de baixo da direita pra esquerda, e assim por diante
    void BuildLayout()
    {
        pos = new Vector2[total + 1];
        if (total == 0) return;

        Vector2 size = GetViewportRect().Size;
        int perRow = System.Math.Min(total, 6);
        int rows = (total + perRow - 1) / perRow;

        float spacing = 150f;
        float rowGap = 160f;
        float x0 = (size.X - (perRow - 1) * spacing) / 2f;
        float y0 = (size.Y - (rows - 1) * rowGap) / 2f + 30f;

        for (int i = 1; i <= total; i++)
        {
            int row = (i - 1) / perRow;
            int col = (i - 1) % perRow;
            if (row % 2 == 1) col = perRow - 1 - col;

            pos[i] = new Vector2(x0 + col * spacing, y0 + row * rowGap);
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        Vector2 size = GetViewportRect().Size;

        DrawRect(new Rect2(Vector2.Zero, size), new Color(0.12f, 0.14f, 0.20f));

        DrawString(font, new Vector2(0, 80), "MAPA DE FASES",
            HorizontalAlignment.Center, size.X, 48, Colors.White);

        if (total == 0)
        {
            DrawString(font, new Vector2(0, size.Y / 2), "Nenhuma fase encontrada na pasta maps",
                HorizontalAlignment.Center, size.X, 24, Colors.White);
            return;
        }

        // Caminho entre as fases
        for (int i = 1; i < total; i++)
        {
            bool open = i + 1 <= unlocked;
            Color c = open ? new Color(0.95f, 0.85f, 0.4f) : new Color(0.3f, 0.32f, 0.4f);
            DrawLine(pos[i], pos[i + 1], c, 8f, true);
        }

        // Círculos das fases
        for (int i = 1; i <= total; i++)
        {
            double best = Progress.BestTime(i);
            bool locked = i > unlocked;
            bool done = best >= 0;

            Color fill;
            if (locked) fill = new Color(0.30f, 0.32f, 0.40f);
            else if (done) fill = new Color(0.30f, 0.75f, 0.45f);
            else fill = new Color(0.95f, 0.75f, 0.25f);

            DrawCircle(pos[i], Radius, fill);

            if (i == selected)
            {
                DrawArc(pos[i], Radius + 9f, 0f, Mathf.Tau, 48, Colors.White, 4f, true);
            }

            Color textColor = locked ? new Color(0.6f, 0.62f, 0.7f) : new Color(0.1f, 0.1f, 0.15f);
            DrawString(font, pos[i] + new Vector2(-Radius, 11f), i.ToString(),
                HorizontalAlignment.Center, Radius * 2f, 32, textColor);

            if (done)
            {
                DrawString(font, pos[i] + new Vector2(-60, Radius + 30f), FormatTime(best),
                    HorizontalAlignment.Center, 120f, 20, Colors.White);
            }
        }

        DrawString(font, new Vector2(0, size.Y - 70), info,
            HorizontalAlignment.Center, size.X, 24, new Color(1f, 0.6f, 0.5f));

        DrawString(font, new Vector2(0, size.Y - 30),
            "Setas / WASD: mover   Enter: jogar   Esc: voltar",
            HorizontalAlignment.Center, size.X, 20, new Color(0.7f, 0.72f, 0.8f));
    }

    string FormatTime(double t)
    {
        int min = (int)(t / 60);
        double sec = t % 60;
        return $"{min:00}:{sec:00.0}";
    }

    void Move(int delta)
    {
        if (total == 0) return;
        selected = Mathf.Clamp(selected + delta, 1, total);
        info = "";
        QueueRedraw();
    }

    void TryPlay()
    {
        if (total == 0) return;

        if (selected > unlocked)
        {
            info = "Fase bloqueada! Termine a fase anterior.";
            QueueRedraw();
            return;
        }

        Maze.level = selected;
        Maze.totalTime = 0;
        GetTree().ChangeSceneToFile("res://Main.tscn");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            switch (key.Keycode)
            {
                case Key.Left:
                case Key.A:
                case Key.Up:
                case Key.W:
                    Move(-1);
                    break;

                case Key.Right:
                case Key.D:
                case Key.Down:
                case Key.S:
                    Move(1);
                    break;

                case Key.Enter:
                case Key.KpEnter:
                    TryPlay();
                    break;

                case Key.Escape:
                    GetTree().ChangeSceneToFile("res://Title.tscn");
                    break;
            }
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (pos == null) return;

        if (@event is InputEventMouseMotion motion)
        {
            int hit = NodeAt(motion.Position);
            if (hit > 0 && hit != selected)
            {
                selected = hit;
                info = "";
                QueueRedraw();
            }
        }
        else if (@event is InputEventMouseButton btn && btn.Pressed && btn.ButtonIndex == MouseButton.Left)
        {
            int hit = NodeAt(btn.Position);
            if (hit > 0)
            {
                selected = hit;
                TryPlay();
            }
        }
    }

    int NodeAt(Vector2 p)
    {
        for (int i = 1; i <= total; i++)
        {
            if (p.DistanceTo(pos[i]) <= Radius + 6f) return i;
        }
        return 0;
    }
}