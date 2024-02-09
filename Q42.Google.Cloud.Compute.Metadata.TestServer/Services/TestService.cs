using Grpc.Core;
using Q42.Google.Cloud.Compute.Metadata.TestServer;

namespace Q42.Google.Cloud.Compute.Metadata.TestServer.Services;

using global::Google.Protobuf.WellKnownTypes;
using Q42.Google.Cloud.Compute.Metadata.V1;

public class TestService(ILogger<TestService> logger) : TestServer.TestService.TestServiceBase
{
    public override async Task<TestReply> TestMetadata(Empty request, ServerCallContext context)
    {
        using var metadata = new MetadataClient();
        // First wait for running on GCE.
        var onGce = await metadata.IsOnGCEAsync(context.CancellationToken);
        logger.LogInformation("On GCE: {OnGce}", onGce ? "Yes" : "No");

        // Then get the rest in parallel.
        var projectId = metadata.GetProjectIdAsync(context.CancellationToken);
        var numericProjectId = metadata.GetNumericProjectIdAsync(context.CancellationToken);
        var instanceId = metadata.GetInstanceIdAsync(context.CancellationToken);
        var internalIp = metadata.GetInternalIpAsync(context.CancellationToken);
        var defaultSaEmail = metadata.GetEmailAsync(null, context.CancellationToken);
        var externalIp = metadata.GetExternalIpAsync(context.CancellationToken);
        var hostname = metadata.GetHostnameAsync(context.CancellationToken);
        var instanceTags = metadata.GetInstanceTagsAsync(context.CancellationToken);
        var instanceName = metadata.GetInstanceNameAsync(context.CancellationToken);
        var zone = metadata.GetZoneAsync(context.CancellationToken);
        var instanceAttributes = metadata.GetInstanceAttributesAsync(context.CancellationToken);
        var projectAttributes = metadata.GetProjectAttributesAsync(context.CancellationToken);
        var defaultSaScopes = metadata.GetScopesAsync(null, context.CancellationToken);

        // Wait for all to complete.
        try
        {
            await Task.WhenAll(projectId, numericProjectId, instanceId, internalIp, defaultSaEmail, externalIp,
                hostname,
                instanceTags, instanceName, zone, instanceAttributes, projectAttributes, defaultSaScopes);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error getting metadata");
            // ignored
        }

        logger.LogInformation("Got all metadata");

        return new TestReply
        {
            OnGce = onGce,
            ProjectId = projectId.GetResultOrEmpty(),
            NumericProjectId = numericProjectId.GetResultOrEmpty(),
            InstanceId = instanceId.GetResultOrEmpty(),
            InternalIp = internalIp.GetResultOrEmpty(),
            DefaultSaEmail = defaultSaEmail.GetResultOrEmpty(),
            ExternalIp = externalIp.GetResultOrEmpty(),
            Hostname = hostname.GetResultOrEmpty(),
            InstanceTags = { instanceTags.GetResultOrEmpty() },
            InstanceName = instanceName.GetResultOrEmpty(),
            Zone = zone.GetResultOrEmpty(),
            InstanceAttributes = { instanceAttributes.GetResultOrEmpty() },
            ProjectAttributes = { projectAttributes.GetResultOrEmpty() },
            DefaultSaScopes = { defaultSaScopes.GetResultOrEmpty() }
        };
    }
}