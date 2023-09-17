using Lumafly.Services;
using Lumafly.Util;
using Lumafly.ViewModels;
using Lumafly.Enums;

namespace Lumafly.Mock;

public static class MockViewModel
{
    public static ModListViewModel DesignInstance => new(null!, new MockDatabase(), null!, null!, new GlobalSettingsFinder(null), new UrlSchemeHandler(true), LumaflyMode.Online);
}