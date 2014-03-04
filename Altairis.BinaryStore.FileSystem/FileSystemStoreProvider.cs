using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Hosting;

namespace Altairis.BinaryStore.FileSystem {

    public class FileSystemStoreProvider : StoreProvider {

        private const int DEFAULT_BUFFER_SIZE = 65536;
        private const string DEFAULT_CONTENT_TYPE = "application/octet-stream";
        private const string TYPE_SUFFIX = ".$type";

        private string folderName;
        private string defaultContentType = DEFAULT_CONTENT_TYPE;
        private int bufferSize = DEFAULT_BUFFER_SIZE;

        #region Initialization and configuration

        public int BufferSize
        {
            get { return bufferSize; }
            set {
                if (value < 1) throw new ConfigurationErrorsException("Buffer size must be positive integer.");
                bufferSize = value;
            }
        }

        public string DefaultContentType {
            get { return defaultContentType; }
            set {
                if (string.IsNullOrWhiteSpace(value)) throw new ConfigurationErrorsException("Invalid default content type.");

                defaultContentType = value; 
            }
        }

        public string FolderName {
            get { return folderName; }
            set {
                if (string.IsNullOrWhiteSpace(value)) throw new ConfigurationErrorsException("Required attribute \"folderName\" not set.");
                if (value.StartsWith("~/")) {
                    // Path is web root relative
                    if (System.Web.HttpContext.Current == null) throw new ConfigurationErrorsException("Can't set folderName to relative path outside of HTTP context.");
                    this.PhysicalFolderName = System.Web.HttpContext.Current.Server.MapPath(value).TrimEnd('\\');
                }
                else {
                    // Path is absolute
                    this.PhysicalFolderName = value.TrimEnd('\\');
                }

                // Try to create the data folder
                Directory.CreateDirectory(this.PhysicalFolderName);

                folderName = value;                
            }
        }

        public string PhysicalFolderName { get; private set; }

        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config) {
            // Perform basic initialization
            base.Initialize(name, config);

            // Get data path
            this.FolderName = config.GetConfigValue("folderName", string.Empty);
            
            // Get other configuration
            this.DefaultContentType = config.GetConfigValue("defaultContentType", DEFAULT_CONTENT_TYPE);
            this.BufferSize = config.GetConfigValue("bufferSize", DEFAULT_BUFFER_SIZE);

            // Throw error on excess attributes
            if (config.Count != 0) throw new ConfigurationErrorsException("Unrecognized configuration attributes found: " + string.Join(", ", config.AllKeys));
        }

        #endregion Initialization and configuration

        public override bool Delete(string name) {
            if (name == null) throw new ArgumentNullException("name");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "name");
            if (!ValidateName(name)) throw new ArgumentException("Invalid name format.", "name");

            var fileName = Path.Combine(this.PhysicalFolderName, name);
            if (!File.Exists(fileName)) return false;

            // Delete file
            File.Delete(fileName);

            // Delete metadata, if any
            var metadataFileName = fileName + TYPE_SUFFIX;
            if (File.Exists(metadataFileName)) File.Delete(metadataFileName);

            // Delete folder, if non-root and empty
            var folderName = Path.GetDirectoryName(fileName);
            if (folderName != this.PhysicalFolderName && !Directory.EnumerateFileSystemEntries(folderName).Any()) {
                Directory.Delete(folderName);
            }

            return true;
        }

        public override bool Exists(string name) {
            if (name == null) throw new ArgumentNullException("name");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "name");
            if (!ValidateName(name)) throw new ArgumentException("Invalid name format.", "name");

            var fileName = Path.Combine(this.PhysicalFolderName, name);
            return File.Exists(fileName);
        }

        public override bool Load(string name, out Stream stream, out string contentType) {
            if (name == null) throw new ArgumentNullException("name");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "name");
            if (!ValidateName(name)) throw new ArgumentException("Invalid name format.", "name");

            // Get full file name
            var fileName = Path.Combine(this.PhysicalFolderName, name);
            if (!File.Exists(fileName)) {
                // File not found
                stream = null;
                contentType = null;
                return false;
            }

            // File found - read it
            stream = File.OpenRead(fileName);
            contentType = this.GetContentType(fileName);
            return true;
        }

        public override bool Load(string name, out byte[] data, out string contentType) {
            if (name == null) throw new ArgumentNullException("name");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "name");
            if (!ValidateName(name)) throw new ArgumentException("Invalid name format.", "name");

            // Get full file name
            var fileName = Path.Combine(this.PhysicalFolderName, name);
            if (!File.Exists(fileName)) {
                // File not found
                data = null;
                contentType = null;
                return false;
            }

            // File found - read it
            data = File.ReadAllBytes(fileName);
            contentType = this.GetContentType(fileName);
            return true;
        }

        public override void Save(string name, Stream stream, string contentType) {
            if (name == null) throw new ArgumentNullException("name");
            if (stream == null) throw new ArgumentNullException("stream");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "name");
            if (!ValidateName(name)) throw new ArgumentException("Invalid name format.", "name");
            if (!ValidateContentType(contentType)) throw new ArgumentException("Invalid content-type format.", "contentType");
            if (!stream.CanRead) throw new InvalidOperationException("The stream does not support reading.");

            // Get full file name
            var fileName = Path.Combine(this.PhysicalFolderName, name);
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            // Write file contents
            using (var f = File.Create(fileName)) {
                stream.CopyTo(f, this.BufferSize);
            }

            // Write content type
            this.SetContentType(fileName, contentType);
        }

        public override void Save(string name, byte[] data, string contentType) {
            if (name == null) throw new ArgumentNullException("name");
            if (data == null) throw new ArgumentNullException("data");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "name");
            if (!ValidateName(name)) throw new ArgumentException("Invalid name format.", "name");
            if (!ValidateContentType(contentType)) throw new ArgumentException("Invalid content-type format.", "contentType");

            // Get full file name
            var fileName = Path.Combine(this.PhysicalFolderName, name);

            // Write file contents
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            File.WriteAllBytes(fileName, data);

            // Write content type
            this.SetContentType(fileName, contentType);
        }

        private string GetContentType(string fileName) {
            if (fileName == null) throw new ArgumentNullException("fileName");
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "fileName");

            var metadataFileName = fileName + TYPE_SUFFIX;

            // Return default type if no metadata file found
            if (!File.Exists(metadataFileName)) return this.DefaultContentType;

            // Return content type from metadata file
            return File.ReadAllText(metadataFileName);
        }

        private void SetContentType(string fileName, string contentType) {
            if (fileName == null) throw new ArgumentNullException("fileName");
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "fileName");
            if (string.IsNullOrWhiteSpace(contentType)) contentType = this.DefaultContentType;

            var metadataFileName = fileName + TYPE_SUFFIX;
            File.WriteAllText(metadataFileName, contentType);
        }
    }
}