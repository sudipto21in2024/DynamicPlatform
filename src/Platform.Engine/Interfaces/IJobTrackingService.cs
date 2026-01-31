namespace Platform.Engine.Interfaces;

using Platform.Core.Domain.Entities;

/// <summary>
/// Service for tracking job execution status
/// </summary>
public interface IJobTrackingService
{
    /// <summary>
    /// Creates a new job instance
    /// </summary>
    Task<JobInstance> CreateJobAsync(JobInstance job);
    
    /// <summary>
    /// Gets a job by ID with current workflow status
    /// </summary>
    Task<JobInstance?> GetJobAsync(string jobId);
    
    /// <summary>
    /// Updates job progress
    /// </summary>
    Task UpdateProgressAsync(string jobId, int progress, long rowsProcessed);
    
    /// <summary>
    /// Marks job as completed
    /// </summary>
    Task CompleteJobAsync(string jobId, string downloadUrl);
    
    /// <summary>
    /// Marks job as failed
    /// </summary>
    Task FailJobAsync(string jobId, string errorMessage);
    
    /// <summary>
    /// Gets all jobs for a user
    /// </summary>
    Task<List<JobInstance>> GetUserJobsAsync(string userId, int skip = 0, int take = 20);
    
    /// <summary>
    /// Cancels a running job
    /// </summary>
    Task CancelJobAsync(string jobId);
}
