using System.Drawing;
using FFMpegCore;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Configuration;
using SoundFingerprinting.Data;
using SoundFingerprinting.InMemory;
using SoundFingerprinting.Query;

namespace VideoAligner;

public static class Utilities
{
    
    //Return string contains the path for the outputted file
    public static async Task<string> ChangeTempo(string filePath, double oldTempo, double newTempo)
    {
        if (oldTempo == newTempo)
        {
            return filePath;
        }
        
        Console.WriteLine("Changing audio tempo: " + filePath);
        
        string rootPath = Path.GetDirectoryName(filePath);
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string outputPath = rootPath + "\\" + fileName + "_TChange.wav";
        
        double tempoPercentChange = newTempo / oldTempo;
        
        await FFMpegArguments
            .FromFileInput(filePath)
            .OutputToFile(outputPath, true, options => options
                .WithCustomArgument("-filter:a \"rubberband=tempo=" + tempoPercentChange + "\""))
            .ProcessAsynchronously();

        Console.WriteLine("Finished changing tempo");
        return outputPath;
    }

    public static async Task<string> ChangeVideoSpeed(string filePath, double speedMultiplier)
    {
        if (speedMultiplier == 1)
        {
            return filePath;
        }
        
        Console.WriteLine("Changing video speed: " + filePath);
        
        string rootPath = Path.GetDirectoryName(filePath);
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string outputPath = rootPath + "\\" + fileName + "_SpeedChange.mp4";

        //Note: setpts filter uses the inverse of what would be the tempo speed multiplier
        //i.e. if you want the video to speed up from tempo of 100 to 200, so twice as fast,
        //  the filter actually wants a value of 0.5, not 2.0.
        await FFMpegArguments
            .FromFileInput(filePath)
            .OutputToFile(outputPath, true, options => options
                .WithCustomArgument("-filter:v \"setpts=" + (1 / speedMultiplier) + "*PTS\""))
            .ProcessAsynchronously();

        Console.WriteLine("Finished changing video speed");
        return outputPath;
    }
    
    public static async Task<string> ChangePitch(string filePath, double pitchChange)
    {
        if (pitchChange == 0)
        {
            return filePath;
        }
        
        Console.WriteLine("Changing pitch: " + filePath);
        
        string rootPath = Path.GetDirectoryName(filePath);
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string outputPath = rootPath + "\\" + fileName + "_PChange.wav";
        
        //How to translate semitone change into percent change:
        //1 semitone change = 2 ^ (1/12)
        //Multiply that value by the number of semitones it changes by
        double pitchPercentChange = pitchChange * Math.Pow(2, Convert.ToDouble(1/12));
        if (pitchPercentChange < 0)
        {
            pitchPercentChange = 1 / Math.Abs(pitchPercentChange);
        }
        
        await FFMpegArguments
            .FromFileInput(filePath)
            .OutputToFile(outputPath, true, options => options
                .WithCustomArgument("-filter:a \"rubberband=pitch=" + pitchPercentChange + "\""))
            .ProcessAsynchronously();

        Console.WriteLine("Finished changing pitch");
        return outputPath;
    }

    //Generates a blank video based on the reference video
    //  same FPS, duration, resolution, video format, and pixel format
    public static async Task GenerateBlankVideo(string destPath, string referenceVideo)
    {
        //Extract video metadata
        var analysis = await FFProbe.AnalyseAsync(referenceVideo);
        //var FPS = analysis.PrimaryVideoStream.FrameRate;
        var duration = analysis.Duration;
        var width = analysis.PrimaryVideoStream.Width;
        var height = analysis.PrimaryVideoStream.Height;
        //var pixelFormat = analysis.PrimaryVideoStream.PixelFormat;
        //Generate black image
        string imagePath = Path.GetDirectoryName(destPath) + "\\BlackImage.png";
        GenerateBlackImage(imagePath, width, height);

        //Generate black video
        //Interesting edge case with FFMpeg: When generating video from an image like this,
        //  the loop flag must go before the input flag
        //This means I can't use FFMpegCore for this command :(
        
        System.Diagnostics.Process process = new System.Diagnostics.Process();
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        startInfo.FileName = "cmd.exe";
        startInfo.Arguments = "/C ffmpeg -loop 1 -i " + imagePath + " -t " + duration + " " +  destPath;
        process.StartInfo = startInfo;
        process.Start();
        await process.WaitForExitAsync().ConfigureAwait(false);
        
    }

    private static void GenerateBlackImage(string destPath, int width, int height)
    {
        Bitmap bmp = new Bitmap(width, height);
        using (Graphics graph = Graphics.FromImage(bmp))
        {
            Rectangle ImageSize = new Rectangle(0, 0, width, height);
            graph.FillRectangle(Brushes.Black, ImageSize);
        }
        
        bmp.Save(destPath);
    }
    
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