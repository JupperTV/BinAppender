using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Threading;

namespace BinAppender
{
    class BinAppender
    {
        private const string HELP_PAGE =
"""

  a, append     Add a batch script
  c, changebin  Change the directory of the batch scripts
  d, delete     Delete a batch script
  g, get        Show the path to the bin directory
  h, help       This help page
  l, ls, dir    Get all of the scripts in the bin directory and their content
  o, overwrite  Overwrite an existing or add a new batch script
  q, quit, exit Exit this Program

""";

        private const string ENVIRONMENT_VARIABLE_NAME = "BinAppender_BinPath";
        private static string BinPath = string.Empty;  // It gets the actual path to the bin directory in InitBinPath()

        private static void WaitBeforeBadThingsHappen()  // The user has time to Ctrl+C just in case accidents happen
            => Thread.Sleep(3000);

        private static bool StringIsBad(string? inputString)
            => string.IsNullOrEmpty(inputString) || string.IsNullOrWhiteSpace(inputString);


        private static void UserInputIsHelp()
            => Console.WriteLine(HELP_PAGE);


        private static void QuitProgram(int exitCode)
        {
            Console.WriteLine("Program is exiting...");
            Environment.Exit(exitCode);
        }


        private static void UserInputIsDelete()
        {
            if (!CheckIfBinPathExists())
                QuitProgram(1);

            string? batchFileName = null;
            Console.Write("\n");
            while (StringIsBad(batchFileName)) {
                Console.Write("Enter the name of the batch script without the file extension:\n-> ");
                batchFileName = Console.ReadLine();

                if (!File.Exists($"{BinPath}\\{batchFileName}.bat")){
                    Console.WriteLine($"\nThe file `{batchFileName}` doesn't exist");
                    batchFileName = string.Empty;
                }
            }

            Console.Write($"\nDelete `{batchFileName}` (y/n)? When anything else than `y` is entered, the process will be canceled\n-> ");
            if (Console.ReadLine().ToLower() == "y") {
                string pathToScript = $"{BinPath}\\{batchFileName}.bat";

                WaitBeforeBadThingsHappen();

                File.Delete(pathToScript);
                Console.WriteLine($"`{pathToScript}` has been deleted");
                Console.WriteLine("Done!");
            } else {
                Console.WriteLine("Process canceled");
            }
        }


        private static void UserInputIsList()
        {
            if (!CheckIfBinPathExists())
                QuitProgram(1);

            const string FILE_EXTENSION_FOR_SCRIPTS_IN_BINPATH = ".bat";

            // TopDirectoryOnly because it's possible that there are batch files in subdirectories of `BinPath`, that aren't part of %BinAppender_BinPath%
            string[] filesInBin = Directory.GetFiles(BinPath, $"*{FILE_EXTENSION_FOR_SCRIPTS_IN_BINPATH}", SearchOption.TopDirectoryOnly);

            foreach (string scriptFilePath in filesInBin)
            {
                string completeFile = File.ReadAllText(scriptFilePath);
                string firstLineInFile = completeFile.Split('\n')[0];
                string everythingAfterEchoOff = completeFile.Substring(firstLineInFile.Length+1);

                // The batch file was created by this program
                if (everythingAfterEchoOff.StartsWith('"')  && firstLineInFile.ToLower() == "@echo off")
                {
                    string lineWithExecution = completeFile.Split('\n')[1];

                    // Get the file that the batch file will run when executed
                    int from = lineWithExecution.IndexOf('"') + 1;  // +1 to not have the " at the start
                    int to = lineWithExecution.LastIndexOf('"') - 1;  // -1 for the same reason

                    string originalFilePath = lineWithExecution.Substring(from, to);

                    Console.WriteLine($"{scriptFilePath} -> {originalFilePath}");
                } else {  // The batch file was created manually
                    Console.Write($"\nContent of {scriptFilePath}:\n");
                    foreach (string line in completeFile.Split("\n"))
                        if (line.Length >= 1)  // If it's not a newline
                            Console.WriteLine($"\t{line}");
                    Console.WriteLine();
                }

                
            }
        }


        #region Write to bin folder
        /// <summary>
        /// Name of the batch file = Name of the executable file
        /// </summary>
        /// <param name="originalFilePath">The file that will be executed when the batch file is called</param>
        /// <exception cref="FileNotFoundException">Path to the original File has not been found</exception>
        private static void WriteBatchToBinDirectory(string originalFilePath)
        {
            string batchFilePath = $"{BinPath}\\{Path.GetFileNameWithoutExtension(originalFilePath)}.bat";

            if (!File.Exists(originalFilePath))
                throw new FileNotFoundException($"The file `{originalFilePath}` has not been found");

            string contentOfBatchFile = $"@echo off\n\"{originalFilePath}\" %*";
            File.WriteAllText(batchFilePath, contentOfBatchFile);
        }

