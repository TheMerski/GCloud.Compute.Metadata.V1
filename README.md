# Compute API

This is a utility library for communicating with Google Cloud metadata service on Google Cloud. Based on the [Go implementation](https://pkg.go.dev/cloud.google.com/go/compute/metadata#section-readme)

## using the package

You can use the package in the following way:

```aspx-csharp
  using var metadata = new MetadataClient();
  var onGce = await metadata.IsOnGCEAsync(context.CancellationToken);
```
