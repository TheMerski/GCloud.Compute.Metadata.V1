namespace Q42.Google.Cloud.Compute.Metadata.V1;

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;

public class MetadataClient: IDisposable
{

    #region Go_Client_Constants
    //
    // The following values and comments are based on https://github.com/googleapis/google-cloud-go/blob/e8223c6ee544237b54b351e421b7092dc3b237a6/compute/metadata/metadata.go#L39C1-L48
    //

    // metadataIP is the documented metadata server IP address.
    // ReSharper disable once InconsistentNaming
    private const string metadataIP = "169.254.169.254";
    // metadataHostEnv is the environment variable specifying the
    // GCE metadata hostname.  If empty, the default value of
    // metadataIP ("169.254.169.254") is used instead.
    // This is variable name is not defined by any spec, as far as
    // I know; it was made up for the Go package.
    // ReSharper disable once InconsistentNaming
    private const string metadataHostEnv = "GCE_METADATA_HOST";
    // ReSharper disable once InconsistentNaming
    private const string userAgent = "Q42/google-cloud-compute-metadata-v1";
    #endregion

    private readonly HttpClient client;
    private readonly bool throwIfNotOnGce;
    private readonly string metadataHost;
    private readonly string baseUrl;

    #region Backing fields

    // ReSharper disable once InconsistentNaming
    private bool? onGCE = null;
    private readonly Dictionary<string, string> cachedStrings = new();

    #endregion

    /// <summary>
    /// Initialize a new MetadataClient.
    /// </summary>
    /// <param name="httpClient">An injected HttpClient to use</param>
    /// <param name="throwIfNotOnGce">
    ///     Defines if the functions should throw a NotOnGceException when not on GCE.
    ///     When `false`, the functions will return null when not on GCE.
    ///     When `true`, the functions will throw when not on GCE.
    /// </param>
    public MetadataClient(HttpClient httpClient, bool throwIfNotOnGce = false)
    {
        this.throwIfNotOnGce = throwIfNotOnGce;
        metadataHost = Environment.GetEnvironmentVariable(metadataHostEnv) ?? metadataIP;
        baseUrl = $"http://{metadataHost}/computeMetadata/v1/";

        client = httpClient;
        client.DefaultRequestHeaders.Add("User-Agent", userAgent);
        // Required header per https://cloud.google.com/compute/docs/metadata/overview#parts-of-a-request
        client.DefaultRequestHeaders.Add("Metadata-Flavor", "Google");

        // Timeout after 1 second, because we don't want to wait for the metadata server if we're not on GCE.
        client.Timeout = TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// Initialize a new MetadataClient.
    /// </summary>
    /// <param name="throwIfNotOnGce">
    ///     Defines if the functions should throw when not on GCE.
    ///     When `false`, the functions will return null when not on GCE.
    ///     When `true`, the functions will throw when not on GCE.
    /// </param>
    public MetadataClient(bool throwIfNotOnGce = false) : this(new HttpClient(), throwIfNotOnGce)
    {
    }

    /// <summary>
    /// reports whether this process is running on Google Compute Engine.
    /// </summary>
    public async Task<bool> IsOnGCEAsync(CancellationToken cancellationToken = default)
    {
        if (onGCE.HasValue)
            return onGCE.Value;

        try
        {
            var response = await client.GetAsync($"http://{metadataHost}", cancellationToken);
            onGCE = response.Headers.TryGetValues("Metadata-Flavor", out var values) && values.Contains("Google");
        }
        catch (Exception)
        {
            // If we get an exception, we're can't connect to the metadata server or receive an error, so we are probably not on GCE.
            onGCE = false;
        }

        return onGCE.Value;
    }

    /// <summary>
    /// returns the current instance's project ID string.
    /// </summary>
    public async Task<string?> GetProjectIdAsync(CancellationToken cancellationToken = default)
    {
        return await GetCachedString("project/project-id", cancellationToken);
    }

    /// <summary>
    /// returns the current instance's numeric project ID.
    /// </summary>
    public async Task<string?> GetNumericProjectIdAsync(CancellationToken cancellationToken = default)
    {
        return await GetCachedString("project/numeric-project-id", cancellationToken);
    }

    /// <summary>
    /// returns the current VM's numeric instance ID.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task<string?> GetInstanceIdAsync(CancellationToken cancellationToken = default)
    {
        return await GetCachedString("instance/id", cancellationToken);
    }

    /// <summary>
    /// returns the instance's primary internal IP address.
    /// </summary>
    public async Task<string?> GetInternalIpAsync(CancellationToken cancellationToken = default)
    {
        return await GetTrimmedCachedString("instance/network-interfaces/0/ip", cancellationToken);
    }

    /// <summary>
    /// returns the email address associated with the service account.
    /// The account may be empty or the string "default" to use the instance's main account
    /// </summary>
    /// <param name="serviceAccount">The service account to get the email for, or null/empty for default</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The email address associated with the service account.</returns>
    public async Task<string?> GetEmailAsync(string? serviceAccount = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceAccount))
        {
            serviceAccount = "default";
        }

