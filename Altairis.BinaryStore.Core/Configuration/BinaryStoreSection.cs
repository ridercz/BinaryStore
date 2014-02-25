using System.Configuration;

namespace Altairis.BinaryStore.Configuration {

    public class BinaryStoreSection : ConfigurationSection {

        [ConfigurationProperty("defaultProvider")]
        public DefaultProviderElement DefaultProvider {
            get { return (DefaultProviderElement)this["defaultProvider"]; }
            set { this["defaultProvider"] = value; }
        }

        [ConfigurationProperty("providers")]
        public ProviderSettingsCollection Providers {
            get { return (ProviderSettingsCollection)this["providers"]; }
            set { this["providers"] = value; }
        }
    }
}