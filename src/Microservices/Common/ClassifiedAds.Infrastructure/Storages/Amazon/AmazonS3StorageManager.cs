using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ClassifiedAds.Infrastructure.Storages.Amazon
{
    public class AmazonS3StorageManager : IFileStorageManager
    {
        private readonly IAmazonS3 _client;
        private readonly string _bucketName;

        public AmazonS3StorageManager(string awsAccessKeyId, string awsSecretAccessKey, string bucketName, string regionEndpoint)
        {
            _client = new AmazonS3Client(awsAccessKeyId, awsSecretAccessKey, RegionEndpoint.GetBySystemName(regionEndpoint));
            _bucketName = bucketName;
        }

        public async Task CreateAsync(IFileEntry fileEntry, Stream stream, CancellationToken cancellationToken = default)
        {
            var fileTransferUtility = new TransferUtility(_client);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = fileEntry.Id.ToString(),
                BucketName = _bucketName,
                CannedACL = S3CannedACL.NoACL,
            };

            await fileTransferUtility.UploadAsync(uploadRequest, cancellationToken);

            fileEntry.FileLocation = fileEntry.Id.ToString();
        }

        public async Task DeleteAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
        {
            await _client.DeleteObjectAsync(_bucketName, fileEntry.FileLocation, cancellationToken);
        }

        public async Task<byte[]> ReadAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = fileEntry.FileLocation,
            };

            using var response = await _client.GetObjectAsync(request, cancellationToken);
            using var responseStream = response.ResponseStream;
            using var reader = new MemoryStream();
            await responseStream.CopyToAsync(reader, cancellationToken);
            return reader.ToArray();
        }
    }
}
