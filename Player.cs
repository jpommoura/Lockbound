using Godot;

public partial class Player : CharacterBody2D
{
    [Export] public float Speed = 200f;
    // Distância (em pixels) que o boneco percorre durante um ciclo completo de 6 quadros.
    // Menor = pernas mexem mais rápido. Maior = pernas mais devagar.
    [Export] public float StridePixels = 64f;

    const int FrameSize = 48;    // cada quadro tem 48x48 px
    const int FrameCount = 6;    // 6 quadros por direção

    // ordem das direções: 0 = baixo, 1 = cima, 2 = esquerda, 3 = direita
    static readonly string[] Names = { "down", "up", "left", "right" };

    Sprite2D sprite;
    Texture2D[] idle = new Texture2D[4];
    Texture2D[][] walk = new Texture2D[4][];
    int facing = 0;
    float animDist = 0;   // quanto ele já andou (a animação segue isso, não o relógio)

    public override void _Ready()
    {
        ZIndex = 1; // fica por cima de itens e do portal

        for (int d = 0; d < 4; d++)
        {
            // imagem parado
            idle[d] = GD.Load<Texture2D>($"res://art/player/player_{Names[d]}.png");

            // animação de andar: uma imagem com 6 quadros lado a lado, cortada em pedaços
            var sheet = GD.Load<Texture2D>($"res://art/player/walk_{Names[d]}.png");
            walk[d] = new Texture2D[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                walk[d][i] = new AtlasTexture
                {
                    Atlas = sheet,
                    Region = new Rect2(i * FrameSize, 0, FrameSize, FrameSize)
                };
            }
        }

        sprite = GetNode<Sprite2D>("Sprite2D");
        sprite.Scale = new Vector2(1.5f, 1.5f);   // boneco de ~27×42 px
        sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest; // pixel art nítido
        sprite.Texture = idle[facing];

        // colisor: alinhado nos pés, mas mais baixo em cima (a cabeça passa um pouco por cima da parede, efeito 3D)
        var col = GetNode<CollisionShape2D>("CollisionShape2D");
        col.Shape = new RectangleShape2D { Size = new Vector2(26, 34) };
        col.Position = new Vector2(0, 4);
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 dir = Vector2.Zero;

        if (GameControls.IsPressed("up")) dir.Y -= 1;
        if (GameControls.IsPressed("down")) dir.Y += 1;
        if (GameControls.IsPressed("left")) dir.X -= 1;
        if (GameControls.IsPressed("right")) dir.X += 1;

        // Direção do boneco (na diagonal vale o lado)
        if (dir != Vector2.Zero)
        {
            if (dir.X < 0) facing = 2;
            else if (dir.X > 0) facing = 3;
            else if (dir.Y < 0) facing = 1;
            else facing = 0;
        }

        Velocity = dir.Normalized() * Speed;
        MoveAndSlide();

        // A animação avança conforme a distância REAL andada (se bater na parede, para)
        float moved = GetRealVelocity().Length();

        if (moved > 10f)
        {
            animDist += moved * (float)delta;
            int frame = (int)(animDist / StridePixels * FrameCount) % FrameCount;
            sprite.Texture = walk[facing][frame];
        }
        else
        {
            animDist = 0;
            sprite.Texture = idle[facing];
        }
    }
}
