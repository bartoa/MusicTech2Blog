using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Data;
using SoundFingerprinting.InMemory;
using SoundFingerprinting.Query;

namespace VideoAligner;

public static class Utilities
{
    public static Task<AVHashes> BuildHash(String path, IAudioService audioService)
    {
        Console.WriteLine("Building hash for " + path);
        var avHashes = FingerprintCommandBuilder.Instance
            .BuildFingerprintCommand()
            .From(path)
            .UsingServices(audioService)
            .Hash();

        return avHashes;
    }

    public static Task<AVQueryResult> BuildQuery(String path, InMemoryModelService modelService, IAudioService audioService)
    {
        Console.WriteLine("Building query for " + path);
        var queryResult = QueryCommandBuilder.Instance
            .BuildQueryCommand()
            .From(path)
            .UsingServices(modelService, audioService)
            .Query();

        return queryResult;
    }
}