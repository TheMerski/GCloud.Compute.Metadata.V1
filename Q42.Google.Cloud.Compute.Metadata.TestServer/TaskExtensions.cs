namespace Q42.Google.Cloud.Compute.Metadata.TestServer;

public static class TaskExtensions
{
    public static string? GetResultOrEmpty(this Task<string?> task) => task.Exception == null ? task.Result : "";
    public static string[]? GetResultOrEmpty(this Task<string[]?> task) => task.Exception == null ? task.Result : [];
}