using System.Net.Http;
using System.Threading.Tasks;
using Lumafly.Interfaces;
using Lumafly.Util;

namespace Lumafly.Services;

/// <summary>
/// Uses pastebin.com to store text data for sharing mod packs
/// </summary>
public class PastebinTextStorage : IOnlineTextStorage
{
    private readonly HttpClient _hc;
    private readonly ISettings _settings;
    public PastebinTextStorage(ISettings settings, HttpClient hc)
    {
        _hc = hc;
        _settings = settings;
    }
    
    // taken from https://github.com/hkmodmanager/HKModManager/blob/master/HKMM-Core/Modules/Upload/PastebinUploadModule.cs#L17-L29
    public async Task<string> Upload(string name, string data)
    {
        var form = new MultipartFormDataContent()
        {
            { new StringContent("w6VD3O4rMTkJ1DKdraH8SgwVqA6waCFN"), "api_dev_key" },
            { new StringContent(data), "api_paste_code" },
            { new StringContent(name ?? ""), "api_paste_name" },
            { new StringContent("paste"), "api_option" },
            { new StringContent("text"), "api_paste_format" },
            { new StringContent("0"), "api_paste_private" },
            { new StringContent("N"), "api_paste_expire_date" },
        };
        var resp = await _hc.PostAsync("https://pastebin.com/api/api_post.php", form);
        resp.EnsureSuccessStatusCode();
        var pasteLink = await resp.Content.ReadAsStringAsync();
        return pasteLink.Replace("https://pastebin.com/", "");
    }

    public async Task<string> Download(string code)
    {
        return await _hc.GetStringAsync2(_settings,"https://pastebin.com/raw/" + code);
    }
}