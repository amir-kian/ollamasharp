using OllamaSharp;
using OllamaSharp.Models;

namespace Rag.Services;

public class EmbeddingService
{
    public const string ModelName = "nomic-embed-text:latest";

    // Keep the small embed model resident so a question does not reload it.
    private const string KeepAlive = "24h";

    private readonly OllamaApiClient _client;

    public EmbeddingService()
    {
        _client = new OllamaApiClient(
            new Uri("http://localhost:11434"),
            ModelName
        );
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var embeddings = await GenerateEmbeddingsAsync([text]);
        return embeddings[0];
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts)
    {
        var response = await _client.EmbedAsync(new EmbedRequest
        {
            Model = ModelName,
            Input = texts.ToList(),
            KeepAlive = KeepAlive
        });

        return response.Embeddings
            .Select(embedding => embedding.ToArray())
            .ToArray();
    }
}