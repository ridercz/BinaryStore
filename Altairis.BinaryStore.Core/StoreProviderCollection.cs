using System;
using System.Configuration.Provider;

namespace Altairis.BinaryStore {

    public class StoreProviderCollection : ProviderCollection {

        new public StoreProvider this[string name] {
            get { return (StoreProvider)base[name]; }
        }

        public override void Add(ProviderBase provider) {
            if (provider == null) throw new ArgumentNullException("The provider parameter cannot be null.");
            if (!(provider is StoreProvider)) throw new ArgumentException("The provider parameter must be of type StoreProvider.");
            base.Add(provider);
        }
    }
}