# Stonehenge 8
An open source .NET Framework to use Web UI technologies for desktop and/or web applications.

See a (very) short [getting started introduction here](docs/GettingStarted.md).

## Version 8
This version is based on .NET 8 but still compatible with .NET 6. 

**Attention:** Microsoft.NET.Sdk.Web is required!

With this version the SimpleHost is removed. Only Kestrel self and IIS hosting is supported.
Also Newtonsoft.JSON is removed, using NET's own JSON serializer.


Used technology

* [Kestrel](https://docs.microsoft.com/de-de/aspnet/core/fundamentals/servers/kestrel) - the Microsoft netcore web stack for self hosting
* [Vue.js 2](https://vuejs.org/) client framework (bootstrap-vue currently not support Vue 3)
* [Bootstrap 5](https://getbootstrap.com/) front-end open source toolkit
* [Fontawesome 6](https://fontawesome.com/) icon library

Read the release history: [ReleaseNotes](ReleaseNotes3.md)

## Still there
* v4.x - Net Core 6.0 based
* v3.x - Net Core 3.1 based
* v3.6 - Aurelia client framework (deprecated, included up to v3.6 only)
* V2.x - (deprecated) .NET Full Framework V4.6, Katana, Aurelia
* V1.x - .NET Full Framework V4.6, ServiceStack, Knockout

