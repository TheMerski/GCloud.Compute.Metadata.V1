namespace Q42.Google.Cloud.Compute.Metadata.Tests;

using System.Net;
using System.Text;
using Q42.Google.Cloud.Compute.Metadata.V1;
using RichardSzalay.MockHttp;

public class Tests
{
    private readonly List<KeyValuePair<string, string>> googleHeaders =
        [new KeyValuePair<string, string>("Metadata-Flavor", "Google")];

    private readonly string baseAddress = "http://169.254.169.254";
    private readonly string metadataBase = "http://169.254.169.254/computeMetadata/v1/";

    private MockHttpMessageHandler mockHttpOnGce = new();

    [SetUp]
    public void Setup()
    {
        mockHttpOnGce = new MockHttpMessageHandler();
        mockHttpOnGce.When($"{baseAddress}")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, new StringContent("", Encoding.UTF8, "application/text"));
    }

    [TearDown]
    public void TearDown()
    {
        mockHttpOnGce.Dispose();
    }

    [Test]
    public async Task IsOnGCETrue()
    {
        var httpContent = new StringContent("", Encoding.UTF8, "application/text");
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{baseAddress}/*")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, httpContent);

        var client = mockHttp.ToHttpClient();
        using var metadata = new MetadataClient(client);
        var onGce = await metadata.IsOnGCEAsync();
        Assert.That(onGce, Is.True);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    // We timeout the test after 1042ms, because the default timeout is 1000ms.
    [Test, Timeout(1042)]
    public async Task IsOnGCEFalse()
    {
        using var metadata = new MetadataClient();
        var onGce = await metadata.IsOnGCEAsync();
        Assert.That(onGce, Is.False);
    }

    [Test]
    public async Task UsesMetadataEnvVar()
    {
        Environment.SetEnvironmentVariable("GCE_METADATA_HOST", "test-metadata");
        var httpContent = new StringContent("", Encoding.UTF8, "application/text");
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("http://test-metadata/*")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, httpContent);

        var client = mockHttp.ToHttpClient();
        using var metadata = new MetadataClient(client);
        var onGce = await metadata.IsOnGCEAsync();
        Assert.That(onGce, Is.True);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task DoesNotThrowWhenNotOnGCEByDefault()
    {
        using var metadata = new MetadataClient();
        var result = await metadata.GetProjectIdAsync();
        Assert.That(result, Is.Null);
    }

    [Test]
    public Task ThrowsWhenNotOnGCE()
    {
        using var metadata = new MetadataClient(throwIfNotOnGce: true);
        Assert.ThrowsAsync<NotOnGceException>(async () => await metadata.GetProjectIdAsync());
        return Task.CompletedTask;
    }

    [Test]
    public async Task GetsProjectId()
    {
        const string projectId = "test-project";
        mockHttpOnGce.When($"{metadataBase}project/project-id")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", projectId);
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetProjectIdAsync();
        Assert.That(result, Is.EqualTo(projectId));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsNumericProjectId()
    {
        const string projectId = "424242";
        mockHttpOnGce.When($"{metadataBase}project/numeric-project-id")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", projectId);
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetNumericProjectIdAsync();
        Assert.That(result, Is.EqualTo(projectId));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsInstanceId()
    {
        const string instanceId = "424242";
        mockHttpOnGce.When($"{metadataBase}instance/id")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", instanceId);
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetInstanceIdAsync();
        Assert.That(result, Is.EqualTo(instanceId));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsInternalIp()
    {
        const string internalIp = "10.0.0.42";
        mockHttpOnGce.When($"{metadataBase}instance/network-interfaces/0/ip")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", internalIp);
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetInternalIpAsync();
        Assert.That(result, Is.EqualTo(internalIp));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsInternalIpTrimmed()
    {
        const string internalIp = "10.0.0.42";
        mockHttpOnGce.When($"{metadataBase}instance/network-interfaces/0/ip")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", " "+ internalIp + " ");
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetInternalIpAsync();
        Assert.That(result, Is.EqualTo(internalIp));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsDefaultSaEmail()
    {
        const string saEmail = "default@serviceaccount.test";
        mockHttpOnGce.When($"{metadataBase}instance/service-accounts/default/email")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", saEmail );
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetEmailAsync();
        Assert.That(result, Is.EqualTo(saEmail));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsSaEmail()
    {
        const string saEmail = "test@serviceaccount.test";
        mockHttpOnGce.When($"{metadataBase}instance/service-accounts/test/email")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", saEmail );
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetEmailAsync("test");
        Assert.That(result, Is.EqualTo(saEmail));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsExternalIp()
    {
        const string expected = "34.0.0.42";
        mockHttpOnGce.When($"{metadataBase}instance/network-interfaces/0/access-configs/0/external-ip")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", expected);
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetExternalIpAsync();
        Assert.That(result, Is.EqualTo(expected));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsHostname()
    {
        const string expected = "internal.test.hostname";
        mockHttpOnGce.When($"{metadataBase}instance/hostname")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", expected);
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetHostnameAsync();
        Assert.That(result, Is.EqualTo(expected));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsTags()
    {
        string[] expected = ["tag1", "tag2"];
        mockHttpOnGce.When($"{metadataBase}instance/tags")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/json", "[\"tag1\",\"tag2\"]");
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetInstanceTagsAsync();
        Assert.That(result, Is.EqualTo(expected));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsTagsEmpty()
    {
        string[] expected = [];
        mockHttpOnGce.When($"{metadataBase}instance/tags")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/json", "[]");
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetInstanceTagsAsync();
        Assert.That(result, Is.EqualTo(expected));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsInstanceName()
    {
        const string expected = "test-name";
        mockHttpOnGce.When($"{metadataBase}instance/name")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", expected);
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetInstanceNameAsync();
        Assert.That(result, Is.EqualTo(expected));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsZone()
    {
        const string expected = "europe-west3-c";
        mockHttpOnGce.When($"{metadataBase}instance/zone")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", $"projects/424242/zones/{expected}");
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetZoneAsync();
        Assert.That(result, Is.EqualTo(expected));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsInstanceAttributes()
    {
        string[] expected = ["ssh-keys", "test"];
        mockHttpOnGce.When($"{metadataBase}instance/attributes/")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", "ssh-keys\ntest");
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetInstanceAttributesAsync();
        Assert.That(result, Is.EqualTo(expected));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsInstanceAttributesEmpty()
    {
        string[] expected = [];
        mockHttpOnGce.When($"{metadataBase}instance/attributes/")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK);
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetInstanceAttributesAsync();
        Assert.That(result, Is.EqualTo(expected));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsProjectAttributes()
    {
        string[] expected = ["ssh-keys", "test"];
        mockHttpOnGce.When($"{metadataBase}project/attributes/")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", "ssh-keys\ntest");
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetProjectAttributesAsync();
        Assert.That(result, Is.EqualTo(expected));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsProjectAttributesEmpty()
    {
        string[] expected = [];
        mockHttpOnGce.When($"{metadataBase}project/attributes/")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", "");
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetProjectAttributesAsync();
        Assert.That(result, Is.EqualTo(expected));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsInstanceAttribute()
    {
        string expected = "value";
        mockHttpOnGce.When($"{metadataBase}instance/attributes/test")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", expected);
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetInstanceAttributeValueAsync("test");
        Assert.That(result, Is.EqualTo(expected));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsProjectAttribute()
    {
        string expected = "value";
        mockHttpOnGce.When($"{metadataBase}project/attributes/test")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", expected);
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetProjectAttributeValueAsync("test");
        Assert.That(result, Is.EqualTo(expected));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsDefaultScopes()
    {
        string[] expected = ["scope1", "scope2"];
        mockHttpOnGce.When($"{metadataBase}instance/service-accounts/default/scopes")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", "scope1\nscope2");
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetScopesAsync();
        Assert.That(result, Is.EqualTo(expected));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task GetsSaScopes()
    {
        string[] expected = ["scope1", "scope2"];
        mockHttpOnGce.When($"{metadataBase}instance/service-accounts/account/scopes")
            .WithHeaders(googleHeaders)
            .Respond(HttpStatusCode.OK, googleHeaders, "application/text", "scope1\nscope2");
        using var metadata = new MetadataClient(mockHttpOnGce.ToHttpClient());
        var result = await metadata.GetScopesAsync("account");
        Assert.That(result, Is.EqualTo(expected));
        mockHttpOnGce.VerifyNoOutstandingExpectation();
    }
}