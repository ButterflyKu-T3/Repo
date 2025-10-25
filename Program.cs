using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=== Игра «Слова» / The Words Game ===");

        //Выбор языка
        string lang;
        bool isRussian;
        while (true)
        {
            Console.Write("Выберите язык / Choose language (ru/en): ");
            lang = Console.ReadLine()?.Trim().ToLower() ?? string.Empty;
            if (lang == "ru" || lang == "en") break;
            Console.WriteLine("Пожалуйста, введите 'ru' или 'en'. / Please enter 'ru' or 'en'.");
        }
        isRussian = lang == "ru";

        //Локализация
        string msgEnterWord = isRussian ? "Введите исходное слово (8–30 символов): " : "Enter the base word (8–30 characters): ";
        string msgTooShort = isRussian ? "Слово должно содержать от 8 до 30 букв!" : "The word must be between 8 and 30 letters!";
        string msgGameStart = isRussian ? "Начинаем игру!" : "Let’s start the game!";
        string msgPlayerTurn = isRussian ? "Ход игрока" : "Player";
        string msgLose = isRussian ? "пропустил ход и проиграл!" : "missed the turn and lost!";
        string msgUsed = isRussian ? "Это слово уже использовалось!" : "This word has already been used!";
        string msgInvalid = isRussian ? "Слово нельзя составить из букв исходного слова!" : "You can’t make this word from the base word!";
        string msgTime = isRussian ? "Время вышло!" : "Time’s up!";
        string msgEnd = isRussian ? "Игра окончена." : "Game over.";
        string msgEmpty = isRussian ? "Пустой ввод — введите слово снова." : "Empty input — enter a word again.";
        string msgWrongAlphabet = isRussian ? "Слово должно содержать только буквы русского алфавита!" : "The word must contain only letters of the English alphabet!";
        string msgPressEnter = isRussian ? "Нажмите Enter для выхода..." : "Press Enter to exit...";

        // Ввод исходного слова
        string baseWord;
        while (true)
        {
            Console.Write(msgEnterWord);
            string? input = Console.ReadLine()?.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(input) || input.Length <8 || input.Length >30)
            {
                Console.WriteLine(msgTooShort);
                continue;
            }

            if (!languageCheck(input, isRussian))
            {
                Console.WriteLine(msgWrongAlphabet);
                continue;
            }

            baseWord = input;
            break;
        }

        //Подготовка начала игры
        Console.WriteLine("\n" + msgGameStart);
        HashSet<string> usedWords = new();
        int currentPlayer =1;
        const int turnTimeoutMs =10000;

        //Основной цикл
        while (true)
        {
            Console.WriteLine($"\n{msgPlayerTurn} {currentPlayer}: (10 секунд)");

            var turnStart = DateTime.UtcNow;
            bool turnCompleted = false;

            while (!turnCompleted)
            {
                var elapsed = (int)(DateTime.UtcNow - turnStart).TotalMilliseconds;
                var remaining = turnTimeoutMs - elapsed;
                if (remaining <=0)
                {
                    Console.WriteLine($"\n{msgTime} {msgPlayerTurn} {currentPlayer} {msgLose}");
                    Console.WriteLine("\n" + msgEnd);
                    Console.WriteLine(msgPressEnter);
                    Console.ReadLine();
                    return;
                }

                Console.Write("> ");
                string? word = readLineWithTimeout(remaining);

                if (word == null)
                {
                    Console.WriteLine($"\n{msgTime} {msgPlayerTurn} {currentPlayer} {msgLose}");
                    Console.WriteLine("\n" + msgEnd);
                    Console.WriteLine(msgPressEnter);
                    Console.ReadLine();
                    return;
                }

                word = word.ToLower().Trim();

                if (string.IsNullOrWhiteSpace(word))
                {
                    Console.WriteLine(msgEmpty);
                    continue;
                }

                if (!languageCheck(word, isRussian))
                {
                    Console.WriteLine(msgWrongAlphabet);
                    continue;
                }

                if (usedWords.Contains(word))
                {
                    Console.WriteLine(msgUsed);
                    continue;
                }

                if (!canMakeWord(word, baseWord))
                {
                    Console.WriteLine(msgInvalid);
                    continue;
                }

                usedWords.Add(word);
                currentPlayer = currentPlayer ==1 ?2 :1;
                turnCompleted = true;
            }
        }
    }

    //Можно ли составить слово из исходного
    static bool canMakeWord(string word, string baseWord)
    {
        var baseLetters = baseWord
            .GroupBy(c => c)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (char c in word)
        {
            if (!baseLetters.TryGetValue(c, out int count) || count <=0)
                return false;
            baseLetters[c] = count -1;
        }

        return true;
    }

    //Чтение строки
    static string? readLineWithTimeout(int timeoutMs)
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
                    if (sb.Length >0)
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

        return null;
    }
    
    //Проверка языка
    static bool languageCheck(string s, bool isRussian)
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
