using System;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace ThermoRawFileParser.Writer
{
    public class S3Loader
    {
        private readonly string _bucketName;

        // Example creates two objects (for simplicity, we upload same file twice).
        // You specify key names for these objects.
        private static readonly RegionEndpoint bucketRegion = RegionEndpoint.EUWest1;
        private static IAmazonS3 _client;
        private readonly string _s3Url;
        private readonly string _s3AccessKeyId;
        private readonly string _s3SecretAccessKey;


        public S3Loader(string s3url, string s3AccessKeyId, string s3SecretAccessKey, string bucketName)
        {
            this._s3Url = s3url;
            this._s3AccessKeyId = s3AccessKeyId;
            this._s3SecretAccessKey = s3SecretAccessKey;
            this._bucketName = bucketName;
            AWSConfigsS3.UseSignatureVersion4 = false;

            var s3Config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.EUWest2,
                ForcePathStyle = true,
                SignatureVersion = "2",
                ServiceURL = s3url,
                SignatureMethod = SigningAlgorithm.HmacSHA1
            };

            _client = new AmazonS3Client(new BasicAWSCredentials(s3AccessKeyId, s3SecretAccessKey), s3Config);
            this._bucketName = bucketName;

            var buckets = _client.ListObjectsAsync(bucketName);

            if (buckets == null)
                throw new AmazonS3Exception("Connection to AWS url -- " + this._s3Url);
        }

        public bool loadObjectToS3(string filePath, string name, string contentType, string label)
        {
            try
            {
                PutObjectRequest putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = name,
                    ContentType = contentType,
                    FilePath = filePath
                };
                // It is important to put the client creation to the request issue: 
                // https://github.com/aws/aws-sdk-net/issues/856. In addition  
                var s3Config = new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.EUWest2,
                    ForcePathStyle = true,
                    SignatureVersion = "2",
                    ServiceURL = _s3Url,
                    SignatureMethod = SigningAlgorithm.HmacSHA1
                };

                putRequest.Metadata.Add("x-amz-meta-title", label);
                putRequest.Metadata.Add("x-amz-meta-original-file-name", filePath);

                using (_client = new AmazonS3Client(_s3AccessKeyId, _s3SecretAccessKey, s3Config))
                {
                    var response = _client.PutObjectAsync(putRequest).Result;
                }
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine(
                    "Error encountered ***. Message:'{0}' when writing an object"
                    , e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "Unknown encountered on server. Message:'{0}' when writing an object"
                    , e.Message);
            }

            return true;
        }
    }
}