using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FFMpegCore;
using NUnit.Framework;
using SoundFingerprinting.Configuration;
using SoundFingerprinting.Data;
using SoundFingerprinting.Emy;
using SoundFingerprinting.InMemory;
using SoundFingerprinting.Strides;
using VideoAligner;

namespace VideoAlignerTest.Experiments;

public static class TimestampAccuracyTest
{
    [Test]
    public static async Task TestRunner()
    {
        //Sanity check to ensure the code actually started running :)
        Console.WriteLine("Begin the test runner!");
        
        String rootPath = "C:/Users/bartoa/RiderProjects/VideoAligner/VideoAlignerTest/Experiments/Audio Files/Sine Waves/";
        //String sineScalePath = rootPath + "Sine Scale.mp3";
        String sineChordPath = rootPath + "Sine Chords.mp3";
        
        //DEFINE TIME INTERVALS FOR MEGALOVANIA TEST FILES
        //(I'm also testing how I could compare a chopped up edited file using user input)
        //sample size is the number of measures long a megalovania test file is
        // float measuresPerSample = 8;
        // float originalBPM = 120;
        // float newBPM = 128;
        // float beatsPerMeasure = 4;
        //
        // float percentChange = newBPM / originalBPM;
        // float beatsPerSecond = newBPM / 60;
        // float secondsPerMeasure = beatsPerMeasure / beatsPerSecond;
        // float secondsPerSample = secondsPerMeasure * measuresPerSample;
        //
        // String megalovaniaOutputPath = rootPath + "Undertale 128 BPM.mp3";
        // FFMpegArguments
        //     .FromFileInput(megalovaniaPath)
        //     .OutputToFile(megalovaniaOutputPath, true, options => options
        //         .WithCustomArgument("-filter:a \"atempo=" + percentChange + "\""))
        //     .ProcessSynchronously();
        
        //INITIALIZATION
        //Hard coded for now. Ideally it would dynamically find the folder,
        //but too much time has been spent making dynamic directors work with the Rider IDE :(

        List<String> testFiles = new List<String>
        {
            // rootPath + "C Tone.mp3",
            // rootPath + "D Tone.mp3",
            // rootPath + "E Tone.mp3",
            // rootPath + "F Tone.mp3",
            // rootPath + "G Tone.mp3",
            // rootPath + "A Tone.mp3",
            // rootPath + "B Tone.mp3",
            // rootPath + "C High Tone.mp3",
            // rootPath + "C D Scale.mp3",
            // rootPath + "E F Scale.mp3",
            // rootPath + "G A Scale.mp3",
            // rootPath + "B High C Scale.mp3",
            // rootPath + "C D E F Scale.mp3",
            // rootPath + "G A B High C Scale.mp3",
            rootPath + "C Major Chord.mp3",
            rootPath + "C Minor Chord.mp3",
            rootPath + "C Diminished Chord.mp3",
            rootPath + "C Augmented Chord.mp3",
            rootPath + "First two chords.mp3",
            rootPath + "Second two chords.mp3"
        };

        var audioService = new FFmpegAudioService();

        var fingerprintConfig = new DefaultAVFingerprintConfiguration();
        //fingerprintConfig.Audio.FrequencyRange = new FrequencyRange(318, 2000);
        //fingerprintConfig.Audio.Stride = new IncrementalStaticStride(512);
        //var sineScaleHash = await Utilities.BuildHash(sineScalePath, audioService, fingerprintConfig);
        var sineChordHash = await Utilities.BuildHash(sineChordPath, audioService, fingerprintConfig);
        
        //var sineScaleTrack = new TrackInfo("1", "Sine Scale", "Bartoa");
        var sineChordTrack = new TrackInfo("2", "Sine Chords", "Bartoa");
        var modelService = new InMemoryModelService();
        //modelService.Insert(sineScaleTrack, sineScaleHash);
        modelService.Insert(sineChordTrack, sineChordHash);

        //QUERY TEST FILES
        foreach (String testFile in testFiles)
        {
            var queryResult = await Utilities.BuildQuery(testFile, modelService, audioService);

            foreach (var (entry, _) in queryResult.ResultEntries)
            {
                // output only those tracks that matched at least x seconds.
                if (entry is {TrackCoverageWithPermittedGapsLength: >= 0.25d})
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