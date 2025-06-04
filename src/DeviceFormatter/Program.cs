using System;
using System.IO;
using System.Management; // Need to add System.Management reference

class DriveFormatter
{
    static void Main()
    {
        Console.WriteLine("Windows Drive Formatter");
        Console.WriteLine("=======================\n");

        try
        {
            // List all removable and fixed drives (excluding CD-ROMs)
            ListDrives();

            Console.Write("\nEnter the drive letter to format (e.g., D): ");
            string driveLetter = Console.ReadLine().ToUpper();

            if (string.IsNullOrEmpty(driveLetter) || driveLetter.Length != 1)
            {
                Console.WriteLine("Invalid drive letter.");
                return;
            }

            Console.Write($"\nWARNING: All data on drive {driveLetter}: will be lost! Continue? (Y/N): ");
            if (Console.ReadLine().ToUpper() != "Y")
            {
                Console.WriteLine("Operation cancelled.");
                return;
            }

            Console.WriteLine("\nAvailable file systems: FAT32, NTFS, exFAT");
            Console.Write("Enter file system (default NTFS): ");
            string fileSystem = Console.ReadLine().ToUpper();
            if (string.IsNullOrEmpty(fileSystem))
            {
                fileSystem = "NTFS";
            }

            Console.Write("Enter volume label (optional, press Enter to skip): ");
            string volumeLabel = Console.ReadLine();

            Console.Write("Enable quick format? (Y/N, default Y): ");
            bool quickFormat = !Console.ReadLine().ToUpper().StartsWith("N");

            FormatDrive(driveLetter, fileSystem, volumeLabel, quickFormat);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static void ListDrives()
    {
        Console.WriteLine("Available drives:");
        Console.WriteLine("Letter | Type       | Size        | Free Space  | Format | Label");
        Console.WriteLine("-------|------------|-------------|-------------|--------|-------");

        DriveInfo[] allDrives = DriveInfo.GetDrives();
        foreach (DriveInfo d in allDrives)
        {
            // Skip CD-ROM drives as they can't be formatted this way
            if (d.DriveType == DriveType.CDRom) continue;

            Console.Write($"{d.Name,-6} | {d.DriveType,-10} | ");

            if (d.IsReady)
            {
                Console.Write($"{BytesToGB(d.TotalSize),-11} | {BytesToGB(d.AvailableFreeSpace),-11} | " +
                              $"{d.DriveFormat,-6} | {d.VolumeLabel}");
            }
            else
            {
                Console.Write("Not Ready  | Not Ready  |        |");
            }

            Console.WriteLine();
        }
    }

    static string BytesToGB(long bytes)
    {
        return $"{(bytes / (1024 * 1024 * 1024)):0.##} GB";
    }

    static void FormatDrive(string driveLetter, string fileSystem, string volumeLabel, bool quickFormat)
    {
        string drivePath = $"{driveLetter}:\\";

        try
        {
            // First check if this is a removable drive
            DriveInfo drive = new DriveInfo(driveLetter);
            if (drive.DriveType == DriveType.Removable && !drive.IsReady)
            {
                Console.WriteLine("\nRemovable drive detected but not ready. Trying to prepare...");

                // Try to create a directory to force mount (may not work for all drives)
                try
                {
                    Directory.CreateDirectory(drivePath + "temp_mount");
                    Directory.Delete(drivePath + "temp_mount");
                }
                catch
                {
                    // Ignore errors - we just wanted to try mounting
                }

                // Check again if ready
                if (!drive.IsReady)
                {
                    throw new Exception("Drive is not ready. Please check if the drive is properly connected and recognized by Windows.");
                }
            }

            string args = $"/C echo y | format {drivePath} /FS:{fileSystem} " +
                         $"{(quickFormat ? "/Q" : "")} " +
                         $"{(string.IsNullOrEmpty(volumeLabel) ? "" : $"/V:{volumeLabel}")}";

            Console.WriteLine($"\nFormatting {drivePath} as {fileSystem}...");

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas" // Run as administrator
            };
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Console.WriteLine($"\nDrive {drivePath} formatted successfully!");
            }
            else
            {
                throw new Exception($"Formatting failed with error code {process.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Cannot format drive {drivePath}. Error: {ex.Message}");
        }
    }

    static bool IsDriveInUse(string driveLetter)
    {
        try
        {
            // Try to get a list of files - if this fails, drive is in use
            Directory.GetFiles($"{driveLetter}:\\");
            return false;
        }
        catch
        {
            return true;
        }
    }
}