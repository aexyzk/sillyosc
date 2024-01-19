using System.Windows;
using System.Diagnostics;
using System.Timers;
using System.Threading;
using System.Globalization;

using SharpOSC;
using DiscordRPC;
using DiscordRPC.Logging;
using System.Windows.Media;

namespace guitest
{
    public partial class MainWindow : System.Windows.Window
    {
        static bool Running = false;
        static string oscADDRESS = "127.0.0.1";
        static int oscPORT = 9000;

        static int musicPlayerIndex = 0;
        // 0 spotify
        // 1 mpd

        static bool scrollMusic = true;
        static string last_song = "";
        static string scrolling_title = "";
        static int max_scroll_length = 20;
        static int scroll_amount_per_frame = 2;
        static bool twenty_four_hour_time = true;
        static bool paused = false;

        // System Status Shiz
        static PerformanceCounter? ramCounter;
        static PerformanceCounter? cpuCounter;

        // Discord
        string clientID = "1164610063269384252";
        public DiscordRpcClient client;

        public MainWindow()
        {
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            cpuCounter = new PerformanceCounter("processor information", "% processor utility", "_total");

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

        }

        public void RunOSC()
        {
            while (Running)
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
                Thread.Sleep(2000);
            }
        }

        public void RunRPC()
        {
            InitDiscordRPC();

            while (Running)
            {
                client.SetPresence(new RichPresence()
                {
                    Details = $"{GetTime()} {GetSystemInfo()}",
                    State = $"Music: {GetMusic()}",
                });
                Thread.Sleep(15000);
            }

            Deinitialize();
        }

        static string SendMessageOSC(string msg)
        {
            var message = new SharpOSC.OscMessage("/chatbox/input", msg, true);
            var sender = new SharpOSC.UDPSender(oscADDRESS, oscPORT);
            sender.Send(message);
            return ($"'{msg}'");
        }

        void InitDiscordRPC() {
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

        static string GetTime()
        {
            DateTime currentTime = DateTime.Now;
            if (twenty_four_hour_time)
            {
                return currentTime.ToString("HH:mm");
            }
            else
            {
                return currentTime.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            }
        }

        public static string GetMusic()
        {
            if (musicPlayerIndex == 0)
            {
                Process[] spotifyProcess = FindProcess();

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
            }
            else if (musicPlayerIndex == 1)
            {
                return "mpd";
            }
            else
            {
                return "Error: not a valid media player option: chose 0: spotify 1: mpd";
            }

            return "Error: hopefully you aren't seeing this error";
        }

        public static string GetSystemInfo()
        {
            return $"CPU: {getCPUusage()}% RAM: {Math.Round(getUsedRAM() * 10) / 10:F1} GB/{Math.Round(getTotalRAM()):F1} GB GPU: {getGPUusage()}%";
        }

        static public double getUsedRAM()
        {
            if (ramCounter != null)
            {
                double usedRAM = getTotalRAM() - ramCounter.NextValue() / 1024.0;
                return usedRAM;
            }
            else
            {
                Console.WriteLine("You have no ram, which i really hope you do cause otherwise you are a magic man :3");
                return 0;
            }
        }

        static double getTotalRAM()
        {
            return new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024.0 * 1024 * 1024.0);
        }

        static double getGPUusage() {
            
            return 0;
        }

        static int getCPUusage()
        {
            return (int)cpuCounter.NextValue();
        }

        static string Scroll(string text)
        {
            if (paused != true)
            {
                if (last_song != text)
                {
                    scrolling_title = $" - {text}";
                    last_song = text;
                }

                string scrolledPart = scrolling_title.Substring(0, scroll_amount_per_frame);
                scrolling_title = scrolling_title.Substring(scroll_amount_per_frame) + scrolledPart;

                return scrolling_title.Substring(0, Math.Min(scrolling_title.Length, max_scroll_length));
            }
            else if (paused)
            {
                return text;
            }
            return "Error: error with scrolling";
        }

        static Process[] FindProcess()
        {
            Process[] p = Process.GetProcessesByName("Spotify");
            return p;
        }

        // Dont cause a memory leak cause that bad i reckon probubly
        void Deinitialize()
        {
            client.Dispose();
        }
    }
}