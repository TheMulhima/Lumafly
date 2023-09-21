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
        private static readonly HttpClient _hc;
        
        public ReadmePopupViewModel(ModItem modItem)
        {
            _modItem = modItem;

            Task.Run(async () =>
            {
                var readmeString = await FetchReadme();
                
                if (readmeString is not null)
                {
                    string pattern = @"(?<!\([^)]*)https://\S+";

                    // Replace matched links with Markdown syntax
                    Readme = Regex.Replace(readmeString, pattern, match => {
                        string link = match.Value;
                        return $"[{link}]({link})";
                    });
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
                    return "Readme is loading....";

                return "Readme not available";
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
    }
}
