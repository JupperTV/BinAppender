using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Threading;

namespace BinAppender
{
    class BinAppender
    {
        private const string HELP_PAGE =
"""

  a, append     Add binary
  c, changebin  Change the directory of the binaries
  d, delete     Delete binary
  g, get        Get all of the files in the bin directory and their content
  h, help       This help page
  o, overwrite  Add or overwrite an existing binary
  p, print      Show the path to the bin directory
  q, quit       Exit this Program

""";

        private const string ENVIRONMENT_VARIABLE_NAME = "BinAppender_BinPath";
        private static string BinPath = string.Empty;


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


        private static void UserInputIsDeleteBinary()
        {
            string? binName = null;
            Console.Write("\n");
            while (StringIsBad(binName)) {
                Console.Write("Enter the name of the binary without the file extension:\n-> ");
                binName = Console.ReadLine();

                if (!File.Exists($"{BinPath}\\{binName}.bat")){
                    Console.WriteLine($"\nThe binary `{binName}` doesn't exist");
                    binName = string.Empty;
                }
            }

            Console.Write($"\nDelete binary `{binName}`? (y/n) Wenn anything else than `y` is entered, the process will be canceled\n-> ");
            if (Console.ReadLine().ToLower() == "y") {
                string pathToBinary = $"{BinPath}\\{binName}.bat";

                WaitBeforeBadThingsHappen();

                File.Delete(pathToBinary);
                Console.WriteLine($"Binary {pathToBinary} has been deleted");
                Console.WriteLine("DONE!");
            } else {
                Console.WriteLine("Process canceled");
            }
        }


        private static void UserInputIsListFiles()
        {
            const string FILE_EXTENSION_FOR_BINARIES_IN_BINPATH = ".bat";
            // TopDirectoryOnly because it's possible that there are batch files in subdirectories of `BinPath`, which won't be part of %BinAppender_BinPath%
            string[] filesInBin = Directory.GetFiles(BinPath, $"*{FILE_EXTENSION_FOR_BINARIES_IN_BINPATH}", SearchOption.TopDirectoryOnly);

            foreach (string batchFilePath in filesInBin)
            {
                string completeFile = File.ReadAllText(batchFilePath);
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

                    Console.WriteLine($"{batchFilePath} -> {originalFilePath}");
                } else {  // The batch file was created manually

                    Console.Write($"\nContent of {batchFilePath}:\n");
                    foreach (string line in completeFile.Split("\n"))
                        if (line.Length >= 1)  // If it's not a newline
                            Console.WriteLine($"\t{line}");

                    Console.WriteLine();
                }

                
            }
        }


        #region Write to bin folder
        /// <summary>
        /// Name of the batch file = Name of the .exe file
        /// </summary>
        /// <param name="originalFilePath"></param>
        /// <exception cref="FileNotFoundException">Path to the original File has not been found</exception>
        private static void WriteBatchToBin(string originalFilePath)
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
        /// <param name="originalFilePath"></param>
        /// <param name="aliasForBatchFile">Der andere Name für die Batchdatei (!OHNE DATEIENDUNG!)</param>
        /// <exception cref="ArgumentNullException"><paramref name="aliasForBatchFile"/> is null</exception>
        /// <exception cref="FileNotFoundException">`File in <paramref name="originalFilePath"/> doesn't exist/exception>
        private static void WriteBatchToBin(string originalFilePath, string? aliasForBatchFile)
        {
            if (StringIsBad(aliasForBatchFile))
                throw new ArgumentNullException($"Alias für das Programm ist leer. alias={aliasForBatchFile}");

            string batchFilePath = $"{BinPath}\\{Path.GetFileNameWithoutExtension(aliasForBatchFile)}.bat";

            if (!File.Exists(originalFilePath))
                throw new FileNotFoundException($"Die eingegebene Datei `{originalFilePath}` wurde nicht gefunden");

            string contentOfBatchFile = $"@echo off\n\"{originalFilePath}\" %*";
            File.WriteAllText(batchFilePath, contentOfBatchFile);
        }


