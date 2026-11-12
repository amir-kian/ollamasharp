

namespace Rag.Models;

public class EmbeddedFaq
{
    public FaqItem Faq { get; set; } = new();

    public float[] Embedding { get; set; } = [];
}
