namespace Platform.Engine.Workflows.Definitions;

using Elsa;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;
using Platform.Engine.Workflows.Activities;

/// <summary>
/// Workflow definition for long-running report generation
/// </summary>
public class LongRunningReportWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder builder)
    {
        builder
            .WithDisplayName("Long-Running Report Generation")
            .WithDescription("Executes data query, generates report, uploads to storage, and notifies user")
            .WithVersion(1)
            
            // Step 1: Execute data query with chunked processing
            .StartWith<ExecuteDataQueryActivity>(activity =>
            {
                activity.Set(x => x.Metadata, context => 
                    context.GetVariable<DataOperationMetadata>("Metadata")!);
                activity.Set(x => x.Parameters, context => 
                    context.GetVariable<Dictionary<string, object>>("Parameters") ?? new());
                activity.Set(x => x.Context, context => 
                    context.GetVariable<ExecutionContext>("Context")!);
                activity.Set(x => x.ProviderType, context => 
                    context.GetVariable<string>("ProviderType") ?? "Entity");
                activity.Set(x => x.ChunkSize, context => 
                    context.GetVariable<int>("ChunkSize") > 0 
                        ? context.GetVariable<int>("ChunkSize") 
                        : 1000);
            })
            .WithName("ExecuteQuery")
            
            // Step 2: Generate output file
            .Then<GenerateReportOutputActivity>(activity =>
            {
                activity.Set(x => x.Data, context => 
                    context.GetVariable<List<object>>("ExecuteQuery:ResultData")!);
                activity.Set(x => x.OutputFormat, context => 
                    context.GetVariable<string>("OutputFormat") ?? "Excel");
                activity.Set(x => x.Title, context => 
                    context.GetVariable<string>("ReportTitle"));
                activity.Set(x => x.IncludeHeaders, context => 
                    context.GetVariable<bool>("IncludeHeaders") || true);
            })
            .WithName("GenerateOutput")
            
            // Step 3: Upload to storage
            .Then<UploadToStorageActivity>(activity =>
            {
                activity.Set(x => x.FileStream, context => 
                    context.GetVariable<Stream>("GenerateOutput:OutputFile")!);
                activity.Set(x => x.FileName, context => 
                    context.GetVariable<string>("GenerateOutput:FileName")!);
                activity.Set(x => x.JobId, context => 
                    context.GetVariable<string>("JobId")!);
                activity.Set(x => x.ContainerName, context => 
                    context.GetVariable<string>("ContainerName") ?? "reports");
            })
            .WithName("UploadFile")
            
            // Step 4: Notify user of success
            .Then<NotifyUserActivity>(activity =>
            {
                activity.Set(x => x.UserId, context => 
                    context.GetVariable<string>("UserId")!);
                activity.Set(x => x.JobId, context => 
                    context.GetVariable<string>("JobId")!);
                activity.Set(x => x.DownloadUrl, context => 
                    context.GetVariable<string>("UploadFile:DownloadUrl"));
                activity.Set(x => x.Status, "Completed");
                activity.Set(x => x.ReportTitle, context => 
                    context.GetVariable<string>("ReportTitle"));
                activity.Set(x => x.TotalRows, context => 
                    context.GetVariable<long>("ExecuteQuery:TotalRows"));
            })
            .WithName("NotifySuccess");
        
        // Add fault handler for errors
        builder
            .Add<Fault>(fault =>
            {
                fault
                    .When(OutcomeNames.Fault)
                    .Then<NotifyUserActivity>(activity =>
                    {
                        activity.Set(x => x.UserId, context => 
                            context.GetVariable<string>("UserId")!);
                        activity.Set(x => x.JobId, context => 
                            context.GetVariable<string>("JobId")!);
                        activity.Set(x => x.Status, "Failed");
                        activity.Set(x => x.ErrorMessage, context => 
                            context.GetVariable<string>("Fault:Message") ?? "Unknown error occurred");
                        activity.Set(x => x.ReportTitle, context => 
                            context.GetVariable<string>("ReportTitle"));
                    })
                    .WithName("NotifyFailure");
            });
    }
}
