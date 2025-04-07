using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SemanticKernelDemo.Agents
{
    /// <summary>
    /// Agent responsible for coordinating the issue triage process
    /// </summary>
    public class TriageCoordinatorAgent
    {
        private readonly Kernel _kernel;
        private readonly AzureOpenAIChatCompletionService _chatService;
        private readonly string _repoOwner;
        private readonly string _repoName;

        public TriageCoordinatorAgent(Kernel kernel, AzureOpenAIChatCompletionService chatService, string repoOwner, string repoName)
        {
            _kernel = kernel;
            _chatService = chatService;
            _repoOwner = repoOwner;
            _repoName = repoName;
        }

        /// <summary>
        /// Creates a Coordinator Agent instance to manage the triage workflow
        /// </summary>
        public async Task<IAgent> CreateAgentAsync()
        {
            var systemPrompt = $@"
You are the coordinator for triaging GitHub issues in the {_repoOwner}/{_repoName} repository.

Your responsibilities:
1. Guide the triage process efficiently
2. Delegate tasks to specialized agents
3. Synthesize information from other agents
4. Create final recommendations and summaries
5. Make decisions when agents disagree

Focus on clear communication and structured output. Provide a final summary with issue numbers, analysis, and label recommendations.
";

            return await new AzureAIAgentBuilder()
                .WithChatCompletionService(_chatService)
                .WithSystemPrompt(systemPrompt)
                .WithName("TriageCoordinator")
                .WithDescription("Coordinates the GitHub issue triage process")
                .WithPlugin(_kernel.Plugins["GitHubPlugin"])
                .BuildAsync();
        }
    }
}