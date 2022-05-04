using FFMpegCore;

namespace VideoAligner
{

    //Splits a track into a sub-tracks with a user-defined length.
    //Splitted tracks are stored in a temp folder at the exe file's directory
    public static class TrackWindowTool
    {
        public static async Task SplitAudioTracks(string filePath, double secondsPerWindow, string destPath)
        {
            Console.WriteLine("Splitting audio track: " + filePath);
            
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string fileExtension = Path.GetExtension(filePath);
            string outputPath = destPath + "\\" + fileName + "_W%03d" + fileExtension;

            await FFMpegArguments
                .FromFileInput(filePath)
                .OutputToFile(outputPath, true, options => options
                    .CopyChannel()
                    //.WithCustomArgument("-map 0")
                    .WithCustomArgument("-f segment -segment_time " + secondsPerWindow)
                    .WithCustomArgument("-reset_timestamps 1"))
                .ProcessAsynchronously();
            
            Console.WriteLine("Finished splitting track " + fileName + fileExtension);
        }
        
        //In order for a video to be cut precisely, keyframes must first be inserted at the spots to be sliced
        //If this isn't done, then FFMpeg will cut at the closest known keyframe, which will create inconsistencies
        public static async Task SplitVideoTracks(string filePath, double secondsPerWindow, string destPath)
        {
            Console.WriteLine("Splitting video track: " + filePath);
            
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string fileExtension = Path.GetExtension(filePath);
            string outputPath = destPath + "\\" + fileName + "_W%03d" + fileExtension;

            string videoWithKeyframesPath = destPath + "\\" + "Forced Keyframes" + fileExtension;
            string keyFrameArgument = "-force_key_frames ";
            var fileAnalysis = await FFProbe.AnalyseAsync(filePath);
            var totalDuration = fileAnalysis.Duration.TotalSeconds;
            for (double timeCounter = 0; timeCounter < totalDuration; timeCounter += secondsPerWindow)
            {
                keyFrameArgument += Convert.ToString(TimeSpan.FromSeconds(timeCounter)) + ",";
            }
            keyFrameArgument = keyFrameArgument.Remove(keyFrameArgument.Length - 1);

            await FFMpegArguments
                .FromFileInput(filePath)
                .OutputToFile(videoWithKeyframesPath, true, options => options
                    .WithCustomArgument(keyFrameArgument))
                .ProcessAsynchronously();

            await FFMpegArguments
                .FromFileInput(videoWithKeyframesPath)
                .OutputToFile(outputPath, true, options => options
                    .CopyChannel()
                    .WithCustomArgument("-f segment -segment_time " + secondsPerWindow)
                    .WithCustomArgument("-reset_timestamps 1"))
                .ProcessAsynchronously();
            
            File.Delete(videoWithKeyframesPath);
            
            Console.WriteLine("Finished splitting track " + fileName + fileExtension);
        }

        public static async Task JoinTracks(int[] windowOrder, string windowFolder, string outputFile)
        {
            var videoWindows = Directory.GetFiles(windowFolder);
            string[] videoWindowsOrdered = new string[windowOrder.Length];
            
            //If any value in windowOrder is -1, the result video needs to be just a black screen
            string blankVideoPath = windowFolder + "\\BlankVideo.mp4";
            await Utilities.GenerateBlankVideo(blankVideoPath, videoWindows[0]);

            //Re-order video windows according to the window order list
            for (int i = 0; i < windowOrder.Length; ++i)
            {
                int windowId = windowOrder[i];
                if (windowId == -1)
                {
                    videoWindowsOrdered[i] = blankVideoPath;
                }
                else
                {
                    videoWindowsOrdered[i] = videoWindows[windowId];
                }
            }

            FFMpeg.Join(outputFile, videoWindowsOrdered);
            Console.WriteLine("Finished joining tracks");
        }
    }
}