using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

internal class Program
{
    public static async Task Main(string[] args)
    {
        string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        string model = "gpt-5-nano";
        string prompt =
            "What are the largest cities in the world? Respondwith NAME and TOTAL AREA in square kilometers";
        OpenAIClient client = new(apiKey);
        ChatClient chatClient = client.GetChatClient(model);
        IChatClient chatClientInterface = chatClient.AsIChatClient();
//        ChatClientAgent aiAgent = chatClient.AsAIAgent(instructions:"You are super helpful", name: "My Agent");
        ChatClientAgent aiAgent = new(chatClientInterface, "You are super helpful", "My Agent");


        ChatMessage chatMessage = new(ChatRole.User, prompt);

        AgentResponse<IEnumerable<LargestCity>>
            response = await aiAgent.RunAsync<IEnumerable<LargestCity>>(chatMessage);

        foreach (LargestCity city in response.Result)
        {
            Console.WriteLine(response.Text);
        }
    }

    private record LargestCity(string Name, decimal? Area);
}