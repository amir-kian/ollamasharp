using OllamaSharp;
using OllamaSharp.Models;
using Rag.Models;

namespace Rag.Services;

public class RagService
{
    private const string AnswerModel = "qwen3:14b";

    // Keep the answer model loaded between questions. Reloading a 14B model
    // is most of the wait on the second question.
    private const string KeepAlive = "24h";

    private readonly EmbeddingService _embeddingService;
    private readonly VectorStore _vectorStore;
    private readonly OllamaApiClient _llmClient;

    public RagService(
        EmbeddingService embeddingService,
        VectorStore vectorStore)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:11434"),
            Timeout = TimeSpan.FromMinutes(5)
        };

        _llmClient = new OllamaApiClient(httpClient, AnswerModel);
    }

    public async Task InitializeAsync(string faqPath)
    {
        var loader = new FaqLoader();
        var faqs = await loader.LoadAsync(faqPath);

        if (faqs.Count == 0)
            return;

        var contents = faqs.Select(faq => $"""
            Question: {faq.Question}
            Answer: {faq.Answer}
            Category: {faq.Category}
            """).ToArray();

        var embeddings =
            await _embeddingService.GenerateEmbeddingsAsync(contents);

        for (var i = 0; i < faqs.Count; i++)
            _vectorStore.Add(faqs[i], embeddings[i]);
    }

    /// <summary>
    /// Loads the answer model once so the first question does not pay the load cost.
    /// </summary>
    public async Task WarmupAsync()
    {
        await foreach (var _ in _llmClient.GenerateAsync(CreateRequest("ok", numPredict: 1)))
        {
        }
    }

    public async Task<IEnumerable<(FaqItem Faq, double Score)>>
        RetrieveAsync(string question)
    {
        var embedding =
            await _embeddingService.GenerateEmbeddingAsync(question);

        return _vectorStore.Search(embedding, topK: 3);
    }

    public async IAsyncEnumerable<string> StreamAnswerAsync(string question)
    {
        var results = (await RetrieveAsync(question)).ToList();

        const double threshold = 0.60;

        if (results.Count == 0 || results[0].Score < threshold)
        {
            yield return "Sorry, I couldn't find relevant information in our FAQs.";
            yield break;
        }

        var context = string.Join("\n\n",
            results
                .Where(x => x.Score >= threshold)
                .Select(x => $"""
                Question: {x.Faq.Question}
                Answer: {x.Faq.Answer}
                """));

        var prompt = $"""
        You are a helpful customer support assistant.

        Answer the user's question using ONLY the FAQ information below.
        Reply in a few short sentences. Do not explain your reasoning.

        If the information is insufficient, say:
        "Sorry, I don't have enough information to answer that question."

        FAQ Context:
        {context}

        User Question:
        {question}

        Answer:
        """;

        await foreach (var response in _llmClient.GenerateAsync(CreateRequest(prompt, numPredict: 200)))
        {
            var token = response?.Response;
            if (!string.IsNullOrEmpty(token))
                yield return token;
        }
    }

    private static GenerateRequest CreateRequest(string prompt, int numPredict)
    {
        return new GenerateRequest
        {
            Model = AnswerModel,
            Prompt = prompt,
            Stream = true,
            // Qwen3 thinks before it answers. That hidden pass is most of the delay
            // for a short FAQ reply, and the answer is not shown until it finishes.
            Think = false,
            KeepAlive = KeepAlive,
            Options = new RequestOptions
            {
                Temperature = 0.2f,
                NumCtx = 2048,
                NumPredict = numPredict
            }
        };
    }
}
