using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
class GunApp
{
    static volatile int processesCount = 0;      // Total number of processes to load (known after fetching)
    static volatile int processesLoaded = 0;     // How many have been loaded so far
    static volatile bool loadingComplete = false;
    static List<int> pidQueue = new List<int>();
    static bool isSequenceMode = false;
    static bool isGbeMode = false;
    static bool isNameMode = false;
    static bool allowCriticalKill = false;
    static bool enableGunLog = false;
    static string logFile = "gunlog.txt";
    static readonly string[] protectedNames = {
        "explorer", "svchost", "csrss", "wininit", "winlogon", "services", 
    	"lsass", "smss", "System", "Idle", "dwm", "conhost", "splitwin",
        };
    static void Log(string message)
    {
        if (!enableGunLog) return;
        try
        {
            File.AppendAllText(logFile, $"[{DateTime.Now}] {message}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Could not write to log: {ex.Message}");
        }
    }
    static void LoadingBarThread()
    {
        int barWidth = 10;
        while (!loadingComplete)
        {
            if (processesCount == 0)
            {
                // No info yet, just spin dots
                Console.Write(": Loading processes: ...\r");
                Thread.Sleep(100);
                continue;
            }
            double progress = (double)processesLoaded / processesCount;
            int pos = (int)(barWidth * progress);
            Console.Write("Loading processes: [");
            Console.Write(new string('#', pos));
            Console.Write(new string(' ', barWidth - pos));
            Console.Write($"] {progress * 100:0}%   \r");
            Thread.Sleep(100);
        }
        // Clear line after done
        Console.Write(new string(' ', Console.WindowWidth) + "\r");
    }
    static void ShowLoadingBar(int totalTicks = 9, int delayMs = 100)
    {
        Console.Write("Loading processes: [");
        for (int i = 0; i <= totalTicks; i++)
        {
            int progressWidth = 9;
            int pos = i;
            Console.Write(new string('#', pos));
            Console.Write(new string(' ', progressWidth - pos));
            Console.Write($"] {i * 100 / totalTicks}%");
            Thread.Sleep(delayMs);
            // Move cursor back to start of the bar inside the line
            Console.SetCursorPosition("Loading processes: [".Length, Console.CursorTop);
        }
        Console.WriteLine(); // Move to next line after done
    }
    static void Main(string[] args)
    {
    	bool clearScreen = args.Contains("--clear");
    	if (clearScreen)
    	    {
                Console.Clear();
  	    }
        if (args.Contains("--help"))
        {
            ShowHelp();
            return; // ✓ Exit the app after showing help
        }
        if (!args.Contains("--noascii"))
        {
            Console.WriteLine(": gun version 1.0 by @kil_l_y  ÆÆÆÆ");
            Console.WriteLine(": TOA x ÆRuSystems             ÆÆÆÆ");
            Console.WriteLine(":   _    ________     _        ÆÆÆÆ");
            Console.WriteLine(":  [*]  |______--\\   |+|       ÆÆÆÆÆÆÆÆ");
            Console.WriteLine(":  |;|   '==='_\\ (   |^|       ÆÆÆÆÆÆÆÆ");
            Console.WriteLine(":  |=|         `}/\\  |_|       ÆÆÆÆ ");
            Console.WriteLine(":  |_|          )_/  |_|       ÆÆÆÆ ");
            Console.WriteLine(":                             ÆÆÆÆÆ");
            Console.WriteLine(": loading----targets-now    ÆÆÆÆÆÆÆ ");
            Console.WriteLine(": press 'k' to fire       ÆÆÆÆÆÆÆÆÆ ");
	          ShowLoadingBar();
            // Handle --list or -l to show processes and exit
        }
        Console.WriteLine("PID: 0 | N/A");
    	  ShowProcesses();

    	if (args.Contains("--list"))
    	{
      	    Console.WriteLine("-- Listing Only Processes --");
    	      ShowLoadingBar();
            Console.WriteLine("PID: 0 | N/A");
       	    ShowProcesses();
       	    return;
    	}
        if (args.Contains("--gunlog"))
        {
            enableGunLog = true;
            Console.WriteLine("-- Gun Logging Active -- Writing to gunlog.txt");
        }
        if (args.Contains("--sequence"))
        {
            isSequenceMode = true;
            Console.WriteLine("-- Sequence Mode Active --");
        }
        else if (args.Contains("--gbe"))
        {
            isGbeMode = true;
        }
        else if (args.Contains("--name"))
        {
            isNameMode = true;
            Console.WriteLine("-- Name Mode Active --");
        }
        // Handle direct kill via --name or -n
        int nameIndex = Array.IndexOf(args, "--name");
        if (nameIndex == -1) nameIndex = Array.IndexOf(args, "-n");
        if (nameIndex != -1 && nameIndex + 1 < args.Length)
        {
            string targetName = string.Join(" ", args.Skip(nameIndex + 1));
            Console.WriteLine($"Attempting to kill by name: {targetName}");
            KillByName(targetName);
            return;
        }
        // Handle direct kill via --pid or -p
        int pidIndex = Array.IndexOf(args, "--pid");
        if (pidIndex == -1) pidIndex = Array.IndexOf(args, "-p");
        if (pidIndex != -1 && pidIndex + 1 < args.Length && int.TryParse(args[pidIndex + 1], out int targetPid))
        {
            Console.WriteLine($"Attempting to kill PID: {targetPid}");
    	      KillProcess(targetPid);
     	    return;
        }
        if (args.Any(a => a.Contains("--safeguard off")))
        {
            allowCriticalKill = true;
            Console.WriteLine("[!] SAFEGUARD DISABLED — Critical processes can be killed.");
        }
        string input = string.Empty;
        while (true)
        {
            Console.WriteLine("last loaded target --> " + (pidQueue.Count > 0 ? pidQueue.Last().ToString() : "None"));
            if (isGbeMode)
            {
                Console.WriteLine("\n-- GBE Mode --");
                Console.WriteLine("Loading killable processes...");
                pidQueue.Clear();
                Process[] processes = Process.GetProcesses();
                foreach (var process in processes)
                {
                    try
                    {
                        string pname = process.ProcessName.ToLower();
                        int id = process.Id;
                        if (!allowCriticalKill &&
                            (protectedNames.Contains(pname) || id == Process.GetCurrentProcess().Id))
                        {
                            continue;
                        }
                        if (!pidQueue.Contains(id))
                        {
                            pidQueue.Add(id);
                            Console.WriteLine($"Queued: PID {id} | {pname}");
                        }
                    }
                    catch
                    {
                        // skip access-denied processes
                    }
                }
                Console.WriteLine($"\n{pidQueue.Count} processes queued. Enter 'k' to kill all.");
            }
            Console.Write("[+]: ");
            input = Console.ReadLine()?.ToLower();
            if (input == "help")
            {
                ShowHelp();
            }
            else if (input == "k")
            {
                if (isGbeMode)
                {
                    Console.Write("Type 'blame' to confirm kill: ");
                    string confirm = Console.ReadLine();
                    if (confirm.ToLower() == "blame")
                    {
                        Console.WriteLine("BLAAAAM");
                        KillAll();
                    }
                    else
                    {
                        Console.WriteLine("Aborted.");
                        continue;
                    }
                }
                else
                {
                    KillAll(); // ✓ Now works in all modes
                }
            }
            else if (int.TryParse(input, out int pid))
            {
                if (isSequenceMode)
                {
                    KillProcess(pid);
                }
                else
                {
                    if (!pidQueue.Contains(pid))
                        pidQueue.Add(pid);
                }
            }
            else if (input == "exit")
            {
                Console.WriteLine("Exiting GunApp.");
                break;
            }
            else if (isNameMode && !string.IsNullOrWhiteSpace(input) && input != "k")
            {
                KillByName(input.Trim());
            }
        }
    }
    static void ShowProcesses()
    {
        Process[] processes = Process.GetProcesses();
        processesCount = processes.Length;
        processesLoaded = 0;
        loadingComplete = false;
        // Start loading bar thread
        Thread loader = new Thread(LoadingBarThread);
        loader.Start();
        foreach (var process in processes)
        {
            try
            {
                Console.WriteLine($"PID: {process.Id} | {process.ProcessName}");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"PID: {process.Id} | {process.ProcessName} [Access Denied]");
            }
            processesLoaded++;
        }
        loadingComplete = true;
        loader.Join();
    }
    static void KillProcess(int pid)
    {
        try
        {
            Process p = Process.GetProcessById(pid);
            if (!allowCriticalKill &&
                (protectedNames.Contains(p.ProcessName.ToLower()) || 
                 pid == Process.GetCurrentProcess().Id))
            {
                Console.WriteLine($"❌ Skipped critical process: {p.ProcessName} ({pid})");
                return;
            }
            p.Kill();
            Console.WriteLine($"✅ Killed {pid} ({p.ProcessName})");
	        Log($"Killed {pid} ({p.ProcessName})");
        }
    	catch (ArgumentException)
    	{
            Console.WriteLine($"⚠️  No process with PID {pid} exists (it may have already exited).");
	    Log($"Attempted to kill non-existent PID {pid}");
  	    }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error killing process {pid}: {ex.Message}");
	    Log($"Error killing process {pid}: {ex.Message}");
        }
    }
    static void KillAll()
    {
        if (pidQueue.Count == 0)
        {
            Console.WriteLine("No PIDs in queue to kill.");
            return;
        }
        foreach (var pid in pidQueue)
        {
            KillProcess(pid);
        }
        pidQueue.Clear();
    }     
    static void KillByName(string name)
    {
        Process[] processes = Process.GetProcesses();
        bool found = false;
	    foreach (var process in processes)
        {
            try
            {
                string pname = process.ProcessName.ToLower();
                string title = process.MainWindowTitle.ToLower();
                // Check against ProcessName, "something.exe", or window title
                if (pname.Contains(name.ToLower()) || 
                    (pname + ".exe").Equals(name.ToLower()) ||
                    title.Contains(name.ToLower()))
                {
                    process.Kill();
                    Console.WriteLine($"✅ Successfully killed PID {process.Id} ({process.ProcessName})");
		                Log($"Killed by name: PID {process.Id} ({process.ProcessName})");
                    found = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Could not kill process: {ex.Message}");
            }
        }
    }
    static void ShowHelp()
    {
        Console.WriteLine("Help Menu:");
        Console.WriteLine("  Enter PID to add it to the queue.");
        Console.WriteLine("  Enter 'k' to kill all processes in the queue.");
        Console.WriteLine("  Enter 'exit' to close the app.");
      	Console.WriteLine("  Arguments:");
        Console.WriteLine("  	'--sequence' mode kills the process immediately upon entry.");
      	Console.WriteLine("  	'--name' mode lets you kill by process name, .exe, or window title.");
        Console.WriteLine("  	'--gbe' mode kills all possible processes upon 'blame' entry.");
      	Console.WriteLine("  	'--safeguard off' disables critical process protection. Use with caution.");
      	Console.WriteLine("  	'--gunlog' mode saves a log to gunlog.txt.");
      	Console.WriteLine("  Quick Usage:");
      	Console.WriteLine("	gun -n 'proces name' will kill a process by name without needing to load.");
      	Console.WriteLine("	gun -p 'PID' will kill a process by it's PID.");
      	Console.WriteLine("	gun -l (--list) lists all running processes and quits.");
        Console.WriteLine("  Typing help at any time shows this menu.");
    }
}
