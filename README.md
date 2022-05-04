# MusicTech2Blog - Video Aligner (Proper name pending)
This project seeks to automate a process which I've found myself doing many times over. I've created projects where I significantly edit the audio for dozens of different videos. I wanted to align the videos so they still matched the audio, regardless of how much they've been cut up, sped up, or slowed down. This required me to manually edit each. single. video file. It sucked. I never want to do that again... Unless it's automated.  

The goal of this project is for me to automatically line up the original video with the new audio track.

# Phase 1: Research
I will likely use FFmpeg to make the video edits through shell scripts. My research began with trying to figure out a way to carry across the edits that *should* be made based on the audio edits.  

Idea 1: Have a plugin that will dynamically listen to the user's DAW (in my case, FL Studio) and keep a log of whatever edits are made. I downloaded the FL Studio SDK to see if this was possible. To my knowledge this is a really weird use of an effects plugin and is not viable. Moving on to idea 2.  

Idea 2: With the edited audio file already exported by the DAW, analyze the waveform and detect the changes made by comparing to the original waveform of the video file. I figured I could accomplish this through a pattern matching algorithm.  

The [Naive algorithm](https://www.geeksforgeeks.org/naive-algorithm-for-pattern-searching/?ref=lbp) obviously wouldn't be ideal, as it's the slowest of the pattern matching algorithms. [KMP](https://www.geeksforgeeks.org/kmp-algorithm-for-pattern-searching/?ref=lbp) or the [Finite Automata](https://www.geeksforgeeks.org/finite-automata-algorithm-for-pattern-searching/?ref=lbp) algorithms could work, though I fear there would be too much variance in the waveforms to make them much better than Naive for this application. Finally, the [Rabin-Karp algorithm](https://www.geeksforgeeks.org/rabin-karp-algorithm-for-pattern-searching/?ref=lbp) could work really well, as it uses a hash function to identify patterns in the data. So that got me thinking, what kind of hash function could I use for audio data?  

As it turns out, there's been a ton of research done on this already, most notably by the app Shazam. Shazam can listen to you surroundings and identify which song is playing at that moment. There's even an open source version of the algorithm called [Panako](https://github.com/JorenSix/Panako). It's not *technically* a hash function, rather a "fingerprinting" algorithm. Hashing would be far too sensitive to changes such as the compression of a WAV file to MP3. Fingerprinting wouldtake into account factors such as an increase/decrease in tempo and pitch. This is kinda perfect for me. I could totally ~~steal~~ borrow the portion of the algorithm which fingerprints the data, using that in a pattern matching algorithm to detect which parts of the video tha audio matches up to.

## Relevant research papers
[Perceptual Audio Hashing Functions - Hamza Özer, Bulent Sankur, Nasir D. Memon, and Emin Anarim](https://www.researchgate.net/publication/26531832_Perceptual_Audio_Hashing_Functions)  
[Robust audio hashing for audio identification - Hamza Özer, Bülent Sankur, Nasir Memon](https://ieeexplore.ieee.org/document/7079698)


## Week of February 14
### Panako - A Promising Venture
So it's been a little bit since I updated this oops. Did you know this is supposed to be a blog? Oops anyways time to write some blog stuff.  

Panako seemed extremely promising. An algorithm that would match up my audio files regardless of pitch or speed? This is exactly what I need! I bet this would work perfectly. Nay, it would not be so...

I started testing Panako by digging into one of my old project files where I lined up dozens of versions of the same song. I copied the original copy of one of the tracks (OG.mp3) and exported the edited version of it (Edited.mp3). The edited version of the track had the same BPM and pitch, but one section was copy-pasted several times over the course of the track. I ran ``panako store`` on the two files, which would hash the files and store them within Panako's database. I then ran the command ``panako query`` on the edit, which would hopefully return some positive results with the OG file. This is where the issues first began...  

``panako query`` on the edited file returned nothing, as though nothing matched. How odd. I tried ``panako same Edited.mp3 OG.mp3`` to force the comparison, which returned a 0% match. Switching the order of the two audio files still returned 0% match. I exported a new file (Solo.mp3) which represented the segment I copy-pasted several times over. This is guaranteed to be featured in either version of the audio file. ``panako same Solo.mp3 OG.mp3`` returned 0%. Only when I ran ``panako same Solo.mp3 Edited.mp3`` did it return a positive result of 93%. It seemed to only match up the audio when it was ripped directly from the source, yet not when identical audio was in a separate file. That seems to defeat the purpoes, so something must still be wrong.  

Rob gave good advice that their may be valuable information being lost due to the mp3 encoding. This is especially due to the fact I'm effecively editing one mp3 file to generate a new mp3. There could be encoding shenanigans I don't know with the DAW I used (FL Studio), or other unforseen issues. He recommended I try the tests again with .WAV files. 

### Panako, Take 2: WAV Files
This time I dug into a project file of one of my original compositions. I exported 4 files: "Sea Shanty.wav", "Chorus.wav", "Verse.wav", and "Violin Solo.wav". I also exported some variations of tempo and pitch with the chorus. This way I could guarantee there was no MP3 shenanigans going on, as these were fully original audio files. I threw the files into Panako's database and re-ran the tests. There some more success, but not much. Some notable results are as follows:
- ``panako same Verse.wav 'Sea Shanty.wav'`` - 0%
- ``panako same 'Sea Shanty.wav' Verse.wav`` - 93%
- ``panako same Chorus.wav 'Sea Shanty.wav'`` - 93% 
- ``panako same 'Chorus Faster.wav' 'Sea Shanty.wav'`` - 0%
- ``panako same 'Chorus Slower.wav' 'Sea Shanty.wav'`` - 0%
- ``panako same 'Chorus Higher Pitch.wav' 'Sea Shanty.wav'`` - 0%
- ``panako same 'Chorus Lower Pitch.wav' 'Sea Shanty.wav'`` - 0%

First, the good news. When the ``panako same`` command is run correctly, it appears to operate more effectively on .WAV files than .MP3... If those files are of the same speed and pitch. The results for any files of varying pitch or speed are negative. As a bonus on the side, I can't remove the files from Panako's database! When I try ``panako delete`` on a file, it says the file is too large to delete??? I found a way to increase this file size limit, but even with an obnoxiously large size limit it tells me the resource isn't stored in Panako in the first place. Even though I literally just stored it in there?? Finally, if it only works on .WAV files, that sorta makes it unusable for me anyways. YouTube automatically converts their video and audio into .MP4 and .MP3 files, so I'm sorta forced to work with that format for my purposes. RIP Panako.

### [SoundFingerprinting](https://github.com/AddictedCS/soundfingerprinting) - A New Hope
Back to the drawing board. While poking aroud the internet trying to solve Panako issues, I found this alternative audio fingerprinting project: SoundFingerprinting. There are some very important differences between this project and Panako. First, Panako was operated via console commands, while SoundFingerprinting is a .NET framework written in C#. Second, SoundFingerprinting appears to be far more customizable than Panako. Through the use of Objects in the C# landscape, it allows users to override most parameters. For instane, the Stride values defines how many samples it analyzes for an individual fingerprint. By default it analyzes roughly every 1.46 seconds of audio. It also references an extremely useful article about how audio fingerprinting works, written by the primary author of the repository.  
[How does Audio Fingerprinting work - Sergiu Ciumac](https://emysound.com/blog/open-source/2020/06/12/how-audio-fingerprinting-works.html#)

## Week of February 28
### Some promising test code
I spent some quality time reading through SoundFingerprinting's documentation to ensure I installed and used it correctly. Unforunately, I am cursed with IT issues, and something *always* goes wrong if it is possible to do so. Firstly, it doesn't seem to (easily) work with the most recent build of FFMPEG, at least at the time of this post. I installed the *full version* of build 4.4.1, ensuring the paths on my windows machine were set correctly. FFMpeg is required for this, as the default "audio service" used by the repo only works with WAV files. Even when using WAV files though, there's weirdness with how it perceives the sample rate. It's honestly best to just stick with using FFMpeg as the audio service. (The audio service is the portion of SoundFingerprinting that decodes the audio file)

SoundFingerprinting first needs to decode and hash the original audio files to be stored in memory. This will be using FFMpeg as discussed above. It then needs to pair that hash with some metadata, stored in a model service. The model service is simply how SoundFingerprinting stores the files in memory. The audio service and model service are then used in building a query with a file to be tested.

For testing purposes I exported the current version of some music I'm working on. I exported small excerpts of that track. For the chorus I exported versions with different speeds and pitches. The code I wrote to test this has been uploaded to the repo (or will be, if you somehow read this as soon as it updates). The test results look very promising. It was able to match all the excerpts with the original tempo and pitch. The chorus files with modified speed were almost all succesfully matched, with the more extreme changes being unsuccesful. None of the files with a modified pitch were succesful in finding a match with the original sound file. The good news is, in my use case this is super easy to account for. I'm going to know exactly how much pitch I modulate a sound by. Actually wait a minute, now that I'm writing this, I realize that the pitch doesn't... actually... matter in determining speed and placement of a video file.

### Plan for the rest of semester
Effectively 2 stages of development: generating the data (SoundFingerprinting) and applying the data (FFMpeg).  

Stage 1 would analyze the original and doctored audios. The data it generates contains information on which sections of the video should be cut up and how it should be re-arranged. If SoundFingerprinting is able to produce specific timestamps for queried audio in a file, that functionality will be used directly. Otherwise the hashing function will be used in a pattern matching algorithm.

Stage 2 would use FFMpeg and the data generated in stage 1 to modify and export the new video files.  

Deadline for stage 1: April 9 (4 weeks, not including spring break)  
Week 1 - Determine how accurate the automatically produced timestamps can be  
Week 2 - Functionality to cut up audio in sections based on length of measure (maybe input how many measures each section should be)  
Week 3 - Generate hash codes for each section, match them together  
Week 4 - Generate information for a new video file based on which the matched sections  

Deadline for stage 2: April 30 (3 weeks)  
Week 1 - Generate new video file based on the generated information  
Weeks 2, 3 - Debugging weeks  

## Week of March 21
### Genesis of code
So it's been a couple weeks, oops. To be fair, one of those weeks was
for spring break. Regardless, back into the thick of things!  

First thing I did was refactoring the folder structure of this repo 
so it's functional as an actual code base. Anyone should now be able
to run the code upon cloning this repo. In other words, it's no longer
"just" a blog. Next thing is to code down the experiments for 
the accuracy of SoundFingerprinting in identifying specific timestamps  

After establishing the code for the timestamp accuracy experiment, I threw in the original Megalovania file. I dove back into one of my old project files and exported 8-measure samples of how I cut up the song. The results seemed promising for this, mainly just misplacing the first 8 measures into the ending phrases of Megalovania. (When it plays just the basic riff, but the first two notes are different). Ok, this shouldn't be too bad. I then tried it with a MIDI piano cover by Jester Musician. This way, when I apply the changes to the video file, I can observe how accuracte it is as the video shows the MIDI file being played. Unfortunately, these results are less promising. More samples were blatantly misplaced. I tried changing some parameters of the fingerpring builder, mainly the sample range and stride, but that only harmed the results. Rob suggested I try some testing on cleaner, simpler audio, such as a simple piano scale or piano chord. Also compare results between WAV and MP3 files.

I threw together a simple C major scale with a simple sine wave. A note plays for a second, then stops for a second, so the next note in the scale plays every two seconds. I did the same with some chords, putting down a major, minor, dimished, and augmented chord. I exported samples of each individual note and chord, and threw them into the algorithm to see how accurate it would be.  

The results (found [here](https://github.com/bartoa/MusicTech2Blog/blob/master/VideoAlignerTest/Experiments/Sine%20Wave%20Timestamp%20Analysis.txt)) are very interesting, in that it is mostly accuracte except for a couple errors in every test case. What's interesting is that the errors seem to be oddly consistent. In the scale, for example, it almost always misplaced the second and last notes. Similar situation for the chords - when there was an error is placing a chord's timestamp, it was usually the second chord played or the last chord played. When I tried changing the stride, for example, it began placing the individual chords accurately, but then misplaced one of the longer samples with multiple chords. The fact these errors are so consistent tells me it's likely a specific issue that can be slved, but I haven't yet been able to identify what that could be.

## Week of April 25
### First working prototype!
Ok so first, the elephant in the room. Yes, it's been a few weeks since the last update - oops.
All my other classes kicked into overdrive for deadlines on final projects, homeworks, exams etc.
I'm finally able to get back to this though! And there's a whole ton to update!!
Instead of going through my code journey in chronological order, I'll just walk through the execution from the top and talk through the roadblocks I overcame from there.

#### File execution & User input
Currently, the program is created by running the .exe file in the command terminal. Upon execution, the program will ask the user for the following:
- Original video file: The original, unedited video that needs to be rearranged
- Remixed audio track: The remixed audio file that the video should be synced up with
- Pitch: By how many semitones did the audio get changed by
- Original tempo
- New tempo
- Beats per measure
- How many measures to analyze at a time: For example, if the user only cut/moved/pasted/etc chunks of 4 measures at a time, then they should input 4 here.
- Name of the output file: Make sure it ends in a valid video extension!

Unfortunately, the video and audio files need to be in the same folder as the executable. The first thing I implement in the future will be a much more user friendly interface - perhaps as a command terminal tool like ffmpeg. 
This program also creates a temp folder in the same directory as the executable, storing all the WIP files generated. The folder is deleted at the end of execution.  

#### Adjusting pitch and speed of original video
First, the program splits apart the audio and video streams so they can be modified independently. The pitch and tempo of the original audio is changed to match the remixed audio. The speed of the video is also adjusted to match the new tempo. 
This, and most audio/video operations, are done with the library FFMpegCore. This library wraps most of FFMpeg's functionality into a suite of C# libraries that can be invoked within the source code itself. 
The libraries have a few pre-coded operations, such as splitting apart the video and audio streams, but for the most part I need to define my FFMpeg inputs as "Custom arguments". This isn't much of an issue, just needs some occasional string formatting wizardry.  

A lot of FFMpeg operates based off of "filters", which are effectively commands passed into the tool to execute various commands on the files. 
To change the tempo of the audio, it just uses the atempo filter. 
Changing the pitch is a bit more complicated. It took me a bit to figure out the exact syntax, but I was able to use a library within FFMpeg called rubberband to change the pitch without changing the tempo.
Changing the video speed is another step more complicated than that. It uses the setpts filter, but the parameter for this filter is a little odd. Whereas changing the tempo would be a straight multiplier (i.e. a value of 2.0 to change the tempo from 100 to 200), setpts requires the *inverse* of that value. So instead of 2.0, it wants a value of 0.5.  

#### Splitting the original video file
Next, VideoAligner splits up the original audio and video tracks into "windows". These windows match the number of measures the user inputted to be analyzed at the start of the program. It calculates how many seconds are in a selection of x measures, which it then uses to evenly divide the tracks. 
Splitting audio in this way with FFMpeg is fairly straightforward. The segment command is used to automatically cut up a video or audio track into a set of sub-files of equal length. For splitting the audio track, it's as simple as throwing this command into FFMpeg with a couple of parameters.  

Unfortunately, splitting the video is a bit more complicated, and was my first major roadblock. 
As it turns out, there's some shenanigans between FFMpeg and something in video files called keyframes. 
When FFMpeg is told to cut a video at a specific timestamp, it doesn't actually cut the video at the *exact* spot. 
Instead, the video gets cut at the keyframe *closest* to the specified timestamp. 
Normally this wouldn't be a big issue, but since I'm trying to be super precise with the timing here, there was some bad syncing in the final output file. 
VideoAligner needs to first re-encode a new video file with a keyframe at each spot to be cut. 
Because the video needs to be re-encoded, this adds some unfortunately significant execution time to the program.  

#### Hashing the original audio with SoundFingerprinting
This is when the much-prophesized, researched, and experimented library SoundFingerprinting comes into play. 
Each original audio window is given a unique ID, starting with id=0 and incrementing with chronological order. 
Those files are then hashed into SoundFingerprinting and stored within the library's database. 
Nothing too crazy to talk about here, all the issues with this step were ironed out in my preliminary experimentation.  

#### Querying the remixed audio with SoundFingerprinting's database
Here's where things get interesting. After VideoAligner splits the remixed audio into windows, it queries each of those windows with SoundFingerprinting. 
When a remixed window matches an original window, the ID of the original window is stored in an array called windowOrder. 
When the algorithm completes, this array stores the order for how the video windows of the matching IDs should be re-arranged.
Whenever a remixed window fails to query with an original window, the value -1 is stored in the array.

#### Gluing the video windows together
This is when it all comes together. VideoAligner reads the information in the windowOrder array to re-arrange the video windows in the correct order. 
It uses the FFMpegCore.Join function, which takes as input an array of strings representing the paths to the video windows. 
It glues those windows together in the order of the array, and puts the output in the given destination path.  

For the case of -1 appearing in the windowOrder array, VideoAligner creates a blank, black video matching the length of the video windows. 
It first generates a black image with a matching resolution of the video with the Bitmap and Graphics libraries. 
To generate a video based off this image, VideoAligner effectively creates a "slideshow" which infinitely loops the black image until a given duration is met. In this case, that duration is the length of the video windows. 
This was the next major roadblock. As it turns out, the -loop flag needs to be set *before* the flag defining the input file. 
From what I could figure out, FFMpegCore has no built-in functionality to set arguments before the input file. 
I had to instead manually invoke the FFMpeg command in a separate command process. 
Since the process is treated as an asynchronous command, I had to figure out how to get VideoAligner to wait for the end of execution.  

Sadly, despite all this setup for the blank video generation, there's still some bugs to iron out. 
It appears that the duration for the generated video is slightly off. Even if it's only a few milliseconds off, enough repeats of this video will cause an incorrect sync between the final video and audio. 
This will be the first edge case that gets developed in the future.  

#### Final output file & Cleanup
Finally, the full remixed audio track is paired with the glued-together video track. 
Fortunately this is super simple, just using FFMpegCore's function ReplaceAudio(). 
The final track is placed in the same directory as the exe file (To be changed alongside a better user experience), and the temp folder is deleted. 

And with all that, I have a prototype for the basic use case of VideoAligner! 
A lot of edge cases need to be ironed out, but an important milestone has been reached.