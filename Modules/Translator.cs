using Newtonsoft.Json.Linq;
using System.Text;

namespace Snout.Modules;

internal class SnoutTranslator

{
    // Provided by DeepL API within the limit of 500,000 characters per month & 3000 characters per request.

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _host;
    private readonly string _userAgent;
    private readonly string _contentType;

    public SnoutTranslator(string apiKey, string host, string userAgent, string contentType)
    {
        _httpClient = new();
        _apiKey = apiKey;
        _host = host;
        _userAgent = userAgent;
        _contentType = contentType;
    }

    public async Task<string> TranslateTextAsync(string text, string targetLanguage)
    {
        var endpoint = "https://" + _host + "/v2/translate";
            
        _httpClient.DefaultRequestHeaders.Authorization = new("DeepL-Auth-Key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", _userAgent);
            
        var body = "text=" + Uri.EscapeDataString(text) + "&target_lang=" + targetLanguage;
        var content = new StringContent(body, Encoding.UTF8, _contentType);
            
        var response = await _httpClient.PostAsync(endpoint, content);

        var result = await response.Content.ReadAsStringAsync();
        var json = JObject.Parse(result);
        var detectedSourceLanguage = json["translations"][0]["detected_source_language"].ToString();
        var translatedText = json["translations"][0]["text"].ToString();

        return (detectedSourceLanguage + "|" +translatedText);
    }
        
    public async Task<int> GetRemainingCharactersAsync()
    {
            
        var endpoint = "https://" + _host + "/v2/usage";
            
        _httpClient.DefaultRequestHeaders.Authorization = new("DeepL-Auth-Key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", _userAgent);
            
        var response = await _httpClient.GetAsync(endpoint);

        var result = await response.Content.ReadAsStringAsync();
        var json = JObject.Parse(result);
        var remainingCharacters = json["character_count"].ToString();

        return int.Parse(remainingCharacters);
    }   
}