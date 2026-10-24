//Microsoft Foundry
using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI.Chat;

//Microsoft AI framework
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace aiagent;

/// <summary>
/// An application to test Microsfot Foundry and AI Framework for AI agentic work 
/// </summary>
internal abstract class Program
{
    public static async Task Main(string[] args)
    {
        if (NumberOptionsMenu(out string? option))
        {
            return;
        }

        switch (option)
        {
            case "1":
                await MsAiAgentFrameworkService();
                break;
            case "2":
                await MsFoundryService();
                break;
        }
    }

    /// <summary>
    /// A console number options menu
    /// </summary>
    /// <param name="option"></param>
    /// <returns></returns>
    private static bool NumberOptionsMenu(out string? option)
    {
        bool gotAnwser = false;
        option = string.Empty;

        while (!gotAnwser)
        {
            Console.WriteLine("1. Use Microsoft AI Agent Framework");
            Console.WriteLine("2. Use Microsoft Foundry");
            Console.WriteLine("0. Exit the program");
            option = Console.ReadLine();
            if (string.IsNullOrEmpty(option))
            {
                Console.WriteLine("Please enter a valid option. Press any key to continue.");
                Console.ReadLine();
                Console.Clear();
            }
            else if (option == "0")
            {
                Console.WriteLine("Thank you for using the program!");
                return true;
            }

            gotAnwser = true;
        }

        Console.Clear();
        return false;
    }

    /// <summary>
    /// Use microsoft foundry for AI work
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private static async Task MsFoundryService()
    {
        string? endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT");
        string? deployment = Environment.GetEnvironmentVariable("FOUNDRY_MODEL_NAME");
        
        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deployment))
        {
            Console.WriteLine(
                "Please set FOUNDRY_PROJECT_ENDPOINT and FOUNDRY_MODEL_NAME environmental variables and try again. Press any key to continue.");
            return;
        }

        DefaultAzureCredential credential = new();
        AIProjectClient projectClient = new(new Uri(endpoint), credential);
        
        ClientConnection connection = projectClient.GetConnection(typeof(AzureOpenAIClient).FullName!);
        if(!connection.TryGetLocatorAsUri(out Uri? uri) || uri is null)
        {
            throw new InvalidOperationException("No Azure OpenAI connection found in the project.");
        }

        uri = new Uri($"https://{uri.Host}");
        
        AzureOpenAIClient azureAIClient = new(uri, credential);
        ChatClient chatClient = azureAIClient.GetChatClient(deployment);

        List<OpenAI.Chat.ChatMessage> history =
        [
            new SystemChatMessage("You are a helpful AI Assistant.")
        ];
        
        Console.WriteLine("Chat started. Type /exit to quit.");
        while (true)
        {
            Console.Write(">");
            string? question = Console.ReadLine();

            if (question is null || question.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(question))
            {
                continue;
            }
            
            history.Add(new UserChatMessage(question));
            try
            {
                ChatCompletion chatCompletion = await chatClient.CompleteChatAsync(history);
            
                string answer = chatCompletion.Content[0].Text;
                Console.WriteLine(answer);
                history.Add(new AssistantChatMessage(answer));
            }
            catch (AuthenticationFailedException e)
            {
                Console.WriteLine($"Authentication failed: {e.Message}");
            }
            catch (ClientResultException e)
            {
                Console.WriteLine($"Client failed: {e.Message}");
            }
            catch (UriFormatException e)
            {
                Console.WriteLine($"UriFormatException: {e.Message}");
            }
            catch (Exception e)
            {
                history.RemoveAt(history.Count - 1);
                Console.WriteLine($"{e.GetType().Name}: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Use Microsoft AI Framework for AI Work
    /// </summary>
    private static async Task MsAiAgentFrameworkService()
    {
        string apiKey? = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("Set OPENAI_API_KEY.");
            return;
        }

        const string model = "gpt-4.1-mini";
        
        OpenAIClient openAiClient = new(apiKey);
        IChatClient chatClient = openAiClient.GetChatClient(model).AsIChatClient();

        ChatClientAgent agent = new(
            chatClient,
            instructions: "You are a helpful AI assistant.",
            name: "My Agent");

            AgentSession session = await agent.CreateSessionAsync();

        Console.WriteLine("Chat started. Type /exit to quit.");
        while (true)
        {
            Console.Write("> ");
            string? question = Console.ReadLine();

            if (question is null || question.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(question))
            {
                continue;
            }

            try
            {
                AgentResponse response = await agent.RunAsync(question, session);
                Console.WriteLine(response.Text);
            }
            catch (ClientResultException e)
            {
                Console.WriteLine($"OpenAI call failed ({e.Status}): {e.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.GetType().Name}: {e.Message}");
            }
        }
    }
}