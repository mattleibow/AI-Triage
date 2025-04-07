using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Octokit;
using SemanticKernelDemo.Plugins;

namespace SemanticKernelDemo.Agents
{
    /// <summary>
    /// Coordinator agent that orchestrates the entire workflow
    /// </summary>
    public class CoordinatorAgent
    {
        private readonly Kernel _kernel;
        private readonly IssueLoaderAgent _issueLoaderAgent;
        private readonly LabelLoaderAgent _labelLoaderAgent;
        private readonly string _chatDeployment;
        private readonly string _apiEndpoint;
        private readonly string _apiKey;
        private readonly string _repoOwner;
        private readonly string _repoName;

        /// <summary>
        /// Creates a new coordinator agent
        /// </summary>
        /// <param name="kernel">Semantic Kernel instance</param>
        /// <param name="issueLoaderAgent">Issue loader agent</param>
        /// <param name="labelLoaderAgent">Label loader agent</param>
        /// <param name="chatDeployment">Azure OpenAI chat deployment name</param>
        /// <param name="apiEndpoint">Azure OpenAI API endpoint</param>
        /// <param name="apiKey">Azure OpenAI API key</param>
        /// <param name="repoOwner">GitHub repository owner</param>
        /// <param name="repoName">GitHub repository name</param>
        public CoordinatorAgent(
            Kernel kernel, 
            IssueLoaderAgent issueLoaderAgent, 
            LabelLoaderAgent labelLoaderAgent,
            string chatDeployment,
            string apiEndpoint,
            string apiKey,
            string repoOwner,
            string repoName)
        {
            _kernel = kernel;
            _issueLoaderAgent = issueLoaderAgent;
            _labelLoaderAgent = labelLoaderAgent;
            _chatDeployment = chatDeployment;
            _apiEndpoint = apiEndpoint;
            _apiKey = apiKey;
            _repoOwner = repoOwner;
            _repoName = repoName;
        }

        /// <summary>
        /// Orchestrates the entire issue triage workflow using AI-driven prompts
        /// </summary>
        /// <param name="maxIssues">Maximum number of issues to process</param>
        /// <param name="autoApplyLabels">Whether to automatically apply suggested labels</param>
        /// <returns>A summary of the triage process</returns>
        public async Task<string> TriageIssuesAsync(int maxIssues = 5, bool autoApplyLabels = false)
        {
            Console.WriteLine("Starting issue triage process...");
            
            // Step 1: Get open issues using a natural language prompt
            var openIssuesPrompt = $"Get {maxIssues} open issues from the GitHub repository {_repoOwner}/{_repoName}.";
            var executionSettings = new OpenAIPromptExecutionSettings 
            { 
                MaxTokens = 2000,
                Temperature = 0.0
            };
            
            var args = new KernelArguments();
            args.Add("executionSettings", executionSettings);
            
            var openIssuesResult = await _kernel.InvokePromptAsync(openIssuesPrompt, args);
            string openIssues = openIssuesResult.ToString();
            
            Console.WriteLine("Retrieved open issues:");
            Console.WriteLine(openIssues);
            
            // Get a summary of the issues
            string issuesSummary = await _issueLoaderAgent.GetIssuesSummaryAsync(openIssues);
            Console.WriteLine("\nIssues Summary:");
            Console.WriteLine(issuesSummary);
            
            // Step 2: Get repository labels using a natural language prompt
            var labelsPrompt = $"Get all labels from the GitHub repository {_repoOwner}/{_repoName}.";
            var labelsResult = await _kernel.InvokePromptAsync(labelsPrompt, args);
            string repositoryLabels = labelsResult.ToString();
            
            Console.WriteLine("\nRetrieved repository labels:");
            Console.WriteLine(repositoryLabels);
            
            // Analyze labels to understand their purpose
            string labelsAnalysis = await _labelLoaderAgent.AnalyzeLabelsAsync(repositoryLabels);
            Console.WriteLine("\nLabels Analysis:");
            Console.WriteLine(labelsAnalysis);
            
            // Parse issues from the text (basic implementation)
            var issueNumbers = ExtractIssueNumbers(openIssues);
            
            // Process each issue
            var results = new List<string>();
            
            foreach (string issueNumber in issueNumbers)
            {
                Console.WriteLine($"\nProcessing issue #{issueNumber}...");
                
                // Get detailed issue info using a natural language prompt
                var issuePrompt = $"Get detailed information about issue #{issueNumber} from the GitHub repository {_repoOwner}/{_repoName}.";
                var issueDetailsResult = await _kernel.InvokePromptAsync(issuePrompt, args);
                string issueDetails = issueDetailsResult.ToString();
                
                // Analyze the issue
                string issueAnalysis = await _issueLoaderAgent.AnalyzeIssueAsync(issueDetails);
                Console.WriteLine("\nIssue Analysis:");
                Console.WriteLine(issueAnalysis);
                
                // Match labels to the issue
                string labelRecommendations = await _labelLoaderAgent.MatchLabelsToIssueAsync(issueDetails, repositoryLabels);
                Console.WriteLine("\nLabel Recommendations:");
                Console.WriteLine(labelRecommendations);
                
                // Make final decision on which labels to apply
                string finalDecision = await MakeFinalLabelDecisionAsync(issueDetails, labelRecommendations);
                Console.WriteLine("\nFinal Label Decision:");
                Console.WriteLine(finalDecision);
                
                // Apply labels if auto-application is enabled
                if (autoApplyLabels)
                {
                    // Extract labels from the final decision (simple implementation)
                    var labelsToApply = ExtractLabelsFromDecision(finalDecision);
                    
                    if (labelsToApply.Count > 0)
                    {
                        // Apply labels using a natural language prompt
                        var applyLabelsPrompt = $"Apply the following labels to issue #{issueNumber} in the GitHub repository {_repoOwner}/{_repoName}: {string.Join(", ", labelsToApply)}.";
                        var applyResult = await _kernel.InvokePromptAsync(applyLabelsPrompt, args);
                        string result = applyResult.ToString();
                        Console.WriteLine(result);
                    }
                }
                
                // Collect results
                results.Add($"Issue #{issueNumber}: {finalDecision}");
            }
            
            // Provide a final summary
            string triageSummary = $"Triage completed for {results.Count} issues.\n\n" + string.Join("\n\n", results);
            
            return triageSummary;
        }
        
