using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace SemanticKernelDemo.Agents;

public static class LabelManagerAgent
{
    public static ChatCompletionAgent Create(Kernel kernel)
    {
        string instructions = $"""
            You are a GitHub labeling expert for open source GitHub repositories.

            Your responsibilities:
            1. Fetch all available repository labels from GitHub
            2. Recommend appropriate labels for issues based on their content
            3. Provide clear rationale for each label recommendation
            4. Suggest label improvements when appropriate
            
            Important rules:
            1. **IMPORTANT** Never make up new labels, only ever use the existing labels
            2. Be precise and accurate with your label suggestions
            3. Always check existing labels before making recommendations
            """;
        
        return new ChatCompletionAgent
        {
            Name = "LabelManager",
            Instructions = instructions,
            Kernel = kernel,
            Arguments = new(new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
        };
    }
}
