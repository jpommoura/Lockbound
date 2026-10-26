using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Maze : Node2D
{
    const int Tile = 48;

    // static: sobrevivem quando a cena reinicia (tecla R ou próxima fase)
public static int level = 1;
public static double totalTime = 0;
    string[] map;
    int totalLevels = 0;

    Dictionary<char, Color> keyColors = new()
    {
        ['y'] = new Color(1f, 0.85f, 0.1f),
        ['r'] = new Color(0.9f, 0.2f, 0.2f),
        ['b'] = new Color(0.2f, 0.5f, 1f),
    };

    Dictionary<char, string> keyNames = new()
    {
        ['y'] = "amarela",
        ['r'] = "vermelha",
        ['b'] = "azul",
    };

    HashSet<char> keys = new();
    int totalKeys = 0;
    Label label;
    Label msg;
    Label timerLabel;

    double elapsed = 0;
    bool finished = false;

    public override void _Ready()
    {
        // Conta quantas fases existem na pasta maps
        while (Godot.FileAccess.FileExists($"res://maps/fase{totalLevels + 1}.txt"))
        {
            totalLevels++;
        }

        map = LoadMap(level);
        if (map == null) return;

		int cols = map.Max(l => l.Length);
		var mapSize = new Vector2(cols * Tile, map.Length * Tile);
		Position = ((GetViewportRect().Size - mapSize) / 2).Floor();

        for (int y = 0; y < map.Length; y++)
        {
            for (int x = 0; x < map[y].Length; x++)
            {
                char c = map[y][x];
                var pos = new Vector2(x * Tile + Tile / 2, y * Tile + Tile / 2);

                if (c == '#')
                {
                    CreateWall(pos);
                }
                else if (c == 'P')
                {
					GetNode<Node2D>("../Player").GlobalPosition = ToGlobal(pos);                }
                else if (keyColors.ContainsKey(c))
                {
                    CreateKey(pos, c);
                    totalKeys++;
                }
                else if (c == 'Y' || c == 'R' || c == 'B')
                {
                    CreateDoor(pos, char.ToLower(c));
                }
                else if (c == 'D')
                {
                    CreateDoor(pos, 'd');
                }
                else if (c == 'S')
                {
                    CreateExit(pos);
                }
            }
        }

        var layer = new CanvasLayer();

        label = new Label { Position = new Vector2(10, 10) };
        label.AddThemeFontSizeOverride("font_size", 24);
        layer.AddChild(label);

        msg = new Label { Position = new Vector2(10, 45) };
        msg.AddThemeFontSizeOverride("font_size", 24);
        layer.AddChild(msg);

        timerLabel = new Label { Position = new Vector2(980, 10), Text = "00:00.0" };
        timerLabel.AddThemeFontSizeOverride("font_size", 24);
        layer.AddChild(timerLabel);

        var hint = new Label
        {
            Position = new Vector2(980, 45),
            Text = $"Fase {level}/{totalLevels}\nR: reiniciar"
        };
        hint.AddThemeFontSizeOverride("font_size", 20);
        layer.AddChild(hint);

        AddChild(layer);
		AddChild(new PauseMenu());
        UpdateLabel();
    }

    string[] LoadMap(int n)
    {
        using var file = Godot.FileAccess.Open($"res://maps/fase{n}.txt", Godot.FileAccess.ModeFlags.Read);

        if (file == null)
        {
            GD.PrintErr($"Não achei o mapa da fase {n} (res://maps/fase{n}.txt)");
            return null;
        }

        return file.GetAsText()
            .Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => l.Length > 0)
            .ToArray();
    }

    public override void _Process(double delta)
    {
        if (finished || timerLabel == null) return;

        elapsed += delta;
        timerLabel.Text = FormatTime(elapsed);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.R)
            {
                GetTree().ReloadCurrentScene();
            }
            else if (finished && (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter))
            {
                if (level < totalLevels)
                {
                    totalTime += elapsed;
                    level++;
                }
                else
                {
                    level = 1;
                    totalTime = 0;
                }

                GetTree().ReloadCurrentScene();
            }
        }
    }

    string FormatTime(double t)
    {
        int min = (int)(t / 60);
        double sec = t % 60;
        return $"{min:00}:{sec:00.0}";
    }

    void CreateWall(Vector2 pos)
    {
        var body = new StaticBody2D { Position = pos };

        var shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(Tile, Tile) }
        };

        var visual = new ColorRect
        {
            Size = new Vector2(Tile, Tile),
            Position = new Vector2(-Tile / 2, -Tile / 2),
            Color = new Color(0.28f, 0.30f, 0.38f)
        };

        body.AddChild(shape);
        body.AddChild(visual);
        AddChild(body);
    }

    void CreateKey(Vector2 pos, char type)
    {
        var area = new Area2D { Position = pos };

        var shape = new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = 12 }
        };

        var visual = new ColorRect
        {
            Size = new Vector2(20, 20),
            Position = new Vector2(-10, -10),
            Color = keyColors[type]
        };

        area.AddChild(shape);
        area.AddChild(visual);

        area.BodyEntered += (body) =>
        {
            if (body is Player)
            {
                keys.Add(type);
                msg.Text = $"Chave {keyNames[type]} coletada!";
                UpdateLabel();
                area.QueueFree();
            }
        };

        AddChild(area);
    }

    void CreateDoor(Vector2 pos, char type)
    {
        bool isFinal = type == 'd';
        Color color = isFinal ? new Color(0.6f, 0.4f, 0.9f) : keyColors[type];

        var door = new StaticBody2D { Position = pos };

        var shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(Tile, Tile) }
        };

        var visual = new ColorRect
        {
            Size = new Vector2(Tile, Tile),
            Position = new Vector2(-Tile / 2, -Tile / 2),
            Color = color
        };

        door.AddChild(shape);
        door.AddChild(visual);
        AddChild(door);

        // Sensor um pouco maior que a porta, para detectar o jogador chegando perto
        var sensor = new Area2D { Position = pos };

        sensor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(Tile + 16, Tile + 16) }
        });

        bool opened = false;

        sensor.BodyEntered += (body) =>
        {
            if (body is not Player || opened) return;

            if (isFinal)
            {
                if (keys.Count >= totalKeys)
                {
                    door.QueueFree();
                    opened = true;
                    msg.Text = "Porta final aberta!";
                }
                else
                {
                    msg.Text = $"A porta final precisa das {totalKeys} chaves! Faltam {totalKeys - keys.Count}.";
                }
            }
            else
            {
                if (keys.Contains(type))
                {
                    door.QueueFree();
                    opened = true;
                    msg.Text = "Porta aberta!";
                }
                else
                {
                    msg.Text = $"Precisa da chave {keyNames[type]}!";
                }
            }
        };

        AddChild(sensor);
    }

    void CreateExit(Vector2 pos)
    {
        var area = new Area2D { Position = pos };

        area.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(30, 30) }
        });

        area.AddChild(new ColorRect
        {
            Size = new Vector2(30, 30),
            Position = new Vector2(-15, -15),
            Color = new Color(0.2f, 0.8f, 0.4f)
        });

        area.BodyEntered += (body) =>
        {
            if (body is Player player)
            {
                finished = true;

                if (level < totalLevels)
                {
                    msg.Text = $"Fase {level} completa em {FormatTime(elapsed)}! ENTER: próxima fase";
                }
                else
                {
                    msg.Text = $"Você terminou o jogo! Tempo total: {FormatTime(totalTime + elapsed)}. ENTER: jogar de novo";
                }

                player.SetPhysicsProcess(false);
            }
        };

        AddChild(area);
    }

    void UpdateLabel()
    {
        string list = keys.Count == 0
            ? "nenhuma"
            : string.Join(", ", keys.Select(k => keyNames[k]));

        label.Text = $"Chaves {keys.Count}/{totalKeys}: {list}";
    }
}