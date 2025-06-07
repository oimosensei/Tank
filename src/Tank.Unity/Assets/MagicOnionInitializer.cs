using Cysharp.Net.Http;
using Grpc.Net.Client;
using MagicOnion.Unity;
using MessagePack;
using MessagePack.Resolvers;
using MessagePack.Formatters;
using MessagePack.Unity;
using UnityEngine;

public class MagicOnionInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void OnRuntimeInitialize()
    {
        // Initialize MessagePack resolver for Unity types
        StaticCompositeResolver.Instance.Register(
            UnityResolver.Instance,
            StandardResolver.Instance
        );

        var options = MessagePackSerializerOptions.Standard.WithResolver(StaticCompositeResolver.Instance);
        MessagePackSerializer.DefaultOptions = options;

        // Initialize gRPC channel provider when the application is loaded.
        GrpcChannelProviderHost.Initialize(new DefaultGrpcChannelProvider(() => new GrpcChannelOptions()
        {
            HttpHandler = new YetAnotherHttpHandler()
            {
                Http2Only = true,
            },
            DisposeHttpClient = true,
        }));
    }
}