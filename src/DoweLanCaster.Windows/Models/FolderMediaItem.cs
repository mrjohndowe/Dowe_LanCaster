namespace DoweLanCaster.Models;

using System.IO;

public sealed class FolderMediaItem
{
    public int Number { get; set; }
    public string FilePath { get; init; } = "";
    public string FileName => Path.GetFileName(FilePath);
    public string Folder => Path.GetDirectoryName(FilePath) ?? "";
    public string Extension => Path.GetExtension(FilePath);
    public bool IsCurrent { get; set; }

    public override string ToString() =>
        $"{Number}. {FileName}";
}
