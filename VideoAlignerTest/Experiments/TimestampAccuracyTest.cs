using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SoundFingerprinting.Data;
using SoundFingerprinting.Emy;
using SoundFingerprinting.InMemory;
using VideoAligner;

namespace VideoAlignerTest.Experiments;

public static class TimestampAccuracyTest
{
    [Test]
    public static async Task TestRunner()
    {
        //Sanity check to ensure the code actually started running :)
        Console.WriteLine("Begin the test runner!");
        
        //Hard coded for now. Ideally it would dynamically find the folder,
        //but too much time has been spent making dynamic directors work with the Rider IDE :(
        String rootPath = "C:/Users/bartoa/RiderProjects/VideoAligner/VideoAlignerTest/Experiments/Audio Files/";
        String seaShantyPath = rootPath + "Sea Shanty.wav";

        List<String> testFiles = new List<String>
        {
            rootPath + "Chorus.wav",
            rootPath + "Verse.wav",
            rootPath + "Violin Solo.wav",
        };

        var audioService = new FFmpegAudioService();

        var seaShantyHash = await Utilities.BuildHash(seaShantyPath, audioService);

        var seaShantyTrack = new TrackInfo("1", "Sea Shanty", "Andrew Barton");
        var modelService = new InMemoryModelService();
        modelService.Insert(seaShantyTrack, seaShantyHash);
        
        foreach (String testFile in testFiles)
        {
            var queryResult = await Utilities.BuildQuery(testFile, modelService, audioService);

            foreach (var (entry, _) in queryResult.ResultEntries)
            {
                // output only those tracks that matched at least 5 seconds.
                if (entry is {TrackCoverageWithPermittedGapsLength: >= 5d})
                {
                    Console.WriteLine("Query match starts at: " + entry.QueryMatchStartsAt);
                    Console.WriteLine("Track match starts at: " + entry.TrackMatchStartsAt);
                    Console.WriteLine("Track starts at: " + entry.TrackStartsAt);
                    Console.WriteLine("Confidence: " + entry.Confidence);
                }
            }

            if (!queryResult.ResultEntries.Any())
            {
                Console.WriteLine("No coverage found for " + testFile);
            }
        }
    }
    
}