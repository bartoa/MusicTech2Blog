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

static class SpeedPitchChangeTest
{
    [Test]
    public static async Task TestRunner()
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

        var seaShantyHash = await Utilities.BuildHash(seaShantyPath, audioService);

        var seaShantyTrack = new TrackInfo("1", "Sea Shanty", "Andrew Barton");
        var modelService = new InMemoryModelService();
        modelService.Insert(seaShantyTrack, seaShantyHash);

        //QUERY TEST FILES
        List<String> successFiles = new List<String>();
        List<String> failedFiles = new List<String>();
        foreach (String testFile in testFiles)
        {
            var queryResult = await Utilities.BuildQuery(testFile, modelService, audioService);

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
}