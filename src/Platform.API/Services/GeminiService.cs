using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Platform.API.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string GeminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";

    public GeminiService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["Gemini:ApiKey"];
    }

    public async Task<string> GenerateContentAsync(string prompt)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "{\"error\": \"Gemini API Key is missing in configuration.\"}";
        }

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{GeminiUrl}?key={_apiKey}", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return $"{{\"error\": \"Gemini API Error: {response.StatusCode}\", \"details\": {error}}}";
        }

        var responseString = await response.Content.ReadAsStringAsync();
        
        try 
        {
            using var doc = JsonDocument.Parse(responseString);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
                
            return text ?? string.Empty;
        }
        catch (Exception ex)
        {
            return $"{{\"error\": \"Failed to parse Gemini response: {ex.Message}\"}}";
        }
    }
}