        /// <summary>
        /// Makes the final decision on which labels to apply to an issue
        /// </summary>
        /// <param name="issueDetails">Full issue details</param>
        /// <param name="labelRecommendations">Label recommendations from the label loader agent</param>
        /// <returns>Final decision on which labels to apply</returns>
        private async Task<string> MakeFinalLabelDecisionAsync(string issueDetails, string labelRecommendations)
        {
            var prompt = @$"
You are the final decision maker in a GitHub issue triage process.
You need to decide which labels should be applied to this issue based on the issue content and recommendations.

ISSUE DETAILS:
{issueDetails}

LABEL RECOMMENDATIONS:
{labelRecommendations}

Please make a final decision on which labels to apply. Format your response as follows:
1. List each label you decide to apply (name only)
2. Give a very brief explanation for why you selected each label
3. End with a 'FINAL LABELS:' section that lists only the label names, one per line

Be selective and only choose the most appropriate labels.
";

            var executionSettings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = 1000,
                Temperature = 0.1
            };

            var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(executionSettings));
            return result.ToString();
        }
        
        /// <summary>
        /// Helper method to extract issue numbers from the open issues text
        /// </summary>
        private List<string> ExtractIssueNumbers(string openIssuesText)
        {
            var issueNumbers = new List<string>();
            var lines = openIssuesText.Split('\n');
            
            foreach (var line in lines)
            {
                if (line.Contains("Issue #"))
                {
                    // Extract the issue number, e.g., from "- Issue #123: Title"
                    int startIndex = line.IndexOf('#') + 1;
                    int endIndex = line.IndexOf(':', startIndex);
                    
                    if (endIndex > startIndex)
                    {
                        string issueNumber = line.Substring(startIndex, endIndex - startIndex).Trim();
                        issueNumbers.Add(issueNumber);
                    }
                }
            }
            
            return issueNumbers;
        }
        
        /// <summary>
        /// Helper method to extract labels from the final decision text
        /// </summary>
        private List<string> ExtractLabelsFromDecision(string finalDecision)
        {
            var labels = new List<string>();
            bool inFinalLabelsSection = false;
            
            var lines = finalDecision.Split('\n');
            
            foreach (var line in lines)
            {
                if (line.Contains("FINAL LABELS:"))
                {
                    inFinalLabelsSection = true;
                    continue;
                }
                
                if (inFinalLabelsSection && !string.IsNullOrWhiteSpace(line))
                {
                    // Add the label, removing any leading special chars like -, *, etc.
                    string label = line.TrimStart('-', '*', ' ', '\t').Trim();
                    if (!string.IsNullOrEmpty(label))
                    {
                        labels.Add(label);
                    }
                }
            }
            
            return labels;
        }
    }
}