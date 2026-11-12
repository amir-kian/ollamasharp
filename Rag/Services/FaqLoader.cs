using Rag.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Rag.Services;

public class FaqLoader
{
    public async Task<List<FaqItem>> LoadAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<List<FaqItem>>(
            json, options) ?? [];
    }
}