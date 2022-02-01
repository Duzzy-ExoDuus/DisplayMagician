﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DisplayMagicianShared;
using Microsoft.Win32;
using Newtonsoft.Json;

// This file is taken from Soroush Falahati's amazing HeliosDisplayManagement software
// available at https://github.com/falahati/HeliosDisplayManagement

// Modifications made by Terry MacDonald

namespace DisplayMagicianShared.Windows
{
    public class TaskBarStuckRectangle
    {
        
        public enum TaskBarEdge : UInt32
        {
            Left = 0,
            Top = 1,
            Right = 2,
            Bottom = 3
        }

        [Flags]
        public enum TaskBarOptions : UInt32
        {
            None = 0,
            AutoHide = 1 << 0,
            KeepOnTop = 1 << 1,
            UseSmallIcons = 1 << 2,
            HideClock = 1 << 3,
            HideVolume = 1 << 4,
            HideNetwork = 1 << 5,
            HidePower = 1 << 6,
            WindowPreview = 1 << 7,
            Unknown1 = 1 << 8,
            Unknown2 = 1 << 9,
            HideActionCenter = 1 << 10,
            Unknown3 = 1 << 11,
            HideLocation = 1 << 12,
            HideLanguageBar = 1 << 13
        }

        private const string MainDisplayAddress =
            "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StuckRects{0:D}";

        private const string MultiDisplayAddress =
            "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MMStuckRects{0:D}";

        /*private static readonly Dictionary<int, byte[]> Headers = new Dictionary<int, byte[]>
        {
            {2, new byte[] {0x28, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF}},
            {3, new byte[] {0x30, 0x00, 0x00, 0x00, 0xFE, 0xFF, 0xFF, 0xFF}}
        };
*/
        public TaskBarStuckRectangle(string devicePath)
        {
            bool MMStuckRectVerFound = false;
            // Check if key exists
            int version = 2;
            string address = string.Format(MultiDisplayAddress, version);
            if (Registry.CurrentUser.OpenSubKey(address) != null)
            {
                MMStuckRectVerFound = true;
            }
            else
            {                
                // If it's not version 2, then try version 3
                version = 3;
                address = string.Format(MultiDisplayAddress, version);
                if (Registry.CurrentUser.OpenSubKey(address) != null)
                {
                    MMStuckRectVerFound = true;
                }
                else
                {
                    // It's not v2 or v3, so it must be a single display
                    MMStuckRectVerFound = false;
                }
            }

            bool foundDevicePath = false;
            if (MMStuckRectVerFound)
            {
                // Check if value exists
                if (version >= 2 && version <= 3)
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(
                                address,
                                RegistryKeyPermissionCheck.ReadSubTree))
                    {
                        var binary = key?.GetValue(devicePath) as byte[];
                        if (binary?.Length > 0)
                        {
                            foundDevicePath = true;
                            MainScreen = false;
                            DevicePath = devicePath;
                            Binary = binary;
                            Version = version;

                            // Extract the values from the binary byte field
                            PopulateFieldsFromBinary();

                            SharedLogger.logger.Trace($"WinLibrary/GetWindowsDisplayConfig: The taskbar for {DevicePath} is against the {Edge} edge, is positioned at ({Location.X},{Location.Y}) and is {Location.Width}x{Location.Height} in size.");
                        }
                        else
                        {
                            SharedLogger.logger.Trace($"WinLibrary/GetWindowsDisplayConfig: Unable to get the TaskBarStuckRectangle binary settings from {devicePath} screen.");
                        }
                    }
                }
            }

