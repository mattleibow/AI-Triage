using Microsoft.SemanticKernel;
using Octokit.GraphQL;
using Octokit.GraphQL.Model;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelDemo.Plugins
{
    /// <summary>
    /// GitHub plugin for Semantic Kernel to access GitHub issues and labels
    /// </summary>
    public class GitHubPlugin
    {
        private readonly Connection _connection;

        /// <summary>
        /// Creates a new instance of the GitHub plugin
        /// </summary>
        /// <param name="connection">GitHub connection instance</param>
        public GitHubPlugin(Connection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Retrieves a list of issues from the GitHub repository
        /// </summary>
        /// <returns>A string containing information about the issues</returns>
        [KernelFunction]
        [Description("Retrieves a list of issues from the GitHub repository")]
        public async Task<string> GetOpenIssues(string owner, string repo, bool includeClosed = false)
        {
            IssueState[] issueStates = includeClosed
                ? [IssueState.Open, IssueState.Closed]
                : [IssueState.Open];

            try
            {
                var query = new Query()
                    .Repository(repo, owner)
                    .Issues(states: issueStates)
                    .AllPages()
                    .Select(issue => new
                    {
                        issue.Number,
                        issue.Title
                    })
                    .Compile();

                var issues = await _connection.Run(query);

                var result = new StringBuilder();
                result.AppendLine($"Found {issues.Count()} issues:");
                
                foreach (var issue in issues)
                {
                    result.AppendLine($"- Issue #{issue.Number}: {issue.Title}");
                }
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                return $"Error fetching issues: {ex.Message}";
            }
        }

        /// <summary>
        /// Retrieves detailed information about a specific GitHub issue by its number
        /// </summary>
        /// <param name="issueNumber">The issue number to retrieve</param>
        /// <returns>A string containing detailed information about the issue</returns>
        [KernelFunction]
        [Description("Retrieves detailed information about a specific GitHub issue by its number")]
        public async Task<string> GetIssueDetails(string owner, string repo, string issueNumber)
        {
            try
            {
                if (!int.TryParse(issueNumber, out var issueNum))
                {
                    return $"Invalid issue number: {issueNumber}";
                }

                var query = new Query()
                    .Repository(repo, owner)
                    .Issue(issueNum)
                    .Select(issue => new
                    {
                        issue.Number,
                        issue.Title,
                        issue.Body,
                        issue.CreatedAt,
                        issue.State,
                        User = issue.Author.Login,
                        Labels = issue.Labels(null, null, null, null, null)
                            .AllPages()
                            .Select(label => label.Name)
                            .ToList()
                    })
                    .Compile();

                var issueDetails = await _connection.Run(query);

                var builder = new StringBuilder();
                builder.AppendLine($"Issue #{issueDetails.Number}: {issueDetails.Title}");
                builder.AppendLine($"Created by: {issueDetails.User} on {issueDetails.CreatedAt}");
                builder.AppendLine($"State: {issueDetails.State}");
                builder.AppendLine($"Current Labels: {string.Join(", ", issueDetails.Labels)}");
                builder.AppendLine();
                builder.AppendLine("Description:");
                builder.AppendLine(issueDetails.Body);
                
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
        [KernelFunction]
        [Description("Retrieves all labels defined in the GitHub repository")]
        public async Task<string> GetRepositoryLabels(string owner, string repo)
        {
            try
            {
                var query = new Query()
                    .Repository(repo, owner)
                    .Labels()
                    .AllPages()
                    .Select(label => new
                    {
                        label.Name,
                        label.Description
                    })
                    .Compile();

                var labels = await _connection.Run(query);

                var builder = new StringBuilder();
                builder.AppendLine($"Found {labels.Count()} labels:");
                
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
    }
}
