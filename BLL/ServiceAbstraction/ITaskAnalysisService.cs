using Shared.DTOS.AI;

namespace BLL.ServiceAbstraction
{
    /// <summary>
    /// Service for analyzing individual tasks using AI
    /// </summary>
    public interface ITaskAnalysisService
    {
        /// <summary>
        /// Analyzes a single task by comparing manager's original details with employee updates
        /// </summary>
        /// <param name="taskId">ID of the task to analyze</param>
        /// <param name="managerUserId">User ID of the manager requesting the analysis</param>
        /// <returns>Detailed task analysis with completion status and recommendations</returns>
        Task<TaskAnalysisResultDto> AnalyzeTaskAsync(int taskId, string managerUserId);

        /// <summary>
        /// Analyzes multiple tasks in batch
        /// </summary>
        /// <param name="taskIds">List of task IDs to analyze</param>
        /// <param name="managerUserId">User ID of the manager requesting the analysis</param>
        /// <returns>List of task analyses</returns>
        Task<List<TaskAnalysisResultDto>> AnalyzeBatchTasksAsync(List<int> taskIds, string managerUserId);
    }
}
