using Godot;

public partial class Player : CharacterBody2D
{
    [Export] public float Speed = 200f;

    Sprite2D sprite;
    Texture2D down, up, left, right;

    public override void _Ready()
    {
        ZIndex = 1; // fica por cima de itens e do portal

        down  = GD.Load<Texture2D>("res://art/player/player_down.png");
        up    = GD.Load<Texture2D>("res://art/player/player_up.png");
        left  = GD.Load<Texture2D>("res://art/player/player_left.png");
        right = GD.Load<Texture2D>("res://art/player/player_right.png");

        sprite = GetNode<Sprite2D>("Sprite2D");
        sprite.Scale = new Vector2(1.5f, 1.5f);   // boneco de ~27×42 px        sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest; // pixel art nítido
        sprite.Texture = down;

        // colisor menor que o corredor (48 px) para não prender nas quinas
        // colisor: alinhado nos pés, mas mais baixo em cima (a cabeça passa um pouco por cima da parede, efeito 3D)
        var col = GetNode<CollisionShape2D>("CollisionShape2D");
        col.Shape = new RectangleShape2D { Size = new Vector2(26, 34) };
        col.Position = new Vector2(0, 4);
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 dir = Vector2.Zero;

        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) dir.Y -= 1;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) dir.Y += 1;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) dir.X -= 1;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) dir.X += 1;

        // Troca o sprite conforme a direção (na diagonal vale o lado)
        if (dir.X < 0) sprite.Texture = left;
        else if (dir.X > 0) sprite.Texture = right;
        else if (dir.Y < 0) sprite.Texture = up;
        else if (dir.Y > 0) sprite.Texture = down;

        Velocity = dir.Normalized() * Speed;
        MoveAndSlide();
    }
}