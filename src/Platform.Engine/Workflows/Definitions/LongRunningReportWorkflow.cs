namespace Platform.Engine.Workflows.Definitions;

using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Extensions;
using Platform.Engine.Workflows.Activities;
using Platform.Engine.Models.DataExecution;

/// <summary>
/// Workflow definition for long-running report generation
/// </summary>
public class LongRunningReportWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        // Define variables
        // Define variables
        var metadata = new Variable<DataOperationMetadata>();
        var parameters = new Variable<Dictionary<string, object>>();
        var context = new Variable<ExecutionContext>();
        var providerType = new Variable<string>();
        var chunkSize = new Variable<int>();
        var outputFormat = new Variable<string>();
        var reportTitle = new Variable<string>();
        var includeHeaders = new Variable<bool>();
        var jobId = new Variable<string>();
        var userId = new Variable<string>();
        var containerName = new Variable<string>();
        
        // Output variables from activities
        var queryResultData = new Variable<List<object>>();
        var queryTotalRows = new Variable<long>();
        var outputFileStream = new Variable<Stream>();
        var outputFileName = new Variable<string>();
        var downloadUrl = new Variable<string>();

        builder.Root = new Sequence
        {
            Variables = 
            { 
                metadata, parameters, context, providerType, chunkSize, 
                outputFormat, reportTitle, includeHeaders, jobId, userId, containerName,
                queryResultData, queryTotalRows, outputFileStream, outputFileName, downloadUrl
            },
            Activities =
            {
                // Init variables from inputs
                new Inline(new Action<ActivityExecutionContext>(ctx => 
                {
                    metadata.Set(ctx, ctx.GetWorkflowInput<DataOperationMetadata>("Metadata"));
                    parameters.Set(ctx, ctx.GetWorkflowInput<Dictionary<string, object>>("Parameters"));
                    context.Set(ctx, ctx.GetWorkflowInput<ExecutionContext>("Context"));
                    providerType.Set(ctx, ctx.GetWorkflowInput<string>("ProviderType"));
                    chunkSize.Set(ctx, ctx.GetWorkflowInput<int>("ChunkSize"));
                    outputFormat.Set(ctx, ctx.GetWorkflowInput<string>("OutputFormat"));
                    reportTitle.Set(ctx, ctx.GetWorkflowInput<string>("ReportTitle"));
                    includeHeaders.Set(ctx, ctx.GetWorkflowInput<bool>("IncludeHeaders"));
                    jobId.Set(ctx, ctx.GetWorkflowInput<string>("JobId"));
                    userId.Set(ctx, ctx.GetWorkflowInput<string>("UserId"));
                    containerName.Set(ctx, ctx.GetWorkflowInput<string>("ContainerName"));
                })),

                // Step 1: Execute data query
                new ExecuteDataQueryActivity
                {
                    QueryMetadata = new Input<DataOperationMetadata>(metadata),
                    Parameters = new Input<Dictionary<string, object>>(parameters),
                    Context = new Input<ExecutionContext>(context),
                    ProviderType = new Input<string>(providerType),
                    ChunkSize = new Input<int>(chunkSize),
                    ResultData = new Output<List<object>>(queryResultData),
                    TotalRows = new Output<long>(queryTotalRows)
                },
                
                // Step 2: Generate output file
                new GenerateReportOutputActivity
                {
                    Data = new Input<List<object>>(queryResultData),
                    OutputFormat = new Input<string>(outputFormat),
                    Title = new Input<string?>(reportTitle),
                    IncludeHeaders = new Input<bool>(includeHeaders),
                    OutputFile = new Output<Stream>(outputFileStream),
                    FileName = new Output<string>(outputFileName)
                },
                
                // Step 3: Upload to storage
                new UploadToStorageActivity
                {
                    FileStream = new Input<Stream>(outputFileStream),
                    FileName = new Input<string>(outputFileName),
                    JobId = new Input<string>(jobId),
                    ContainerName = new Input<string>(containerName),
                    DownloadUrl = new Output<string>(downloadUrl)
                },
                
                // Step 4: Notify user of success
                new NotifyUserActivity
                {
                    UserId = new Input<string>(userId),
                    JobId = new Input<string>(jobId),
                    DownloadUrl = new Input<string?>(downloadUrl),
                    Status = new Input<string>("Completed"),
                    ReportTitle = new Input<string?>(reportTitle),
                    TotalRows = new Input<long>(queryTotalRows)
                }
            }
        };
    }
}
