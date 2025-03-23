using Basis.Network;
using Basis.Network.Server;
using BasisNetworking.InitalData;
using BasisNetworkServer.Security;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Basis
{
    class Program
    {
        public static BasisNetworkHealthCheck Check;

        private const string ConfigFileName = "config.xml";
        private const string LogsFolderName = "Logs";
        private const string InitialResources = "initalresources";
        private const int ThreadSleepTime = 15000;
        private static bool isRunning = true;
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            Configuration config = Configuration.LoadFromXml(configFilePath);
            config.ProcessEnvironmentalOverrides();

            ThreadPool.SetMinThreads(config.MinThreadPoolThreads, config.MinThreadPoolThreads);
            ThreadPool.SetMaxThreads(config.MaxThreadPoolThreads, config.MaxThreadPoolThreads);

            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogsFolderName);
            BasisServerSideLogging.Initialize(config, folderPath);

            BNL.Log("Server Booting");

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Check = new BasisNetworkHealthCheck(config);

            Task serverTask = Task.Run(() =>
            {
                try
                {
                    NetworkServer.StartServer(config);
                    BasisLoadableLoader.LoadXML(InitialResources);
                }
                catch (Exception ex)
                {
                    BNL.LogError($"Server encountered an error: {ex.Message} {ex.StackTrace}");
                }
            }, cancellationTokenSource.Token);

            AppDomain.CurrentDomain.ProcessExit += async (sender, eventArgs) =>
            {
                BNL.Log("Shutting down server...");
                cancellationTokenSource.Cancel();

                try { await serverTask; }
                catch (Exception ex) { BNL.LogError($"Error during server shutdown: {ex.Message}"); }

                if (config.EnableStatistics) BasisStatistics.StopWorkerThread();
                await BasisServerSideLogging.ShutdownAsync();
                BNL.Log("Server shut down successfully.");
            };

            // Start console command processing
            Task.Run(() => ProcessConsoleCommands());

            while (isRunning)
            {
                Thread.Sleep(ThreadSleepTime);
            }
        }

        private static void ProcessConsoleCommands()
        {
            while (isRunning)
            {
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                string[] parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3 && parts[0].ToLower() == "/admin" && parts[1].ToLower() == "add")
                {
                    string value = parts[2];
                    if (NetworkServer.authIdentity.AddNetPeerAsAdmin(value))
                    {
                        BNL.Log($"Added Admin {value}");
                    }
                    else
                    {
                        BNL.Log("Already Have Admin Added");
                    }
                }
                else
                {
                    BNL.Log("Unknown command. Usage: /admin add <string>");
                }
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                BNL.LogError($"Fatal exception: {exception.Message}");
                BNL.LogError($"Stack trace: {exception.StackTrace}");
            }
            else
            {
                BNL.LogError("An unknown fatal exception occurred.");
            }
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            foreach (var exception in e.Exception.InnerExceptions)
            {
                BNL.LogError($"Unobserved task exception: {exception.Message}");
                BNL.LogError($"Stack trace: {exception.StackTrace}");
            }
            e.SetObserved();
        }
    }
}
