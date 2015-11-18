﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using KeySndr.Base.BeaconLib;
using KeySndr.Base.Domain;
using KeySndr.Base.Exceptions;
using KeySndr.Base.Providers;
using KeySndr.Common;
using KeySndr.Common.Providers;
using Microsoft.Owin.Hosting;

namespace KeySndr.Base
{
    public class KeySndrApp
    {
        public const string WebFolderName = "Web";
        public const string ConfigurationsFolderName = "Configurations";
        public const string ScriptsFolderName = "Scripts";

        private IDisposable webServer;
        private AppConfig AppConfig;
        private Beacon beacon;
        private List<IProvider> providers;

        public KeySndrApp()
        {
            providers = new List<IProvider>();
        }

        public KeySndrApp(List<IProvider> p)
        {
            providers = p;
        }

        public void Run()
        {
            RegisterProviders();
            LoadAppConfig();
            VerifyStorage();
            StartWebServer();

            TempEventNotifier.OnReloadRequested += OnReloadRequested;
            LoadInputConfigurations();
            LoadInputScripts();
        }

        private void OnReloadRequested(object sender, EventArgs eventArgs)
        {
            ReloadAll();
        }


        public void StopAll()
        {
            TempEventNotifier.OnReloadRequested -= OnReloadRequested;
            StopServer();
            beacon.Dispose();
            ObjectFactory.Destroy();
        }

        public void ReloadAll()
        {
            StopAll();
            Run();
        }

        private void VerifyStorage()
        {
            var fs = ObjectFactory.GetProvider<IFileSystemProvider>();
            try
            {
                fs.Verify();
                fs.SaveAppConfiguration();
            }
            catch (DataFolderDoesNotExistException e)
            {
                ObjectFactory.GetProvider<IAppConfigProvider>().AppConfig.FirstTimeRunning = true;
            }
        }

        private void RegisterProviders()
        {
            foreach (var provider in providers)
            {
                ObjectFactory.AddProvider(provider);
            }
            ObjectFactory.AddProvider(new FileSystemProvider());
            ObjectFactory.AddProvider(new AppConfigProvider());
            ObjectFactory.AddProvider(new ScriptProvider());
            ObjectFactory.AddProvider(new InputConfigProvider());
            ObjectFactory.AddProvider(new SystemProvider());
        }

        private void LoadAppConfig()
        {
            ObjectFactory.GetProvider<ILoggingProvider>().Debug("Loading App Config");
            var fs = ObjectFactory.GetProvider<IFileSystemProvider>();
            var acp = ObjectFactory.GetProvider<IAppConfigProvider>();
            var config = fs.LoadAppConfiguration();
            if (config != null)
                acp.AppConfig = config;
            else
            {
                acp.AppConfig = new AppConfig();
                //fs.SaveAppConfiguration();
            }
            AppConfig = acp.AppConfig;
        }

        private void SetupBeacon(int port)
        {
            beacon = new Beacon("KeySndrServer", (ushort)port)
            {
                BeaconData = AppConfig.BroadcastIdentifier
            };
            beacon.Start();
        }

        private void StartWebServer()
        {
            SetupBeacon(AppConfig.LastPort);
            ObjectFactory.GetProvider<ILoggingProvider>().Debug("Starting web server");
            var url = $"http://{AppConfig.LastIp}:{AppConfig.LastPort}";
            webServer = WebApp.Start<Startup>(url: url);
        }

        private void StopServer()
        {
            ObjectFactory.GetProvider<ILoggingProvider>().Debug("Stopping web server");
            webServer.Dispose();
        }

        public async Task LoadInputConfigurations()
        {
            ObjectFactory.GetProvider<ILoggingProvider>().Debug("Loading input configurations");
            var inputConfigProvider = ObjectFactory.GetProvider<IInputConfigProvider>();
            inputConfigProvider.Clear();

            await Task.Run(() =>
            {
                foreach (var f in GetAllConfigurationFiles())
                {
                    try
                    {
                        var c = LoadInputConfiguration(f);
                        c.FileName = f;
                        inputConfigProvider.Add(c);
                    }
                    catch (Exception e)
                    {
                        ObjectFactory.GetProvider<ILoggingProvider>().Error("Error loading configuration " + f + " " + e.Message, e);
                    }
                }
            });
        }

        public async Task LoadInputScripts()
        {
            ObjectFactory.GetProvider<ILoggingProvider>().Debug("Loading Scripts");
            var sp = ObjectFactory.GetProvider<IScriptProvider>();
            sp.Clear();

            var fs = ObjectFactory.GetProvider<IFileSystemProvider>();
            await Task.Run(async () =>
            {
                foreach (var f in GetAllScriptFiles())
                {
                    var script = LoadInputScript(f);
                    script.FileName = f;
                    
                    foreach (var sourceFile in script.SourceFiles)
                    {
                        try
                        {
                            sourceFile.Contents = fs.LoadStringFromDisk(sourceFile.FileName);
                        }
                        catch (Exception e)
                        {
                            
                        }
                    }
                    sp.AddScript(script, true);
                    await script.RunTest();
                }
                
            });
        }

        private IEnumerable<string> GetAllScriptFiles()
        {
            if (string.IsNullOrEmpty(AppConfig.ScriptsFolder))
                return new string[0];

            var fs = ObjectFactory.GetProvider<IFileSystemProvider>();
            return fs.GetDirectoryFileNames(AppConfig.ScriptsFolder, "script", true);
        }

        private IEnumerable<string> GetAllConfigurationFiles()
        {
            if (string.IsNullOrEmpty(AppConfig.ConfigFolder))
                return new string[0];

            var fs = ObjectFactory.GetProvider<IFileSystemProvider>();
            return fs.GetDirectoryFileNames(AppConfig.ConfigFolder, "json", true);
        }

        private InputScript LoadInputScript(string fileName)
        {
            if (string.IsNullOrEmpty(AppConfig.ScriptsFolder))
                return null;

            var fs = ObjectFactory.GetProvider<IFileSystemProvider>();
            return fs.LoadObjectFromDisk<InputScript>(Path.Combine(AppConfig.ScriptsFolder, fileName));
        }

        private InputConfiguration LoadInputConfiguration(string fileName)
        {
            if (string.IsNullOrEmpty(AppConfig.ConfigFolder))
                return null;

            var fs = ObjectFactory.GetProvider<IFileSystemProvider>();
            return fs.LoadObjectFromDisk<InputConfiguration>(Path.Combine(AppConfig.ConfigFolder, fileName));
        }
    }

    public static class TempEventNotifier
    {
        public static EventHandler<EventArgs> OnReloadRequested;

        public static void ReloadRequested()
        {
            var t = new Timer {Interval = 1000};
            t.Elapsed += delegate(object sender, ElapsedEventArgs args)
            {
                t.Stop();
                OnReloadRequested?.Invoke(new object(), EventArgs.Empty);
            };
            t.Start();
        }
    }
}
