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
I spent some quality time reading through SoundFingerprinting's documentation to ensure I installed and used it correctly. Unforunately, I am cursed with IT issues, and something *always* goes wrong if it is possible to do so. Firstly, it doesn't seem to (easily) work with the most recent build of FFMPEG, at least at the time of this post. I installed the *full version* of build 4.4.1, ensuring the paths on my windows machine were set correctly. FFMpeg is required for this, as the default "audio service" used by the repo only works with WAV files. Even when using WAV files though, there's weirdness with how it perceives the sample rate. It's honestly best to just stick with using FFMpeg as the aduio service. (The audio service is the portion of SoundFingerprinting that decodes the audio file)

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