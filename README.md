BinaryStore
===========
.NET binary store abstraction over file system and Azure Blob Storage simplifies development of applications, that should run either in Windows Azure, or on premises.

How to install
--------------
The best way to install this library is using [NuGet](http://www.nuget.org). There are three NuGet packages available:

* `Altairis.BinaryStore.Core` is the core library without providers. Most likely you won't need to install it directly, as it's included as dependency for the other two packages.
* `Altairis.BinaryStore.FileSystem` installs core library and file system provider.
* `Altairis.BinaryStore.WindowsAzure` installs core library and Windows Azure Blob Storage provider.

License
-------
This application is licensed under terms of [MIT License](LICENSE.md).
