using Microsoft.SemanticKernel;
using Octokit;
using System.ComponentModel;
using System.Text;

namespace SemanticKernelDemo.Plugins
{
    /// <summary>
    /// GitHub plugin for Semantic Kernel to access GitHub issues and labels
    /// </summary>
    public class GitHubPlugin
    {
        private readonly GitHubClient _client;
        private readonly string _owner;
        private readonly string _repo;

        /// <summary>
        /// Creates a new instance of the GitHub plugin
        /// </summary>
        /// <param name="client">GitHub client instance</param>
        /// <param name="owner">Repository owner</param>
        /// <param name="repo">Repository name</param>
        public GitHubPlugin(GitHubClient client, string owner, string repo)
        {
            _client = client;
            _owner = owner;
            _repo = repo;
        }

        /// <summary>
        /// Retrieves a list of open issues from the GitHub repository
        /// </summary>
        /// <param name="count">Maximum number of issues to retrieve</param>
        /// <returns>A string containing information about the open issues</returns>
        [KernelFunction, Description("Retrieves a list of open issues from the GitHub repository")]
        public string GetOpenIssues(int count = 5)
        {
            try
            {
                var issues = _client.Issue.GetAllForRepository(_owner, _repo, new RepositoryIssueRequest
                {
                    State = ItemStateFilter.Open
                }).Result.Take(count);

                var result = new StringBuilder();
                result.AppendLine($"Found {issues.Count()} open issues:");
                
                foreach (var issue in issues)
                {
                    result.AppendLine($"- Issue #{issue.Number}: {issue.Title}");
                }
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                return $"Error fetching open issues: {ex.Message}";
            }
        }

        /// <summary>
        /// Retrieves detailed information about a specific GitHub issue by its number
        /// </summary>
        /// <param name="issueNumber">The issue number to retrieve</param>
        /// <returns>A string containing detailed information about the issue</returns>
        [KernelFunction, Description("Retrieves detailed information about a specific GitHub issue by its number")]
        public string GetIssueDetails(string issueNumber)
        {
            try
            {
                if (!int.TryParse(issueNumber, out var issueNum))
                {
                    return $"Invalid issue number: {issueNumber}";
                }

                var issue = _client.Issue.Get(_owner, _repo, issueNum).Result;
                
                var builder = new StringBuilder();
                builder.AppendLine($"Issue #{issue.Number}: {issue.Title}");
                builder.AppendLine($"Created by: {issue.User.Login} on {issue.CreatedAt}");
                builder.AppendLine($"State: {issue.State}");
                builder.AppendLine($"Current Labels: {string.Join(", ", issue.Labels.Select(l => l.Name))}");
                builder.AppendLine();
                builder.AppendLine("Description:");
                builder.AppendLine(issue.Body);
                
                return builder.ToString();
            }
            catch (Exception ex)
            {
                return $"Error fetching issue #{issueNumber}: {ex.Message}";
            }
        }

        /// <summary>
        /// Retrieves all labels defined in the GitHub repository
        /// </summary>
        /// <returns>A string containing information about all repository labels</returns>
        [KernelFunction, Description("Retrieves all labels defined in the GitHub repository")]
        public string GetRepositoryLabels()
        {
            try
            {
                var labels = _client.Issue.Labels.GetAllForRepository(_owner, _repo).Result;
                
                var builder = new StringBuilder();
                builder.AppendLine($"Found {labels.Count} labels:");
                
                foreach (var label in labels)
                {
                    builder.AppendLine($"- {label.Name}: {label.Description}");
                }
                
                return builder.ToString();
            }
            catch (Exception ex)
            {
                return $"Error fetching repository labels: {ex.Message}";
            }
        }

        /// <summary>
        /// Analyzes issue content and suggests the most appropriate labels from the available set
        /// </summary>
        /// <param name="issueContent">The content of the issue to analyze</param>
        /// <param name="availableLabels">List of available labels to match against</param>
        /// <returns>A string containing recommended labels with confidence scores</returns>
        [KernelFunction, Description("Analyzes issue content and suggests the most appropriate labels from the available set")]
        public string MatchLabelsToIssue(string issueContent, List<string> availableLabels)
        {
            // This would normally use embeddings or other semantic matching logic
            // For now, we'll just use a simple keyword matching approach
            var results = new Dictionary<string, double>();
            
            foreach (var label in availableLabels)
            {
                // Simple word matching - in a real application, you would use embeddings for semantic matching
                var labelWords = label.ToLower().Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                var matchScore = labelWords.Count(word => issueContent.ToLower().Contains(word)) / (double)labelWords.Length;
                
                if (matchScore > 0)
                {
                    results[label] = matchScore;
                }
            }
            
            // Sort by score
            var recommendations = results
                .OrderByDescending(r => r.Value)
                .Take(3)
                .Select(r => $"{r.Key} (confidence: {r.Value:P2})")
                .ToList();
            
            return recommendations.Count > 0 
                ? $"Recommended labels:\n{string.Join("\n", recommendations)}" 
                : "No matching labels found for this issue.";
        }

        /// <summary>
        /// Assigns specified labels to a GitHub issue by its number
        /// </summary>
        /// <param name="issueNumber">The issue number to update</param>
        /// <param name="labelNames">List of label names to apply</param>
        /// <returns>A string indicating success or failure</returns>
        [KernelFunction, Description("Assigns specified labels to a GitHub issue by its number")]
        public string AssignLabelsToIssue(string issueNumber, List<string> labelNames)
        {
            try
            {
                if (!int.TryParse(issueNumber, out var issueNum))
                {
                    return $"Invalid issue number: {issueNumber}";
                }

                var update = new IssueUpdate();
                foreach (var label in labelNames)
                {
                    update.AddLabel(label);
                }

                var updatedIssue = _client.Issue.Update(_owner, _repo, issueNum, update).Result;
                
                return $"Successfully applied labels to issue #{issueNumber}: {string.Join(", ", labelNames)}";
            }
            catch (Exception ex)
            {
                return $"Error assigning labels to issue #{issueNumber}: {ex.Message}";
            }
        }
    }
}