// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json.Serialization;
using Aspire.Hosting.ApplicationModel;
using k8s.Models;

namespace Aspire.Hosting.Dcp.Model;

internal sealed class ContainerSpec
{
    // Container name displayed in Docker. If not specified, the metadata name + random suffix is used.
    [JsonPropertyName("containerName")] public string? ContainerName { get; set; }

    // Image to be used to create the container
    [JsonPropertyName("image")] public string? Image { get; set; }

    // Optional configuration to build an image from a Dockerfile instead of using a pre-built image
    [JsonPropertyName("build")] public BuildContext? Build { get; set; }

    // Volumes that should be mounted into the container
    [JsonPropertyName("volumeMounts")] public List<VolumeMount>? VolumeMounts { get; set; }

    // Exposed ports
    [JsonPropertyName("ports")] public List<ContainerPortSpec>? Ports { get; set; }

    // Environment variables to be used for the container
    [JsonPropertyName("env")] public List<EnvVar>? Env { get; set; }

    // Environment files to use to populate Container environment during startup
    [JsonPropertyName("envFiles")] public List<string>? EnvFiles { get; set; }

    // Container restart policy
    [JsonPropertyName("restartPolicy")] public string? RestartPolicy { get; set; } = ContainerRestartPolicy.None;

    // Command to run in the container (entrypoint)
    [JsonPropertyName("command")] public string? Command { get; set; }

    // Arguments to pass to the command that starts the container
    [JsonPropertyName("args")] public List<string>? Args { get; set; }

    // Optional labels to apply to the container instance
    [JsonPropertyName("labels")] public List<ContainerLabel>? Labels { get; set; }

    // Additional arguments to pass to the container run command
    [JsonPropertyName("runArgs")] public List<string>? RunArgs { get; set; }

    // Should this container be created and persisted between DCP runs?
    [JsonPropertyName("persistent")] public bool? Persistent { get; set; }

    [JsonPropertyName("networks")] public List<ContainerNetworkConnection>? Networks { get; set; }
}

internal sealed class BuildContext
{
    // The path to the directory that will serve as the root of the image build context
    [JsonPropertyName("context")] public string? Context { get; set; }

    // Optional path to a specific Dockerfile to use in the build (defaults to looking for a Dockerfile in the root Context folder)
    [JsonPropertyName("dockerfile")] public string? Dockerfile { get; set; }

    // Optional build --build-args to pass to the build command
    [JsonPropertyName("args")] public List<EnvVar>? Args { get; set; }

    // Optional build secret mounts to pass to the build command
    [JsonPropertyName("secrets")] public List<BuildContextSecret>? Secrets { get; set; }

    // Optional specific stage to use when building a multiple stage Dockerfile
    [JsonPropertyName("stage")] public string? Stage { get; set; }

    // Optional additional tags to apply to the built image
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }

    // Optional labels to apply to the built image
    [JsonPropertyName("labels")] public List<ContainerLabel>? Labels { get; set; }
}

internal sealed class BuildContextSecret
{
    // The ID of the secret (a secret can be used in a Dockerfile with `RUN --mount-type=secret,id=<id>,target=<targetpath>`)
    [JsonPropertyName("id")] public string? Id { get; set; }

    // Type of the secret, can be "env" or "file".
    [JsonPropertyName("type")] public string? Type { get; set; }

    // Value of the secret to be used in the build when the type of the secret is "env".
    [JsonPropertyName("value")] public string? Value { get; set; }

    // Path to secret file/folder that will be mounted as a build secret using --secret
    [JsonPropertyName("source")] public string? Source { get; set; }
}

internal static class VolumeMountType
{
    // A volume mount to a host directory
    public const string Bind = "bind";

    // A volume mount to a volume managed by the container orchestrator
    public const string Volume = "volume";
}

internal sealed class VolumeMount
{
    [JsonPropertyName("type")] public string Type { get; set; } = VolumeMountType.Bind;

    // Bind mounts: the host directory to mount
    // Volume mounts: name of the volume to mount
    [JsonPropertyName("source")] public string? Source { get; set; }

    // The path within the container that the mount will use
    [JsonPropertyName("target")] public string? Target { get; set; }

    // True if the mounted file system is supposed to be read-only
    [JsonPropertyName("readOnly")] public bool IsReadOnly { get; set; } = false;
}

internal sealed class ContainerNetworkConnection
{
    // DCP Resource name of a ContainerNetwork to connect to
    // A container won't start running until it can be conneced to all specified networks
    [JsonPropertyName("name")] public string? Name { get; set; }

    // Aliases of the container on the network
    // This enables container DNS resolution
    [JsonPropertyName("aliases")] public List<string>? Aliases { get; set; }
}

internal sealed class ContainerLabel
{
    // The label key
    [JsonPropertyName("key")] public string? Key { get; set; }

    // The label value
    [JsonPropertyName("value")] public string? Value { get; set; }
}

internal static class ContainerRestartPolicy
{
    // Do not automatically restart the container when it exits (default)
    public const string None = "no";

    // Restart only if the container exits with non-zero status
    public const string OnFailure = "on-failure";

    // Restart container, except if container is explicitly stopped (or container daemon is stopped/restarted)
    public const string UnlessStopped = "unless-stopped";

    // Always try to restart the container
    public const string Always = "always";
}

internal static class PortProtocol
{
    public const string TCP = "TCP";

    public const string UDP = "UDP";

    public static string Canonicalize(string protocol)
    {
        var protocolUC = protocol.ToUpperInvariant();
        switch (protocolUC)
        {
            case TCP:
            case UDP:
                return protocolUC;
            default:
                throw new ArgumentException("Port protocol value must be 'TCP' or 'UDP'");
        }
    }

