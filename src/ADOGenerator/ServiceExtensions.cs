﻿using ADOGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADOGenerator
{
    public static class ServiceExtensions
    {
        public static readonly object objLock = new object();

        public static string ReadJsonFile(this Project file, string filePath)
        {
            string fileContents = string.Empty;

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    fileContents = sr.ReadToEnd();
                }
            }

            return fileContents;
        }

        public static string ErrorId(this string str)
        {
            str = str + "_Errors";
            return str;
        }

        public static void AddMessage(this string id, string message)
        {
            lock (objLock)
            {
                // Create Log floder
                if (!Directory.Exists("Log"))
                {
                    Directory.CreateDirectory("Log");
                }
                string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Log");
                string fileName = $"{DateTime.Now.ToString("yyyy-MM-dd")}-{id}.txt";
                if (id.EndsWith("_Errors"))
                {
                    // Create Log file
                    if (!File.Exists(Path.Combine(logFilePath, "Errors", fileName)))
                    {
                        File.Create(Path.Combine(logFilePath, "Errors", fileName)).Dispose();
                    }
                    File.AppendAllLines(Path.Combine(logFilePath, "Errors", fileName), new string[] { message });
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(message);
                    Console.ResetColor();
                }
                else
                {
                    if (!File.Exists(Path.Combine(logFilePath, fileName)))
                    {
                        File.Create(Path.Combine(logFilePath, fileName)).Dispose();
                    }
                    File.AppendAllLines(Path.Combine(logFilePath, fileName), new string[] { message });
                    // Create Log file
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(message);
                    Console.ResetColor();
                }
            }
        }

    }
}
