using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.AspNetCore.Mvc;
using ServerlessOrderApi.Models;
using System.Text.Json;

namespace ServerlessOrderApi.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrderApiController : ControllerBase
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IAmazonSQS _sqsClient;
        private readonly string _bucketName;
        private readonly string _sqsQueueUrl;

        public OrderApiController(IAmazonS3 s3Client, IAmazonSQS sqsClient, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _sqsClient = sqsClient;
            _bucketName = configuration["AWS:BucketName"] ?? throw new Exception("AWS:BucketName is missing");
            _sqsQueueUrl = configuration["AWS:SqsQueueUrl"] ?? throw new Exception("AWS:SqsQueueUrl is missing");
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            order.Id = new Random().Next(10000, 99999).ToString();
            order.CreatedDate = DateTime.UtcNow;
            order.Status = "Created";

            var json = JsonSerializer.Serialize(order);

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = $"{order.Id}.json",
                ContentBody = json,
                ContentType = "application/json"
            };
            await _s3Client.PutObjectAsync(putRequest);

            var message = new SendMessageRequest
            {
                QueueUrl = _sqsQueueUrl,
                MessageBody = $"New order created! OrderId: {order.Id}"
            };
            await _sqsClient.SendMessageAsync(message);

            return Created($"/api/orders/{order.Id}", order);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var request = new ListObjectsV2Request { BucketName = _bucketName };
                var response = await _s3Client.ListObjectsV2Async(request);
                var orders = new List<Order>();

                foreach (var s3Object in response.S3Objects)
                {
                    if (s3Object.Key.EndsWith(".json"))
                    {
                        var getRequest = new GetObjectRequest { BucketName = _bucketName, Key = s3Object.Key };
                        using var getResponse = await _s3Client.GetObjectAsync(getRequest);
                        using var reader = new StreamReader(getResponse.ResponseStream);
                        var order = JsonSerializer.Deserialize<Order>(await reader.ReadToEndAsync());
                        if (order != null) orders.Add(order);
                    }
                }

                return Ok(orders.OrderByDescending(o => o.CreatedDate));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Помилка при отриманні списку з S3", Details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(string id)
        {
            try
            {
                var getRequest = new GetObjectRequest { BucketName = _bucketName, Key = $"{id}.json" };
                using var response = await _s3Client.GetObjectAsync(getRequest);
                using var reader = new StreamReader(response.ResponseStream);
                var order = JsonSerializer.Deserialize<Order>(await reader.ReadToEndAsync());
                return Ok(order);
            }
            catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
            {
                return NotFound(new { Message = "Order not found" });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] string newStatus)
        {
            try
            {
                var getRequest = new GetObjectRequest { BucketName = _bucketName, Key = $"{id}.json" };
                using var response = await _s3Client.GetObjectAsync(getRequest);
                using var reader = new StreamReader(response.ResponseStream);
                var order = JsonSerializer.Deserialize<Order>(await reader.ReadToEndAsync());

                if (order == null) return NotFound();

                order.Status = newStatus;

                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = $"{id}.json",
                    ContentBody = JsonSerializer.Serialize(order),
                    ContentType = "application/json"
                };
                await _s3Client.PutObjectAsync(putRequest);

                return Ok(order);
            }
            catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
            {
                return NotFound(new { Message = "Order not found" });
            }
        }
    }
}