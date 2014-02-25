using System;
using System.Configuration;
using System.Configuration.Provider;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Altairis.BinaryStore.WindowsAzure {

    public class BlobStoreProvider : Altairis.BinaryStore.StoreProvider {
        private const string DEFAULT_CONTAINER_NAME = "blob-store-provider";
        private const string DEFAULT_CONTENT_TYPE = "application/octet-stream";
        private CloudBlobContainer container;

        #region Initialization and configuration

        public int BufferSize { get; private set; }

        public string ConnectionString { get; private set; }

        public string ConnectionStringName { get; private set; }

        public string ContainerName { get; private set; }

        public string DefaultContentType { get; private set; }

        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config) {
            // Perform basic initialization
            base.Initialize(name, config);

            // Initialize connection string
            this.ConnectionStringName = config.GetConfigValue("connectionStringName", null);
            if (string.IsNullOrWhiteSpace(this.ConnectionStringName)) throw new ProviderException("Connection string name cannot be null or empty.");
            ConnectionStringSettings ConnectionStringSettings = ConfigurationManager.ConnectionStrings[this.ConnectionStringName];
            if (ConnectionStringSettings == null || ConnectionStringSettings.ConnectionString.Trim() == "") throw new ProviderException("Connection string cannot be blank.");
            this.ConnectionString = ConnectionStringSettings.ConnectionString;

            // Get other configuration
            this.ContainerName = config.GetConfigValue("ContainerName", DEFAULT_CONTAINER_NAME);
            if (string.IsNullOrWhiteSpace(this.ContainerName)) throw new ConfigurationErrorsException("Invalid container name");
            this.DefaultContentType = config.GetConfigValue("defaultContentType", DEFAULT_CONTENT_TYPE);
            if (string.IsNullOrWhiteSpace(this.DefaultContentType)) throw new ConfigurationErrorsException("Invalid default content type");

            // Throw error on excess attributes
            if (config.Count != 0) throw new ConfigurationErrorsException("Unrecognized configuration attributes found: " + string.Join(", ", config.AllKeys));

            // Initialize storage and create container
            CloudStorageAccount account;
            var result = CloudStorageAccount.TryParse(this.ConnectionString, out account);
            if (!result) throw new ProviderException("Invalid storage connection string");
            var client = account.CreateCloudBlobClient();
            this.container = client.GetContainerReference(this.ContainerName);
            this.container.CreateIfNotExists();
        }

        #endregion Initialization and configuration

        public override bool Delete(string name) {
            if (name == null) throw new ArgumentNullException("name");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "name");
            if (!ValidateName(name)) throw new ArgumentException("Invalid name format.", "name");

            var blob = this.container.GetBlobReferenceFromServer(name);
            try {
                blob.Delete();
            }
            catch (StorageException sex) {
                if (sex.RequestInformation.HttpStatusCode == 404) return false;
                throw;
            }
            return true;
        }

        public override bool Exists(string name) {
            if (name == null) throw new ArgumentNullException("name");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "name");
            if (!ValidateName(name)) throw new ArgumentException("Invalid name format.", "name");

            var blob = this.container.GetBlobReferenceFromServer(name);
            try {
                blob.FetchAttributes();
            }
            catch (StorageException sex) {
                if (sex.RequestInformation.HttpStatusCode == 404) return false;
                throw;
            }
            return true;
        }

        public override bool Load(string name, out System.IO.Stream stream, out string contentType) {
            if (name == null) throw new ArgumentNullException("name");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "name");
            if (!ValidateName(name)) throw new ArgumentException("Invalid name format.", "name");

            var blob = this.container.GetBlobReferenceFromServer(name);
            try {
                stream = new MemoryStream();
                blob.DownloadToStream(stream);
                contentType = blob.Properties.ContentType;
            }
            catch (StorageException sex) {
                if (sex.RequestInformation.HttpStatusCode == 404) {
                    stream = null;
                    contentType = null;
                    return false;
                }
                throw;
            }
            return true;
        }

        public override bool Load(string name, out byte[] data, out string contentType) {
            if (name == null) throw new ArgumentNullException("name");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "name");
            if (!ValidateName(name)) throw new ArgumentException("Invalid name format.", "name");

            var blob = this.container.GetBlobReferenceFromServer(name);
            try {
                data = new byte[0];
                blob.DownloadToByteArray(data, 0);
                contentType = blob.Properties.ContentType;
            }
            catch (StorageException sex) {
                if (sex.RequestInformation.HttpStatusCode == 404) {
                    data = null;
                    contentType = null;
                    return false;
                }
                throw;
            }
            return true;
        }

        public override void Save(string name, System.IO.Stream stream, string contentType) {
            if (name == null) throw new ArgumentNullException("name");
            if (stream == null) throw new ArgumentNullException("stream");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "name");
            if (!ValidateName(name)) throw new ArgumentException("Invalid name format.", "name");
            if (!ValidateContentType(contentType)) throw new ArgumentException("Invalid content-type format.", "contentType");
            if (!stream.CanRead) throw new InvalidOperationException("The stream does not support reading.");

            // Set default content type if none specified
            if (string.IsNullOrWhiteSpace(contentType)) contentType = DEFAULT_CONTENT_TYPE;

            // Upload blob
            var blob = this.container.GetBlobReferenceFromServer(name);
            blob.Properties.ContentType = contentType;
            blob.UploadFromStream(stream);
        }

        public override void Save(string name, byte[] data, string contentType) {
            if (name == null) throw new ArgumentNullException("name");
            if (data == null) throw new ArgumentNullException("data");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "name");
            if (!ValidateName(name)) throw new ArgumentException("Invalid name format.", "name");
            if (!ValidateContentType(contentType)) throw new ArgumentException("Invalid content-type format.", "contentType");

            // Set default content type if none specified
            if (string.IsNullOrWhiteSpace(contentType)) contentType = DEFAULT_CONTENT_TYPE;

            // Upload blob
            var blob = this.container.GetBlobReferenceFromServer(name);
            blob.Properties.ContentType = contentType;
            blob.UploadFromByteArray(data, 0, data.Length);
        }
    }
}