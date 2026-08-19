using System.IO;
using DoweLanCaster.Models;

namespace DoweLanCaster.Services;

public sealed class FolderPlaylistService
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".m4v", ".mov", ".mkv", ".webm",
            ".avi", ".ts", ".m2ts", ".mts",
            ".mpg", ".mpeg", ".wmv", ".flv"
        };

    public IReadOnlyList<FolderMediaItem> Scan(
        string folder,
        bool includeSubfolders)
    {
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException(folder);

        var option =
            includeSubfolders
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

        var items =
            Directory.EnumerateFiles(folder, "*.*", option)
                .Where(path =>
                    SupportedExtensions.Contains(
                        Path.GetExtension(path)))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select((path, index) =>
                    new FolderMediaItem
                    {
                        Number = index + 1,
                        FilePath = path
                    })
                .ToList();

        return items;
    }

    public static void Renumber(
        IList<FolderMediaItem> items)
    {
        for (var i = 0; i < items.Count; i++)
            items[i].Number = i + 1;
    }
}