        return await GetTrimmedCachedString($"instance/service-accounts/{serviceAccount}/email",
            cancellationToken);
    }

    /// <summary>
    /// returns the instance's primary external (public) IP address
    /// </summary>
    public async Task<string?> GetExternalIpAsync(CancellationToken cancellationToken = default)
    {
        return await GetTrimmedCachedString("instance/network-interfaces/0/access-configs/0/external-ip", cancellationToken);
    }

    /// <summary>
    /// returns the instance's hostname. This will be of the form `{instanceID}.c.{projID}.internal`.
    /// </summary>
    public async Task<string?> GetHostnameAsync(CancellationToken cancellationToken = default)
    {
        return await GetTrimmedCachedString("instance/hostname", cancellationToken);
    }

    /// <summary>
    /// returns the list of user-defined instance tags, assigned when initially creating a GCE instance.
    /// </summary>
    public async Task<string[]?> GetInstanceTagsAsync(CancellationToken cancellationToken = default)
    {
        var str = await GetCachedString("instance/tags", cancellationToken);
        return str == null ? null : JsonSerializer.Deserialize<string[]>(str);
    }

    /// <summary>
    /// returns the current VM's instance ID string.
    /// </summary>
    public async Task<string?> GetInstanceNameAsync(CancellationToken cancellationToken = default)
    {
        return await GetTrimmedCachedString("instance/name", cancellationToken);
    }

    /// <summary>
    /// returns the current VM's zone, such as "us-central1-b".
    /// </summary>
    public async Task<string?> GetZoneAsync(CancellationToken cancellationToken = default)
    {
        var zone = await GetTrimmedCachedString("instance/zone", cancellationToken);
        // zone is of the form "projects/<projNum>/zones/<zoneName>".
        return zone?.Split('/').Last();
    }

    /// <summary>
    /// returns the list of user-defined attributes,
    /// assigned when initially creating a GCE VM instance.
    /// The value of an attribute can be obtained with InstanceAttributeValue.
    /// </summary>
    public async Task<string[]?> GetInstanceAttributesAsync(CancellationToken cancellationToken = default)
    {
       return await GetCachedLines("instance/attributes/", cancellationToken);
    }

    /// <summary>
    /// returns the list of user-defined attributes
    /// applying to the project as a whole, not just this VM.  The value of
    /// an attribute can be obtained with GetProjectAttributeValueAsync.
    /// </summary>
    public async Task<string[]?> GetProjectAttributesAsync(CancellationToken cancellationToken = default)
    {
        return await GetCachedLines("project/attributes/", cancellationToken);
    }

    /// <summary>
    /// returns the value of the provided VM instance attribute.
    /// </summary>
    /// <param name="key">The attribute to get</param>
    /// <param name="cancellationToken"></param>
    public async Task<string?> GetInstanceAttributeValueAsync(string key, CancellationToken cancellationToken = default)
    {
        return await GetTrimmedCachedString($"instance/attributes/{key}", cancellationToken);
    }

    /// <summary>
    /// returns the value of the provided project attribute.
    /// </summary>
    /// <param name="key">The attribute to get</param>
    /// <param name="cancellationToken"></param>
    public async Task<string?> GetProjectAttributeValueAsync(string key, CancellationToken cancellationToken = default)
    {
        return await GetTrimmedCachedString($"project/attributes/{key}", cancellationToken);
    }

    /// <summary>
    /// returns the service account scopes for the given account.
    /// The account may be empty or the string "default" to use the instance's main account
    /// </summary>
    /// <param name="serviceAccount">The service account to get the scopes for, or null/empty for default</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The scopes associated with the service account.</returns>
    public async Task<string[]?> GetScopesAsync(string? serviceAccount = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceAccount))
        {
            serviceAccount = "default";
        }

        return await GetCachedLines($"instance/service-accounts/{serviceAccount}/scopes",
            cancellationToken);
    }


    private async Task<string[]?> GetCachedLines(string suffix, CancellationToken cancellationToken)
    {
        var str = await GetCachedString(suffix, cancellationToken);
        return str?.Trim().Split('\n').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
    }

    private async Task<string?> GetTrimmedCachedString(string suffix, CancellationToken cancellationToken)
    {
        var str = await GetCachedString(suffix, cancellationToken);
        return str?.Trim();
    }

    private async Task<string?> GetCachedString(string suffix, CancellationToken cancellationToken)
    {
        if (cachedStrings.TryGetValue(suffix, out var value))
        {
            return value;
        }

        if (!await IsOnGCEAsync(cancellationToken))
        {
            if (throwIfNotOnGce)
                throw new NotOnGceException();
            return null;
        }

        var response = await client.GetAsync($"{baseUrl}{suffix}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new Exception($"{suffix} not found");
        response.EnsureSuccessStatusCode();
        cachedStrings[suffix] = await response.Content.ReadAsStringAsync(cancellationToken);
        return cachedStrings[suffix];
    }


    public void Dispose()
    {
        client.Dispose();
    }
}