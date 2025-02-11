using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS.AprovalService.Lambda.Model
{
    [DynamoDBTable("ApprovalRequest")]
    public class ApprovalRequest
    {
        /// <summary>
        /// Unique identifier for the approval request.
        /// </summary>
        [DynamoDBHashKey("RequestId")]
        public Guid RequestId { get; set; }

        /// <summary>
        /// The task ID that requires approval.
        /// </summary>
        [DynamoDBProperty("TaskId")]
        public Guid TaskId { get; set; }

        /// <summary>
        /// The requested status change.
        /// </summary>
        [DynamoDBProperty("Status")]
        public string? Status { get; set; }

        /// <summary>
        /// Timestamp of when the request was created.
        /// </summary>
        [DynamoDBProperty("RequestedAt")]
        public DateTime RequestedAt { get; set; }
    }
}
