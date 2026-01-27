namespace Platform.Engine.Services;

using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.DataExecution;

/// <summary>
/// Service for tracking job execution with Elsa workflow integration
/// </summary>
public class JobTrackingService : IJobTrackingService
{
    private readonly IWorkflowInstanceStore _workflowStore;
    private readonly IRepository<JobInstance> _jobRepository;
    private readonly IWorkflowRegistry _workflowRegistry;
    
    public JobTrackingService(
        IWorkflowInstanceStore workflowStore,
        IRepository<JobInstance> jobRepository,
        IWorkflowRegistry workflowRegistry)
    {
        _workflowStore = workflowStore;
        _jobRepository = jobRepository;
        _workflowRegistry = workflowRegistry;
    }
    
    public async Task<JobInstance> CreateJobAsync(JobInstance job)
    {
        job.Id = Guid.NewGuid();
        job.CreatedAt = DateTime.UtcNow;
        job.Status = JobStatus.Queued;
        
        await _jobRepository.AddAsync(job);
        return job;
    }
    
    public async Task<JobInstance?> GetJobAsync(string jobId)
    {
        var job = await _jobRepository.FirstOrDefaultAsync(j => j.JobId == jobId);
        if (job == null) return null;
        
        // Get workflow instance for current status
        if (!string.IsNullOrEmpty(job.WorkflowInstanceId))
        {
            var workflowInstance = await _workflowStore.FindByIdAsync(job.WorkflowInstanceId);
            if (workflowInstance != null)
            {
                // Update status from workflow
                job.Status = workflowInstance.WorkflowStatus switch
                {
                    WorkflowStatus.Idle => JobStatus.Queued,
                    WorkflowStatus.Running => JobStatus.Running,
                    WorkflowStatus.Finished => JobStatus.Completed,
                    WorkflowStatus.Faulted => JobStatus.Failed,
                    WorkflowStatus.Cancelled => JobStatus.Cancelled,
                    WorkflowStatus.Suspended => JobStatus.Running,
                    _ => job.Status
                };
                
                // Get progress from workflow variables
                if (workflowInstance.Variables.TryGetValue("Progress", out var progress))
                {
                    job.Progress = Convert.ToInt32(progress);
                }
                
                if (workflowInstance.Variables.TryGetValue("RowsProcessed", out var rows))
                {
                    job.RowsProcessed = Convert.ToInt64(rows);
                }
                
                if (workflowInstance.Variables.TryGetValue("TotalRows", out var total))
                {
                    job.TotalRows = Convert.ToInt64(total);
                }
                
                // Update started time if running
                if (job.Status == JobStatus.Running && !job.StartedAt.HasValue)
                {
                    job.StartedAt = workflowInstance.CreatedAt;
                }
                
                // Update completed time if finished
                if ((job.Status == JobStatus.Completed || job.Status == JobStatus.Failed) 
                    && !job.CompletedAt.HasValue)
                {
                    job.CompletedAt = workflowInstance.FinishedAt ?? DateTime.UtcNow;
                }
                
                // Save updated status
                await _jobRepository.UpdateAsync(job);
            }
        }
        
        return job;
    }
    
    public async Task UpdateProgressAsync(string jobId, int progress, long rowsProcessed)
    {
        var job = await _jobRepository.FirstOrDefaultAsync(j => j.JobId == jobId);
        if (job != null)
        {
            job.Progress = progress;
            job.RowsProcessed = rowsProcessed;
            job.Status = JobStatus.Running;
            
            if (!job.StartedAt.HasValue)
            {
                job.StartedAt = DateTime.UtcNow;
            }
            
            // Estimate completion time based on progress
            if (progress > 0 && job.StartedAt.HasValue)
            {
                var elapsed = DateTime.UtcNow - job.StartedAt.Value;
                var totalEstimated = elapsed.TotalSeconds * (100.0 / progress);
                job.EstimatedCompletion = job.StartedAt.Value.AddSeconds(totalEstimated);
            }
            
            await _jobRepository.UpdateAsync(job);
        }
    }
    
    public async Task CompleteJobAsync(string jobId, string downloadUrl)
    {
        var job = await _jobRepository.FirstOrDefaultAsync(j => j.JobId == jobId);
        if (job != null)
        {
            job.Status = JobStatus.Completed;
            job.Progress = 100;
            job.CompletedAt = DateTime.UtcNow;
            job.DownloadUrl = downloadUrl;
            
            await _jobRepository.UpdateAsync(job);
        }
    }
    
    public async Task FailJobAsync(string jobId, string errorMessage)
    {
        var job = await _jobRepository.FirstOrDefaultAsync(j => j.JobId == jobId);
        if (job != null)
        {
            job.Status = JobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = errorMessage;
            
            await _jobRepository.UpdateAsync(job);
        }
    }
    
    public async Task<List<JobInstance>> GetUserJobsAsync(string userId, int skip = 0, int take = 20)
    {
        var jobs = await _jobRepository.GetAllAsync(
            filter: j => j.UserId == userId,
            orderBy: q => q.OrderByDescending(j => j.CreatedAt),
            skip: skip,
            take: take
        );
        
        return jobs.ToList();
    }
    
    public async Task CancelJobAsync(string jobId)
    {
        var job = await _jobRepository.FirstOrDefaultAsync(j => j.JobId == jobId);
        if (job != null && !string.IsNullOrEmpty(job.WorkflowInstanceId))
        {
            // Cancel the workflow instance
            var workflowInstance = await _workflowStore.FindByIdAsync(job.WorkflowInstanceId);
            if (workflowInstance != null && workflowInstance.WorkflowStatus == WorkflowStatus.Running)
            {
                workflowInstance.WorkflowStatus = WorkflowStatus.Cancelled;
                await _workflowStore.SaveAsync(workflowInstance);
            }
            
            // Update job status
            job.Status = JobStatus.Cancelled;
            job.CompletedAt = DateTime.UtcNow;
            await _jobRepository.UpdateAsync(job);
        }
    }
}

/// <summary>
/// Generic repository interface
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<T?> GetByIdAsync(Guid id);
    Task<T?> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> GetAllAsync(
        System.Linq.Expressions.Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int? skip = null,
        int? take = null);
    Task DeleteAsync(T entity);
}
