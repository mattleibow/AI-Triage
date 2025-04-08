#pragma warning disable SKEXP0110 // Suppress experimental warnings
#pragma warning disable SKEXP0001 // Suppress experimental warnings

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Octokit.GraphQL;
using SemanticKernelDemo.Agents;
using SemanticKernelDemo.Plugins;

// Load configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>()
    .Build();

// Configure Azure OpenAI
var endpoint = configuration.GetOrThrow("AzureOpenAI:Endpoint", "Azure OpenAI endpoint");
var apiKey = configuration.GetOrThrow("AzureOpenAI:ApiKey", "Azure OpenAI API key");
var deploymentName = configuration.GetOrDefault("AzureOpenAI:ChatCompletionDeployment", "gpt-4");

// Configure GitHub access
var githubToken = configuration.GetOrThrow("GitHub:PersonalAccessToken", "GitHub token");

// Initialize GitHub connection
var connection = new Connection(new ProductHeaderValue("AI-Triage"), githubToken);

// Create the kernel
var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey)
    .Build();

var filters = new FunctionInvocationFilter();
kernel.AutoFunctionInvocationFilters.Add(filters);
kernel.FunctionInvocationFilters.Add(filters);
kernel.PromptRenderFilters.Add(filters);

// Create and register the GitHub-access kernel
var githubKernel = kernel.Clone();
var githubPlugin = new GitHubPlugin(connection);
githubKernel.Plugins.AddFromObject(githubPlugin, "GitHub");

Console.WriteLine("Beginning GitHub Issue triage process with AgentGroupChat...");

// Create the specialized agents
var issueAgent = IssueAnalyzerAgent.Create(githubKernel);
var labelAgent = LabelManagerAgent.Create(githubKernel);
var coordinatorAgent = TriageCoordinatorAgent.Create(kernel);

// Set up the agent group chat with ApprovalTerminationStrategy
var chat = new AgentGroupChat(issueAgent, labelAgent, coordinatorAgent)
{
    ExecutionSettings = new()
    {
        TerminationStrategy = new GitHubApprovalTerminationStrategy()
        {
            Agents = [coordinatorAgent],
            MaximumIterations = 10
        }
    }
};

try
{
    // Process the agent conversation and display messages in real-time
    Console.WriteLine("\n--- STARTING AGENT CONVERSATION ---\n");
    
    // Add the user message to start the conversation
    var initialMessage =
        $"I need you to triage some issues in a GitHub repository. " +
        $"Please analyze each issue and recommend appropriate labels.";
    
    chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, initialMessage));

    // var userMessage = Console.ReadLine();
    // if (!string.IsNullOrWhiteSpace(userMessage))
    // {
    //     chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userMessage));
    // }
    // else
    // {
    //     throw new Exception("User input is required to start the triage process.");
    // }

        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, "dotnet/maui #22332"));


    await foreach (var message in chat.InvokeAsync())
    {
        Console.ForegroundColor = message.AuthorName switch
        {
            "IssueAnalyzer" => ConsoleColor.Blue,
            "LabelManager" => ConsoleColor.Green,
            "TriageCoordinator" => ConsoleColor.Yellow,
            _ => ConsoleColor.White
        };

        Console.WriteLine($"{message.AuthorName}: {message.Content}");
        Console.ResetColor();
    }

    Console.WriteLine($"\n--- TRIAGE COMPLETED: {chat.IsComplete} ---");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error during triage process: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Console.ResetColor();
}

Console.WriteLine("\nIssue triage process completed. Press any key to exit.");
Console.ReadKey();
