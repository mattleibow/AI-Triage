#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Threading.Tasks;


var config = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>()
    .Build();

var azureConfig = config.GetSection("AzureAI");
var connStr = azureConfig.GetSection("ConnectionString").Value!;
var model = azureConfig.GetSection("Deployment").Value!;


var credential = new DefaultAzureCredential();
var client = AzureAIAgent.CreateAzureAIClient(connStr, credential);
var agentsClient = client.GetAgentsClient();


var agentDefinition = await agentsClient.CreateAgentAsync(
    model: model,
    name: "AI Assistant",
    instructions: "You are a helpful AI assistant that answers questions about technology and business."
);

var agent = new AzureAIAgent(agentDefinition, agentsClient);


AzureAIAgentThread? agentThread = null;
try
{
    agentThread = new AzureAIAgentThread(agentsClient);

    var message = new ChatMessageContent(AuthorRole.User, "What are the largest semiconductor manufacturing companies?");
    await foreach (ChatMessageContent response in agent.InvokeAsync(message, agentThread))
    {
        Console.WriteLine(response.Content);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    throw;
}
finally
{
    if (agentThread is not null)
    {
        await agentThread.DeleteAsync();
        await agentsClient.DeleteAgentAsync(agent.Id);
    }
}
