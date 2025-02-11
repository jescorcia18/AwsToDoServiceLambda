using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using AWS.AprovalService.Lambda.Model;
using System.Text.Json;
using Amazon.Lambda.SQSEvents;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWS.AprovalService.Lambda;

public class Function
{
    /// <summary>
    /// SQS function that runs the approval process
    /// </summary>
    /// <param name="sqsEvent"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        DynamoDBContext dBContext = new DynamoDBContext(client);

        foreach (var record in sqsEvent.Records)
        {
            try
            {
                var approvalRequest = JsonSerializer.Deserialize<ApprovalRequest>(record.Body);

                if (approvalRequest != null)
                {
                    approvalRequest.RequestId = Guid.NewGuid();
                    approvalRequest.RequestedAt = DateTime.UtcNow;
                    await dBContext.SaveAsync(approvalRequest);
                    context.Logger.LogInformation($"Approval request saved for TaskId: {approvalRequest.TaskId}");

                    //update the Task table to change the state.
                    var existingTask = await dBContext.LoadAsync<TodoTasks>(approvalRequest.TaskId);
                    var updatedTask = new TodoTasks
                    {
                        Id = existingTask.Id,
                        Title = $"{existingTask.Title} - Approved!",
                        Status = "2" // Status Approved by default
                    };

                    await dBContext.SaveAsync(updatedTask);
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error processing message: {ex.Message}");
            }
        }
    }
}