        /// <summary>
        /// Name of the batch file = aliasForBatchFile
        /// </summary>
        /// <param name="originalFilePath">The file that will be executed when the batch file is called</param>
        /// <param name="aliasForBatchFile">The name for the batch file (without the file extension)</param>
        /// <exception cref="ArgumentNullException"><paramref name="aliasForBatchFile"/> is null</exception>
        /// <exception cref="FileNotFoundException">File in <paramref name="originalFilePath"/> doesn't exist</exception>
        private static void WriteBatchToBinDirectory(string originalFilePath, string? aliasForBatchFile)
        {
            if (StringIsBad(aliasForBatchFile))
                throw new ArgumentNullException($"The provided name for the batch file is empty");

            string batchFilePath = $"{BinPath}\\{Path.GetFileNameWithoutExtension(aliasForBatchFile)}.bat";

            if (!File.Exists(originalFilePath))
                throw new FileNotFoundException($"The file `{originalFilePath}` doesn't exist");

            string contentOfBatchFile = $"@echo off\n\"{originalFilePath}\" %*";
            File.WriteAllText(batchFilePath, contentOfBatchFile);
        }


        private static string WhenBatchFileAlreadyExists(string pathOfOriginalFile, string batchFilePath)
        {
            while (File.Exists(batchFilePath))
            {
                ConsoleColor originalTextColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("A script with the same name already exists");
                Console.ForegroundColor = originalTextColor;

                Console.Write("(o)verwrite the batch script, (c)ontinue with a different name, or cancel the process (x)? (When anything else is entered, the process will be canceled)\n-> ");

                switch (Console.ReadLine().ToLower())
                {
                    case "o":
                        Console.Write($"Really overwrite the {batchFilePath} (y/n)? (When anything else than `y` is entered, the process will be canceled): \n-> ");
                        if (Console.ReadLine().ToLower() == "y")
                        {
                            Console.WriteLine($"`{batchFilePath}` gets overwriten");
                            WaitBeforeBadThingsHappen();
                            return batchFilePath;
                        } else { 
                            return string.Empty;
                        }
                        

                    case "c":
                        string? aliasInput = null;
                        Console.Write("\n");

                        while (StringIsBad(aliasInput)){
                            Console.Write("Enter the name for the batch file without the file extension:\n-> ");
                            aliasInput = Console.ReadLine();
                        }

                        batchFilePath = $"{BinPath}\\{aliasInput}.bat";
                        if (!StringIsBad(aliasInput) && !File.Exists(batchFilePath))
                            return batchFilePath;

                        break;


                    default:
                        return string.Empty;  // Process should be canceled
                }
                batchFilePath = $"{BinPath}\\{Path.GetFileNameWithoutExtension(pathOfOriginalFile)}.bat";  // This code gets executed when 1. A different alias has been entered and 2. A script with the newly entered alias already exists
            }
            throw new FileNotFoundException("The file has not been found");
        }


        private static void UserWantsAlias(string pathOfOriginalFile, string? aliasForBatchFile)
        {
            if (!CheckIfBinPathExists())
                QuitProgram(1);

            string batchFilePath = $"{BinPath}\\{Path.GetFileNameWithoutExtension(aliasForBatchFile)}.bat";
            if (File.Exists(batchFilePath))
            {
                    string? returnedBinPath = WhenBatchFileAlreadyExists(pathOfOriginalFile, batchFilePath);
                    // If string.Empty is returned then the user wants to cancel the process
                    if (returnedBinPath == string.Empty) {
                        Console.WriteLine("Process canceled");
                        return;
                    }
                    aliasForBatchFile = returnedBinPath;
            }

            WriteBatchToBinDirectory(pathOfOriginalFile, aliasForBatchFile);
            Console.WriteLine("Done!");
        }


