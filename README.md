# Compute API

This is a utility library for communicating with [Google Cloud metadata service](https://cloud.google.com/compute/docs/metadata/predefined-metadata-keys) on Google Cloud. Based on the [Go implementation](https://pkg.go.dev/cloud.google.com/go/compute/metadata#section-readme)

## using the package

You can use the package in the following way:

```csharp
  using var metadata = new MetadataClient();
  var onGce = await metadata.IsOnGCEAsync(context.CancellationToken);
```

Alternatively you can also inject the client using DI, the client will cache any responses gotten from the metadata api.

### Errors when not running on GCE

By default the package does not throw any errors if you are not running on GCE and functions called will return null, however you can enable throwing errors by setting `throwIfNotOnGCE` to true when creating the client.

```csharp
using var metadata = new MetadataClient(throwIfNotOnGce: true);
```
