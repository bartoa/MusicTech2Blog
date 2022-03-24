using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Configuration;
using SoundFingerprinting.Data;
using SoundFingerprinting.InMemory;
using SoundFingerprinting.Query;

namespace VideoAligner;

public static class Utilities
{
    public static Task<AVHashes> BuildHash(String path, IAudioService audioService,
        AVFingerprintConfiguration config = null)
    {
        if (config is null)
        {
            config = Constants.fingerprintConfig;
        }

        Console.WriteLine("Building hash for " + path);
        var avHashes = FingerprintCommandBuilder.Instance
            .BuildFingerprintCommand()
            .From(path)
            .WithFingerprintConfig(config)
            .UsingServices(audioService)
            .Hash();

        return avHashes;
    }

    public static Task<AVQueryResult> BuildQuery(String path, InMemoryModelService modelService,
        IAudioService audioService, AVQueryConfiguration config = null)
    {
        if (config is null)
        {
            config = Constants.queryConfig;
        }
            
        Console.WriteLine("Building query for " + path);
        var queryResult = QueryCommandBuilder.Instance
            .BuildQueryCommand()
            .From(path)
            .WithQueryConfig(config)
            .UsingServices(modelService, audioService)
            .Query();

        return queryResult;
    }
}