using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using AWS.TodoServive.Lambda.Model;
using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime.SharedInterfaces;
using Amazon.SQS;
using Amazon.SQS.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWS.TodoServive.Lambda;

public class Function
{
    private static readonly AmazonSQSClient _sqsClient = new AmazonSQSClient();
    private const string QueueUrl = "https://sqs.us-east-2.amazonaws.com/354918394806/TaskApprovalQueue";
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation("TaskTodo Start...");
        try
        {
            AmazonDynamoDBClient client = new AmazonDynamoDBClient();
            DynamoDBContext dBContext = new DynamoDBContext(client);

            if (request.RouteKey.Contains("GET /"))
            {
                request.PathParameters.TryGetValue("TaskId", out var taskId);
                Guid.TryParse(taskId, out var idTask);

                var taskData = await dBContext.LoadAsync<TodoTask>(idTask);

                if (taskData != null)
                {
                    return new APIGatewayHttpApiV2ProxyResponse
                    {
                        Body = JsonSerializer.Serialize(taskData),
                        StatusCode = 200
                    };
                }

                return new APIGatewayHttpApiV2ProxyResponse
                {
                    Body = $"TaskId Not Exists.",
                    StatusCode = 404
                };
            }
            else if (request.RouteKey.Contains("POST /Task"))
            {
                var todoTask = JsonSerializer.Deserialize<TodoTask>(request.Body);

                if (todoTask == null || string.IsNullOrEmpty(todoTask.Title))
                {
                    return new APIGatewayHttpApiV2ProxyResponse
                    {
                        Body = "Invalid request. Title is required.",
                        StatusCode = 400
                    };
                }

                todoTask.Id = Guid.NewGuid();
                await dBContext.SaveAsync(todoTask);

                return new APIGatewayHttpApiV2ProxyResponse
                {
                    Body = JsonSerializer.Serialize(todoTask),
                    StatusCode = 201
                };
            }
            else if (request.RouteKey.Contains("PUT /Task/Update/{TaskId}"))
            {
                request.PathParameters.TryGetValue("TaskId", out var taskId);
                if (Guid.TryParse(taskId, out var idTask))
                {
                    var existingTask = await dBContext.LoadAsync<TodoTask>(idTask);
                    if (existingTask == null)
                    {
                        return new APIGatewayHttpApiV2ProxyResponse
                        {
                            Body = "Task not found.",
                            StatusCode = 404
                        };
                    }

                    var updatedTask = JsonSerializer.Deserialize<TodoTask>(request.Body);
                    if (updatedTask == null)
                    {
                        return new APIGatewayHttpApiV2ProxyResponse
                        {
                            Body = "Task Invalid request.",
                            StatusCode = 400
                        };
                    }

                    updatedTask.Id = idTask;
                    updatedTask.Title = updatedTask.Title == null ? existingTask.Title : updatedTask.Title;
                    updatedTask.Status = updatedTask.Status == null ? existingTask.Status : updatedTask.Status;

                    await dBContext.SaveAsync(updatedTask);

                    return new APIGatewayHttpApiV2ProxyResponse
                    {
                        Body = JsonSerializer.Serialize(existingTask),
                        StatusCode = 202
                    };
                }

                return new APIGatewayHttpApiV2ProxyResponse
                {
                    Body = "Invalid TaskId.",
                    StatusCode = 400
                };
            }
            else if (request.RouteKey.Contains("POST /Approval"))
            {
                var approvalRequest = JsonSerializer.Deserialize<ApprovalRequests>(request.Body);

                if (approvalRequest == null || approvalRequest.TaskId == Guid.Empty)
                {
                    return new APIGatewayHttpApiV2ProxyResponse
                    {
                        Body = "Invalid approval request.",
                        StatusCode = 400
                    };
                }

                var sendMessageRequest = new SendMessageRequest
                {
                    QueueUrl = QueueUrl,
                    MessageBody = JsonSerializer.Serialize(approvalRequest)
                };

                await _sqsClient.SendMessageAsync(sendMessageRequest);

                return new APIGatewayHttpApiV2ProxyResponse
                {
                    Body = "Approval request sent.",
                    StatusCode = 202
                };
            }

            return new APIGatewayHttpApiV2ProxyResponse
            {
                Body = $"No method was executed.",
                StatusCode = 404
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Task Error: {ex.Message}");
            return new APIGatewayHttpApiV2ProxyResponse
            {
                Body = $"Error : {ex.Message}",
                StatusCode = 500
            };
        }

    }
}