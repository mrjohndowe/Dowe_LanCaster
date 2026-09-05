using System.IO;
using System.Text.Json;
using DoweLanCaster.Models;
using Microsoft.AspNetCore.DataProtection;

namespace DoweLanCaster.Services;

public sealed class TeraBoxConnectionStore
{
    private readonly string _path;
    private readonly IDataProtector _protector;

    public TeraBoxConnectionStore()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DoweLanCaster",
            "TeraBox");
        Directory.CreateDirectory(directory);

        _path = Path.Combine(directory, "connection.dat");
        _protector = DataProtectionProvider
            .Create(new DirectoryInfo(Path.Combine(directory, "keys")))
            .CreateProtector("DoweLanCaster.TeraBox.Connection.v1");
    }

    public (TeraBoxCredentials Credentials, TeraBoxSession? Session) Load()
    {
        try
        {
            if (!File.Exists(_path))
                return (new TeraBoxCredentials(), null);

            var json = _protector.Unprotect(File.ReadAllText(_path));
            var state = JsonSerializer.Deserialize<StoredState>(json);
            return state is null
                ? (new TeraBoxCredentials(), null)
                : (state.Credentials, state.Session);
        }
        catch
        {
            return (new TeraBoxCredentials(), null);
        }
    }

    public void Save(TeraBoxCredentials credentials, TeraBoxSession? session)
    {
        var json = JsonSerializer.Serialize(new StoredState
        {
            Credentials = credentials,
            Session = session
        });
        File.WriteAllText(_path, _protector.Protect(json));
    }

    public void Clear()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    private sealed class StoredState
    {
        public TeraBoxCredentials Credentials { get; set; } = new();
        public TeraBoxSession? Session { get; set; }
    }
}
