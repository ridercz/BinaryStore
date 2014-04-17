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

        private string connectionStringName;
        private string containerName = DEFAULT_CONTAINER_NAME;
        private string defaultContentType = DEFAULT_CONTENT_TYPE;

        #region Initialization and configuration

        protected CloudBlobContainer Container {
            get {
                EnsureStorageClientReady();
                return container;
            }
        }

        public string ConnectionString { get; private set; }

        public string ConnectionStringName {
            get { return connectionStringName; }
            set {
                ValidateStorageClientNotUsed();
                if (string.IsNullOrWhiteSpace(value)) throw new ProviderException("Connection string name cannot be null or empty.");

                ConnectionStringSettings connectionStringSettings = ConfigurationManager.ConnectionStrings[value];
                if (connectionStringSettings == null || string.IsNullOrWhiteSpace(connectionStringSettings.ConnectionString)) {
                    throw new ProviderException("Connection string cannot be blank.");
                }

                connectionStringName = value;
                this.ConnectionString = connectionStringSettings.ConnectionString;
            }
        }

        public string ContainerName {
            get { return containerName; }
            set {
                ValidateStorageClientNotUsed();
                if (string.IsNullOrWhiteSpace(value)) throw new ConfigurationErrorsException("Invalid container name.");
                containerName = value;
            }
        }

        public string DefaultContentType {
            get { return defaultContentType; }
            set {
                ValidateStorageClientNotUsed();
                if (string.IsNullOrWhiteSpace(value)) throw new ConfigurationErrorsException("Invalid default content type.");
                defaultContentType = value;
            }
        }

        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config) {
            // Perform basic initialization
            base.Initialize(name, config);

            // Initialize connection string
            this.ConnectionStringName = config.GetConfigValue("connectionStringName", null);

            // Get other configuration
            this.ContainerName = config.GetConfigValue("containerName", DEFAULT_CONTAINER_NAME);
            this.DefaultContentType = config.GetConfigValue("defaultContentType", DEFAULT_CONTENT_TYPE);

            // Throw error on excess attributes
            if (config.Count != 0) throw new ConfigurationErrorsException("Unrecognized configuration attributes found: " + string.Join(", ", config.AllKeys));
        }

        private void EnsureStorageClientReady() {
            if (this.container != null) return;

            CloudStorageAccount account;
            var result = CloudStorageAccount.TryParse(this.ConnectionString, out account);
            if (!result) {
                throw new ProviderException("Invalid storage connection string.");
            }
            var client = account.CreateCloudBlobClient();
            this.container = client.GetContainerReference(this.ContainerName);
            this.container.CreateIfNotExists();
        }

        protected virtual void ValidateStorageClientNotUsed() {
            if (this.container != null) {
                throw new InvalidOperationException("The property value cannot be changed because this instance has already been used.");
            }
        }

        #endregion Initialization and configuration

        public override bool Delete(string name) {
            if (name == null) throw new ArgumentNullException("name");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "name");
            if (!ValidateName(name)) throw new ArgumentException("Invalid name format.", "name");

            var blob = this.Container.GetBlobReferenceFromServer(name);
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

            try {
                var blob = this.Container.GetBlobReferenceFromServer(name);
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

            try {
                var blob = this.Container.GetBlobReferenceFromServer(name);
                stream = new MemoryStream();
                blob.DownloadToStream(stream);
                stream.Position = 0;
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

            try {
                var blob = this.Container.GetBlobReferenceFromServer(name);
                using (var ms = new MemoryStream()) {
                    blob.DownloadToStream(ms);
                    data = ms.ToArray();
                }
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
            if (string.IsNullOrWhiteSpace(contentType)) contentType = DefaultContentType;

            // Upload blob
            var blob = GetOrCreateBlob(name);
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
            if (string.IsNullOrWhiteSpace(contentType)) contentType = DefaultContentType;

            // Upload blob
            var blob = GetOrCreateBlob(name);
            blob.Properties.ContentType = contentType;
            blob.UploadFromByteArray(data, 0, data.Length);
        }

        private ICloudBlob GetOrCreateBlob(string name) {
            ICloudBlob blob;
            try {
                blob = this.Container.GetBlobReferenceFromServer(name);
            }
            catch (StorageException sex) {
                if (sex.RequestInformation.HttpStatusCode == 404) {
                    blob = this.Container.GetBlockBlobReference(name);
                }
                else {
                    throw;
                }
            }
            return blob;
        }
    }
}