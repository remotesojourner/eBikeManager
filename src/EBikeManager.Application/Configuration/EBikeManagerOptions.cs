namespace EBikeManager.Application.Configuration;

public sealed class EBikeManagerOptions
{
    public int Port { get; set; } = 2004;

    public string DataDirectory { get; set; } = "data";

    public bool DisableAuth { get; set; }

    public string DatabasePath => Path.Combine(DataDirectory, "storage.db");

    public string KeysDirectory => Path.Combine(DataDirectory, "keys");

    public string FitDirectory => Path.Combine(DataDirectory, "fit");
}
