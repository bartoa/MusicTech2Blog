using SoundFingerprinting.Audio;
using SoundFingerprinting.InMemory;

namespace VideoAligner
{

    public class OrderGenerator
    {
        public static async Task<int[]> findWindowOrder(String newWindowPath, InMemoryModelService modelService,
            IAudioService audioService)
        {
            //Size of the results array is number of files in the newWindow directory.
            //AKA the number of windows to move around
            int[] result = new int[Directory.GetFiles(newWindowPath).Length];
            
            var newWindows = Directory.EnumerateFiles(newWindowPath);
            int counter = 0;
            
            foreach (string newWindow in newWindows)
            {
                Console.WriteLine("Querying newWindow " + counter);
                var queryResult = await Utilities.BuildQuery(newWindow, modelService, audioService);
                
                var maxCoverage = 0.0;
                var maxCoverageEntry = "Temp";
                foreach (var (entry, _) in queryResult.ResultEntries)
                {
                    if (entry == null)
                    {
                        continue;
                    }
                    
                    var trackRelativeCoverage = entry.TrackRelativeCoverage;
                    // check only those tracks that matched 0.25% or more of a window
                    if (/*trackRelativeCoverage >= 0.25 && */trackRelativeCoverage > maxCoverage)
                    {
                        maxCoverage = trackRelativeCoverage;
                        maxCoverageEntry = entry.Track.Id;
                    }
                }

                if (!queryResult.ResultEntries.Any() || maxCoverage == 0)
                {
                    result[counter] = -1;
                }
                else
                {
                    result[counter] = Convert.ToInt32(maxCoverageEntry);
                }
                
                counter += 1;
            }

            Console.WriteLine("Finished querying windows");
            return result;
        }
    }
}