        private static void UserInputIsAppend()
        {
            if (!CheckIfBinPathExists())
                QuitProgram(1);

            string? pathOfOriginalFile = null;
            Console.WriteLine();  // This newline is being printed out, before the user input, because it looks prettier that way
            while (StringIsBad(pathOfOriginalFile))
            {
                Console.Write("Enter the path to the file (with the file extension) that the batch script will execute:\n-> ");
                pathOfOriginalFile = Console.ReadLine();
            }

            // Remove any " since Windows-Explorer likes to copy the path with an " at the beginning and another " the end.
            // Windows-Explorer does this when the filepath is copied through file > right-click > Copy as Path. (Or atleast Windows 11 does that)
            if (pathOfOriginalFile.Contains('\"'))
                pathOfOriginalFile = pathOfOriginalFile.Trim('\"');

            while (!File.Exists(pathOfOriginalFile))
            {
                Console.Write("\nThe file doesn't exist. Enter again:\n-> ");
                pathOfOriginalFile = Console.ReadLine();
            }

            Console.Write("\nEnter the name for the batch script without the file extension: (Press enter if the name of the script should be the same as the name of the executable)\n-> ");
            string? aliasInput = Console.ReadLine();
            if (!StringIsBad(aliasInput))
            {
                UserWantsAlias(pathOfOriginalFile, aliasInput);
                return;
            }

            string batchFilePath = $"{BinPath}\\{Path.GetFileNameWithoutExtension(pathOfOriginalFile)}.bat";

            if (File.Exists(batchFilePath))
            {
                string returnedBinPath = WhenBatchFileAlreadyExists(pathOfOriginalFile, batchFilePath);
                // If string.Empty is returned, then the user wants to cancel the process
                if (returnedBinPath == string.Empty)
                {
                    Console.WriteLine("Process cancelled");
                    return;
                }

                pathOfOriginalFile = returnedBinPath;
            }

            WriteBatchToBinDirectory(pathOfOriginalFile);
            Console.WriteLine("Done!");
        }


        private static void UserInputIsOverwrite()
        {
            if (!CheckIfBinPathExists())
                QuitProgram(1);

            Console.WriteLine();
            string? batchFileName = null;
            while (StringIsBad(batchFileName))
            {
                Console.Write("Enter the name of the batch script without the file extension:\n-> ");
                batchFileName = Console.ReadLine();
            }

            Console.WriteLine();
            string? originalFilePath = null;
            while (StringIsBad(originalFilePath) || !File.Exists(originalFilePath))
            {
                Console.Write("Enter the path to the file (with the file extension) that the batch script should execute instead:\n-> ");
                originalFilePath = Console.ReadLine();

                // Remove any " since Windows Explorer likes to copy the path with 2 " at the beginning and at the end.
                // Windows Explorer does this when the filepath is copied through {filename} > right-click > Copy as Path
                if (originalFilePath.Contains('\"'))
                    originalFilePath = originalFilePath.Trim('\"');

                if (!File.Exists(originalFilePath))
                    Console.Write("The file doesn't exist.\n");
            }
            
            if (File.Exists(batchFileName))
                Console.WriteLine($"`{batchFileName}` will be overwritten");
            else
                Console.WriteLine($"`{batchFileName}` will be created");
            WriteBatchToBinDirectory(originalFilePath, batchFileName);
            Console.WriteLine("Done!");
        }
        #endregion


        #region Set and change environment variables
        private static bool CheckIfBinPathExists()
        {
            // It's possible that the directory of BinPath has been deleted midway through a process

            if (Directory.Exists(BinPath))
                return true;

            ConsoleColor originalTextColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nThe directory for the batch scripts `{BinPath}` doesn't exist.");
            Console.ForegroundColor = originalTextColor;
            Console.Write("Create the (d)irectory, (e)nter a different directory, or (c)ancel the process? (When anything else than `d` or `e` is entered, the process will be canceled):\n-> ");

            switch (Console.ReadLine().ToLower())
            {
                case "d":
                    Directory.CreateDirectory(BinPath);
                    Console.WriteLine($"Directory `{BinPath}` created\n");
                    break;

                case "e":
                    ChangeBinPath();
                    break;

                default:
                    return false;
            }
            return true;
        }


