using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS.AprovalService.Lambda.Model
{
    [DynamoDBTable("TodoTask")]
    public class TodoTasks
    {
        [DynamoDBHashKey("Id")]
        public Guid? Id { get; set; }   // Unique identifier

        [DynamoDBProperty("Title")]  // Clave de ordenación correcta
        public string? Title { get; set; }  // Task title

        [DynamoDBProperty("Status")]
        public string? Status { get; set; }  // Status: "Todo", "Doing", "Done"
    }
}
