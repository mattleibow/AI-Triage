using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Octokit;

namespace SemanticKernelDemo.Agents
{
    /// <summary>
    /// Agent responsible for loading and analyzing GitHub issues
    /// </summary>
    public class IssueLoaderAgent
    {
        private readonly Kernel _kernel;
        private readonly string _chatDeployment;
        private readonly string _apiEndpoint;
        private readonly string _apiKey;

        /// <summary>
        /// Creates a new issue loader agent
        /// </summary>
        /// <param name="kernel">Semantic Kernel instance</param>
        /// <param name="chatDeployment">Azure OpenAI chat deployment name</param>
        /// <param name="apiEndpoint">Azure OpenAI API endpoint</param>
        /// <param name="apiKey">Azure OpenAI API key</param>
        public IssueLoaderAgent(Kernel kernel, string chatDeployment, string apiEndpoint, string apiKey)
        {
            _kernel = kernel;
            _chatDeployment = chatDeployment;
            _apiEndpoint = apiEndpoint;
            _apiKey = apiKey;
        }

        /// <summary>
        /// Process an issue to extract key information and prepare it for analysis
        /// </summary>
        /// <param name="issueDetails">The full details of the issue</param>
        /// <returns>Structured analysis of the issue</returns>
        public async Task<string> AnalyzeIssueAsync(string issueDetails)
        {
            var prompt = @$"
You are an expert at analyzing GitHub issues. Please analyze this issue and extract the following information:
1. The main topic/area of the issue
2. The key problem being described
3. Whether this is a bug report, feature request, question, or other type of issue
4. Technologies or components mentioned in the issue

ISSUE DETAILS:
{issueDetails}

Provide a concise summary with the extracted information to help with categorization.
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
        /// Get a summary of all available issues
        /// </summary>
        /// <param name="issuesList">List of issues to summarize</param>
        /// <returns>A summary overview of all issues</returns>
        public async Task<string> GetIssuesSummaryAsync(string issuesList)
        {
            var prompt = @$"
You are an expert at analyzing GitHub issues. Please analyze this list of issues and provide a brief summary:
1. How many issues are there?
2. What are the common themes or patterns?
3. Are there any urgent issues that should be prioritized?

ISSUES LIST:
{issuesList}

Provide a concise overview to help with triaging these issues.
";

            var executionSettings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = 1000,
                Temperature = 0.0
            };

            var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(executionSettings));
            return result.ToString();
        }
    }
}