    public static ProtocolType ToProtocolType(string protocol)
    {
        var canonical = Canonicalize(protocol);
        switch (canonical)
        {
            case TCP:
                return ProtocolType.Tcp;
            case UDP:
                return ProtocolType.Udp;
            default:
                throw new ArgumentException("Supported protocols are TCP and UDP");
        }
    }

    public static string FromProtocolType(ProtocolType protocolType)
    {
        switch (protocolType)
        {
            case ProtocolType.Tcp:
                return TCP;
            case ProtocolType.Udp:
                return UDP;
            default:
                throw new ArgumentException("Supported protocols are TCP and UDP");
        }
    }
}

internal sealed class ContainerPortSpec
{
    // Optional: If specified, this must be a valid port number, 0 < x < 65536.
    [JsonPropertyName("hostPort")] public int? HostPort { get; set; }

    // Required: This must be a valid port number, 0 < x < 65536.
    [JsonPropertyName("containerPort")] public int? ContainerPort { get; set; }

    // The network protocol to be used, defaults to TCP
    [JsonPropertyName("protocol")] public string Protocol { get; set; } = PortProtocol.TCP;

    // Optional: What host IP to bind the external port to.
    [JsonPropertyName("hostIP")] public string? HostIP { get; set; }
}

internal sealed class ContainerStatus : V1Status
{
    // Container name displayed in Docker
    [JsonPropertyName("containerName")] public string? ContainerName { get; set; }

    // Current state of the Container.
    [JsonPropertyName("state")] public string? State { get; set; }

    // ID of the Container (if an attempt to start the Container was made)
    [JsonPropertyName("containerId")] public string? ContainerId { get; set; }

    // Timestamp of the Container start attempt
    [JsonPropertyName("startupTimestamp")] public DateTimeOffset? StartupTimestamp { get; set; }

    // Timestamp when the Container was terminated last
    [JsonPropertyName("finishTimestamp")] public DateTimeOffset? FinishTimestamp { get; set; }

    // Exit code of the Container.
    // Default is -1, meaning the exit code is not known, or the container is still running.
    [JsonPropertyName("exitCode")] public int ExitCode { get; set; } = Conventions.UnknownExitCode;

    // Effective values of environment variables, after all substitutions have been applied
    [JsonPropertyName("effectiveEnv")] public List<EnvVar>? EffectiveEnv { get; set; }

    // Effective values of launch arguments to be passed to the Container, after all substitutions are applied.
    [JsonPropertyName("effectiveArgs")] public List<string>? EffectiveArgs { get; set; }

    // Any ContainerNetworks this container is attached to
    [JsonPropertyName("networks")] public List<string>? Networks { get; set; }

    // Note: the ContainerStatus has "Message" property that represents a human-readable information about Container state.
    // It is provided by V1Status base class.
}

internal static class ContainerState
{
    // Pending is the initial Container state. No attempt has been made to run the container yet.
    public const string Pending = "Pending";

    // Building indicates an image is being built from a Dockerfile, but a container hasn't been created yet.
    public const string Building = "Building";

    // Starting indicates a container is in the process of starting (pulling images, waiting to join to initial networks, etc.)
    public const string Starting = "Starting";

    // A start attempt was made, but it failed
    public const string FailedToStart = "FailedToStart";

    // Container has been started and is executing
    public const string Running = "Running";

    // Container is paused
    public const string Paused = "Paused";

    // Container finished execution
    public const string Exited = "Exited";

    // Container is in the process of stopping (waiting for container processes to exit, etc.).
    public const string Stopping = "Stopping";

    // Unknown means for some reason container state is unavailable.
    public const string Unknown = "Unknown";
}

internal sealed class Container : CustomResource<ContainerSpec, ContainerStatus>
{
    [JsonConstructor]
    public Container(ContainerSpec spec) : base(spec)
    {
    }

    public static Container Create(string name, string image, ResourceAnnotationCollection containerAnnotations)
    {
        var c = new Container(new ContainerSpec { Image = image, RestartPolicy = GetRestartPolicy(containerAnnotations) });

        c.Kind = Dcp.ContainerKind;
        c.ApiVersion = Dcp.GroupVersion.ToString();
        c.Metadata.Name = name;
        c.Metadata.NamespaceProperty = string.Empty;

        return c;
    }

    private static string GetRestartPolicy(ResourceAnnotationCollection containerAnnotations)
    {
        var policy =
            (RestartPolicyAnnotation?)containerAnnotations.FirstOrDefault(x =>
                x.GetType().Name == "RestartPolicyAnnotation");
        return policy is null ? ContainerRestartPolicy.Always : GetRestartPolicyValue(policy);
    }

    private static string GetRestartPolicyValue(RestartPolicyAnnotation annotation)
    {
        return annotation.RestartPolicy switch
        {
            RestartPolicy.Always => ContainerRestartPolicy.Always,
            RestartPolicy.Never => ContainerRestartPolicy.None,
            RestartPolicy.OnFailure => ContainerRestartPolicy.OnFailure,
            RestartPolicy.UnlessStopped => ContainerRestartPolicy.UnlessStopped,
            _ => ContainerRestartPolicy.None
        };
    }

    public bool LogsAvailable =>
        this.Status?.State == ContainerState.Starting
        || this.Status?.State == ContainerState.Building
        || this.Status?.State == ContainerState.Running
        || this.Status?.State == ContainerState.Paused
        || this.Status?.State == ContainerState.Stopping
        || this.Status?.State == ContainerState.Exited
        || (this.Status?.State == ContainerState.FailedToStart && this.Status?.ContainerId is not null);
}
