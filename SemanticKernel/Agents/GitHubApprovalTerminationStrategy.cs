#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;

namespace SemanticKernelDemo.Agents;

sealed class GitHubApprovalTerminationStrategy : TerminationStrategy
{
    // Terminate when the coordinator agent says they "approve"
    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
    {
        if (history.Count == 0) return Task.FromResult(false);

        var lastMessage = history[history.Count - 1];
        return Task.FromResult(
            lastMessage.AuthorName == "TriageCoordinator" && 
            (lastMessage.Content?.Contains("approve", StringComparison.OrdinalIgnoreCase) ?? false));
    }
}