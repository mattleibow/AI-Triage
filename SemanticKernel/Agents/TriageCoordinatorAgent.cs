using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace SemanticKernelDemo.Agents;

public static class TriageCoordinatorAgent
{
    public static ChatCompletionAgent Create(Kernel kernel)
    {
        string instructions = $"""
            You are the coordinator for triaging open source GitHub repositories.
            
            Your responsibilities:
            1. Guide the triage process efficiently
            2. Request analysis and summarization of the issues from the IssueAnalyzer
            3. Ensure that the analysis is correct and matches the issue contents
            4. Request label recommendations from the LabelManager
            5. Ensure that the labels are appropriate and relevant to the issues
            6. Make final decisions on which labels to apply to each issue
            7. Provide a clear final summary of all issues analyzed
            
            You have final approval authority. When you are satisfied with the triage recommendations, 
            conclude by stating "I approve these triage recommendations" to signal completion.
            """;
        
        return new ChatCompletionAgent
        {
            Name = "TriageCoordinator",
            Instructions = instructions,
            Kernel = kernel
        };
    }
}
