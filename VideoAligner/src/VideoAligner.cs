using FFMpegCore;
using SoundFingerprinting.Configuration;
using SoundFingerprinting.Data;
using SoundFingerprinting.Emy;
using SoundFingerprinting.InMemory;

namespace VideoAligner
{

    public class VideoAligner
    {
        public static async Task Main()
        {
            Console.WriteLine("Hello Music Mashup Master!");

            //Read in user inputs
            string rootPath = Directory.GetCurrentDirectory();
            string tempFolderPath = rootPath + "\\temp";
            Directory.CreateDirectory(tempFolderPath);
            
            Console.WriteLine("Input video file of the original track:");
            var oldTrack = Console.ReadLine();
            if (oldTrack == null)
            {
                throw new NullReferenceException();
            }
            var ogUnmutedVideoTrackPath = tempFolderPath + "\\" + oldTrack;
            var ogAudioTrackPath = tempFolderPath + "\\" + Path.GetFileNameWithoutExtension(oldTrack) + ".mp3";
            var ogVideoTrackPath = tempFolderPath + "\\" + Path.GetFileNameWithoutExtension(oldTrack) + " (Muted).mp4";
            File.Copy(rootPath + "\\" + oldTrack, ogUnmutedVideoTrackPath, true);
            FFMpeg.ExtractAudio(ogUnmutedVideoTrackPath, ogAudioTrackPath);
            FFMpeg.Mute(ogUnmutedVideoTrackPath, ogVideoTrackPath);
            

            Console.WriteLine("Input audio file of the new track:");
            var newTrack = Console.ReadLine();
            if (newTrack == null)
            {
                throw new NullReferenceException();
            }
            var newTrackPath = tempFolderPath + "\\" + newTrack;
            File.Copy(rootPath + "\\" + newTrack, newTrackPath, true);

            Console.WriteLine("By how many semitones did the pitch change?\n" +
                              "Negative value for lower pitch, positive value for higher pitch");
            var pitch = Convert.ToDouble(Console.ReadLine());

            Console.WriteLine("Tempo of original track?");
            var oldTempo = Convert.ToDouble(Console.ReadLine());

            Console.WriteLine("Tempo of new track?");
            var newTempo = Convert.ToDouble(Console.ReadLine());

            Console.WriteLine("How many beats in a measure?");
            var timeSig = Convert.ToDouble(Console.ReadLine());
            
            Console.WriteLine("How many measures should be analyzed at a time?");
            var windowSize = Convert.ToDouble(Console.ReadLine());

            Console.WriteLine("Finally, name the output file");
            var outputName = Console.ReadLine();
            if (outputName == null)
            {
                throw new NullReferenceException();
            }
            var outputTrackPath = rootPath + "\\" + outputName;
            
            //Set pitch and tempo of old track
            //Also change speed of video file so it still lines up with the audio
            var ogTrackPitchPath = await Utilities.ChangePitch(ogAudioTrackPath, pitch);
            var ogTrackEditedPath = await Utilities.ChangeTempo(ogTrackPitchPath, oldTempo, newTempo);
            double speedMultiplier = newTempo / oldTempo;
            var ogVideoEditedPath = await Utilities.ChangeVideoSpeed(ogVideoTrackPath, speedMultiplier);

            //Split edited original track (both audio and video files) into windows for hashing
            double beatsPerSecond = newTempo / 60;
            double secondsPerMeasure = timeSig / beatsPerSecond;
            double secondsPerWindow = secondsPerMeasure * windowSize;
            
            string ogAudioWindowPath = tempFolderPath + "\\OGAudioWindows";
            string ogVideoWindowPath = tempFolderPath + "\\OGVideoWindows";
            Directory.CreateDirectory(ogAudioWindowPath);
            Directory.CreateDirectory(ogVideoWindowPath);
            await TrackWindowTool.SplitAudioTracks(ogTrackEditedPath, secondsPerWindow, ogAudioWindowPath);
            await TrackWindowTool.SplitVideoTracks(ogVideoEditedPath, secondsPerWindow, ogVideoWindowPath);
            
            //Build hashes out of the audio windows
            var audioService = new FFmpegAudioService();
            var fingerprintConfig = new DefaultAVFingerprintConfiguration();
            var modelService = new InMemoryModelService();
            
            var windows = Directory.EnumerateFiles(ogAudioWindowPath);
            int counter = 0;
            foreach (string window in windows)
            {
                string counterString = Convert.ToString(counter);
                Console.WriteLine("Hashing window " + counterString);
                var windowTrack = new TrackInfo(counterString, "OG_W" + counterString, "Lorem Ipsum");
                var windowHash = await Utilities.BuildHash(window, audioService, fingerprintConfig);
                modelService.Insert(windowTrack, windowHash);
                counter += 1;
            }
            Console.WriteLine("Finished hashing OG windows");
            
            //Split new track into windows for querying
            string newWindowPath = tempFolderPath + "\\NewWindows";
            Directory.CreateDirectory(newWindowPath);
            await TrackWindowTool.SplitAudioTracks(newTrackPath, secondsPerWindow, newWindowPath);
            
            //Query each newTrack window with OGTrack windows.
            //Return variable is an array which stores information on how video should be re-arranged
            var windowOrder = await OrderGenerator.findWindowOrder(newWindowPath, modelService, audioService);
            
            //Reorder video windows to match the window order.
            string finalVideoFilePath = tempFolderPath + "\\finalVideoFile.mp4";
            await TrackWindowTool.JoinTracks(windowOrder, ogVideoWindowPath, finalVideoFilePath);
            
            //Combine the reordered video with the user's edited audio file
            //Outputs file into the exe's directory
            FFMpeg.ReplaceAudio(finalVideoFilePath, newTrackPath, outputTrackPath);

            //At the end of everything, delete the temp folder
            Directory.Delete(tempFolderPath, true);
        }
    }
}