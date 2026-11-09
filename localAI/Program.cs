
using Microsoft.Extensions.AI;
using OllamaSharp;


IChatClient chatClient =
    new OllamaApiClient(
        new Uri("http://localhost:11434/"),
        "gemma3:12b"
    );

// Start conversation with context
List<ChatMessage> chatHistory = new();

while (true)
{
    Console.WriteLine("Your prompt:");
    var userPrompt = Console.ReadLine();

    // Exit when the user enters "exit"
    if (string.IsNullOrWhiteSpace(userPrompt))
        continue;

    if (userPrompt.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    // Add user message to history
    chatHistory.Add(
        new ChatMessage(ChatRole.User, userPrompt)
    );

    Console.WriteLine("AI Response:");

    var response = "";

    // Stream AI response
    await foreach (ChatResponseUpdate item in
        chatClient.GetStreamingResponseAsync(chatHistory))
    {
        Console.Write(item.Text);
        response += item.Text;
    }

    // Add AI response to history
    chatHistory.Add(
        new ChatMessage(ChatRole.Assistant, response)
    );

    Console.WriteLine("\n");
}
