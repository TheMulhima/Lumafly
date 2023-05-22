using Scarab.Services;
using Scarab.Util;
using Scarab.ViewModels;
using Scarab.Enums;

namespace Scarab.Mock;

public static class MockViewModel
{
    public static ModListViewModel DesignInstance => new(null!, new MockDatabase(), null!, null!, new GlobalSettingsFinder(null), new UrlSchemeHandler(true), ScarabMode.Online);
}