        private static string WhenBatchFileAlreadyExists(string pathOfOriginalFile, string batchFilePath)
        {
            while (File.Exists(batchFilePath))
            {
                ConsoleColor originalTextColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("A binary with the same name already exists");
                Console.ForegroundColor = originalTextColor;

                Console.Write("(o)verwrite the file, (c)ontinue with a different alias, or cancel program (x)? (Default is cancel program)\n-> ");

                switch (Console.ReadLine().ToLower())
                {
                    case "o":
                        Console.Write("Really overwrite the file? (y/n): (Default is no and the process will be canceled)\n-> ");
                        if (Console.ReadLine().ToLower() == "y")
                        {
                            Console.WriteLine($"The binary `{batchFilePath}` gets overwriten");
                            WaitBeforeBadThingsHappen();
                            return batchFilePath;
                        } else { 
                            return string.Empty;
                        }
                        

                    case "c":
                        string? aliasInput = null;
                        Console.Write("\n");
                        while (StringIsBad(aliasInput)){
                            Console.Write("Enter alias without the file extension:\n-> ");
                            aliasInput = Console.ReadLine();
                        }

                        batchFilePath = $"{BinPath}\\{aliasInput}.bat";
                        if (!StringIsBad(aliasInput) && !File.Exists(batchFilePath))
                            return batchFilePath;
                        
                        break;


                    default:
                        return string.Empty;  // Es soll abgebrochen werden
                }
                batchFilePath = $"{BinPath}\\{Path.GetFileNameWithoutExtension(pathOfOriginalFile)}.bat";  // This code gets executed when a different alias is being entered and the file exists
            }
            throw new FileNotFoundException("The file has not been found");
        }


        private static void UserWantsAlias(string pathOfOriginalFile, string? aliasForBatchFile)
        {
            string batchFilePath = $"{BinPath}\\{Path.GetFileNameWithoutExtension(aliasForBatchFile)}.bat";
            if (File.Exists(batchFilePath))
            {
                    string? returnedBinPath = WhenBatchFileAlreadyExists(pathOfOriginalFile, batchFilePath);
                    // If string.Empty is returned, then the user wants to cancel the process
                    if (returnedBinPath == string.Empty) {
                        Console.WriteLine("Process canceled");
                        return;
                    }
                    aliasForBatchFile = returnedBinPath;
            }

            WriteBatchToBin(pathOfOriginalFile, aliasForBatchFile);
            Console.WriteLine("Done!");
        }


        private static void UserInputIsAppend()
        {
            string? pathOfOriginalFile = null;
            Console.Write("\n");  // This newline is only being printed out before the user input because it looks prettier
            while (StringIsBad(pathOfOriginalFile))
            {
                Console.Write("Enter the path to the original file (with the file extension):\n-> ");
                pathOfOriginalFile = Console.ReadLine();
            }

            // Remove any " since Windows Explorer likes to copy the path with 2 " at the beginning and at the end.
            // Windows Explorer does this when the filepath is copied through {filename} > right-click > Copy as Path
            if (pathOfOriginalFile.Contains('\"'))
                pathOfOriginalFile = pathOfOriginalFile.Trim('\"');

            while (!File.Exists(pathOfOriginalFile))
            {
                Console.Write("\nThe file doesn't exist. Enter again:\n-> ");
                pathOfOriginalFile = Console.ReadLine();
            }

            Console.Write("\nEnter an alias for the binary without a file extension: (Just press enter if there shouldn't be a different name for the binary)\n-> ");
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

                pathOfOriginalFile = returnedBinPath;  // The file will be written outside this if-block
            }

