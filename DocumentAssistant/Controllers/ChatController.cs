using System.Net.Http.Json;
using DocumentAssistant.Models;
using Microsoft.AspNetCore.Mvc;

namespace DocumentAssistant.Controllers;

public class ChatController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ChatController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new ChatViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask(
        [FromForm] string question,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(question))
            return BadRequest(new { error = "Please enter a question." });

        try
        {
            var client = _httpClientFactory.CreateClient("Ollama");

            using var response = await client.PostAsJsonAsync(
                "/api/generate",
                new
                {
                    model = "gemma3:4b",
                    prompt = question,
                    stream = false
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return StatusCode(502, new { error = $"Ollama returned HTTP {(int)response.StatusCode}." });

            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(
                cancellationToken);

            if (string.IsNullOrWhiteSpace(result?.Response))
                return StatusCode(502, new { error = "Ollama returned an empty answer." });

            return Ok(new { answer = result.Response });
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new { error = "Could not connect to Ollama. Check that it is running." });
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new { error = "Ollama took too long to respond." });
        }
    }

    private sealed record OllamaResponse(string Response);
}