using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Program
{
    class Program
    {
        // Das ist der String, der gezeigt wird, wenn h eingegeben wird
        private const string HILFE_SEITE =
"""

  a, append    Binary Hinzufügen
  b, bin_set   Pfad zum bin-Ordner ändern
  d, delete    Binary Löschen
  h, hilfe     Hilfe
  q, quit      Schließen

""";

        private const string ENVIRONMENT_VARIABLE_NAME = "BinAppender_BinPath";
        private static string BinPath = string.Empty;

        private static void WaitBeforeBadThingsHappen()  // Hierdurch hat der Nutzer Zeit Ctrl+C zu drücken
            => Thread.Sleep(3000);

        private static bool StringIsBad(string? inputString)
            => string.IsNullOrEmpty(inputString) || string.IsNullOrWhiteSpace(inputString);


        private static void UserInputIsHelp()
            => p.cw(HILFE_SEITE);


        private static void QuitProgram(int exitCode)
        {
            p.cw("Programm wird beendet");
            Environment.Exit(exitCode);
        }


        private static void UserInputIsDeleteBinary()
        {
            string? binName = null;
            p.w("\n");
            while (StringIsBad(binName)) {
                p.w("Gebe den Namen des Binaries, ohne Dateiendung, ein, der gelöscht werden soll:\n-> ");
                binName = Console.ReadLine();

                if (!File.Exists($"{BinPath}\\{binName}.bat")){
                    p.cw($"\nDas binary `{binName}` existiert nicht.");
                    binName = string.Empty;
                }
            }

            p.w($"\nBinary `{binName}` wirklich löschen? (j/n) Wenn etwas anderes als `j` angegeben wird, wird abgebrochen\n-> ");
            if (Console.ReadLine().ToLower() == "j") {
                string pathToBinary = $"{BinPath}\\{binName}.bat";

                WaitBeforeBadThingsHappen();

                File.Delete(pathToBinary);
                p.cw($"Binary {pathToBinary} wurde gelöscht");
                p.cw("Erledigt!");
            } else {
                p.cw("Vorgang wird abgebrochen");
            }
        }


        #region Zum bin-Ordner Schreiben
        /// <summary>
        /// Name der Batchdatei = Name der .exe Datei
        /// </summary>
        /// <param name="originalFilePath"></param>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        private static void WriteBatchToBin(string originalFilePath)
        {
            string batchFilePath = $"{BinPath}\\{Path.GetFileNameWithoutExtension(originalFilePath)}.bat";

            if (!File.Exists(originalFilePath))
                throw new FileNotFoundException($"Die eingegebene Datei `{originalFilePath}`wurde nicht gefunden");

            string contentOfBatchFile = $"@echo off\n\"{originalFilePath}\" %*";
            File.WriteAllText(batchFilePath, contentOfBatchFile);
        }

        /// <summary>
        /// Name der Batchdatei = aliasForBatchFile
        /// </summary>
        /// <param name="originalFilePath"></param>
        /// <param name="aliasForBatchFile">Der andere Name für die Batchdatei (!OHNE DATEIENDUNG!)</param>
        /// <exception cref="FileNotFoundException">Datei in <paramref name="originalFilePath"/>existiert nicht</exception>
        /// <exception cref="Exception">Batchdatei mit dem Namen<paramref name="aliasForBatchFile"/>existiert bereits</exception>
        /// <exception cref="ArgumentNullException"><paramref name="aliasForBatchFile"/>ist Null</exception>
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
                p.cw("Eine Datei mit demselben Namen existert bereits im bin Ordner");
                Console.ForegroundColor = originalTextColor;

                p.w("Datei (u)eberschreiben, mit einem anderen Alias (f)ortfahren, oder Vorgang a(b)brechen? (Default ist abbrechen)\n-> ");

                switch (Console.ReadLine().ToLower())
                {
                    case "u":
                        p.w("Datei wirklich überschreiben? (j/n): (Default ist nein und der Vorgang abgebrochen)\n-> ");
                        if (Console.ReadLine().ToLower() == "j")
                        {
                            p.cw($"Der Binary `{batchFilePath}` wird überschrieben");
                            WaitBeforeBadThingsHappen();
                            return batchFilePath;
                        } else { 
                            return string.Empty;
                        }
                        

                    case "f":
                        string? aliasInput = null;
                        p.w("\n");
                        while (StringIsBad(aliasInput)){
                            p.w("Alias ohne Dateiendung eingeben:\n-> ");
                            aliasInput = Console.ReadLine();
                        }

                        batchFilePath = $"{BinPath}\\{aliasInput}.bat";
                        if (!StringIsBad(aliasInput) && !File.Exists(batchFilePath))
                            return batchFilePath;
                        
                        break;


                    default:
                        return string.Empty;  // Es soll abgebrochen werden
                }
                batchFilePath = $"{BinPath}\\{Path.GetFileNameWithoutExtension(pathOfOriginalFile)}.bat";
            }
            throw new FileNotFoundException("Die Datei wurde nicht gefunden");
        }


        private static void UserWantsAlias(string pathOfOriginalFile, string? aliasForBatchFile, bool ueberschreiben)
        {
            string batchFilePath = $"{BinPath}\\{Path.GetFileNameWithoutExtension(aliasForBatchFile)}.bat";
            if (File.Exists(batchFilePath))
            {
                if (ueberschreiben)
                    WriteBatchToBin(pathOfOriginalFile, aliasForBatchFile);

                if (!ueberschreiben)
                {
                    string? returnedBinPath = WhenBatchFileAlreadyExists(pathOfOriginalFile, batchFilePath);
                    if (returnedBinPath == string.Empty) {  // Wenn string.Empty wiedergegeben wird, dann will der Nutzer das Programm abbrechen
                        p.cw("Vorgang wird abgebrochen");
                        return;
                    }
                    WriteBatchToBin(pathOfOriginalFile, Path.GetFileNameWithoutExtension(returnedBinPath));
                }

                p.cw("Erledigt!");
                return;
            }

            WriteBatchToBin(pathOfOriginalFile, aliasForBatchFile);  // Es it normal, wenn die Datei nicht existiert
            p.cw("Erledigt!");
        }


        private static void UserInputIsAppend(bool ueberschreiben)
        {
            string? pathOfOriginalFile = null;
            p.w("\n");  // Die neue Zeile wird erst hier geschrieben, weil der string, vor dem Nutzerinput, ohne 2 Zeilenumbruche ausgegeben werden sikk
            while (StringIsBad(pathOfOriginalFile))
            {
                p.w("Gib den Pfad zum originalen Programm ein (Mit Dateiendung):\n-> ");
                pathOfOriginalFile = Console.ReadLine();
            }

            // Entferne " vorne und hinten. Windows Explorer kopiert den Pfad mit zwei " vorne und hinten, wenn man es durch `{Dateiname} > Rechts Klick > Als Pfad kopieren` kopiert
            if (pathOfOriginalFile.Contains('\"'))
                pathOfOriginalFile = pathOfOriginalFile.Trim('\"');

            while (!File.Exists(pathOfOriginalFile))
            {
                p.w("\nDie Datei existiert nicht. Nochmal eingeben:\n-> ");
                pathOfOriginalFile = Console.ReadLine();
            }


            p.w("\nAlias ohne Dateiendung eingeben: (Bei keinem Alias einfach `Enter` drücken)\n-> ");
            string? aliasInput = Console.ReadLine();
            if (!StringIsBad(aliasInput))
            {
                UserWantsAlias(pathOfOriginalFile, aliasInput, false);
                return;
            }

            string batchFilePath = $"{BinPath}\\{Path.GetFileNameWithoutExtension(pathOfOriginalFile)}.bat";

            if (!ueberschreiben && File.Exists(batchFilePath))
            {
                string returnedBinPath = WhenBatchFileAlreadyExists(pathOfOriginalFile, batchFilePath);
                if (returnedBinPath == string.Empty)
                {  // Wenn returnedBinPath string.Empty ist, dann wurde abgebrochen
                    p.cw("Vorgang wird abgebrochen");
                    return;
                }

                WriteBatchToBin(returnedBinPath);
            }

            p.cw("Es wird kein Alias benutzt");
            WriteBatchToBin(pathOfOriginalFile);
            p.cw("Erledigt!");
        }
        #endregion


        private static void ChangeBinPath()
        {
            string? path = null;
            p.w("\nPfad zum bin-Ordner eingeben:\n-> ");
            while (!Directory.Exists(path) || StringIsBad(path)) {
                path = Console.ReadLine();

                if (!Directory.Exists(path))
                    p.cw("Der Ordner existiert nicht. Pfad zum Ordner nochmal eingeben:\n-> ");
                else if (StringIsBad(path))
                    p.w("Pfad zum bin-Ordner eingeben:\n-> ");
            }
            BinPath = path;
            Environment.SetEnvironmentVariable(ENVIRONMENT_VARIABLE_NAME, BinPath, EnvironmentVariableTarget.User);
            p.cw("Erledigt!");
            p.cw("\n");
        }


        private static void InitBinPath()
        {
            string? environmentVariable = Environment.GetEnvironmentVariable(ENVIRONMENT_VARIABLE_NAME, EnvironmentVariableTarget.User);
            if (!StringIsBad(environmentVariable))
                BinPath = environmentVariable;
            else
            {
                p.w("Es wurde noch keine Umgebungsvariable für den bin-Ordner erstellt");
                ChangeBinPath();
            }

            string? pathVariable = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
            if (StringIsBad(pathVariable)){
                p.w("Umgebungsvariable \"Path\" wurde nicht gefunden");
                QuitProgram(1);
                return;
            }

            if (!pathVariable.Split(';').Contains($"%{ENVIRONMENT_VARIABLE_NAME}%"))
            {
                p.w($"\nUmgebungsvariable %Path% enthält %{ENVIRONMENT_VARIABLE_NAME}% nicht. %{ENVIRONMENT_VARIABLE_NAME}% wird jetzt hinzugefügt\n");
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
                p.w("Was soll gemacht werden? (h für file):\n-> ");
                if (StringIsBad(userInput = Console.ReadLine()))  // Benutzer hat Enter eingegeben 
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

                    case "h":
                    case "help":
                        UserInputIsHelp();
                        break;

                    case "d":
                    case "delete":
                        UserInputIsDeleteBinary();
                        break;

                    case "q":
                    case "quit":
                        QuitProgram(0);
                        break;

                    default: continue;
                }

                p.cw($"\n{new string('-', Console.WindowWidth)}\n");
            }
        }
    }
}
