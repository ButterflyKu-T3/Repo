using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace WordsGame
{
    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== Игра «Слова» / The Words Game ===");

            // Language selection loop
            string lang;
            while (true)
            {
                Console.Write("Выберите язык / Choose language (ru/en): ");
                lang = Console.ReadLine()?.Trim().ToLower() ?? string.Empty;
                if (lang == "ru" || lang == "en") break;
                Console.WriteLine("Пожалуйста, введите 'ru' или 'en' / Please enter 'ru' or 'en'.");
            }

            // Set culture for resources
            var culture = new CultureInfo(lang);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            // Create UI and start the game
            IGameUI ui = new ConsoleGameUI(culture);
            var game = new Game(ui);
            game.Start();
        }
    }

    /// <summary>
    /// Interface for user interaction layer (decoupled from game logic).
    /// </summary>
    public interface IGameUI
    {
        bool IsRussianLanguage();
        string GetBaseWord();
        string? ReadWordWithTimeout(int timeoutMs);
        void ShowMessage(string key);
        void ShowMessageFormatted(string key, string arg);
        void WaitForExit();
    }

    /// <summary>
    /// Main game logic class.
    /// </summary>
    public class Game
    {
        private readonly IGameUI ui;
        private readonly HashSet<string> usedWords = new();
        private const int TurnTimeoutMs = 10000;
        private string baseWord = string.Empty;
        private bool isRussian;
        private int currentPlayer = 1;

        /// <summary>
        /// Constructor for Game, injecting the UI dependency.
        /// </summary>
        /// <param name="ui">The user interface implementation.</param>
        public Game(IGameUI ui)
        {
            this.ui = ui;
        }

        /// <summary>
        /// Starts the game loop.
        /// </summary>
        public void Start()
        {
            isRussian = ui.IsRussianLanguage();
            baseWord = ui.GetBaseWord();
            ui.ShowMessage("msgGameStart");

            while (true)
            {
                ui.ShowMessageFormatted("msgPlayerTurn", currentPlayer.ToString());
                var turnStart = DateTime.UtcNow;
                bool turnCompleted = false;

                while (!turnCompleted)
                {
                    var elapsed = (int)(DateTime.UtcNow - turnStart).TotalMilliseconds;
                    var remaining = TurnTimeoutMs - elapsed;
                    if (remaining <= 0)
                    {
                        ui.ShowMessageFormatted("msgLoseTimeout", currentPlayer.ToString());
                        ui.ShowMessage("msgEnd");
                        ui.WaitForExit();
                        return;
                    }

                    string? word = ui.ReadWordWithTimeout(remaining);
                    if (word == null)
                    {
                        ui.ShowMessageFormatted("msgLoseTimeout", currentPlayer.ToString());
                        ui.ShowMessage("msgEnd");
                        ui.WaitForExit();
                        return;
                    }

                    word = word.ToLower().Trim();
                    if (string.IsNullOrWhiteSpace(word))
                    {
                        ui.ShowMessage("msgEmpty");
                        continue;
                    }

                    if (!GameHelper.IsValidForLanguage(word, isRussian))
                    {
                        ui.ShowMessage("msgWrongAlphabet");
                        continue;
                    }

                    if (usedWords.Contains(word))
                    {
                        ui.ShowMessage("msgUsed");
                        continue;
                    }

                    if (!CanMakeWord(word, baseWord))
                    {
                        ui.ShowMessage("msgInvalid");
                        continue;
                    }

                    usedWords.Add(word);
                    currentPlayer = currentPlayer == 1 ? 2 : 1;
                    turnCompleted = true;
                }
            }
        }

        /// <summary>
        /// Checks if a word can be formed from the letters in the base word.
        /// </summary>
        /// <param name="word">The word to check.</param>
        /// <param name="baseWord">The base word providing letters.</param>
        /// <returns>True if the word can be made; otherwise, false.</returns>
        private static bool CanMakeWord(string word, string baseWord)
        {
            var baseLetters = baseWord
                .GroupBy(c => c)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (char c in word)
            {
                if (!baseLetters.TryGetValue(c, out int count) || count <= 0)
                    return false;
                baseLetters[c] = count - 1;
            }

            return true;
        }
    }

    /// <summary>
    /// Console-based UI that uses resource files for localization.
    /// </summary>
    public class ConsoleGameUI : IGameUI
    {
        private readonly CultureInfo culture;

        /// <summary>
        /// Constructor for ConsoleGameUI.
        /// </summary>
        /// <param name="culture">The culture info for localization.</param>
        public ConsoleGameUI(CultureInfo culture)
        {
            this.culture = culture;
        }

        public bool IsRussianLanguage() => culture.TwoLetterISOLanguageName == "ru";

        /// <summary>
        /// Gets the base word from user input, validating length and alphabet.
        /// </summary>
        /// <returns>The validated base word.</returns>
        public string GetBaseWord()
        {
            while (true)
            {
                Console.Write(Strings.msgEnterWord);
                string? input = Console.ReadLine()?.Trim().ToLower();
                if (string.IsNullOrWhiteSpace(input) || input.Length < 8 || input.Length > 30)
                {
                    Console.WriteLine(Strings.msgTooShort);
                    continue;
                }

                if (!GameHelper.IsValidForLanguage(input, IsRussianLanguage()))
                {
                    Console.WriteLine(Strings.msgWrongAlphabet);
                    continue;
                }

                return input;
            }
        }

        /// <summary>
        /// Reads a word from console with a timeout.
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds.</param>
        /// <returns>The input word or null if timed out.</returns>
        public string? ReadWordWithTimeout(int timeoutMs)
        {
            var sb = new StringBuilder();
            var sw = Stopwatch.StartNew();

            while (true)
            {
                if (sw.ElapsedMilliseconds >= timeoutMs)
                    return null;

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        return sb.ToString();
                    }
                    else if (key.Key == ConsoleKey.Backspace)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Length--;
                            Console.Write("\b \b");
                        }
                    }
                    else if (!char.IsControl(key.KeyChar))
                    {
                        sb.Append(key.KeyChar);
                        Console.Write(key.KeyChar);
                    }
                }

                Thread.Sleep(20);
            }
        }

        public void ShowMessage(string key)
        {
            string message = key switch
            {
                "msgGameStart" => Strings.msgGameStart,
                "msgEmpty" => Strings.msgEmpty,
                "msgWrongAlphabet" => Strings.msgWrongAlphabet,
                "msgUsed" => Strings.msgUsed,
                "msgInvalid" => Strings.msgInvalid,
                "msgEnd" => Strings.msgEnd,
                _ => key // Fallback
            };
            Console.WriteLine(message);
        }

        public void ShowMessageFormatted(string key, string arg)
        {
            string format = key switch
            {
                "msgPlayerTurn" => Strings.msgPlayerTurn,
                "msgLoseTimeout" => Strings.msgLoseTimeout,
                _ => key // Fallback
            };
            Console.WriteLine(string.Format(format, arg));
        }

        public void WaitForExit()
        {
            Console.WriteLine(Strings.msgPressEnter);
            Console.ReadLine();
        }
    }

    /// <summary>
    /// Shared helper functions for validation.
    /// </summary>
    public static class GameHelper
    {
        /// <summary>
        /// Validates if a string uses only the allowed alphabet for the language.
        /// </summary>
        /// <param name="s">The string to validate.</param>
        /// <param name="isRussian">True for Russian alphabet; false for English.</param>
        /// <returns>True if valid; otherwise, false.</returns>
        public static bool IsValidForLanguage(string s, bool isRussian)
        {
            if (string.IsNullOrEmpty(s)) return false;

            if (isRussian)
            {
                foreach (char c in s)
                {
                    if (c >= 'а' && c <= 'я') continue;
                    if (c == 'ё') continue;
                    return false;
                }
                return true;
            }
            else
            {
                foreach (char c in s)
                {
                    if (c >= 'a' && c <= 'z') continue;
                    return false;
                }
                return true;
            }
        }
    }
}