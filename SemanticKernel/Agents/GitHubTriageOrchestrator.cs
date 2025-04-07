#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Agents.Chat.TerminationStrategies;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Linq;

namespace SemanticKernelDemo.Agents
{
    /// <summary>
    /// Orchestrates the GitHub issue triage process using AgentGroupChat with ApprovalTerminationStrategy
    /// </summary>
    public class GitHubTriageOrchestrator
    {
        private readonly Kernel _kernel;
        private readonly AzureOpenAIChatCompletionService _chatService;
        private readonly string _repoOwner;
        private readonly string _repoName;
        private readonly bool _autoApplyLabels;
        private readonly int _maxIssues;

        public GitHubTriageOrchestrator(
            Kernel kernel, 
            AzureOpenAIChatCompletionService chatService, 
            string repoOwner, 
            string repoName, 
            bool autoApplyLabels = false,
            int maxIssues = 5)
        {
            _kernel = kernel;
            _chatService = chatService;
            _repoOwner = repoOwner;
            _repoName = repoName;
            _autoApplyLabels = autoApplyLabels;
            _maxIssues = maxIssues;
        }

        /// <summary>
        /// Runs the GitHub issue triage process using the modern AgentGroupChat with ApprovalTerminationStrategy
        /// </summary>
        public async Task<string> RunTriageAsync()
        {
            Console.WriteLine("Creating agents for GitHub issue triage...");

            // Create the specialized agents
            var issueAgent = new IssueAgent(_kernel, _chatService, _repoOwner, _repoName);
            var labelAgent = new LabelAgent(_kernel, _chatService, _repoOwner, _repoName);
            var triageCoordinatorAgent = new TriageCoordinatorAgent(_kernel, _chatService, _repoOwner, _repoName);

            // Create agent instances
            var issueAnalyzer = await issueAgent.CreateAgentAsync();
            var labelManager = await labelAgent.CreateAgentAsync();
            var coordinator = await triageCoordinatorAgent.CreateAgentAsync();

            // Create an AgentGroupChat with specialized agents
            var groupChat = new AgentGroupChat(
                groupName: "GitHub Triage Team",
                participants: new List<IAgent> { coordinator, issueAnalyzer, labelManager });

            // Set up the initial message
            var initialMessage = $"Hello team, I need you to triage up to {_maxIssues} open issues from the {_repoOwner}/{_repoName} repository. " +
                                 $"Auto-apply labels is set to: {_autoApplyLabels}. " +
                                 "Please coordinate to analyze the issues and recommend appropriate labels.";

            // Configure the GroupChat options
            var options = new GroupChatOptions
            {
                // Set ApprovalTerminationStrategy with a modern clean approach
                TerminationStrategy = new ApprovalTerminationStrategy(
                    "Do you approve the final triage recommendations?",
                    // Must include coordinator as the approval agent
                    coordinator),
                
                MaximumRounds = 15, // Reasonable limit for the conversation
                AgentResponseTimeout = TimeSpan.FromMinutes(2)
            };

            // Execute the group chat session
            Console.WriteLine("Starting agent group chat for issue triage...");
            var chatResult = await groupChat.InvokeAsync(initialMessage, options);

            // Extract the final message
            string finalSummary = chatResult.LastAgentMessage.Content;

            Console.WriteLine("\n--- TRIAGE SUMMARY ---");
            Console.WriteLine(finalSummary);

            return finalSummary;
        }
    }
}