using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lumafly.Models;

namespace Lumafly.ViewModels
{
    public class ReadmePopupViewModel : ViewModelBase
    {
        private readonly ModItem _modItem;
        public bool IsRequestingReleaseNotes { get; }
        private static readonly HttpClient _hc;
        private string requestName => IsRequestingReleaseNotes ? "Release Notes" : "Readme";

        private string ReadmeLink = "";
        
        public ReadmePopupViewModel(ModItem modItem, bool requestingReleaseNotes = false)
        {
            _modItem = modItem;
            IsRequestingReleaseNotes = requestingReleaseNotes;

            Task.Run(async () =>
            {
                var displayString = IsRequestingReleaseNotes ? await FetchReleaseNotes() : await FetchReadme();
                
                if (displayString is not null)
                {
                    string rawLinkPattern = @"(?<!\([^)]*)https://\S+";

                    // Replace matched links with Markdown syntax
                    displayString = Regex.Replace(displayString, rawLinkPattern, match => {
                        string link = match.Value;
                        return $"[{link}]({link})";
                    });
                    
                    string anchorPattern = @"\[(.*?)\]\(#(.*?)\)";
                    string anchorPatternReplacement = $"[$1]({ReadmeLink}#$2)";
                    
                    displayString = Regex.Replace(displayString, anchorPattern, anchorPatternReplacement);
                    
                    Readme = displayString;

                }
                else
                {
                    Readme = string.Empty;
                }
            });
        }

        public string? Readme
        {
            get
            {
                if (!string.IsNullOrEmpty(_modItem.Readme))
                    return _modItem.Readme;

                if (_modItem.Readme is null)
                    return $"{requestName} is loading....";

                return $"{requestName} not available";
            }
            set
            {
                _modItem.Readme = value;
                RaisePropertyChanged(nameof(Readme));
            }
        }
        
        static ReadmePopupViewModel()
        {
            _hc = new HttpClient();
            _hc.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            _hc.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Lumafly", "1.0"));
        }
        
        private async Task<string?> FetchReadme()
        {
            try
            {
                var uri = new Uri(_modItem.Repository);

                string apiUrl = $"https://api.github.com/repos/{uri.AbsolutePath.TrimEnd('/').TrimStart('/')}/readme";

                HttpResponseMessage response = await _hc.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    JsonDocument json = JsonDocument.Parse(jsonResponse);
                    json.RootElement.TryGetProperty("download_url", out var downloadUrlProperty);

                    ReadmeLink = json.RootElement.GetProperty("html_url").GetString();

                    // Make a request to fetch the README content
                    HttpResponseMessage readmeResponse = await _hc.GetAsync(downloadUrlProperty.GetString());

                    if (readmeResponse.IsSuccessStatusCode)
                    {
                        return await readmeResponse.Content.ReadAsStringAsync();
                    }
                }

                throw new Exception($"Failed to fetch readme for {_modItem.Name} from {apiUrl}");
            }
            catch
            {
                return null;
            }
        }
        
        private async Task<string?> FetchReleaseNotes()
        {
            try
            {
                var uri = new Uri(_modItem.Repository);

                string releaseInfo = $"https://api.github.com/repos/{uri.AbsolutePath.TrimEnd('/').TrimStart('/')}/releases/latest";

                HttpResponseMessage response = await _hc.GetAsync(releaseInfo);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    JsonDocument json = JsonDocument.Parse(jsonResponse);
                    // the release info is in the body property
                    return json.RootElement.GetProperty("body").ToString();
                }
                
                throw new Exception($"Failed to fetch changelog for {_modItem.Name} from {releaseInfo}");
            }
            catch
            {
                return null;
            }
        }
        
        public void Close()
        {
            if (IsRequestingReleaseNotes)
            {
                // we don't want view readme button to display release notes
                _modItem.Readme = null;
            }
        }
    }
}
