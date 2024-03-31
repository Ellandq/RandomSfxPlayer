using System;
using System.Collections;
using System.IO;
using System.Net.Mime;
using System.Threading;
using NAudio.Wave;

namespace RandomSfxPlayer
{
    class Program
    {
        private const string DirectoryPath = "\\sfx";
        private const string ConfigFilePath = "\\sfxConfig.txt";
        private const int SfxDefaultOdds = 1200;
        private static string configFilePath;
        
        static void Main(string[] args)
        {
            Console.WriteLine(Environment.CurrentDirectory);
            var currentDirectory = Environment.CurrentDirectory + DirectoryPath;
            configFilePath = Environment.CurrentDirectory + ConfigFilePath;
            
            Console.WriteLine(currentDirectory);
            
            if (!Directory.Exists(currentDirectory))
            {
                Console.WriteLine("Directory does not exist. ");
                Console.ReadLine();
                return;
            }

            if (!File.Exists(configFilePath))
            {
                Console.WriteLine("Config file does not exist. ");
                Console.ReadLine();
                return;
            }
            
            var mp3Files = Directory.GetFiles(currentDirectory, "*.mp3");
            SFX[] soundEffects = new SFX[mp3Files.Length];
            
            if (mp3Files.Length == 0) return;
            var index = 0;
            foreach (string file in mp3Files)
            {
                soundEffects[index] = new SFX(Path.GetFileName(file), file);
                soundEffects[index].Activate();
                index++;
            }
            LoadSettings(soundEffects);

            while (true)
            {
                WriteOptions(mp3Files);

                string input = Console.ReadLine();

                if (input == "q") break;
                if (int.TryParse(input, out var result))
                {
                    OpenOptions(soundEffects[result - 1]);
                    SaveSettings(soundEffects);
                }
            }
        }

        private static void WriteOptions(string[] options)
        {
            Console.Clear();
            Console.WriteLine("Found the following .mp3 files in the directory:");
            var index = 0;
            foreach (string file in options)
            {
                index++;
                Console.WriteLine(index + ". " + Path.GetFileName(file));
                    
            }
            Console.WriteLine("q - quit");
        }

        private static void OpenOptions(SFX sfx)
        {
            Console.Clear();
            
            Console.WriteLine($"1. Odds - 1 : {sfx.PlayOdds}\n2. Volume - {100*sfx.Volume}%");
            
            if (!int.TryParse(Console.ReadLine(), out var result)) return;

            Console.WriteLine("New value: ");
            
            if (result == 1 && int.TryParse(Console.ReadLine(), out var odds)) sfx.PlayOdds = odds;
            else if (result == 2 && int.TryParse(Console.ReadLine(), out var volume)) sfx.Volume = volume / 100f;
            Console.Clear();
        }

        private static void SaveSettings(SFX[] soundEffects)
        {
            using (StreamWriter writer = new StreamWriter(configFilePath))
            {
                foreach (SFX sfx in soundEffects)
                {
                    writer.WriteLine(sfx.ToString());
                }
            }
        }

        
        private static void LoadSettings(SFX[] soundEffects)
        {
            if (!File.Exists(configFilePath))
            {
                Console.WriteLine("Configuration file not found.");
                return;
            }

            string[] lines = File.ReadAllLines(configFilePath);

            foreach (string line in lines)
            {
                string[] parts = line.Split('|');
                if (parts.Length == 3)
                {
                    string name = parts[0];
                    int playOdds;
                    float volume;

                    if (int.TryParse(parts[1], out playOdds) && float.TryParse(parts[2], out volume))
                    {
                        SFX sfx = soundEffects.FirstOrDefault(s => s.Name == name);
                        if (sfx != null)
                        {
                            sfx.PlayOdds = playOdds;
                            sfx.Volume = volume;
                        }
                    }
                }
            }
        }

        private class SFX
        {
            private Task sfxTask;
            private CancellationTokenSource cancellationTokenSource;
            
            public string Name { get; set; }
            public string Path { get; set; }
            public int PlayOdds { get; set; }
            public bool IsActive { get; set; }
            public float Volume { get; set; }

            public SFX(string name, string path)
            {
                Name = name;
                Path = path;
                PlayOdds = SfxDefaultOdds;
                Volume = 1f;
            }
            
            public SFX(string name, string path, int odds, float volume)
            {
                Name = name;
                Path = path;
                PlayOdds = odds;
                Volume = volume;
            }

            public void Activate()
            {
                if (sfxTask == null || sfxTask.IsCompleted)
                {
                    IsActive = true;
                    cancellationTokenSource = new CancellationTokenSource();
                    sfxTask = Task.Run(() => SfxPlayer(cancellationTokenSource.Token));
                    
                    
                }
            }

            public void Deactivate()
            {
                if (cancellationTokenSource != null)
                {
                    IsActive = false;
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = null;
                }
            }
            
            private async Task SfxPlayer(CancellationToken cancellationToken)
            {
                
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(250);
                    var random = new Random();
                    var randomNumber = random.Next(0, 100 * PlayOdds);
                    if (randomNumber >= 100) continue;
                    using (var audioFile = new AudioFileReader(Path))
                    using (var outputDevice = new WaveOutEvent())
                    {
                        outputDevice.Init(audioFile);
                        outputDevice.Play();
                        outputDevice.Volume = Volume;

                        while (outputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            
                        }
                    }
                }
            }

            public override string ToString()
            {
                return $"{Name}|{PlayOdds}|{Volume}";
            }
        }
    }
}