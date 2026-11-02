using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Maze : Node2D
{
    const int Tile = 48;

    // static: sobrevivem quando a cena reinicia (tecla R ou próxima fase)
    public static int level = 1;
    public static double totalTime = 0;

    // Cada fase tem 1 ou mais andares (separados por "---" no arquivo do mapa)
    List<string[]> floors;
    int currentFloor = 0;
    Node2D floorNode;                                   // tudo do andar atual fica aqui dentro
    HashSet<(int f, int x, int y)> taken = new();       // chaves/portas/lanterna já usadas
    bool changingFloor = false;
    int totalLevels = 0;

    Dictionary<char, Color> keyColors = new()
    {
        ['y'] = new Color(0.89f, 0.74f, 0.28f),
        ['r'] = new Color(0.80f, 0.31f, 0.29f),
        ['b'] = new Color(0.35f, 0.50f, 0.82f),
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
    double msgTimer = 0;     // quanto tempo a mensagem ainda fica na tela
    string lastMsg = "";
    Label timerLabel;
    Label hint;

    double elapsed = 0;
    bool finished = false;

    CanvasModulate darkness;
    PointLight2D playerLight;
    const float LightRadius = Tile * 1.8f;   // alcance do círculo de visão

    public override void _Ready()
    {
        // Conta quantas fases existem na pasta maps
        while (Godot.FileAccess.FileExists($"res://maps/fase{totalLevels + 1}.txt"))
        {
            totalLevels++;
        }

        floors = LoadFloors(level);
        if (floors == null) return;

        // Total de chaves da fase (somando todos os andares)
        totalKeys = floors.Sum(fl => fl.Sum(row => row.Count(c => keyColors.ContainsKey(c))));

        // Fases que têm uma lanterna (letra L em algum andar) começam escuras
        if (floors.Any(fl => fl.Any(row => row.Contains('L'))))
        {
            SetupDarkness();
        }

        var layer = new CanvasLayer();

        label = new Label { Position = new Vector2(10, 10) };
        label.AddThemeFontSizeOverride("font_size", 24);
        layer.AddChild(label);
        msg = new Label { HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        msg.AddThemeFontSizeOverride("font_size", 28);
        msg.AddThemeColorOverride("font_outline_color", Colors.Black);
        msg.AddThemeConstantOverride("outline_size", 8);
        layer.AddChild(msg);
        msg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        msg.OffsetTop = -170;      // altura na tela: mais negativo = mais alto
        msg.OffsetBottom = -120;
        msg.AddThemeFontSizeOverride("font_size", 24);
        layer.AddChild(msg);

        timerLabel = new Label { Position = new Vector2(980, 10), Text = "00:00.0" };
        timerLabel.AddThemeFontSizeOverride("font_size", 24);
        layer.AddChild(timerLabel);

        hint = new Label { Position = new Vector2(980, 45) };
        hint.AddThemeFontSizeOverride("font_size", 20);
        layer.AddChild(hint);

        AddChild(layer);
        AddChild(new PauseMenu());

        BuildFloor(0, -1);
        UpdateLabel();
    }

    // Lê o arquivo da fase. Andares são separados por uma linha com "---"
    List<string[]> LoadFloors(int n)
    {
        using var file = Godot.FileAccess.Open($"res://maps/fase{n}.txt", Godot.FileAccess.ModeFlags.Read);

        if (file == null)
        {
            GD.PrintErr($"Não achei o mapa da fase {n} (res://maps/fase{n}.txt)");
            return null;
        }

        var result = new List<string[]>();
        var current = new List<string>();

        foreach (var raw in file.GetAsText().Split('\n'))
        {
            var line = raw.TrimEnd('\r');

            if (line.Trim() == "---")
            {
                if (current.Count > 0) result.Add(current.ToArray());
                current = new List<string>();
            }
            else if (line.Length > 0)
            {
                current.Add(line);
            }
        }

        if (current.Count > 0) result.Add(current.ToArray());

        return result.Count == 0 ? null : result;
    }

    // Monta um andar. fromFloor = andar de onde o jogador veio (-1 = início da fase)
    void BuildFloor(int f, int fromFloor)
    {
        currentFloor = f;
        var map = floors[f];

        // Remove o andar anterior
        if (floorNode != null)
        {
            RemoveChild(floorNode);
            floorNode.QueueFree();
        }
        floorNode = new Node2D();
        AddChild(floorNode);

        // Centraliza este andar na tela
        int cols = map.Max(l => l.Length);
        var mapSize = new Vector2(cols * Tile, map.Length * Tile);
        Position = ((GetViewportRect().Size - mapSize) / 2).Floor();

        CreateFloor(map);

        Vector2? spawn = null;

        for (int y = 0; y < map.Length; y++)
        {
            for (int x = 0; x < map[y].Length; x++)
            {
                char c = map[y][x];
                var pos = new Vector2(x * Tile + Tile / 2, y * Tile + Tile / 2);

                if (c == '#')
                {
                    CreateWall(pos, x, y);
                }
                else if (c == 'P')
                {
                    if (fromFloor < 0) spawn = pos;
                }
                else if (keyColors.ContainsKey(c))
                {
                    if (!taken.Contains((f, x, y))) CreateKey(pos, c, f, x, y);
                }
                else if (c == 'Y' || c == 'R' || c == 'B')
                {
                    if (!taken.Contains((f, x, y))) CreateDoor(pos, char.ToLower(c), f, x, y);
                }
                else if (c == 'D')
                {
                    if (!taken.Contains((f, x, y))) CreateDoor(pos, 'd', f, x, y);
                }
                else if (c == 'S')
                {
                    CreateExit(pos);
                }
                else if (c == 'L')
                {
                    if (!taken.Contains((f, x, y))) CreateLantern(pos, f, x, y);
                }
                else if (c == '>' || c == '<')
                {
                    CreateStairs(pos, c, f);

                    // Chegou de baixo (subiu) -> aparece ao lado da escada '<'
                    // Chegou de cima (desceu) -> aparece ao lado da escada '>'
                    if ((c == '<' && fromFloor == f - 1) || (c == '>' && fromFloor == f + 1))
                    {
                        spawn = pos + SpawnOffset(map, x, y);
                    }
                }
            }
        }

        if (spawn.HasValue)
        {
            var player = GetNode<CharacterBody2D>("../Player");
            player.GlobalPosition = ToGlobal(spawn.Value);
            player.Velocity = Vector2.Zero;
        }

        changingFloor = false;
        UpdateHint();
    }

    // Procura o quadrado livre ao lado da escada (onde o jogador vai aparecer)
    Vector2 SpawnOffset(string[] map, int x, int y)
    {
        var dirs = new (int dx, int dy)[] { (0, -1), (0, 1), (-1, 0), (1, 0) };

        foreach (var d in dirs)
        {
            int nx = x + d.dx, ny = y + d.dy;
            if (ny < 0 || ny >= map.Length || nx < 0 || nx >= map[ny].Length) continue;
            if (map[ny][nx] == ' ') return new Vector2(d.dx * Tile, d.dy * Tile);
        }

        return Vector2.Zero;
    }

    void ChangeFloor(int to, int from)
    {
        BuildFloor(to, from);
    }

    void UpdateHint()
    {
        hint.Text = HintText();
    }

    string HintText()
    {
        string floorText = floors.Count > 1 ? $"\nAndar {currentFloor + 1}/{floors.Count}" : "";
        return $"Fase {level}/{totalLevels}{floorText}\n{GameControls.Label("restart")}: reiniciar";
    }

    public override void _Process(double delta)
    {
        // Mantém o texto "tecla: reiniciar" certo se o jogador trocar a tecla no pause
        if (hint != null)
        {
            string t = HintText();
            if (hint.Text != t) hint.Text = t;
        }

                // Mensagem: aparece, fica ~2,5 s e some devagar
        if (msg != null)
        {
            if (msg.Text != lastMsg)
            {
                lastMsg = msg.Text;
                msgTimer = 2.5;
            }

            if (msgTimer > 0)
            {
                msgTimer -= delta;
                msg.Modulate = new Color(1, 1, 1, Mathf.Clamp((float)msgTimer / 0.5f, 0f, 1f));
                if (msgTimer <= 0)
                {
                    msg.Text = "";
                    lastMsg = "";
                }
            }
        }

        if (finished || timerLabel == null) return;

        elapsed += delta;
        timerLabel.Text = FormatTime(elapsed);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (GameControls.Matches("restart", key))
            {
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

    Sprite2D MakeSprite(string file)
    {
        return new Sprite2D
        {
            Texture = GD.Load<Texture2D>($"res://art/{file}"),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
    }

    // número "aleatório" fixo por posição (a fase fica igual toda vez)
    int Hash(int x, int y) => Mathf.Abs(x * 73 + y * 151 + x * y * 7);

    void CreateWall(Vector2 pos, int gx, int gy)
    {
        var body = new StaticBody2D { Position = pos };

        var shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(Tile, Tile) }
        };

        var visual = MakeSprite($"environment/wall_{Hash(gx, gy) % 3 + 1}.png");

        body.AddChild(shape);
        body.AddChild(visual);
        floorNode.AddChild(body);
    }

    void CreateKey(Vector2 pos, char type, int f, int gx, int gy)
    {
        var area = new Area2D { Position = pos };

        var shape = new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = 12 }
        };

        var visual = MakeSprite("items/key.png");
        visual.Modulate = keyColors[type];

        area.AddChild(shape);
        area.AddChild(visual);

        area.BodyEntered += (body) =>
        {
            if (body is Player)
            {
                keys.Add(type);
                taken.Add((f, gx, gy));
                msg.Text = $"Chave {keyNames[type]} coletada!";
                UpdateLabel();
                area.QueueFree();
            }
        };

        floorNode.AddChild(area);
    }

    void CreateDoor(Vector2 pos, char type, int f, int gx, int gy)
    {
        bool isFinal = type == 'd';

        var door = new StaticBody2D { Position = pos };

        var shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(Tile, Tile) }
        };

        string doorFile = type switch { 'y' => "yellow", 'r' => "red", 'b' => "blue", _ => "final" };
        var visual = MakeSprite($"items/door_{doorFile}.png");

        door.AddChild(shape);
        door.AddChild(visual);
        floorNode.AddChild(door);

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
                    taken.Add((f, gx, gy));
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
                    taken.Add((f, gx, gy));
                    msg.Text = "Porta aberta!";
                }
                else
                {
                    msg.Text = $"Precisa da chave {keyNames[type]}!";
                }
            }
        };

        floorNode.AddChild(sensor);
    }

    void CreateStairs(Vector2 pos, char type, int f)
    {
        var area = new Area2D { Position = pos };

        area.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(26, 26) }
        });

        area.AddChild(MakeSprite(type == '>' ? "environment/stairs_up.png" : "environment/stairs_down.png"));

        area.BodyEntered += (body) =>
        {
            if (body is not Player || changingFloor || finished) return;

            changingFloor = true;
            int to = type == '>' ? f + 1 : f - 1;
            msg.Text = type == '>' ? $"Você subiu para o andar {to + 1}." : $"Você desceu para o andar {to + 1}.";

            // Troca de andar fora do cálculo de colisões (senão o Godot reclama)
            Callable.From(() => ChangeFloor(to, f)).CallDeferred();
        };

        floorNode.AddChild(area);
    }

    void CreateExit(Vector2 pos)
    {
        var area = new Area2D { Position = pos };

        area.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(30, 30) }
        });

        area.AddChild(MakeSprite("environment/exit.png"));

        area.BodyEntered += (body) =>
        {
            if (body is Player player)
            {
                finished = true;

                bool record = Progress.Complete(level, elapsed, totalLevels);

                msg.Text = "";
                player.SetPhysicsProcess(false);

                // Mostra o menu de fim de fase (adicionado fora do cálculo de colisões)
                string info = $"Tempo: {FormatTime(elapsed)}" + (record ? "   Novo recorde!" : "");
                bool hasNext = level < totalLevels;
                Callable.From(() => AddChild(new FinishMenu { Info = info, HasNext = hasNext })).CallDeferred();
            }
        };

        floorNode.AddChild(area);
    }

    // Textura do círculo de luz: centro claro, borda que vai escurecendo
    GradientTexture2D MakeLightTexture()
    {
        var grad = new Gradient();
        grad.Offsets = new float[] { 0f, 0.55f, 1f };
        grad.Colors = new Color[] { Colors.White, Colors.White, Colors.Black };

        return new GradientTexture2D
        {
            Gradient = grad,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(1f, 0.5f),
            Width = 256,
            Height = 256
        };
    }

    void SetupDarkness()
    {
        // Escurece tudo, menos a interface (que fica em outra camada)
        darkness = new CanvasModulate { Color = new Color(0.02f, 0.02f, 0.05f) };
        AddChild(darkness);

        // Círculo de luz que acompanha o jogador
        playerLight = new PointLight2D
        {
            Texture = MakeLightTexture(),
            TextureScale = LightRadius / 128f,
            Energy = 1f
        };
        GetNode<Node2D>("../Player").AddChild(playerLight);
    }

    void CreateLantern(Vector2 pos, int f, int gx, int gy)
    {
        var area = new Area2D { Position = pos };

        area.AddChild(new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = 14 }
        });

        area.AddChild(MakeSprite("items/lantern.png"));

        // Brilho para dar para ver a lanterna um pouco de longe no escuro
        area.AddChild(new PointLight2D
        {
            Texture = MakeLightTexture(),
            TextureScale = (Tile * 1.2f) / 128f,
            Color = new Color(1f, 0.8f, 0.4f)
        });

        area.BodyEntered += (body) =>
        {
            if (body is Player)
            {
                taken.Add((f, gx, gy));
                area.QueueFree();
                RevealMap();
            }
        };

        floorNode.AddChild(area);
    }

    void RevealMap()
    {
        msg.Text = "Você pegou a lanterna! O labirinto está iluminado.";

        // Clareia o mapa aos poucos e apaga o círculo
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(darkness, "color", Colors.White, 1.2);
        tween.TweenProperty(playerLight, "energy", 0.0, 1.2);
    }

    void CreateFloor(string[] map)
    {
        // "Vazio" ao redor do mapa (substitui o cinza do fundo)
        floorNode.AddChild(new ColorRect
        {
            Position = new Vector2(-4000, -4000),
            Size = new Vector2(8000, 8000),
            Color = new Color(0.12f, 0.14f, 0.20f),
            ZIndex = -2,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });

        // Chão do labirinto (uma lajota por quadrado)
        for (int y = 0; y < map.Length; y++)
            for (int x = 0; x < map[y].Length; x++)
            {
                if (map[y][x] == '#') continue;
                int h = Hash(x, y) % 5;
                var f = MakeSprite($"environment/floor_{(h == 0 ? 2 : h == 1 ? 3 : 1)}.png");
                f.Position = new Vector2(x * Tile + Tile / 2, y * Tile + Tile / 2);
                f.ZIndex = -1;
                floorNode.AddChild(f);
            }
    }

    void UpdateLabel()
    {
        string list = keys.Count == 0
            ? "nenhuma"
            : string.Join(", ", keys.Select(k => keyNames[k]));

        label.Text = $"Chaves {keys.Count}/{totalKeys}: {list}";
    }
}
