using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace DocumentAssistant.Controllers;

public class RagController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _environment;

    public RagController(
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment environment)
    {
        _httpClientFactory = httpClientFactory;
        _environment = environment;
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask([FromForm] string question, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(question)) return BadRequest(new { error = "Please enter a question." });
        var path = Path.Combine(_environment.ContentRootPath, "Documents", "LeavePolicy.txt");

        if (!System.IO.File.Exists(path))
            return StatusCode(500, new { error = "LeavePolicy.txt was not found." });

        var text = await System.IO.File.ReadAllTextAsync(path, cancellationToken);

        var passages = text.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(paragraph => (Source: "LeavePolicy.txt", Text: paragraph))
            .ToArray();

        if (passages.Length == 0)
            return BadRequest(new { error = "LeavePolicy.txt is empty." });

        try
        {
            var client = _httpClientFactory.CreateClient("Ollama");

            // The first embedding is the question; the rest are our passages.
            string[] inputs = [question, .. passages.Select(p => p.Text)];

            using var embeddingResponse = await client.PostAsJsonAsync(
                "/api/embed",
                new { model = "embeddinggemma", input = inputs },
                cancellationToken);

            embeddingResponse.EnsureSuccessStatusCode();

            var embeddingResult =
                await embeddingResponse.Content.ReadFromJsonAsync<EmbedResponse>(
                    cancellationToken);

            if (embeddingResult?.Embeddings is null ||
                embeddingResult.Embeddings.Length != inputs.Length)
            {
                return StatusCode(502, new { error = "Embedding response was incomplete." });
            }

            var questionVector = embeddingResult.Embeddings[0];

            var bestMatch = passages.Select((passage, index) => new
                {
                    passage.Source,
                    passage.Text,
                    Similarity = CosineSimilarity(
                        questionVector,
                        embeddingResult.Embeddings[index + 1])
                }).OrderByDescending(match => match.Similarity).First();

            var prompt = $"""
                Answer the question using only the source passage below.
                If the passage does not contain the answer, say:
                "I cannot answer that from the provided source."
                Keep your answer brief.

                Source: {bestMatch.Source}
                Passage: {bestMatch.Text}

                Question: {question}
                """;

            using var generationResponse = await client.PostAsJsonAsync(
                "/api/generate",
                new
                {
                    model = "gemma3:4b",
                    prompt,
                    stream = false,
                    options = new { num_predict = 120 }
                },
                cancellationToken);

            generationResponse.EnsureSuccessStatusCode();

            var generationResult =
                await generationResponse.Content.ReadFromJsonAsync<GenerateResponse>(
                    cancellationToken);

            return Ok(new
            {
                answer = generationResult?.Response ?? "",
                source = bestMatch.Source,
                passage = bestMatch.Text,
                similarity = Math.Round(bestMatch.Similarity, 3)
            });
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new { error = "Could not connect to Ollama." });
        }
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
    private sealed record GenerateResponse(string Response);
}