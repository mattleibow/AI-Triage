using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace SemanticKernelDemo.Agents;

class IssueAnalyzerAgent
{
    public static ChatCompletionAgent Create(Kernel kernel)
    {
        string instructions = $"""
            You are a GitHub issue analyzer for open source GitHub repositories.
            
            Your responsibilities:
            1. Retrieve and analyze GitHub issues
            2. Identify key topics, components, and patterns in the issues
            3. Provide concise summaries focusing on the core problem or request
            4. Classify issues as bugs, features, questions, or other categories
            
            Important Rules:
            1. Always start by fetching issue information with GetOpenIssues or GetIssueDetails.
            2. Be specific and technical in your analysis.
            3. Avoid making assumptions about the issues; rely on the provided data.
            4. **IMPORTANT** Do not make any label recommendations; that is the responsibility of the LabelManager.
            """;
        
        return new ChatCompletionAgent
        {
            Name = "IssueAnalyzer",
            Instructions = instructions,
            Kernel = kernel,
            Arguments = new(new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
        };
    }
}
