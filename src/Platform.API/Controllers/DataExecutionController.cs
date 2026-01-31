using Microsoft.AspNetCore.Mvc;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.DataExecution;
using Platform.Engine.Services.DataExecution;
using Platform.Engine.Services;
using Platform.Core.Domain.Entities;

namespace Platform.API.Controllers;

[ApiController]
[Route("api/data")]
public class DataExecutionController : ControllerBase
{
    private readonly DataExecutionEngine _executionEngine;
    private readonly IJobTrackingService _jobTracking;
    private readonly ILogger<DataExecutionController> _logger;

    public DataExecutionController(
        DataExecutionEngine executionEngine,
        IJobTrackingService jobTracking,
        ILogger<DataExecutionController> logger)
    {
        _executionEngine = executionEngine;
        _jobTracking = jobTracking;
        _logger = logger;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] DataExecutionRequest request)
    {
        try
        {
            var context = new Platform.Engine.Models.DataExecution.ExecutionContext
            {
                UserId = request.UserId ?? "anonymous",
                TenantId = "default"
            };

            if (request.ExecutionMode == "LongRunning")
            {
                var jobId = await _executionEngine.QueueLongRunningJobAsync(
                    request.ProviderType,
                    request.Metadata,
                    request.Parameters ?? new Dictionary<string, object>(),
                    context,
                    request.OutputFormat ?? "Excel",
                    request.ReportTitle
                );

                return Ok(new { JobId = jobId, Status = "Queued" });
            }
            else
            {
                var result = await _executionEngine.ExecuteQuickJobAsync(
                    request.ProviderType,
                    request.Metadata,
                    request.Parameters ?? new Dictionary<string, object>(),
                    context,
                    request.OutputFormat ?? "JSON"
                );

                return Ok(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing data operation");
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("jobs/user/{userId}")]
    public async Task<IActionResult> GetUserJobs(string userId)
    {
        var jobs = await _jobTracking.GetUserJobsAsync(userId);
        return Ok(jobs);
    }

    [HttpGet("jobs/{jobId}/status")]
    public async Task<IActionResult> GetJobStatus(string jobId)
    {
        var job = await _jobTracking.GetJobAsync(jobId);
        if (job == null) return NotFound();
        return Ok(job);
    }
}

public class DataExecutionRequest
{
    public string ProviderType { get; set; } = "Entity";
    public DataOperationMetadata Metadata { get; set; } = default!;
    public Dictionary<string, object>? Parameters { get; set; }
    public string ExecutionMode { get; set; } = "Quick"; // Quick or LongRunning
    public string? OutputFormat { get; set; }
    public string? ReportTitle { get; set; }
    public string? UserId { get; set; }
}
