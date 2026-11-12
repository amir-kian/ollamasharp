using Rag.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Services;

public class VectorStore
{
    private readonly List<EmbeddedFaq> _items = [];

    public void Add(FaqItem faq, float[] embedding)
    {
        _items.Add(new EmbeddedFaq
        {
            Faq = faq,
            Embedding = embedding
        });
    }

    public IEnumerable<(FaqItem Faq, double Score)> Search(
        float[] queryEmbedding,
        int topK = 3)
    {
        return _items
            .Select(item => (
                Faq: item.Faq,
                Score: CosineSimilarity.Calculate(
                    queryEmbedding,
                    item.Embedding)
            ))
            .OrderByDescending(x => x.Score)
            .Take(topK);
    }
}