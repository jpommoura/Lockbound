using Godot;

public static class Progress
{
    const string SavePath = "user://progress.cfg";

    // Maior fase liberada (a 1 já nasce liberada)
    public static int Unlocked()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(SavePath) != Error.Ok) return 1;
        return cfg.GetValue("progress", "unlocked", 1).AsInt32();
    }

    // Melhor tempo da fase, ou -1 se nunca foi concluída
    public static double BestTime(int level)
    {
        var cfg = new ConfigFile();
        if (cfg.Load(SavePath) != Error.Ok) return -1;
        return cfg.GetValue("best", level.ToString(), -1.0).AsDouble();
    }

    // Registra a fase concluída. Devolve true se bateu o recorde anterior
    public static bool Complete(int level, double time, int totalLevels)
    {
        var cfg = new ConfigFile();
        cfg.Load(SavePath); // se o arquivo não existir ainda, tudo bem

        int unlocked = cfg.GetValue("progress", "unlocked", 1).AsInt32();
        if (level + 1 > unlocked && level < totalLevels)
        {
            cfg.SetValue("progress", "unlocked", level + 1);
        }

        double best = cfg.GetValue("best", level.ToString(), -1.0).AsDouble();
        bool newRecord = best >= 0 && time < best;

        if (best < 0 || time < best)
        {
            cfg.SetValue("best", level.ToString(), time);
        }

        cfg.Save(SavePath);
        return newRecord;
    }
}