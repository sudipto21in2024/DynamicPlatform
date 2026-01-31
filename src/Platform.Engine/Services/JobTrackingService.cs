namespace Platform.Engine.Services;

using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.Logging;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;
using Platform.Engine.Interfaces;
using System.Linq.Expressions;

/// <summary>
/// Service for tracking job execution with Elsa 3 workflow integration
/// </summary>
public class JobTrackingService : IJobTrackingService
{
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IRepository<JobInstance> _jobRepository;
    private readonly ILogger<JobTrackingService> _logger;
    
    public JobTrackingService(
        IWorkflowInstanceStore workflowInstanceStore,
        IRepository<JobInstance> jobRepository,
        ILogger<JobTrackingService> logger)
    {
        _workflowInstanceStore = workflowInstanceStore;
        _jobRepository = jobRepository;
        _logger = logger;
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
            var filter = new WorkflowInstanceFilter { Id = job.WorkflowInstanceId };
            var workflowInstance = await _workflowInstanceStore.FindAsync(filter);
            
            if (workflowInstance != null)
            {
                // Update status from workflow
                job.Status = workflowInstance.Status switch
                {
                    WorkflowStatus.Running => JobStatus.Running,
                    WorkflowStatus.Finished => JobStatus.Completed,
                    _ => job.Status
                };

                // In Elsa 3, faulted status is often a sub-state or indicated by IncidentCount
                if (workflowInstance.SubStatus == WorkflowSubStatus.Faulted)
                {
                    job.Status = JobStatus.Failed;
                }
                else if (workflowInstance.SubStatus == WorkflowSubStatus.Cancelled)
                {
                    job.Status = JobStatus.Cancelled;
                }
                
                // Get progress from workflow state
                // Note: WorkflowState is a dictionary in Elsa 3 management entities
                if (workflowInstance.WorkflowState.Properties.TryGetValue("Progress", out var progress))
                {
                    job.Progress = Convert.ToInt32(progress);
                }
                
                if (workflowInstance.WorkflowState.Properties.TryGetValue("RowsProcessed", out var rows))
                {
                    job.RowsProcessed = Convert.ToInt64(rows);
                }
                
                if (workflowInstance.WorkflowState.Properties.TryGetValue("TotalRows", out var total))
                {
                    job.TotalRows = Convert.ToInt64(total);
                }
                
                // Update started time if running
                if (job.Status == JobStatus.Running && !job.StartedAt.HasValue)
                {
                    job.StartedAt = workflowInstance.CreatedAt.UtcDateTime;
                }
                
                // Update completed time if finished
                if ((job.Status == JobStatus.Completed || job.Status == JobStatus.Failed) 
                    && !job.CompletedAt.HasValue)
                {
                    job.CompletedAt = workflowInstance.UpdatedAt.UtcDateTime;
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
            // Note: Detailed workflow cancellation in Elsa 3 usually goes through IWorkflowRuntime
            // For now, we update the job status
            job.Status = JobStatus.Cancelled;
            job.CompletedAt = DateTime.UtcNow;
            await _jobRepository.UpdateAsync(job);
        }
    }
}
