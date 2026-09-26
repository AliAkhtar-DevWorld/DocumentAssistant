using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace DocumentAssistant.Controllers;

public class EmbeddingsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public EmbeddingsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Sample(CancellationToken cancellationToken)
    {
        const string text = "Employees receive 20 days of annual leave.";

        var client = _httpClientFactory.CreateClient("Ollama");

        using var response = await client.PostAsJsonAsync(
            "/api/embed",
            new { model = "embeddinggemma", input = text },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbedResponse>(
            cancellationToken);

        var vector = result?.Embeddings?.FirstOrDefault();

        if (vector is null)
            return StatusCode(502, "No embedding was returned.");

        return Json(new
        {
            text,
            dimensions = vector.Length,
            firstEightNumbers = vector.Take(8)
        });
    }
    [HttpGet]
    public async Task<IActionResult> Compare(CancellationToken cancellationToken)
    {
        const string question = "How much vacation time do employees get?";

        string[] documents =
        [
            "Employees receive 20 days of annual leave.",
        "The office opens at 9 AM on weekdays.",
        "Employees must change their password every 90 days."
        ];

        string[] inputs = [question, .. documents];

        var client = _httpClientFactory.CreateClient("Ollama");

        using var response = await client.PostAsJsonAsync(
            "/api/embed",
            new { model = "embeddinggemma", input = inputs },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbedResponse>(
            cancellationToken);

        if (result?.Embeddings is null ||
            result.Embeddings.Length != inputs.Length)
        {
            return StatusCode(502, "Unexpected embedding response.");
        }

        var questionVector = result.Embeddings[0];

        var matches = documents
            .Select((text, index) => new
            {
                text,
                similarity = Math.Round(
                    CosineSimilarity(questionVector, result.Embeddings[index + 1]),
                    3)
            })
            .OrderByDescending(item => item.similarity)
            .ToArray();

        return Json(new { question, matches });
    }

    private static double CosineSimilarity(float[] first, float[] second)
    {
        if (first.Length != second.Length || first.Length == 0)
            throw new ArgumentException("Vectors must have the same dimensions.");

        double dot = 0;
        double firstLength = 0;
        double secondLength = 0;

        for (int i = 0; i < first.Length; i++)
        {
            dot += first[i] * second[i];
            firstLength += first[i] * first[i];
            secondLength += second[i] * second[i];
        }

        return dot / (Math.Sqrt(firstLength) * Math.Sqrt(secondLength));
    }
    private sealed record EmbedResponse(float[][] Embeddings);
}