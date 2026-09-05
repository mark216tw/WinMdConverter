using System.Drawing.Text;

namespace WinMdConverter.Core;

public sealed class FontService
{
    private readonly Lazy<IReadOnlyList<string>> _families = new(LoadFamilies);

    private static readonly IReadOnlyDictionary<string, string[]> Aliases =
        new Dictionary<string, string[]>(StringComparer.CurrentCultureIgnoreCase)
        {
            ["Microsoft JhengHei"] = ["Microsoft JhengHei", "微軟正黑體"],
            ["PMingLiU"] = ["PMingLiU", "新細明體"],
            ["MingLiU"] = ["MingLiU", "細明體"],
            ["DFKai-SB"] = ["DFKai-SB", "標楷體"]
        };

    public IReadOnlyList<string> GetInstalledFamilies() => _families.Value;

    public bool Exists(string family) => ResolveFamily(family) is not null;

    public string? ResolveFamily(string family)
    {
        var installed = GetInstalledFamilies();
        var exact = installed.FirstOrDefault(name => name.Equals(family, StringComparison.CurrentCultureIgnoreCase));
        if (exact is not null)
            return exact;

        return Aliases.TryGetValue(family, out var candidates)
            ? candidates.FirstOrDefault(candidate => installed.Contains(candidate, StringComparer.CurrentCultureIgnoreCase))
            : null;
    }

    private static IReadOnlyList<string> LoadFamilies()
    {
        using var fonts = new InstalledFontCollection();
        return fonts.Families
            .Select(family => family.Name)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }
}
