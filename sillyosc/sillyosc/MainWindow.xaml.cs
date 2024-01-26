using System.IO;
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

        readonly string fileName = "config.silly";

        //Vars set by config.ini
        string clientID = "1164610063269384252";
        string oscAddress = "127.0.0.1";
        int oscPort = 9000;

        int musicPlayerIndex = 2;
        // 0 disabled
        // 1 spotify
        // 2 winamp
        // 3 mpd
        
        bool scrollMusic = false;
        int maxScrollLength = 20;
        int scrollAmountPerFrame = 2;
        bool twentyFourHourTime = true;
        //End

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
            //createOrFindConfigFile();

            try
            {
                ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                cpuCounter = new PerformanceCounter("processor information", "% processor utility", "_total");
            }catch(Exception e)
            {
                error($"Couldn't Initalize CPU or RAM counter: {e.Message}");
            }

            InitializeComponent();
        }

        private void GithubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/aexyzk/discord-vrchat-presence") { UseShellExecute = true });
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
            try
            {
                sillyosc.Settings settingsMenuInst = new sillyosc.Settings();
                settingsMenuInst.Show();
            }
            catch
            {
                error("Couldn't open settings menu!");
            }
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
                Thread.Sleep(2000);
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
                            Thread.Sleep(15000);
                        }
                        else
                        {
                            error("Error: Couldn't create a connection to a Discord RPC client. (verify your clientID) Trying again...");
                            InitDiscordRPC();
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
            return ($"'{msg}'");
        }

        void InitDiscordRPC() {
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

        public  string GetMusic()
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
                try
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
                }
                catch
                {
                    return "Winamp isn't detected!";
                }
            }
            else
            {
                return "None";
            }
            return ";opahghusg";
        }

        public  string GetSystemInfo()
        {
            return $"CPU: {getCPUusage()}% RAM: {Math.Round(getUsedRAM() * 10) / 10:F1}/{Math.Round(getTotalRAM()):F1} GB GPU: {0}%";
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
                error("Couldn't destroy Discord RPC Client Connection, as it doesn't exist.");
            }
        }

        public  void createOrFindConfigFile()
        {
            if (File.Exists(fileName))
            {
                warn("Config already exists");
            }
            else
            {
                try
                {
                    warn("Can't find configuration file, creating a config file for you with defaults");
                    using (FileStream fs = File.Create(fileName))
                    {
                        string defaultConfig = "clientID: '1164610063269384252' \n oscAddress: \n '127.0.0.1' \n oscPort: 9000 \n scrollMusic: false \n maxScrollLength: 20 \n scrollAmountPerFrame: 2 \n twentyFourHourTime: true \n musicPlayerIndex: 0 \n # 0 disabled \n # 1 spotify \n # 2 winamp \n # 3 mpd (not setup yet)";
                        Byte[] configFileBytes = new UTF8Encoding(true).GetBytes(defaultConfig);
                        fs.Write(configFileBytes);
                    }
                }
                catch (Exception e)
                {
                    error($"Couldn't create a configuration file: (please restart the program): {e.Message}");
                }
            }
        }

        public void error(string message)
        {
            ErrorBox.Fill = new SolidColorBrush(Colors.Red);
            ErrorMsg.Text = $"Error: {message}";
        }

        public void warn(string message)
        {
            ErrorBox.Fill = new SolidColorBrush(Colors.Orange);
            ErrorMsg.Text = $"Warning: {message}";
        }
    }
}