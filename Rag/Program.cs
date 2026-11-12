using Rag.Services;

namespace Rag;

internal class Program
{
    static async Task Main(string[] args)
    {
        var embeddingService = new EmbeddingService();
        var vectorStore = new VectorStore();

        var ragService = new RagService(
            embeddingService,
            vectorStore
        );

        var faqPath = Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "faqs.json"
        );

        Console.WriteLine("Indexing FAQs...");
        await ragService.InitializeAsync(faqPath);

        Console.WriteLine("Loading answer model...");
        await ragService.WarmupAsync();

        Console.WriteLine("FAQ RAG system is ready!");

        while (true)
        {
            Console.Write("\nAsk a question (or type 'exit'): ");

            var question = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(question))
                continue;

            if (question.Equals(
                "exit",
                StringComparison.OrdinalIgnoreCase))
                break;

            Console.WriteLine("\nAI Answer:");

            await foreach (var token in ragService.StreamAnswerAsync(question))
                Console.Write(token);

            Console.WriteLine();
        }
    }
}