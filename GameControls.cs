using System.Collections.Generic;
using Godot;

// Teclas do jogo: cada comando tem até 2 teclas, que o jogador pode trocar.
// Fica salvo em user://controls.cfg
public static class GameControls
{
    const string SavePath = "user://controls.cfg";

    public static readonly string[] Actions = { "up", "down", "left", "right", "restart" };

    public static readonly Dictionary<string, string> Names = new()
    {
        ["up"] = "Cima",
        ["down"] = "Baixo",
        ["left"] = "Esquerda",
        ["right"] = "Direita",
        ["restart"] = "Reiniciar fase",
    };

    static readonly Dictionary<string, Key[]> defaults = new()
    {
        ["up"] = new[] { Key.W, Key.Up },
        ["down"] = new[] { Key.S, Key.Down },
        ["left"] = new[] { Key.A, Key.Left },
        ["right"] = new[] { Key.D, Key.Right },
        ["restart"] = new[] { Key.R, Key.None },
    };

    static Dictionary<string, Key[]> bindings;

    // Carrega do arquivo (ou usa o padrão) na primeira vez que alguém pedir
    static void Ensure()
    {
        if (bindings != null) return;

        var cfg = new ConfigFile();
        bool loaded = cfg.Load(SavePath) == Error.Ok;

        bindings = new Dictionary<string, Key[]>();
        foreach (var a in Actions)
        {
            var keys = new Key[2];
            for (int i = 0; i < 2; i++)
            {
                Key def = defaults[a][i];
                keys[i] = loaded ? (Key)cfg.GetValue("keys", $"{a}_{i}", (long)def).AsInt64() : def;
            }
            bindings[a] = keys;
        }

        if (!IsValid()) ResetToDefault();
    }

    static void Save()
    {
        var cfg = new ConfigFile();
        foreach (var a in Actions)
            for (int i = 0; i < 2; i++)
                cfg.SetValue("keys", $"{a}_{i}", (long)bindings[a][i]);
        cfg.Save(SavePath);
    }

    // Todo comando precisa ter pelo menos uma tecla
    static bool IsValid()
    {
        foreach (var a in Actions)
            if (bindings[a][0] == Key.None && bindings[a][1] == Key.None) return false;
        return true;
    }

    static Dictionary<string, Key[]> Snapshot()
    {
        var copy = new Dictionary<string, Key[]>();
        foreach (var a in Actions) copy[a] = (Key[])bindings[a].Clone();
        return copy;
    }

    public static Key Get(string action, int slot)
    {
        Ensure();
        return bindings[action][slot];
    }

    // O comando está sendo segurado agora?
    public static bool IsPressed(string action)
    {
        Ensure();
        foreach (var k in bindings[action])
            if (k != Key.None && Input.IsKeyPressed(k)) return true;
        return false;
    }

    // A tecla desse evento é de algum slot do comando?
    public static bool Matches(string action, InputEventKey e)
    {
        Ensure();
        foreach (var k in bindings[action])
            if (k != Key.None && e.Keycode == k) return true;
        return false;
    }

    public static string KeyName(Key k) => k == Key.None ? "—" : OS.GetKeycodeString(k);

    // Nome da primeira tecla do comando (para mostrar na tela)
    public static string Label(string action)
    {
        Ensure();
        foreach (var k in bindings[action])
            if (k != Key.None) return KeyName(k);
        return "—";
    }

    // Define a tecla de um slot. Se a tecla já era de outro comando, as duas trocam de lugar.
    public static bool TrySet(string action, int slot, Key key, out string message)
    {
        Ensure();
        var backup = Snapshot();
        Key old = bindings[action][slot];
        message = "";

        foreach (var a in Actions)
            for (int i = 0; i < 2; i++)
            {
                if (bindings[a][i] != key || (a == action && i == slot)) continue;

                bindings[a][i] = old;
                message = old == Key.None
                    ? $"{KeyName(key)} foi tirada de \"{Names[a]}\"."
                    : $"{KeyName(key)} e {KeyName(old)} trocaram entre \"{Names[a]}\" e \"{Names[action]}\".";
            }

        bindings[action][slot] = key;

        if (!IsValid())
        {
            bindings = backup;
            message = "Cada comando precisa ter pelo menos uma tecla.";
            return false;
        }

        Save();
        return true;
    }

    public static bool Clear(string action, int slot, out string message)
    {
        Ensure();
        var backup = Snapshot();
        message = "";

        bindings[action][slot] = Key.None;

        if (!IsValid())
        {
            bindings = backup;
            message = "Cada comando precisa ter pelo menos uma tecla.";
            return false;
        }

        Save();
        return true;
    }

    public static void ResetToDefault()
    {
        bindings = new Dictionary<string, Key[]>();
        foreach (var a in Actions) bindings[a] = (Key[])defaults[a].Clone();
        Save();
    }
}
