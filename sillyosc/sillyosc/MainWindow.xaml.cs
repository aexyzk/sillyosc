﻿using System.IO;
using System.Windows;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Media;

using SharpOSC;
using DiscordRPC;
using DiscordRPC.Logging;
using System.Text;

// Pls dont mind my spagetti code :3

namespace guitest
{
    public partial class MainWindow : System.Windows.Window
    {
        bool Running = false;
        bool isSettingsOpen = false;

        static string appdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SillyOSC");
        static string configFile = Path.Combine(appdata, "config.silly");

        string clientID = "";
        string oscAddress = "127.0.0.1";
        int oscPort = 9000;

        int musicPlayerIndex = 0;
        // 0 disabled
        // 1 spotify
        // 2 winamp
        // 3 mpd
        
        bool scrollMusic = false;
        int maxScrollLength = 20;
        int scrollAmountPerFrame = 2;
        bool twentyFourHourTime = true;

        bool paused = false;
        string lastSong = "";
        string scrollingTitle = "";

        // System Status Shiz
        PerformanceCounter? ramCounter;
        PerformanceCounter? cpuCounter;

        // Discord
        public DiscordRpcClient? client;

        public MainWindow()
        {
            Directory.CreateDirectory(appdata); 

            try
            {
                ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                cpuCounter = new PerformanceCounter("processor information", "% processor utility", "_total");
            }catch(Exception e)
            {
                error($"Couldn't Initalize CPU or RAM counter: {e.Message}");
            }

            InitializeComponent();

            loadSettings();
        }

        private void GithubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://aexyzk.github.io/sillyosc") { UseShellExecute = true });
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (Running)
            {
                ToggleButton.Content = "Start";
                Status.Text = "Not Running.";
                SettingsButton.IsEnabled = true;
            }
            else
            {
                ToggleButton.Content = "Stop";
                Status.Text = "Running...";
                SettingsButton.IsEnabled = false;

                //run osc
                var oscTask = Task.Run(() => RunOSC());
                //run rpc
                var rpcTask = Task.Run(() => RunRPC());
            }

            Running = !Running;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isSettingsOpen)
            {
                MainMenu.Visibility = Visibility.Hidden;
                SettingsMenu.Visibility = Visibility.Visible;
                SettingsButton.Content = "Back";
                ToggleButton.IsEnabled = false;
            }
            else
            {
                MainMenu.Visibility = Visibility.Visible;
                SettingsMenu.Visibility = Visibility.Hidden;
                SettingsButton.Content = "Settings";
                ToggleButton.IsEnabled = true;
            }

