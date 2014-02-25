using System;
using System.Configuration;
using System.Configuration.Provider;
using System.IO;
using Altairis.BinaryStore.Configuration;

namespace Altairis.BinaryStore {

    public static class StoreManager {

        #region Provider infrastructure implementation

        static StoreManager() {
            // Get configuration info
            var config = ConfigurationManager.GetSection("altairis.binaryStore") as BinaryStoreSection;
            if (config == null || config.Providers == null || config.Providers.Count == 0) throw new ProviderException("No Store providers specified.");

            // Instantiate the providers
            Providers = new StoreProviderCollection();
            foreach (ProviderSettings settings in config.Providers) {
                Providers.Add(InstantiateProvider(settings));
            }
            Providers.SetReadOnly();

            // Get default provider
            var defaultProviderName = config.DefaultProvider.Name;
            if (string.IsNullOrWhiteSpace(defaultProviderName)) throw new ProviderException("No default Store provider specified.");
            DefaultProvider = Providers[defaultProviderName];
            if (DefaultProvider == null) throw new ProviderException("Default Store provider was not found.");
        }

        public static StoreProvider DefaultProvider { get; private set; }

        public static StoreProviderCollection Providers { get; private set; }

        private static StoreProvider InstantiateProvider(ProviderSettings settings) {
            try {
                // Get type and assembly name
                var providerType = Type.GetType(settings.Type);

                // Create instance of provider class
                var providerClass = Activator.CreateInstance(providerType) as StoreProvider;
                if (providerClass == null) throw new ConfigurationErrorsException(string.Format("The {0} type is not StoreProvider.", settings.Type), settings.ElementInformation.Properties["type"].Source, settings.ElementInformation.Properties["type"].LineNumber);
                providerClass.Initialize(settings.Name, settings.Parameters);
                return providerClass;
            }
            catch (Exception ex) {
                if (ex is ConfigurationException) throw;
                throw new ConfigurationErrorsException(ex.Message, settings.ElementInformation.Properties["type"].Source, settings.ElementInformation.Properties["type"].LineNumber);
            }
        }

        #endregion Provider infrastructure implementation

        public static bool Delete(string name) {
            return DefaultProvider.Delete(name);
        }

        public static bool Exists(string name) {
            return DefaultProvider.Exists(name);
        }

        public static bool Load(string name, out Stream stream) {
            return DefaultProvider.Load(name, out stream);
        }

        public static bool Load(string name, out byte[] data) {
            return DefaultProvider.Load(name, out data);
        }

        public static bool Load(string name, out Stream stream, out string contentType) {
            return DefaultProvider.Load(name, out stream, out contentType);
        }

        public static bool Load(string name, out byte[] data, out string contentType) {
            return DefaultProvider.Load(name, out data, out contentType);
        }

        public static void Save(string name, byte[] data) {
            DefaultProvider.Save(name, data);
        }

        public static void Save(string name, Stream stream) {
            DefaultProvider.Save(name, stream);
        }

        public static void Save(string name, Stream stream, string contentType) {
            DefaultProvider.Save(name, stream, contentType);
        }

        public static void Save(string name, byte[] data, string contentType) {
            DefaultProvider.Save(name, data, contentType);
        }
    }
}