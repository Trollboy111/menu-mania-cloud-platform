using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using System.Text.Json;

public class PubSubService
{
    private readonly string _projectId;
    private readonly string _topicId;
    private readonly ILogger<PubSubService> _logger;

    public PubSubService(IConfiguration configuration, ILogger<PubSubService> logger)
    {
        _logger = logger;

        _projectId = configuration["GoogleCloud:ProjectId"];
        _topicId = configuration["GoogleCloud:PubSubTopic"];

        if (string.IsNullOrWhiteSpace(_projectId))
        {
            throw new InvalidOperationException("ProjectId is missing.");
        }

        if (string.IsNullOrWhiteSpace(_topicId))
        {
            throw new InvalidOperationException("PubSub topic is missing.");
        }
    }

    public async Task PublishAsync(string restaurantId, string menuId, string imageId, string bucketName, string fileName)
    {
        TopicName topicName = TopicName.FromProjectTopic(_projectId, _topicId);

        PublisherClient publisher = await PublisherClient.CreateAsync(topicName);

        var payload = new
        {
            RestaurantId = restaurantId,
            MenuId = menuId,
            ImageId = imageId,
            BucketName = bucketName,
            FileName = fileName
        };

        string json = JsonSerializer.Serialize(payload);

        ByteString messageData = ByteString.CopyFromUtf8(json);

        string messageId = await publisher.PublishAsync(messageData);

        _logger.LogInformation("Message sent: {MessageId}", messageId);
    }
}