using WinMdConverter.Core;

namespace WinMdConverter.Tests;

public sealed class FontServiceTests
{
    [Fact]
    public void ResolveFamily_AcceptsMicrosoftJhengHeiAliasOnTraditionalChineseWindows()
    {
        var service = new FontService();
        if (!service.GetInstalledFamilies().Contains("微軟正黑體"))
            return;

        Assert.Equal("微軟正黑體", service.ResolveFamily("Microsoft JhengHei"));
    }
}
