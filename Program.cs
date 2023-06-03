using System;
using System.IO;
using System.Threading;

namespace Program
{
    class Program
    {
        private const string HELP_PAGE =
"""

  a, append    Add binary
  b, bin_set   Change the path of the folder for the binaries
  d, delete    Delete binary
  h, hilfe     This help page
  q, quit      Exit

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


        private static void UserWantsAlias(string pathOfOriginalFile, string? aliasForBatchFile, bool overwrite)
        {
            string batchFilePath = $"{BinPath}\\{Path.GetFileNameWithoutExtension(aliasForBatchFile)}.bat";
            if (File.Exists(batchFilePath))
            {
                if (!overwrite)
                {
                    string? returnedBinPath = WhenBatchFileAlreadyExists(pathOfOriginalFile, batchFilePath);
                    // If string.Empty is returned, then the user wants to cancel the process
                    if (returnedBinPath == string.Empty) {
                        Console.WriteLine("Process canceled");
                        return;
                    }
                    aliasForBatchFile = returnedBinPath;
                }
            }

            WriteBatchToBin(pathOfOriginalFile, aliasForBatchFile);
            Console.WriteLine("Done!");
        }


        private static void UserInputIsAppend(bool overwrite)
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

            Console.Write("\nEnter alias without a file extension: (Just press enter if there shouldn't be a different name for the binary)\n-> ");
            string? aliasInput = Console.ReadLine();
            if (!StringIsBad(aliasInput))
            {
                UserWantsAlias(pathOfOriginalFile, aliasInput, false);
                return;
            }

            string batchFilePath = $"{BinPath}\\{Path.GetFileNameWithoutExtension(pathOfOriginalFile)}.bat";

            if (!overwrite && File.Exists(batchFilePath))
            {
                string returnedBinPath = WhenBatchFileAlreadyExists(pathOfOriginalFile, batchFilePath);
                // If string.Empty is returned, then the user wants to cancel the process
                if (returnedBinPath == string.Empty)
                {
                    Console.WriteLine("Process cancelled");
                    return;
                }

                WriteBatchToBin(returnedBinPath);
            }

            Console.WriteLine("No alias is being used");
            WriteBatchToBin(pathOfOriginalFile);
            Console.WriteLine("Done!");
        }
        #endregion


        private static void ChangeBinPath()
        {
            string? path = null;
            Console.Write("\nEnter the path to the bin folder:\n-> ");
            while (!Directory.Exists(path) || StringIsBad(path)) {
                path = Console.ReadLine();

                if(File.Exists(path))
                    Console.Write("\nThe given Path is a File. Enter the path to the bin folder again:\n->");

                else if (!Directory.Exists(path))
                {
                    Console.Write($"\nThe directory doesn't exist. Create the (d)irectory `{path}` or (c)ancel the process? (Default ist to cancel the process):\n-> ");

                    if (Console.ReadLine().ToLower() == "d")
                    {
                        Directory.CreateDirectory(path);
                        Console.WriteLine($"Directory `{path}` created");
                        break;
                    } else
                        return;
                } else if (StringIsBad(path))
                    Console.Write("\nEnter the path to the bin folder:\n-> ");
            }
            BinPath = path;
            Environment.SetEnvironmentVariable(ENVIRONMENT_VARIABLE_NAME, BinPath, EnvironmentVariableTarget.User);
            Console.WriteLine("Done!");
            Console.WriteLine("\n");
        }


        // TODO: A binary/batch file for binappender should automatically be added to the BinPath (Path of .exe: System.Reflection.Assembly.GetEntryAssembly().Location)
        private static void InitBinPath()
        {
            string? environmentVariable = Environment.GetEnvironmentVariable(ENVIRONMENT_VARIABLE_NAME, EnvironmentVariableTarget.User);
            if (!StringIsBad(environmentVariable))
                BinPath = environmentVariable;
            else
            {
                ConsoleColor originalTextColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("No environment variable fo the bin folder has been created");
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
                        UserInputIsAppend(false);
                        break;

                    case "b":
                    case "bin_set":
                        ChangeBinPath();
                        break;

                    case "d":
                    case "delete":
                        UserInputIsDeleteBinary();
                        break;

                    case "h":
                    case "help":
                        UserInputIsHelp();
                        break;

                    case "o":
                    case "overwrite":
                        throw new NotImplementedException("TODO: Add Option to overwrite binaries (with a new executable)");
                        UserInputIsAppend(true);  // It might be this easy, but I haven't tested anything yet
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
