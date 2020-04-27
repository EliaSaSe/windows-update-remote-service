/*
    Windows Update Remote Service
    Copyright(C) 2016-2020  Elia Seikritt

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
    GNU General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.If not, see<https://www.gnu.org/licenses/>.
*/
using System;
using System.IO;
using System.Windows;
using WcfWuRemoteClient.Views;

namespace WcfWuRemoteClient
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        public static DirectoryInfo AppDataFolder { get; private set; } = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppDomain.CurrentDomain.FriendlyName));
        private static log4net.ILog Log;

        [STAThread]
        public static void Main()
        {          
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            SetupLogging();
            Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            Log.Info("Application startup.");

            var application = new App();            
            application.Run(new MainWindow());
        }

        private static void SetupLogging()
        {
            if (!AppDataFolder.Exists) AppDataFolder.Create();

            log4net.GlobalContext.Properties["logFilePath"] = AppDataFolder.FullName;
            log4net.Config.XmlConfigurator.Configure();

        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (!AppDataFolder.Exists) AppDataFolder.Create();               
                var ex = e.ExceptionObject as Exception;
                if (ex != null)
                {
                    var content = $"{ex.GetType().Name}{Environment.NewLine}HResult:{ex.HResult}{Environment.NewLine}Message:{ex.Message}{Environment.NewLine}Source:{ex.Source}{Environment.NewLine}StackTrace:{ex.StackTrace}{Environment.NewLine}InnerException:{ex.InnerException}{Environment.NewLine}";
                    File.WriteAllText(Path.Combine(AppDataFolder.FullName, "crashexception.txt"), content);
                }
                Log?.Fatal("Unhandled exception in application.", e.ExceptionObject as Exception);
            }
            catch{}
        }
    }
}
