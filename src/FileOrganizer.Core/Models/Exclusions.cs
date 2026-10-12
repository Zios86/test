namespace FileOrganizer.Core.Models;

public enum ExclusionKind { Folder, Extension, File, Mask }

public sealed record Exclusion(ExclusionKind Kind, string Pattern);

/// <summary>Набор пользовательских исключений «Не анализировать».</summary>
public sealed class ExclusionSet
{
    public List<Exclusion> Items { get; set; } = new();

    public static ExclusionSet Default() => new()
    {
        Items =
        {
            new(ExclusionKind.Mask, "~$*"),
            new(ExclusionKind.Mask, "desktop.ini"),
            new(ExclusionKind.Mask, "thumbs.db"),
            new(ExclusionKind.Folder, ".git"),
            new(ExclusionKind.Folder, "node_modules"),
            new(ExclusionKind.Folder, "FileOrganizer_Recovery"),
        }
    };
}
