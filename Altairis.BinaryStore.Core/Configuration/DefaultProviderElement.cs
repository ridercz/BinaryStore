using System.Configuration;

namespace Altairis.BinaryStore.Configuration {

    public class DefaultProviderElement : ConfigurationElement {

        [ConfigurationProperty("name", IsRequired = true)]
        public string Name {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }
    }
}