using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SemanticKernelDemo.Agents
{
    /// <summary>
    /// Agent responsible for retrieving and analyzing GitHub issues
    /// </summary>
    public class IssueAgent
    {
        private readonly Kernel _kernel;
        private readonly AzureOpenAIChatCompletionService _chatService;
        private readonly string _repoOwner;
        private readonly string _repoName;

        public IssueAgent(Kernel kernel, AzureOpenAIChatCompletionService chatService, string repoOwner, string repoName)
        {
            _kernel = kernel;
            _chatService = chatService;
            _repoOwner = repoOwner;
            _repoName = repoName;
        }

        /// <summary>
        /// Creates an Issue Agent instance with clear, focused functionality
        /// </summary>
        public async Task<IAgent> CreateAgentAsync()
        {
            var systemPrompt = $@"
You analyze GitHub issues from the {_repoOwner}/{_repoName} repository.

Focus on:
1. Retrieving issues using GitHubPlugin
2. Identifying key topics and components
3. Classifying issues (bug, feature, question)
4. Providing concise analysis

Always fetch data first with GetOpenIssues or GetIssueDetails before analysis.
";

            return await new AzureAIAgentBuilder()
                .WithChatCompletionService(_chatService)
                .WithSystemPrompt(systemPrompt)
                .WithName("IssueAnalyzer")
                .WithDescription("Analyzes GitHub issues to extract key information")
                .WithPlugin(_kernel.Plugins["GitHubPlugin"])
                .BuildAsync();
        }
    }
}