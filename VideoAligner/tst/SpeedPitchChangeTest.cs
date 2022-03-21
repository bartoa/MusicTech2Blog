using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Data;
using SoundFingerprinting.Emy;
using SoundFingerprinting.InMemory;
using SoundFingerprinting.Query;

namespace VideoAligner.tst;

static class SpeedPitchChangeTest
{

    private static Task<AVHashes> BuildHash(String path, IAudioService audioService)
    {
        Console.WriteLine("Building hash for " + path);
        var avHashes = FingerprintCommandBuilder.Instance
            .BuildFingerprintCommand()
            .From(path)
            .UsingServices(audioService)
            .Hash();

        return avHashes;
    }

    private static Task<AVQueryResult> BuildQuery(String path, InMemoryModelService modelService, IAudioService audioService)
    {
        Console.WriteLine("Building query for " + path);
        var queryResult = QueryCommandBuilder.Instance
            .BuildQueryCommand()
            .From(path)
            .UsingServices(modelService, audioService)
            .Query();

        return queryResult;
    }

    private static async Task TestRunner()
    {
        //Sanity check to ensure the code actually started running :)
        Console.WriteLine("Begin the test runner!");
        
        //Hard coded for now. Ideally it would dynamically find the folder,
        //but too much time has been spent making dynamic directors work with the Rider IDE :(
        String rootPath = "C:/Users/bartoa/RiderProjects/VideoAligner/VideoAligner/tst/Audio Files/";
        String seaShantyPath = rootPath + "Sea Shanty.wav";

        List<String> testFiles = new List<String>
        {
            rootPath + "Chorus.wav",
            rootPath + "Verse.wav",
            rootPath + "Violin Solo.wav",
            rootPath + "Chorus 10 BPM Slower.wav",
            rootPath + "Chorus 30 BPM Slower.wav",
            rootPath + "Chorus 60 BPM Slower.wav",
            rootPath + "Chorus 100 BPM Slower.wav",
            rootPath + "Chorus 10 BPM Faster.wav",
            rootPath + "Chorus 30 BPM Faster.wav",
            rootPath + "Chorus 60 BPM Faster.wav",
            rootPath + "Chorus 100 BPM Faster.wav",
            rootPath + "Chorus 1 Semitone Lower.wav",
            rootPath + "Chorus 3 Semitone Lower.wav",
            rootPath + "Chorus 1 Semitone Higher.wav",
            rootPath + "Chorus 3 Semitone Higher.wav"
        };

        var audioService = new FFmpegAudioService();

        var seaShantyHash = await BuildHash(seaShantyPath, audioService);

        var seaShantyTrack = new TrackInfo("1", "Sea Shanty", "Andrew Barton");
        var modelService = new InMemoryModelService();
        modelService.Insert(seaShantyTrack, seaShantyHash);

        //QUERY TEST FILES
        List<String> successFiles = new List<String>();
        List<String> failedFiles = new List<String>();
        foreach (String testFile in testFiles)
        {
            var queryResult = await BuildQuery(testFile, modelService, audioService);

            foreach (var (entry, _) in queryResult.ResultEntries)
            {
                // output only those tracks that matched at least 5 seconds.
                if (entry is {TrackCoverageWithPermittedGapsLength: >= 5d})
                {
                    Console.WriteLine("Coverage for file " + testFile + " Is " + entry.TrackCoverageWithPermittedGapsLength + " seconds.");
                    successFiles.Add(testFile);
                }
            }

            if (!queryResult.ResultEntries.Any())
            {
                Console.WriteLine("No coverage found for " + testFile);
                failedFiles.Add(testFile);
            }
        }
        
        //PRINT RESULTS
        Console.WriteLine("SUMMARY:");
        Console.WriteLine("Successful matches:");
        foreach (String file in successFiles)
        {
            Console.WriteLine(file);
        }
        Console.WriteLine("Failed matches:");
        foreach (String file in failedFiles)
        {
            Console.WriteLine(file);
        }

    }

    public static async Task Main(String[] args)
    {
        await TestRunner();
    }
}