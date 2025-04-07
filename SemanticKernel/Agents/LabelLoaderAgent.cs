using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Octokit;

namespace SemanticKernelDemo.Agents
{
    /// <summary>
    /// Agent responsible for loading and analyzing GitHub labels
    /// </summary>
    public class LabelLoaderAgent
    {
        private readonly Kernel _kernel;
        private readonly string _chatDeployment;
        private readonly string _apiEndpoint;
        private readonly string _apiKey;

        /// <summary>
        /// Creates a new label loader agent
        /// </summary>
        /// <param name="kernel">Semantic Kernel instance</param>
        /// <param name="chatDeployment">Azure OpenAI chat deployment name</param>
        /// <param name="apiEndpoint">Azure OpenAI API endpoint</param>
        /// <param name="apiKey">Azure OpenAI API key</param>
        public LabelLoaderAgent(Kernel kernel, string chatDeployment, string apiEndpoint, string apiKey)
        {
            _kernel = kernel;
            _chatDeployment = chatDeployment;
            _apiEndpoint = apiEndpoint;
            _apiKey = apiKey;
        }

        /// <summary>
        /// Analyzes the repository labels to understand their meanings
        /// </summary>
        /// <param name="labelsData">The repository labels information</param>
        /// <returns>An analysis of the labels and their meanings</returns>
        public async Task<string> AnalyzeLabelsAsync(string labelsData)
        {
            var prompt = @$"
You are an expert at understanding GitHub repository labels. Please analyze this list of labels and provide insights:
1. Categorize the labels by type (e.g., bug, feature, priority, area, etc.)
2. Identify any patterns or organization in the labeling system
3. Note any important or commonly used labels

LABELS DATA:
{labelsData}

Provide a concise analysis to help understand how these labels are used to categorize issues.
";

            var executionSettings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = 1000,
                Temperature = 0.0
            };

            var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(executionSettings));
            return result.ToString();
        }

        /// <summary>
        /// Matches the best labels for a given issue based on its content
        /// </summary>
        /// <param name="issueContent">The issue content to analyze</param>
        /// <param name="labelsData">The repository labels information</param>
        /// <returns>Recommended labels with explanation</returns>
        public async Task<string> MatchLabelsToIssueAsync(string issueContent, string labelsData)
        {
            var prompt = @$"
You are an expert at matching GitHub issues with the most appropriate labels. 
Your task is to analyze an issue and suggest the most appropriate labels from the repository.

ISSUE CONTENT:
{issueContent}

AVAILABLE LABELS:
{labelsData}

For each label you recommend:
1. List the label name
2. Provide a confidence score (high, medium, low)
3. Briefly explain why this label is appropriate for the issue

List only the most relevant labels (maximum 5).
";

            var executionSettings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = 1500,
                Temperature = 0.1
            };

            var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(executionSettings));
            return result.ToString();
        }
    }
}