            if (!foundDevicePath)
            {
                bool StuckRectVerFound = false;
                // Check if string exists
                version = 2;
                address = string.Format(MainDisplayAddress, version);
                if (Registry.CurrentUser.OpenSubKey(address) != null)
                {
                    StuckRectVerFound = true;
                }
                else
                {
                    // If it's not version 2, then try version 3
                    version = 3;
                    address = string.Format(MainDisplayAddress, version);
                    if (Registry.CurrentUser.OpenSubKey(address) != null)
                    {
                        StuckRectVerFound = true;
                    }
                    else 
                    {
                        return;
                    }
                }

                if (StuckRectVerFound)
                {
                    // Check if value exists
                    if (version >= 2 && version <= 3)
                    {
                        using (var key = Registry.CurrentUser.OpenSubKey(
                                    address,
                                    RegistryKeyPermissionCheck.ReadSubTree))
                        {
                            var binary = key?.GetValue(devicePath) as byte[];
                            if (binary?.Length > 0)
                            {
                                foundDevicePath = true;
                                MainScreen = true;
                                DevicePath = devicePath;
                                Binary = binary;
                                Version = version;

                                // Extract the values from the binary byte field
                                PopulateFieldsFromBinary();

                                SharedLogger.logger.Trace($"WinLibrary/GetWindowsDisplayConfig: The taskbar for {DevicePath} is against the {Edge} edge, is positioned at ({Location.X},{Location.Y}) and is {Location.Width}x{Location.Height} in size.");
                            }
                            else
                            {
                                SharedLogger.logger.Trace($"WinLibrary/GetWindowsDisplayConfig: Unable to get the TaskBarStuckRectangle binary settings from {devicePath} screen.");
                            }
                        }
                    }
                }
            }
        }
        
        public TaskBarStuckRectangle()
        {
        }

        public byte[] Binary { get; set; }

        public byte[] OriginalBinary { get; set; }

        public string DevicePath { get; set; }

        public bool MainScreen { get; set; }

        public UInt32 DPI { get; set; }

        public TaskBarEdge Edge { get; set; }

        public Rectangle Location { get; set; }

        public Size MinSize { get; set; }

        public TaskBarOptions Options { get; set; }

        public uint Rows { get; set; }

        /*[JsonIgnore]
        public UInt32 DPI
        {
            get
            {
                if (Binary.Length < 44)
                {
                    return 0;
                }

                return BitConverter.ToUInt32(Binary, 40);
            }
            set
            {
                if (Binary.Length < 44)
                {
                    return;
                }

                var bytes = BitConverter.GetBytes(value);
                Array.Copy(bytes, 0, Binary, 40, 4);
            }
        }*/

        /*[JsonIgnore]
        public TaskBarEdge Edge
        {
            get
            {
                if (Binary.Length < 16)
                {
                    return TaskBarEdge.Bottom;
                }

                return (TaskBarEdge)BitConverter.ToUInt32(Binary, 12);
            }
            set
            {
                if (Binary.Length < 16)
                {
                    return;
                }

                var bytes = BitConverter.GetBytes((uint)value);
                Array.Copy(bytes, 0, Binary, 12, 4);
            }
        }*/

        /*[JsonIgnore]
        public Rectangle Location
        {
            get
            {
                if (Binary.Length < 40)
                {
                    return Rectangle.Empty;
                }

                var left = BitConverter.ToInt32(Binary, 24);
                var top = BitConverter.ToInt32(Binary, 28);
                var right = BitConverter.ToInt32(Binary, 32);
                var bottom = BitConverter.ToInt32(Binary, 36);

                return Rectangle.FromLTRB(left, top, right, bottom);
            }
            set
            {
                if (Binary.Length < 40)
                {
                    return;
                }

                var bytes = BitConverter.GetBytes(value.Left);
                Array.Copy(bytes, 0, Binary, 24, 4);

                bytes = BitConverter.GetBytes(value.Top);
                Array.Copy(bytes, 0, Binary, 28, 4);

                bytes = BitConverter.GetBytes(value.Right);
                Array.Copy(bytes, 0, Binary, 32, 4);

                bytes = BitConverter.GetBytes(value.Bottom);
                Array.Copy(bytes, 0, Binary, 36, 4);
            }
        }*/

        /*[JsonIgnore]
        public Size MinSize
        {
            get
            {
                if (Binary.Length < 24)
                {
                    return Size.Empty;
                }

                var width = BitConverter.ToInt32(Binary, 16);
                var height = BitConverter.ToInt32(Binary, 20);

                return new Size(width, height);
            }
            set
            {
                if (Binary.Length < 24)
                {
                    return;
                }

                var bytes = BitConverter.GetBytes(value.Width);
                Array.Copy(bytes, 0, Binary, 16, 4);

                bytes = BitConverter.GetBytes(value.Height);
                Array.Copy(bytes, 0, Binary, 20, 4);
            }
        }*/

        /*[JsonIgnore]
        public TaskBarOptions Options
        {
            get
            {
                if (Binary.Length < 12)
                {
                    return 0;
                }

                return (TaskBarOptions)BitConverter.ToUInt32(Binary, 8);
            }
            set
            {
                if (Binary.Length < 12)
                {
                    return;
                }

                var bytes = BitConverter.GetBytes((uint)value);
                Array.Copy(bytes, 0, Binary, 8, 4);
            }
        }*/

        /*[JsonIgnore]
        public uint Rows
        {
            get
            {
                if (Binary.Length < 48)
                {
                    return 1;
                }

                return BitConverter.ToUInt32(Binary, 44);
            }
            set
            {
                if (Binary.Length < 48)
                {
                    return;
                }

                var bytes = BitConverter.GetBytes(value);
                Array.Copy(bytes, 0, Binary, 44, 4);
            }
        }*/

        public int Version { get; set; }

        public override bool Equals(object obj) => obj is TaskBarStuckRectangle other && this.Equals(other);
        public bool Equals(TaskBarStuckRectangle other)
        {
            return Version == other.Version &&
                DevicePath == other.DevicePath &&
                MainScreen == other.MainScreen &&
                DPI == other.DPI &&
                Edge == other.Edge &&
                Location == other.Location &&
                MinSize == other.MinSize &&
                Options == other.Options &&
                Rows == other.Rows;
        }

        public override int GetHashCode()
        {
            //return (Version, MainScreen, DevicePath, Binary).GetHashCode();
            return (Version, MainScreen, DevicePath, DPI, Edge, Location, MinSize, Options, Rows).GetHashCode();
        }
        public static bool operator ==(TaskBarStuckRectangle lhs, TaskBarStuckRectangle rhs) => lhs.Equals(rhs);

        public static bool operator !=(TaskBarStuckRectangle lhs, TaskBarStuckRectangle rhs) => !(lhs == rhs);

        static bool Xor(byte[] a, byte[] b)

        {

            int x = a.Length ^ b.Length;

            for (int i = 0; i < a.Length && i < b.Length; ++i)

            {

                x |= a[i] ^ b[i];

            }

            return x == 0;

        }

        private bool PopulateFieldsFromBinary()
        {
            // Now we decipher the binary properties features to populate the stuckrectangle 
            // DPI 
            if (Binary.Length < 44)
            {
                DPI = 0;
            }
            else
            {
                DPI = BitConverter.ToUInt32(Binary, 40);
            }
            // Edge
            if (Binary.Length < 16)
            {
                Edge = TaskBarEdge.Bottom;
            }
            else
            {
                Edge = (TaskBarEdge)BitConverter.ToUInt32(Binary, 12);
            }
            // Location
            if (Binary.Length < 40)
            {
                Location = Rectangle.Empty;
            }
            else
            {
                var left = BitConverter.ToInt32(Binary, 24);
                var top = BitConverter.ToInt32(Binary, 28);
                var right = BitConverter.ToInt32(Binary, 32);
                var bottom = BitConverter.ToInt32(Binary, 36);

                Location = Rectangle.FromLTRB(left, top, right, bottom);
            }
            // MinSize
            if (Binary.Length < 24)
            {
                MinSize = Size.Empty;
            }
            else
            {
                var width = BitConverter.ToInt32(Binary, 16);
                var height = BitConverter.ToInt32(Binary, 20);

                MinSize = new Size(width, height);
            }
            // Options
            if (Binary.Length < 12)
            {
                Options = 0;
            }
            else
            {
                Options = (TaskBarOptions)BitConverter.ToUInt32(Binary, 8);
            }
            // Rows
            if (Binary.Length < 48)
            {
                Rows = 1;
            }
            else
            {
                Rows = BitConverter.ToUInt32(Binary, 44);
            }
            
            return true;
        }

        public bool PopulateBinaryFromFields()
        {
            // Set the DPI
            if (Binary.Length < 44)
            {
                DPI = 0;
            }
            else
            {
                var bytes = BitConverter.GetBytes(DPI);
                Array.Copy(bytes, 0, Binary, 40, 4);
            }
            // Edge
            if (Binary.Length < 16)
            {
                Edge = TaskBarEdge.Bottom;
            }
            else
            {
                var bytes = BitConverter.GetBytes((uint)Edge);
                Array.Copy(bytes, 0, Binary, 12, 4);
            }
            // Location
            if (Binary.Length < 40)
            {
                var bytes = BitConverter.GetBytes(0);
                Array.Copy(bytes, 0, Binary, 24, 4);

                bytes = BitConverter.GetBytes(0);
                Array.Copy(bytes, 0, Binary, 28, 4);

                bytes = BitConverter.GetBytes(0);
                Array.Copy(bytes, 0, Binary, 32, 4);

                bytes = BitConverter.GetBytes(0);
                Array.Copy(bytes, 0, Binary, 36, 4);
            }
            else
            {
                var bytes = BitConverter.GetBytes(Location.Left);
                Array.Copy(bytes, 0, Binary, 24, 4);

                bytes = BitConverter.GetBytes(Location.Top);
                Array.Copy(bytes, 0, Binary, 28, 4);

                bytes = BitConverter.GetBytes(Location.Right);
                Array.Copy(bytes, 0, Binary, 32, 4);

                bytes = BitConverter.GetBytes(Location.Bottom);
                Array.Copy(bytes, 0, Binary, 36, 4);
            }
            // MinSize
            if (Binary.Length < 24)
            {
                var bytes = BitConverter.GetBytes(0);
                Array.Copy(bytes, 0, Binary, 16, 4);

                bytes = BitConverter.GetBytes(0);
                Array.Copy(bytes, 0, Binary, 20, 4);
            }
            else
            {
                var bytes = BitConverter.GetBytes(MinSize.Width);
                Array.Copy(bytes, 0, Binary, 16, 4);

                bytes = BitConverter.GetBytes(MinSize.Height);
                Array.Copy(bytes, 0, Binary, 20, 4);
            }
            // Options
            if (Binary.Length < 12)
            {
                var bytes = BitConverter.GetBytes((uint)0);
                Array.Copy(bytes, 0, Binary, 8, 4);
            }
            else
            {
                var bytes = BitConverter.GetBytes((uint)Options);
                Array.Copy(bytes, 0, Binary, 8, 4);
            }
            // Rows
            if (Binary.Length < 48)
            {
                var bytes = BitConverter.GetBytes(1);
                Array.Copy(bytes, 0, Binary, 44, 4);
            }
            else
            {
                var bytes = BitConverter.GetBytes(Rows);
                Array.Copy(bytes, 0, Binary, 44, 4);
            }
            return true;
        }

        public bool WriteToRegistry()
        {
            // Update the binary with the current settings from the object
            PopulateBinaryFromFields();

            // Write the binary field to registry
            string address;
            if (MainScreen)
            {
                address = string.Format(MainDisplayAddress, Version);
                // Set the Main Screen 
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(
                        address,
                        RegistryKeyPermissionCheck.ReadWriteSubTree))
                    {
                        key.SetValue(DevicePath, Binary);
                        SharedLogger.logger.Trace($"TaskBarStuckRectangle/Apply: Successfully applied TaskBarStuckRectangle registry settings for the {DevicePath} Screen in {address}!");
                    }
                }
                catch (Exception ex)
                {
                    SharedLogger.logger.Error(ex, $"TaskBarStuckRectangle/GetCurrent: Unable to set the {DevicePath} TaskBarStuckRectangle registry settings in {address} due to an exception!");
                }
            }
            else
            {
                address = string.Format(MultiDisplayAddress, Version);
                // Grab the main screen taskbar placement
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(
                        address,
                        RegistryKeyPermissionCheck.ReadWriteSubTree))
                    {
                        key.SetValue(DevicePath, Binary);
                        SharedLogger.logger.Trace($"TaskBarStuckRectangle/Apply: Successfully applied TaskBarStuckRectangle registry settings for the {DevicePath} Screen in {address}!");
                    }
                }
                catch (Exception ex)
                {
                    SharedLogger.logger.Error(ex, $"TaskBarStuckRectangle/GetCurrent: Unable to set the {DevicePath} TaskBarStuckRectangle registry settings in {address} due to an exception!");
                }
            }

            return true;
        }

       
    }
}