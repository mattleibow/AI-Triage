using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SemanticKernelDemo.Agents
{
    /// <summary>
    /// Agent responsible for managing GitHub labels and recommending them for issues
    /// </summary>
    public class LabelAgent
    {
        private readonly Kernel _kernel;
        private readonly AzureOpenAIChatCompletionService _chatService;
        private readonly string _repoOwner;
        private readonly string _repoName;

        public LabelAgent(Kernel kernel, AzureOpenAIChatCompletionService chatService, string repoOwner, string repoName)
        {
            _kernel = kernel;
            _chatService = chatService;
            _repoOwner = repoOwner;
            _repoName = repoName;
        }

        /// <summary>
        /// Creates a Label Agent instance focused on label management
        /// </summary>
        public async Task<IAgent> CreateAgentAsync()
        {
            var systemPrompt = $@"
You are a GitHub labeling expert for the {_repoOwner}/{_repoName} repository.

Your responsibilities:
1. Retrieve available labels using GetRepositoryLabels
2. Suggest appropriate labels for issues based on content analysis
3. Provide clear rationale for label recommendations
4. If needed, recommend creating new labels

Always check existing labels before making recommendations. Be precise and accurate.
";

            return await new AzureAIAgentBuilder()
                .WithChatCompletionService(_chatService)
                .WithSystemPrompt(systemPrompt)
                .WithName("LabelManager")
                .WithDescription("Manages and recommends GitHub issue labels")
                .WithPlugin(_kernel.Plugins["GitHubPlugin"])
                .BuildAsync();
        }
    }
}