            Console.WriteLine("No alias is being used");
            WriteBatchToBin(pathOfOriginalFile);
            Console.WriteLine("Done!");
        }


        private static void UserInputIsOverwriteBinary()
        {
            Console.WriteLine();
            string? binaryName = null;
            while (StringIsBad(binaryName))
            {
                Console.Write("Enter the alias without the file extension:\n-> ");
                binaryName = Console.ReadLine();
            }

            Console.WriteLine();
            string? originalFilePath = null;
            while (StringIsBad(originalFilePath) || !File.Exists(originalFilePath))
            {
                Console.Write("Enter the path to the original file (with the file extension):\n-> ");
                originalFilePath = Console.ReadLine();

                // Remove any " since Windows Explorer likes to copy the path with 2 " at the beginning and at the end.
                // Windows Explorer does this when the filepath is copied through {filename} > right-click > Copy as Path
                if (originalFilePath.Contains('\"'))
                    originalFilePath = originalFilePath.Trim('\"');

                if (!File.Exists(originalFilePath))
                    Console.Write("The file doesn't exist.\n"); 
            }
            
            // A file will be writen or overwriten, whether the file exists or not
            Console.WriteLine($"Binary `{binaryName}` will be overwritten");
            WriteBatchToBin(originalFilePath, binaryName);
            Console.WriteLine("Done!");
        }
        #endregion


        private static void ChangeBinPath()
        {
            string? binPathByUser = null;
            Console.Write("\nEnter the path to the bin folder:\n-> ");
            while (!Directory.Exists(binPathByUser) || StringIsBad(binPathByUser)) {
                binPathByUser = Console.ReadLine();

                if(File.Exists(binPathByUser))
                    Console.Write("\nThe given Path is a File. Enter the path to the bin folder again:\n->");

                else if (!Directory.Exists(binPathByUser))
                {
                    Console.Write($"\nThe directory doesn't exist. Create the (d)irectory `{binPathByUser}` or (c)ancel the process? (Default ist to cancel the process):\n-> ");

                    if (Console.ReadLine().ToLower() == "d")
                    {
                        Directory.CreateDirectory(binPathByUser);
                        Console.WriteLine($"Directory `{binPathByUser}` created");
                        break;
                    } else
                        return;
                } else if (StringIsBad(binPathByUser))
                    Console.Write("\nEnter the path to the bin folder:\n-> ");
            }

            BinPath = binPathByUser;
            Environment.SetEnvironmentVariable(ENVIRONMENT_VARIABLE_NAME, BinPath, EnvironmentVariableTarget.User);

            // Add binappender to the new bin directory if it it isn't there already
            if (!File.Exists($"{BinPath}\\binappender.bat"))  // It's possible that the batch file for binappender is already in the new bin directory
            {
                Console.WriteLine($"BinAppender.exe will be added to the path `{BinPath}`");
                string directoryOfExe = Path.GetDirectoryName(AppContext.BaseDirectory);  // TODO: Check if this stuff works when it's executed as a single file and normally through Visual Studio
                string pathToExe = Path.Combine(directoryOfExe, "BinAppender.exe");
                WriteBatchToBin(pathToExe, "binappender");
            }

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
                Console.Write("No environment variable for the bin folder has been created");
                Console.ForegroundColor = originalTextColor;
                ChangeBinPath();
            }

            string? pathVariable = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
            if (StringIsBad(pathVariable)){
                Console.Write("Environment variable \"Path\" has not been found");
                QuitProgram(1);
                return;
            }

            if (!pathVariable.Split(';').Contains($"%{ENVIRONMENT_VARIABLE_NAME}%"))
            {
                Console.Write($"\nEnvironment variable %Path% doesn't contain %{ENVIRONMENT_VARIABLE_NAME}%. %{ENVIRONMENT_VARIABLE_NAME}% is being added\n");
                pathVariable = string.Join(';', pathVariable.Split(';').Append($"%{ENVIRONMENT_VARIABLE_NAME}%"));
                Environment.SetEnvironmentVariable("Path", pathVariable, EnvironmentVariableTarget.User);
            }
        }

        
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
                        UserInputIsDeleteBinary();
                        break;

                    case "g":
                    case "get":
                        UserInputIsListFiles();
                        break;

                    case "h":
                    case "help":
                        UserInputIsHelp();
                        break;

                    case "o":
                    case "overwrite":
                        UserInputIsOverwriteBinary();
                        break;

                    case "p":
                    case "print":
                        Console.WriteLine();
                        Console.WriteLine(BinPath);
                        break;

                    case "q":
                    case "quit":
                        QuitProgram(0);
                        break;

                    default: continue;
                }
                Console.WriteLine($"\n{new string('-', Console.WindowWidth)}\n");  // After any option has been completed or cancelled
            }
        }
    }
}
