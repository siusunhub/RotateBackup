using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using RotateBackupSetting;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace Rotatebackup
{
    class Program
    {
        static string[] protectPath = { @"C:\WINDOWS", @"C:\Users", @"C:\Program Files", @"C:\Program Files (x86)" };

        static bool isDebug = false;
        static string debugcommand = "testbackup";
        static bool writeDebug = true;
        static bool writeDebugAlready = false;
        static string commandName = "";

        static int Main(string[] args)
        {


            BackupSetting _item;

            if (args.Length == 0 && !isDebug)
            {
                ShowLog("Please enter the BackupCommand");
                ShowLog("Example: RotateBackup.exe command1");
                return 1;
            }
            else
            {
                string _command = "";

                if (isDebug)
                {
                    _command = debugcommand;
                }
                else
                {
                    _command = args[0];
                }
                if (_command != "")
                {
                    using (var db = new LiteDatabase(AppDomain.CurrentDomain.BaseDirectory + @"BackupSet.db"))
                    {
                        var col = db.GetCollection<BackupSetting>("backup");
                        _item = col.FindOne(Query.EQ("Command", _command));

                    }

                    if (_item == null)
                    {
                        ShowLog("Backup Command [" + _command + "] not found");
                        return 1;
                    }
                    else
                    {

                        commandName = _item.Command;

                        bool isDirectory = _item.isDirectory;
                        int maxPath = _item.maxPath;
                        if (_item.mainPath == null) _item.mainPath = "";
                        if (_item.preCommand == null) _item.preCommand = "";
                        if (_item.postCommand == null) _item.postCommand = "";
                        if (_item.Path1 == null) _item.Path1 = "";
                        if (_item.Path2 == null) _item.Path2 = "";
                        if (_item.Path3 == null) _item.Path3 = "";
                        if (_item.Path4 == null) _item.Path4 = "";
                        if (_item.Path5 == null) _item.Path5 = "";
                        if (_item.Path6 == null) _item.Path6 = "";
                        if (_item.Path7 == null) _item.Path7 = "";
                        if (_item.Path8 == null) _item.Path8 = "";
                        if (_item.Path9 == null) _item.Path9 = "";
                        if (_item.Path10 == null) _item.Path10 = "";
                        if (_item.Path11 == null) _item.Path11 = "";
                        if (_item.Path12 == null) _item.Path12 = "";
                        if (_item.Path13 == null) _item.Path13 = "";
                        if (_item.Path14 == null) _item.Path14 = "";

                        if (_item.disable)
                        {
                            ShowLog("Command Disabled.");
                        }
                        else
                        {

                            if (_item.preCommand != "")
                            {
                                ShowLog("Execute Pre-Command: " + _item.preCommand);

                                String command = @"/C " + _item.preCommand;
                                ProcessStartInfo cmdsi = new ProcessStartInfo("cmd.exe");
                                cmdsi.Arguments = command;
                                Process cmd = Process.Start(cmdsi);
                                cmd.WaitForExit();
                            }

                            if (isDirectory)
                            {

                                // Directory
                                if (_item.mainPath == "")
                                {
                                    ShowLog("Main Backup Directory not set.");
                                    return 1;
                                }
                                else
                                {
                                    bool isfail = false;
                                    ShowLog("Main Directory: " + _item.mainPath);

                                    if (!Directory.Exists(_item.mainPath))
                                    {
                                        ShowLog("Main Directory not exist, Skipping!");
                                    }
                                    else
                                    {

                                        System.IO.DirectoryInfo di = new DirectoryInfo(_item.mainPath);

                                        if (di.GetFiles().Length == 0 && di.GetDirectories().Length == 0)
                                        {
                                            ShowLog("Main Directory is empty, Skipping!");

                                        }
                                        else if (File.Exists(_item.mainPath + "\\STOP-ROTATE") || File.Exists(_item.mainPath + "\\STOP-ROTATE.txt"))
                                        {
                                            ShowLog("STOP-ROTATE file detected, skip!");


                                        }
                                        else
                                        {
                                            if (maxPath >= 14 && _item.Path14 != "" && _item.Path13 != "")
                                            {
                                                ShowLog("Clear Directory 14: " + _item.Path14);
                                                isfail = !RemoveDirectoryAllFile(_item.Path14);
                                                if (!isfail)
                                                {
                                                    ShowLog("Move File from Directory 13 to 14: " + _item.Path13);
                                                    isfail = !MoveDirectoryFileOut(_item.Path13, _item.Path14);
                                                }
                                            }
                                            if (maxPath >= 13 && _item.Path13 != "" && _item.Path12 != "" && !isfail)
                                            {
                                                ShowLog("Clear Directory 13: " + _item.Path13);
                                                isfail = !RemoveDirectoryAllFile(_item.Path13);
                                                if (!isfail)
                                                {
                                                    ShowLog("Move File from Directory 12 to 13: " + _item.Path12);
                                                    isfail = !MoveDirectoryFileOut(_item.Path12, _item.Path13);
                                                }
                                            }
                                            if (maxPath >= 12 && _item.Path12 != "" && _item.Path11 != "" && !isfail)
                                            {
                                                ShowLog("Clear Directory 12: " + _item.Path12);
                                                isfail = !RemoveDirectoryAllFile(_item.Path12);
                                                if (!isfail)
                                                {
                                                    ShowLog("Move File from Directory 11 to 12: " + _item.Path11);
                                                    isfail = !MoveDirectoryFileOut(_item.Path11, _item.Path12);
                                                }
                                            }
                                            if (maxPath >= 11 && _item.Path11 != "" && _item.Path10 != "" && !isfail)
                                            {
                                                ShowLog("Clear Directory 11: " + _item.Path11);
                                                isfail = !RemoveDirectoryAllFile(_item.Path11);
                                                if (!isfail)
                                                {
                                                    ShowLog("Move File from Directory 10 to 11: " + _item.Path10);
                                                    isfail = !MoveDirectoryFileOut(_item.Path10, _item.Path11);
                                                }
                                            }
                                            if (maxPath >= 10 && _item.Path10 != "" && _item.Path9 != "" && !isfail)
                                            {
                                                ShowLog("Clear Directory 10: " + _item.Path10);
                                                isfail = !RemoveDirectoryAllFile(_item.Path10);
                                                if (!isfail)
                                                {
                                                    ShowLog("Move File from Directory 9 to 10: " + _item.Path9);
                                                    isfail = !MoveDirectoryFileOut(_item.Path9, _item.Path10);
                                                }
                                            }
                                            if (maxPath >= 9 && _item.Path9 != "" && _item.Path8 != "" && !isfail)
                                            {
                                                ShowLog("Clear Directory 9: " + _item.Path9);
                                                isfail = !RemoveDirectoryAllFile(_item.Path9);
                                                if (!isfail)
                                                {
                                                    ShowLog("Move File from Directory 8 to 9: " + _item.Path8);
                                                    isfail = !MoveDirectoryFileOut(_item.Path8, _item.Path9);
                                                }
                                            }
                                            if (maxPath >= 8 && _item.Path8 != "" && _item.Path7 != "" && !isfail)
                                            {
                                                ShowLog("Clear Directory 8: " + _item.Path8);
                                                isfail = !RemoveDirectoryAllFile(_item.Path8);
                                                if (!isfail)
                                                {
                                                    ShowLog("Move File from Directory 7 to 8: " + _item.Path7);
                                                    isfail = !MoveDirectoryFileOut(_item.Path7, _item.Path8);
                                                }
                                            }
                                            if (maxPath >= 7 && _item.Path7 != "" && _item.Path6 != "" && !isfail)
                                            {
                                                ShowLog("Clear Directory 7: " + _item.Path7);
                                                isfail = !RemoveDirectoryAllFile(_item.Path7);
                                                if (!isfail)
                                                {
                                                    ShowLog("Move File from Directory 6 to 7: " + _item.Path6);
                                                    isfail = !MoveDirectoryFileOut(_item.Path6, _item.Path7);
                                                }
                                            }
                                            if (maxPath >= 6 && _item.Path6 != "" && _item.Path5 != "" && !isfail)
                                            {
                                                ShowLog("Clear Directory 6: " + _item.Path6);
                                                isfail = !RemoveDirectoryAllFile(_item.Path6);
                                                if (!isfail)
                                                {
                                                    ShowLog("Move File from Directory 5 to 6: " + _item.Path5);
                                                    isfail = !MoveDirectoryFileOut(_item.Path5, _item.Path6);
                                                }
                                            }
                                            if (maxPath >= 5 && _item.Path5 != "" && _item.Path4 != "" && !isfail)
                                            {
                                                ShowLog("Clear Directory 5: " + _item.Path5);
                                                isfail = !RemoveDirectoryAllFile(_item.Path5);
                                                if (!isfail)
                                                {
                                                    ShowLog("Move File from Directory 4 to 5: " + _item.Path4);
                                                    isfail = !MoveDirectoryFileOut(_item.Path4, _item.Path5);
                                                }
                                            }
                                            if (maxPath >= 4 && _item.Path4 != "" && _item.Path3 != "" && !isfail)
                                            {
                                                ShowLog("Clear Directory 4: " + _item.Path4);
                                                isfail = !RemoveDirectoryAllFile(_item.Path4);
                                                if (!isfail)
                                                {
                                                    ShowLog("Move File from Directory 3 to 4: " + _item.Path3);
                                                    isfail = !MoveDirectoryFileOut(_item.Path3, _item.Path4);
                                                }
                                            }
                                            if (maxPath >= 3 && _item.Path3 != "" && _item.Path2 != "" && !isfail)
                                            {
                                                ShowLog("Clear Directory 3: " + _item.Path3);
                                                isfail = !RemoveDirectoryAllFile(_item.Path3);
                                                if (!isfail)
                                                {
                                                    ShowLog("Move File from Directory 2 to 3: " + _item.Path2);
                                                    isfail = !MoveDirectoryFileOut(_item.Path2, _item.Path3);
                                                }
                                            }
                                            if (maxPath >= 2 && _item.Path2 != "" && _item.Path1 != "" && !isfail)
                                            {
                                                ShowLog("Clear Directory 2: " + _item.Path2);
                                                isfail = !RemoveDirectoryAllFile(_item.Path2);
                                                if (!isfail)
                                                {
                                                    ShowLog("Move File from Directory 1 to 2: " + _item.Path1);
                                                    isfail = !MoveDirectoryFileOut(_item.Path1, _item.Path2);
                                                }
                                            }
                                            if (maxPath >= 1 && _item.Path1 != "" && _item.mainPath != "" && !isfail)
                                            {
                                                ShowLog("Clear Directory 1: " + _item.Path1);
                                                isfail = !RemoveDirectoryAllFile(_item.Path1);
                                                if (!isfail)
                                                {
                                                    ShowLog("Move File from Main Directory to 1: " + _item.mainPath);

                                                    ArrayList keepDirListArray = new ArrayList();

                                                    if (_item.keepFirstDir)
                                                    {
                                                        // cache this directory for recreate

                                                        string[] filePaths = Directory.GetDirectories(_item.mainPath);

                                                        foreach (string st in filePaths)
                                                        {
                                                            keepDirListArray.Add(st);
                                                        }

                                                    }

                                                    isfail = !MoveDirectoryFileOut(_item.mainPath, _item.Path1, _item.keepMain);

                                                    if (_item.keepFirstDir)
                                                    {
                                                        ShowLog("Re-create Sub-Dir in Main Directory: " + _item.mainPath);
                                                        // cache this directory for recreate

                                                        foreach (string st in keepDirListArray)
                                                        {
                                                            if (!Directory.Exists(st))
                                                            {
                                                                Directory.CreateDirectory(st);
                                                            }
                                                            
                                                        }
                                                    }

                                                }
                                            }
                                        }
                                    }
                                }

                            }
                            else
                            {
                                // File Process
                                if (_item.Path1 != "")
                                {
                                    ShowLog("Rotate Backup 1: " + _item.Path1);
                                    BackupFileProcess(_item.Path1, maxPath, _item.keepPath1);
                                }
                                if (_item.Path2 != "")
                                {
                                    ShowLog("Rotate Backup 2: " + _item.Path2);
                                    BackupFileProcess(_item.Path2, maxPath, _item.keepPath2);
                                }
                                if (_item.Path3 != "")
                                {
                                    ShowLog("Rotate Backup 3: " + _item.Path3);
                                    BackupFileProcess(_item.Path3, maxPath, _item.keepPath3);
                                }
                                if (_item.Path4 != "")
                                {
                                    ShowLog("Rotate Backup 4: " + _item.Path4);
                                    BackupFileProcess(_item.Path4, maxPath, _item.keepPath4);
                                }
                                if (_item.Path5 != "")
                                {
                                    ShowLog("Rotate Backup 5: " + _item.Path5);
                                    BackupFileProcess(_item.Path5, maxPath, _item.keepPath5);
                                }
                                if (_item.Path6 != "")
                                {
                                    ShowLog("Rotate Backup 6: " + _item.Path6);
                                    BackupFileProcess(_item.Path6, maxPath, _item.keepPath6);
                                }
                                if (_item.Path7 != "")
                                {
                                    ShowLog("Rotate Backup 7: " + _item.Path7);
                                    BackupFileProcess(_item.Path7, maxPath, _item.keepPath7);
                                }
                                if (_item.Path8 != "")
                                {
                                    ShowLog("Rotate Backup 8: " + _item.Path8);
                                    BackupFileProcess(_item.Path8, maxPath, _item.keepPath8);
                                }
                                if (_item.Path9 != "")
                                {
                                    ShowLog("Rotate Backup 9: " + _item.Path9);
                                    BackupFileProcess(_item.Path9, maxPath, _item.keepPath9);
                                }
                                if (_item.Path10 != "")
                                {
                                    ShowLog("Rotate Backup 10: " + _item.Path10);
                                    BackupFileProcess(_item.Path10, maxPath, _item.keepPath10);
                                }
                                if(_item.Path11 != "")
                                {
                                    ShowLog("Rotate Backup 11: " + _item.Path11);
                                    BackupFileProcess(_item.Path11, maxPath, _item.keepPath11);
                                }
                                if (_item.Path12 != "")
                                {
                                    ShowLog("Rotate Backup 12: " + _item.Path12);
                                    BackupFileProcess(_item.Path12, maxPath, _item.keepPath12);
                                }
                                if (_item.Path13 != "")
                                {
                                    ShowLog("Rotate Backup 13: " + _item.Path13);
                                    BackupFileProcess(_item.Path13, maxPath, _item.keepPath13);
                                }
                                if (_item.Path14 != "")
                                {
                                    ShowLog("Rotate Backup 14: " + _item.Path14);
                                    BackupFileProcess(_item.Path14, maxPath, _item.keepPath14);
                                }
                            }


                            if (_item.postCommand != "" && _item.postCommand != null)
                            {
                                ShowLog("Execute Post-Command: " + _item.postCommand);

                                String command = @"/C " + _item.postCommand;
                                ProcessStartInfo cmdsi = new ProcessStartInfo("cmd.exe");
                                cmdsi.Arguments = command;
                                Process cmd = Process.Start(cmdsi);
                                cmd.WaitForExit();

                            }

                            ShowLog("Process End");
                        }

                    }
                }
                else
                {
                    ShowLog("Please enter the BackupCommand");
                    ShowLog("Example: RotateBackup.exe command1");
                    return 1;
                }
            }
            return 0;
        }

        static private void BackupFileProcess(string filepath, int max, bool keep)
        {
            if (!findProtectedFolder(filepath))
            {
                if (File.Exists(filepath))
                {

                    if (File.Exists(Path.GetDirectoryName(filepath) + "\\STOP-ROTATE") || File.Exists(Path.GetDirectoryName(filepath) + "\\STOP-ROTATE.txt"))
                    {
                        ShowLog("STOP-ROTATE file detected, skip roteate!");

                    }
                    else
                    {
                        bool isfail = false;
                        for (int j = max; j >= 1; j--)
                        {
                            if (!isfail)
                            {
                                string fname = filepath + "." + j.ToString();
                                int j2 = j - 1;
                                string mvname = (j > 1) ? filepath + "." + j2.ToString() : filepath;
                                if (j == max)
                                {
                                    if (File.Exists(fname))
                                    {
                                        try
                                        {
                                            File.Delete(fname);
                                        }
                                        catch (Exception)
                                        {
                                            ShowLog("Fail to delete file: " + fname);
                                            isfail = true;
                                        }
                                    }
                                }
                                if (!isfail)
                                {
                                    if (File.Exists(mvname))
                                    {
                                        try
                                        {
                                            if (j2 == 0 && keep)
                                            {
                                                ShowLog("Copying Backup V" + j2 + " to " + j);
                                                File.Copy(mvname, fname);
                                            }
                                            else
                                            {
                                                ShowLog("Moving Backup V" + j2 + " to " + j);
                                                File.Move(mvname, fname);
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            ShowLog("Fail to move/copy file: " + mvname + " to " + fname);
                                            isfail = true;
                                        }
                                    }
                                    else
                                    {
                                        //    ShowLog("File " + mvname + " not found"); ;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    ShowLog("Main Backup File not found, skip rotate! ");
                }

            }
            else
            {
                ShowLog("Protected Directory found, Blocked!");
            }
        }

        static private bool MoveDirectoryFileOut(string fromfilepath, string tofilepath)
        {
            return MoveDirectoryFileOut(fromfilepath, tofilepath, false);
        }
        static private bool MoveDirectoryFileOut(string fromfilepath, string tofilepath, bool keep)
        {

            string _currentFile = "";
            string _thisfilename = "";

            if (Directory.Exists(fromfilepath))
            {
                if (Directory.Exists(tofilepath))
                {
                    DirectoryInfo di = new DirectoryInfo(fromfilepath);


                    foreach (FileInfo file in di.GetFiles())
                    {
                        try
                        {
                            _currentFile = file.FullName;
                            _thisfilename = file.Name;
                            if (keep)
                            {
                                file.CopyTo(tofilepath + "\\" + _thisfilename);
                            }
                            else
                            {
                                file.MoveTo(tofilepath + "\\" + _thisfilename);
                            }
                        }
                        catch (Exception)
                        {
                            ShowLog("Fail to move file: " + _currentFile + " to " + tofilepath + "\\" + _thisfilename);
                            return false;
                        }
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        try
                        {
                            _currentFile = dir.FullName;
                            _thisfilename = dir.Name;
                            if (keep)
                            {
                                // TO DO
                                DirectoryCopy(_currentFile, tofilepath + "\\" + _thisfilename);
                            }
                            else
                            {
                                dir.MoveTo(tofilepath + "\\" + _thisfilename);
                            }
                        }
                        catch (Exception)
                        {
                            ShowLog("Fail to move directory: " + _currentFile + " to " + tofilepath + "\\" + _thisfilename);
                            return false;
                        }
                    }

                    return true;
                }
                else
                {
                    ShowLog("To Directory Not Exist: " + tofilepath);
                    return false;
                }
            }
            else
            {
                ShowLog("From Directory Not Exist: " + fromfilepath);
                return false;
            }

        }

        static private void ShowLog(string s)
        {
            System.Console.WriteLine(s);

            if (writeDebug)
            {
                var path = AppDomain.CurrentDomain.BaseDirectory + @"Rotatelog.txt";
                var sw = new StreamWriter(path, true);
                if (!writeDebugAlready) sw.WriteLine("------------------------------------------");
                sw.WriteLine("[" + commandName + "] " + DateTime.Now.ToString("h:mm:ss tt") + " " + s);
                writeDebugAlready = true;
                sw.Close();
            }

        }

        static private bool RemoveDirectoryAllFile(string filepath)
        {
            if (!findProtectedFolder(filepath))
            {



                if (!Directory.Exists(filepath))
                {
                    ShowLog("Creating directory: " + filepath);
                    Directory.CreateDirectory(filepath);
                }



                if (Directory.Exists(filepath))
                {
                    System.IO.DirectoryInfo di = new DirectoryInfo(filepath);

                    foreach (FileInfo file in di.GetFiles())
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception)
                        {
                            ShowLog("Fail delete file: " + file.FullName);
                        }
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        try
                        {
                            dir.Delete(true);
                        }
                        catch (Exception)
                        {
                            ShowLog("Fail delete directory: " + dir.FullName);
                        }
                    }
                    return true;

                }
                else
                {
                    ShowLog("Directory Not Exist, Stop Backup!");
                    return false;
                }

            }
            else
            {
                ShowLog("Protected Directory found, Blocked!");
                return false;
            }

        }

        static private bool findProtectedFolder(string path)
        {
            foreach (string s in protectPath)
            {
                if (path.Length >= s.Length)
                {
                    if (path.ToLower().Substring(0, s.Length) == s.ToLower())
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        static private void DirectoryCopy(string sourceDirName, string destDirName)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the source directory does not exist, throw an exception.
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory does not exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }


            // Get the file contents of the directory to copy.
            FileInfo[] files = dir.GetFiles();

            foreach (FileInfo file in files)
            {
                // Create the path to the new copy of the file.
                string temppath = Path.Combine(destDirName, file.Name);

                // Copy the file.
                file.CopyTo(temppath, false);
            }


            foreach (DirectoryInfo subdir in dirs)
            {
                // Create the subdirectory.
                string temppath = Path.Combine(destDirName, subdir.Name);

                // Copy the subdirectories.
                DirectoryCopy(subdir.FullName, temppath);
            }

        }
    }
}
