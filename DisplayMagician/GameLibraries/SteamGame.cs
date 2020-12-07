﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DisplayMagician.Resources;
using DisplayMagician.Shared;
//using HtmlAgilityPack;
using Microsoft.Win32;
using Newtonsoft.Json;
//using VdfParser;
//using Gameloop.Vdf;
using System.Collections.ObjectModel;
using ValveKeyValue;
using System.Security.Cryptography;
using System.ServiceModel.Configuration;
using DisplayMagician.GameLibraries.SteamAppInfoParser;
using TsudaKageyu;
using System.Drawing.IconLib;
using System.Drawing.IconLib.Exceptions;
using System.Diagnostics;

namespace DisplayMagician.GameLibraries
{
    public class SteamGame : Game
    {
        /*private static string SteamLibrary.SteamExe;
        private static string SteamLibrary.SteamPath;
        private static string _steamConfigVdfFile;
        private static string _registrySteamKey = @"SOFTWARE\\Valve\\Steam";
        private static string _registryAppsKey = $@"{_registrySteamKey}\\Apps";*/
        private string _gameRegistryKey;
        private uint _steamGameId;
        private string _steamGameName;
        private string _steamGameExePath;
        private string _steamGameDir;
        private string _steamGameExe;
        private string _steamGameProcessName;
        private string _steamGameIconPath;
        private static List<SteamGame> _allInstalledSteamGames = null;

        /*private struct SteamAppInfo
        {
            public uint GameID; 
            public string GameName;
            public List<string> GameExes;
            public string GameInstallDir;
            public string GameSteamIconPath;
        }*/

        static SteamGame()
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (send, certificate, chain, sslPolicyErrors) => true;
        }


        public SteamGame(uint steamGameId, string steamGameName, string steamGameExePath, string steamGameIconPath)
        {

            _gameRegistryKey = $@"{SteamLibrary.SteamAppsRegistryKey}\\{steamGameId}";
            _steamGameId = steamGameId;
            _steamGameName = steamGameName;
            _steamGameExePath = steamGameExePath;
            _steamGameDir = Path.GetDirectoryName(steamGameExePath);
            _steamGameExe = Path.GetFileName(_steamGameExePath);
            _steamGameProcessName = Path.GetFileNameWithoutExtension(_steamGameExePath);
            _steamGameIconPath = steamGameIconPath;

        }

        public override uint Id { 
            get => _steamGameId;
            set => _steamGameId = value;
        }

        public override string Name
        {
            get => _steamGameName;
            set => _steamGameName = value;
        }

        public override SupportedGameLibrary GameLibrary { 
            get => SupportedGameLibrary.Steam; 
        }

        public override string IconPath { 
            get => _steamGameIconPath; 
            set => _steamGameIconPath = value;
        }

        public override string ExePath
        {
            get => _steamGameExePath;
            set => _steamGameExePath = value;
        }

        public override string Directory
        {
            get => _steamGameExePath;
            set => _steamGameExePath = value;
        }

        public override bool IsRunning
        {
            get
            {
                /*try
                {
                    using (
                        var key = Registry.CurrentUser.OpenSubKey(_gameRegistryKey, RegistryKeyPermissionCheck.ReadSubTree))
                    {
                        if ((int)key?.GetValue(@"Running", 0) == 1)
                        {
                            return true;
                        }
                        return false;
                    }
                }
                catch (SecurityException ex)
                {
                    Console.WriteLine($"SteamGame/IsRunning securityexception: {ex.Message}: {ex.StackTrace} - {ex.InnerException}");
                    if (ex.Source != null)
                        Console.WriteLine("SecurityException source: {0} - Message: {1}", ex.Source, ex.Message);
                    throw;
                }
                catch (IOException ex)
                {
                    // Extract some information from this exception, and then
                    // throw it to the parent method.
                    Console.WriteLine($"SteamGame/IsRunning ioexception: {ex.Message}: {ex.StackTrace} - {ex.InnerException}");
                    if (ex.Source != null)
                        Console.WriteLine("IOException source: {0} - Message: {1}", ex.Source, ex.Message);
                    throw;
                }*/

                bool isRunning = Process.GetProcessesByName(_steamGameProcessName)
                    .FirstOrDefault(p => p.MainModule.FileName
                    .StartsWith(ExePath,StringComparison.OrdinalIgnoreCase)) != default(Process);
                return isRunning;
            }
        }

        public override bool IsUpdating
        {
            get
            {
                try
                {
                    using (
                        var key = Registry.CurrentUser.OpenSubKey(_gameRegistryKey, RegistryKeyPermissionCheck.ReadSubTree))
                    {
                        if ((int)key?.GetValue(@"Updating", 0) == 1)
                        {
                            return true;
                        }
                        return false;
                    }
                }
                catch (SecurityException ex)
                {
                    Console.WriteLine($"SteamGame/IsUpdating securityexception: {ex.Message}: {ex.StackTrace} - {ex.InnerException}");
                    if (ex.Source != null)
                        Console.WriteLine("SecurityException source: {0} - Message: {1}", ex.Source, ex.Message);
                    throw;
                }
                catch (IOException ex)
                {
                    // Extract some information from this exception, and then
                    // throw it to the parent method.
                    Console.WriteLine($"SteamGame/IsUpdating ioexception: {ex.Message}: {ex.StackTrace} - {ex.InnerException}");
                    if (ex.Source != null)
                        Console.WriteLine("IOException source: {0} - Message: {1}", ex.Source, ex.Message);
                    throw;
                }
            }
        }

        public bool CopyInto(SteamGame steamGame)
        {
            if (!(steamGame is SteamGame))
                return false;

            // Copy all the game data over to the other game
            steamGame.IconPath = IconPath;
            steamGame.Id = Id;
            steamGame.Name = Name;
            steamGame.ExePath = ExePath;
            return true;
        }

        public override string ToString()
        {
            var name = _steamGameName;

            if (string.IsNullOrWhiteSpace(name))
            {
                name = Language.Unknown;
            }

            if (IsRunning)
            {
                return name + " " + Language.Running;
            }

            if (IsUpdating)
            {
                return name + " " + Language.Updating;
            }

            return name;
        }

    }
}