        private static void ChangeBinPath()
        {
            string? binPathByUser = null;
            Console.Write("\nEnter the path to the bin directory:\n-> ");
            while (!Directory.Exists(binPathByUser) || StringIsBad(binPathByUser)) {
                binPathByUser = Console.ReadLine();

                if(File.Exists(binPathByUser))
                    Console.Write("\nThe given path is a file. Enter the path to the bin directory again:\n->");

                else if (!Directory.Exists(binPathByUser))
                {
                    Console.Write($"\nThe directory doesn't exist. Create the (d)irectory `{binPathByUser}` or (c)ancel the process? (When anything else than `d` is entered, the process will be canceled):\n-> ");

                    if (Console.ReadLine().ToLower() == "d")
                    {
                        Directory.CreateDirectory(binPathByUser);
                        Console.WriteLine($"Directory `{binPathByUser}` created");
                        break;
                    } else
                        QuitProgram(1);
                } else if (StringIsBad(binPathByUser))
                    Console.Write("\nEnter the path to the bin folder:\n-> ");
            }

            BinPath = binPathByUser;
            Environment.SetEnvironmentVariable(ENVIRONMENT_VARIABLE_NAME, BinPath, EnvironmentVariableTarget.User);

            string directoryOfBinAppenerExe = Path.GetDirectoryName(AppContext.BaseDirectory);

            // This adds a script for binappender.exe to the new bin directory if it isn't already there
            if (!File.Exists($"{BinPath}\\binappender.bat"))
            {
                Console.WriteLine($"A batch script for BinAppender.exe will be added to `{BinPath}`");
                string pathToBinAppenderExe = Path.Combine(directoryOfBinAppenerExe, "BinAppender.exe");
                WriteBatchToBinDirectory(pathToBinAppenderExe, "binappender");
            }

            // TODO: Test if this works
            // Copied from chocolatey (https://github.com/chocolatey/choco/blob/develop/src/chocolatey.resources/redirects/RefreshEnv.cmd)
            // This is the easiest way for me to refresh all of the Environment Variables in the console instance
            // Process.Start(Path.Combine(directoryOfBinAppenerExe, "RefreshEnv.cmd")).WaitForExit();
            string pathToRefreshEnv = Path.Combine(directoryOfBinAppenerExe, "RefreshEnv.cmd");
            new ProcessStartInfo("cmd.exe", pathToRefreshEnv);

            Console.WriteLine("Done!");
            Console.WriteLine("\n");
        }

        private static void InitBinPath()
        {
            string? environmentVariable = Environment.GetEnvironmentVariable(ENVIRONMENT_VARIABLE_NAME, EnvironmentVariableTarget.User);
            if (!StringIsBad(environmentVariable))
                BinPath = environmentVariable;
            else
            {
                ConsoleColor originalTextColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("No environment variable for the bin directory has been created");
                Console.ForegroundColor = originalTextColor;
                ChangeBinPath();
            }

            if (!CheckIfBinPathExists())
                QuitProgram(1);

            string? pathVariable = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
            if (StringIsBad(pathVariable)) {
                Console.Write("Environment variable \"Path\" has not been found");
                QuitProgram(1);
                return;
            }

            if (!pathVariable.Split(';').Contains($"%{ENVIRONMENT_VARIABLE_NAME}%"))
            {
                Console.Write($"\nEnvironment variable \"Path\" doesn't contain \"%{ENVIRONMENT_VARIABLE_NAME}%\". \"%{ENVIRONMENT_VARIABLE_NAME}%\" is being added\n");
                pathVariable = string.Join(';', pathVariable.Split(';').Append($"%{ENVIRONMENT_VARIABLE_NAME}%"));
                Environment.SetEnvironmentVariable("Path", pathVariable, EnvironmentVariableTarget.User);
            }

        }
        #endregion
        

        public static void Main()
        {
            InitBinPath();

            string? userInput;

            while (true)
            {
                Console.Write("Enter an option (h for help):\n-> ");
                if (StringIsBad(userInput = Console.ReadLine())) 
                    continue;

                switch (userInput.ToLower())
                {
                    case "a":
                    case "append":
                        UserInputIsAppend();
                        break;

                    case "c":
                    case "changebin":
                        ChangeBinPath();
                        break;

                    case "d":
                    case "delete":
                        UserInputIsDelete();
                        break;

                    case "g":
                    case "get":
                        Console.WriteLine();
                        Console.WriteLine(BinPath);
                        break;

                    case "h":
                    case "help":
                        UserInputIsHelp();
                        break;
                    
                    // Some use WSL or Powershell, some use CMD, some use both
                    case "l":
                    case "ls":
                    case "dir":
                        UserInputIsList();
                        break;

                    case "o":
                    case "overwrite":
                        UserInputIsOverwrite();
                        break;

                    case "q":
                    case "quit":
                    case "exit":
                        QuitProgram(0);
                        break;

                    default: continue;
                }
                Console.WriteLine($"\n{new string('-', Console.WindowWidth)}\n");  // After a command has been completed or cancelled
            }
        }
    }
}