            isSettingsOpen = !isSettingsOpen;
        }

        public void RunOSC()
        {
            while (Running)
            {
                try
                {
                    if (musicPlayerIndex != 0)
                    {
                        if (scrollMusic)
                        {
                            string message_to_send = $"Music: |{Scroll(GetMusic())}| {GetTime()} | {GetSystemInfo()}";
                            Dispatcher.Invoke(() => OutputBox.Text = SendMessageOSC(message_to_send));
                        }
                        else
                        {
                            string message_to_send = $"Music: {GetMusic()} | {GetTime()} | {GetSystemInfo()}";
                            Dispatcher.Invoke(() => OutputBox.Text = SendMessageOSC(message_to_send));
                        }
                    }
                    else
                    {
                        string message_to_send = $"{GetTime()} | {GetSystemInfo()}";
                        Dispatcher.Invoke(() => OutputBox.Text = SendMessageOSC(message_to_send));
                    }
                }catch (Exception e)
                {
                    error($"something went wrong with sending osc: {e.Message}: stopping osc/rpc...");
                    Running = false;
                }
                // 2 seconds but without one second that gets taken up by the GetSystemInfo() method
                Thread.Sleep(1000);
            }
        }

        public void RunRPC()
        {
            try
            {
                InitDiscordRPC();

                while (Running)
                {
                    try
                    {
                        if (client != null)
                        {
                            client.SetPresence(new RichPresence()
                            {
                                Details = $"{GetSystemInfo()}",
                                State = $"{GetTime()} | Music: {GetMusic()}",
                            });
                            // 15 seconds but without one second that gets taken up by the GetSystemInfo() method
                            Thread.Sleep(14000);
                        }
                        else
                        {
                            error("Error: Couldn't create a connection to a Discord RPC client. verify your clientID then try again");
                        }
                    }
                    catch (Exception e)
                    {
                        error($"Couldn't find Disord RPC client: {e}");
                    }
                }
            }
            catch (Exception e)
            {
                error($"something went wrong with sending rpc: {e.Message}: stopping osc/rpc...");
                Running = false;
            }

            if (client != null)
            {
                Deinitialize();
            }
        }

        string SendMessageOSC(string msg)
        {
            var message = new SharpOSC.OscMessage("/chatbox/input", msg, true);
            var sender = new SharpOSC.UDPSender(oscAddress, oscPort);
            sender.Send(message);
            return ($"{msg}");
        }

        void InitDiscordRPC()
        {
            try
            {
                if (clientID != "")
                {
                    client = new DiscordRpcClient(clientID);

                    client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

                    client.OnReady += (sender, e) =>
                    {
                        Console.WriteLine("Recived ready from user {0}", e.User.Username);
                    };

                    client.OnPresenceUpdate += (sender, e) =>
                    {
                        Console.WriteLine("Recived update: {0}", e.Presence);
                    };

                    client.Initialize();
                }
            }catch (Exception e)
            {
                error($"Couldn't create a vaild connection to Discord RPC (bad clientID?) {e.Message}");
            }
        }

        string GetTime()
        {
            DateTime currentTime = DateTime.Now;
            if (twentyFourHourTime)
            {
                return currentTime.ToString("HH:mm");
            }
            else
            {
                return currentTime.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            }
        }

        public string GetMusic()
        {
            if (musicPlayerIndex == 1) // Spotify
            {
                Process[] spotifyProcess = FindProcess("Spotify");

                if (spotifyProcess.Length > 0)
                {
                    if (spotifyProcess.Length > 1)
                    {
                        string[] spotifyTitleArray = spotifyProcess[0].MainWindowTitle.Split(" - ");
                        if (spotifyTitleArray.Length > 1)
                        {
                            paused = false;
                            return $"{spotifyTitleArray[0]} - {spotifyTitleArray[1]}";
                        }
                        else
                        {
                            paused = true;
                            return "Paused";
                        }
                    }
                }
                else {
                    return "Spotify isn't detected!";
                }
            }
            else if (musicPlayerIndex == 2) // Winamp
            {
                Process[] winampProcess = FindProcess("Winamp");

                if (winampProcess.Length > 0)
                {
                    if (winampProcess[0].MainWindowTitle.ToString().Contains("[Stopped]"))
                    {
                        paused = true;
                        return "[Stopped]";
                    }
                    else if (winampProcess[0].MainWindowTitle.ToString().Contains("[Paused]"))
                    {
                        paused = true;
                        return "Paused";
                    }
                    else
                    {
                        paused = false;

                        string[] words = (winampProcess[0].MainWindowTitle.ToString()).Split(". ");

                        return words[1].Substring(0, words[1].Length - 9);
                    }
                }
                else
                {
                    return "Winamp isn't detected!";
                }
            }
            else if (musicPlayerIndex == 3) // MPD
            {
                return "MPD not set up yet";
            }
            else
            {
                return "None";
            }
            return "Error Detecting Music uhh idk x3";
        }

        public  string GetSystemInfo()
        {
            return $"CPU: {getCPUusage()}% GPU: {getGPUusage()}% RAM: {Math.Round(getUsedRAM() * 10) / 10:F1}/{Math.Round(getTotalRAM()):F1} GB";
        }

        public double getUsedRAM()
        {
            if (ramCounter != null)
            {
                double usedRAM = getTotalRAM() - ramCounter.NextValue() / 1024.0;
                return usedRAM;
            }
            else
            {
                error("Couldn't find ram or you have no ram, which i really hope you do cause otherwise you are a magic man :3");
                return 0;
            }
        }

        static double getTotalRAM()
        {
            return new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024.0 * 1024 * 1024.0);
        }

        int getCPUusage()
        {
            if (cpuCounter != null)
            {
                return (int)cpuCounter.NextValue();
            }
            else
            {
                error("Couldn't find CPU");
                return (0);
            }
        }

        public static double getGPUusage()
        {
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var counterNames = category.GetInstanceNames();
                var gpuCounters = new List<PerformanceCounter>();
                var result = 0f;

                foreach (string counterName in counterNames)
                {
                    if (counterName.EndsWith("engtype_3D"))
                    {
                        foreach (PerformanceCounter counter in category.GetCounters(counterName))
                        {
                            if (counter.CounterName == "Utilization Percentage")
                            {
                                gpuCounters.Add(counter);
                            }
                        }
                    }
                }

                gpuCounters.ForEach(x =>
                {
                    _ = x.NextValue();
                });

                // idk why i need to sleep for a sec but it makes results more accurate soooooooo
                Thread.Sleep(1000);

                gpuCounters.ForEach(x =>
                {
                    result += x.NextValue();
                });

                return Math.Round(result);
            }
            catch
            {
                return 0f;
            }
        }

        string Scroll(string text)
        {
            if (paused != true)
            {
                if (lastSong != text)
                {
                    scrollingTitle = $" - {text}";
                    lastSong = text;
                }

                string scrolledPart = scrollingTitle.Substring(0, scrollAmountPerFrame);
                scrollingTitle = scrollingTitle.Substring(scrollAmountPerFrame) + scrolledPart;

                return scrollingTitle.Substring(0, Math.Min(scrollingTitle.Length, maxScrollLength));
            }
            else if (paused)
            {
                return text;
            }
            return "Scrolling broke, somehow?";
        }

        static Process[] FindProcess(string windowTitle)
        {
            Process[] p = Process.GetProcessesByName(windowTitle);
            return p;
        }

        // Dont cause a memory leak cause that bad i reckon probubly
        void Deinitialize()
        {
            if (client != null)
            {
                client.Dispose();
            }
            else
            {
                warn("Couldn't destroy Discord RPC Client Connection, as it doesn't exist.");
            }
        }

        public void saveSettings()
        {
            if (Disabled_Toggle.IsChecked == true)
            {
                musicPlayerIndex = 0;
            }
            else if (Spotify_Toggle.IsChecked == true)
            {
                musicPlayerIndex = 1;
            }
            else if (Winamp_Toggle.IsChecked == true)
            {
                musicPlayerIndex = 2;
            }
            else if (MPD_Toggle.IsChecked == true)
            {
                musicPlayerIndex = 3;
            }

            clientID = DiscordRPC_ID.Text;
            if (client != null)
            {
                Deinitialize();
            }
            
            oscAddress = OSCaddressTEXTBOX.Text;

            try
            {
                oscPort = Int32.Parse(OSCportTEXTBOX.Text);
            }
            catch (Exception e)
            {
                warn($"port should be an int: {e.Message}");
            }

            twentyFourHourTime = (bool)TwentyFourHourTime_Toggle.IsChecked;
            scrollMusic = (bool)ScrollMusic_Toggle.IsChecked;

            using (FileStream fs = File.Create(configFile))
            {
                string defaultConfig = $"{musicPlayerIndex}\n{clientID}\n{oscAddress}\n{oscPort}\n{twentyFourHourTime}\n{scrollMusic}";
                Byte[] configFileBytes = new UTF8Encoding(true).GetBytes(defaultConfig);
                fs.Write(configFileBytes);
            }
        }

        public void loadSettings()
        {
            if (File.Exists(configFile))
            {
                string[] options = File.ReadAllLines(configFile);

                if (options.Length >= 5)
                {
                    musicPlayerIndex = Int32.Parse(options[0]);
                    if (options[1] == "")
                    {
                        error("Make sure you set Discord ID in the settings!");
                    }
                    else
                    {
                        clientID = options[1];
                    }
                    oscAddress = options[2];
                    oscPort = Int32.Parse(options[3]);
                    twentyFourHourTime = bool.Parse(options[4]);
                    scrollMusic = bool.Parse(options[5]);

                    displaySettingsInMenu();
                }
                else
                {
                    error("Config not setup correctly");
                }
            }
            else
            {
                try
                {
                    using (FileStream fs = File.Create(configFile))
                    {
                        string defaultConfig = $"0\n\n127.0.0.1\n9000\nTrue\nFalse";
                        Byte[] configFileBytes = new UTF8Encoding(true).GetBytes(defaultConfig);
                        fs.Write(configFileBytes, 0, configFileBytes.Length);
                    }
                    loadSettings();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public void displaySettingsInMenu()
        {
            if (musicPlayerIndex == 0) {
                Disabled_Toggle.IsChecked = true;
            }
            else if (musicPlayerIndex == 1)
            {
                Spotify_Toggle.IsChecked = true;
            }
            else if (musicPlayerIndex == 2)
            {
                Winamp_Toggle.IsChecked = true;
            }
            else if (musicPlayerIndex == 3)
            {
                MPD_Toggle.IsChecked = true;
            }

            DiscordRPC_ID.Text = clientID;

            OSCaddressTEXTBOX.Text = oscAddress;
            OSCportTEXTBOX.Text = oscPort.ToString();

            TwentyFourHourTime_Toggle.IsChecked = twentyFourHourTime;
            ScrollMusic_Toggle.IsChecked = scrollMusic;
        }

        public void error(string message)
        {
            ErrorBox.Background = new SolidColorBrush(Colors.Red);
            ErrorMsg.Text = $"Error: {message}";
        }

        public void warn(string message)
        {
            ErrorBox.Background = new SolidColorBrush(Colors.Orange);
            ErrorMsg.Text = $"Warning: {message}";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            saveSettings();
        }
    }
}