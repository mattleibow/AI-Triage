#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Octokit;
using SemanticKernelDemo.Agents;
using SemanticKernelDemo.Plugins;

// Load configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>()
    .Build();

// Configure Azure OpenAI
var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new Exception("Azure OpenAI endpoint not found");
var apiKey = configuration["AzureOpenAI:ApiKey"] ?? throw new Exception("Azure OpenAI API key not found");
var chatDeployment = configuration["AzureOpenAI:ChatCompletionDeployment"] ?? "gpt-4";
var embeddingDeployment = configuration["AzureOpenAI:TextEmbeddingDeployment"] ?? "text-embedding-ada-002";

// Configure GitHub access
var githubToken = configuration["GitHub:PersonalAccessToken"] ?? throw new Exception("GitHub token not found");
var repoOwner = configuration["GitHub:RepositoryOwner"] ?? throw new Exception("GitHub repository owner not found");
var repoName = configuration["GitHub:RepositoryName"] ?? throw new Exception("GitHub repository name not found");

// Initialize GitHub client
var githubClient = new GitHubClient(new ProductHeaderValue("AI-Triage"));
githubClient.Credentials = new Credentials(githubToken);

// Set up the kernel with Azure OpenAI
var builder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        deploymentName: chatDeployment,
        endpoint: endpoint,
        apiKey: apiKey);

var kernel = builder.Build();

// Create and register the GitHub plugin
var githubPlugin = new GitHubPlugin(githubClient, repoOwner, repoName);
kernel.Plugins.AddFromObject(githubPlugin, "GitHubPlugin");

// Create the Azure OpenAI chat completion service for agents
var chatService = new AzureOpenAIChatCompletionService(
    deploymentName: chatDeployment,
    endpoint: endpoint,
    apiKey: apiKey);

// Create the GitHub Triage Orchestrator
var triageOrchestrator = new GitHubTriageOrchestrator(
    kernel: kernel,
    chatService: chatService,
    repoOwner: repoOwner,
    repoName: repoName,
    autoApplyLabels: false,  // Set to true to automatically apply labels
    maxIssues: 5);           // Process up to 5 issues

Console.WriteLine("Beginning GitHub Issue triage process using ApprovalTerminationStrategy...");

try
{
    // Run the triage process
    var triageSummary = await triageOrchestrator.RunTriageAsync();
    
    Console.WriteLine("\n--- TRIAGE PROCESS COMPLETED ---");
}
catch (Exception ex)
{
    Console.WriteLine($"Error during triage process: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

Console.WriteLine("Issue triage process completed. Press any key to exit.");
Console.ReadKey();