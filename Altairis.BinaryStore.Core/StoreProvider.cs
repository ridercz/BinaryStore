using System.Configuration.Provider;
using System.IO;

namespace Altairis.BinaryStore {

    public abstract class StoreProvider : ProviderBase {
        protected const string CONTENT_TYPE_FORMAT = @"^[a-z-]+/[a-z0-9-+\.]+$";
        protected const string NAME_FORMAT = @"^([a-zA-Z0-9-_\.]+)(/[a-zA-Z0-9-_\.]+)*$";

        public abstract bool Delete(string name);

        public abstract bool Exists(string name);

        public virtual bool Load(string name, out Stream stream) {
            string contentType;
            return this.Load(name, out stream, out contentType);
        }

        public virtual bool Load(string name, out byte[] data) {
            string contentType;
            return this.Load(name, out data, out contentType);
        }

        public abstract bool Load(string name, out Stream stream, out string contentType);

        public abstract bool Load(string name, out byte[] data, out string contentType);

        public virtual void Save(string name, byte[] data) {
            this.Save(name, data, null);
        }

        public virtual void Save(string name, Stream stream) {
            this.Save(name, stream, null);
        }

        public abstract void Save(string name, Stream stream, string contentType);

        public abstract void Save(string name, byte[] data, string contentType);

        protected static bool ValidateContentType(string contentType) {
            if (string.IsNullOrEmpty(contentType)) return true;
            return System.Text.RegularExpressions.Regex.IsMatch(contentType, CONTENT_TYPE_FORMAT);
        }

        protected static bool ValidateName(string name) {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return System.Text.RegularExpressions.Regex.IsMatch(name, NAME_FORMAT);
        }
    }
}