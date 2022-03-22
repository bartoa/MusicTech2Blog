using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FFMpegCore;
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
        
        String rootPath = "C:/Users/bartoa/RiderProjects/VideoAligner/VideoAlignerTest/Experiments/Audio Files/";
        String megalovaniaPath = rootPath + "Undertale.mp3";
        
        //DEFINE TIME INTERVALS FOR MEGALOVANIA TEST FILES
        //(I'm also testing how I could compare a chopped up edited file using user input)
        //sample size is the number of measures long a megalovania test file is
        float measuresPerSample = 8;
        float originalBPM = 120;
        float newBPM = 128;
        float beatsPerMeasure = 4;

        float percentChange = newBPM / originalBPM;
        float beatsPerSecond = newBPM / 60;
        float secondsPerMeasure = beatsPerMeasure / beatsPerSecond;
        float secondsPerSample = secondsPerMeasure * measuresPerSample;
        
        String megalovaniaOutputPath = rootPath + "Undertale 128 BPM.mp3";
        FFMpegArguments
            .FromFileInput(megalovaniaPath)
            .OutputToFile(megalovaniaOutputPath, true, options => options
                .WithCustomArgument("-filter:a \"atempo=" + percentChange + "\""))
            .ProcessSynchronously();
        
        //INITIALIZATION
        //Hard coded for now. Ideally it would dynamically find the folder,
        //but too much time has been spent making dynamic directors work with the Rider IDE :(

        List<String> testFiles = new List<String>
        {
            rootPath + "Megalovania Sample 01.mp3",
            rootPath + "Megalovania Sample 02.mp3",
            rootPath + "Megalovania Sample 03.mp3",
            rootPath + "Megalovania Sample 04.mp3",
            rootPath + "Megalovania Sample 05.mp3",
            rootPath + "Megalovania Sample 06.mp3",
            rootPath + "Megalovania Sample 07.mp3",
            rootPath + "Megalovania Sample 08.mp3",
            rootPath + "Megalovania Sample 09.mp3",
            rootPath + "Megalovania Sample 10.mp3",

        };

        var audioService = new FFmpegAudioService();
        
        var megalovaniaHash = await Utilities.BuildHash(megalovaniaOutputPath, audioService);
        
        var megalovaniaTrack = new TrackInfo("1", "Megalovania", "Toby Fox");
        var modelService = new InMemoryModelService();
        modelService.Insert(megalovaniaTrack, megalovaniaHash);

        //QUERY TEST FILES
        foreach (String testFile in testFiles)
        {
            var queryResult = await Utilities.BuildQuery(testFile, modelService, audioService);

            foreach (var (entry, _) in queryResult.ResultEntries)
            {
                // output only those tracks that matched at least 2 seconds.
                if (entry is {TrackCoverageWithPermittedGapsLength: >= 2d})
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