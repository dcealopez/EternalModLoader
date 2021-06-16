using System.Diagnostics;
using System.IO;

namespace EternalModLoader.Mods.Sounds
{
    /// <summary>
    /// Sound encoding class
    /// </summary>
    public static class SoundEncoding
    {
        /// <summary>
        /// List of supported file formats for sound mods
        /// </summary>
        public static string[] SupportedFileFormats = { ".ogg", ".opus", ".wav", ".wem", ".flac", ".aiff", ".pcm"};

        /// <summary>
        /// List of supported file formats to convert to .ogg (Opus)
        /// </summary>
        public static string[] SupportedOggConversionFileFormats = { ".ogg", ".opus", ".wav", ".flac", ".aiff", ".pcm" };

        /// <summary>
        /// Decodes the given opus sound mod file using opusdec
        /// </summary>
        /// <param name="opusDecPath">path to the opusdec executable</param>
        /// <param name="soundMod">sound mod</param>
        /// <returns>the decoded size of the opus file</returns>
        public static int GetDecodedOpusSoundModFileSize(string opusDecPath, SoundModFile soundMod)
        {
            int decodedSize = -1;

            // Write the encoded file to a temp file in the disk
            var tempEncSoundFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(soundMod.Name) + ".ogg");
            var tempDecSoundFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(soundMod.Name) + ".wav");

            // Delete the temp files first in case they exist for some reason
            if (File.Exists(tempEncSoundFilePath))
            {
                File.Delete(tempEncSoundFilePath);
            }

            if (File.Exists(tempDecSoundFilePath))
            {
                File.Delete(tempDecSoundFilePath);
            }

            File.WriteAllBytes(tempEncSoundFilePath, soundMod.FileData.GetBuffer());

            // Decode the file to .wav to get the decoded size
            var opusDecProcess = new Process();
            opusDecProcess.StartInfo.UseShellExecute = false;
            opusDecProcess.StartInfo.FileName = opusDecPath;
            opusDecProcess.StartInfo.Arguments = $"--quiet \"{tempEncSoundFilePath}\" \"{tempDecSoundFilePath}\"";
            opusDecProcess.StartInfo.CreateNoWindow = false;
            opusDecProcess.StartInfo.RedirectStandardError = true;
            opusDecProcess.StartInfo.RedirectStandardOutput = true;
            opusDecProcess.Start();
            opusDecProcess.WaitForExit();

            // If the .wav file doesn't exist, that means that the conversion failed
            if (!File.Exists(tempDecSoundFilePath))
            {
                File.Delete(tempEncSoundFilePath);
                return -1;

            }

            decodedSize = (int)new FileInfo(tempDecSoundFilePath).Length + 20;
            File.Delete(tempDecSoundFilePath);
            File.Delete(tempEncSoundFilePath);

            return decodedSize;
        }

        /// <summary>
        /// Encodes the given sound mod file to Opus
        /// </summary>
        /// <param name="opusEncPath">path to the opusenc executable</param>
        /// <param name="soundMod">sound mod</param>
        /// <returns>the encoded Opus file data</returns>
        public static byte[] EncodeSoundModFileToOpus(string opusEncPath, SoundModFile soundMod)
        {
            // Write the .wav file to a temp file in the disk
            var tempSoundFilePath = Path.Combine(Path.GetTempPath(), soundMod.Name);
            var tempEncSoundFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(soundMod.Name) + ".ogg");

            // Delete the temp files first in case they exist for some reason
            if (File.Exists(tempSoundFilePath))
            {
                File.Delete(tempSoundFilePath);
            }

            if (File.Exists(tempEncSoundFilePath))
            {
                File.Delete(tempEncSoundFilePath);
            }

            File.WriteAllBytes(tempSoundFilePath, soundMod.FileData.GetBuffer());

            // Encode the file to .ogg
            var opusEncProcess = new Process();
            opusEncProcess.StartInfo.UseShellExecute = false;
            opusEncProcess.StartInfo.FileName = opusEncPath;
            opusEncProcess.StartInfo.Arguments = $"--quiet \"{tempSoundFilePath}\" \"{tempEncSoundFilePath}\"";
            opusEncProcess.StartInfo.RedirectStandardError = true;
            opusEncProcess.StartInfo.RedirectStandardOutput = true;
            opusEncProcess.StartInfo.CreateNoWindow = false;
            opusEncProcess.Start();
            opusEncProcess.WaitForExit();

            // If the .ogg file doesn't exist, that means that the conversion failed
            if (!File.Exists(tempEncSoundFilePath))
            {
                File.Delete(tempSoundFilePath);
                return null;
            }

            // Grab the Opus file data
            byte[] opusFileData;

            using (var streamReader = new StreamReader(tempEncSoundFilePath))
            {
                using (var sndFileMemoryStream = new MemoryStream((int)streamReader.BaseStream.Length))
                {
                    streamReader.BaseStream.CopyTo(sndFileMemoryStream);
                    opusFileData = sndFileMemoryStream.GetBuffer();
                }
            }

            File.Delete(tempEncSoundFilePath);
            File.Delete(tempSoundFilePath);

            return opusFileData;
        }
    }
}
