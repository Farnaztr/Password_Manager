using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Password_Manager
{
    internal class Program
    {
        static byte[] Generate(string masterpass, byte[] salt, int iterations = 10000, int keySize = 32)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(masterpass, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(keySize);
            }
        }

        static byte[] SaltGeneration(int size = 16)
        {
            byte[] salt = new byte[size];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
            }
            return salt;
        }

        static byte[] Encrypt(string plaintext, byte[] key, out byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                iv = aes.IV;

                using (var memstr = new MemoryStream())
                {
                    using (var cs = new CryptoStream(memstr, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plaintext);
                    }
                    return memstr.ToArray();
                }
            }
        }

        static string Decrypt(byte[] cipherText, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var ms = new MemoryStream(cipherText))
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        class PassRecord
        {
            public string Title { get; set; }
            public string username { get; set; }
            public string password { get; set; }
        }

        static string VaultPath = "vault.dat";

        class FileWrapper
        {
            public string SaltBase64 { get; set; }
            public string IVBase64 { get; set; }
            public string CipherTextBase64 { get; set; }
        }

        static List<PassRecord> LoadRecordsFromFile(string masterPass)
        {
            if (!File.Exists(VaultPath))
                return new List<PassRecord>();

            var wrapper = JsonSerializer.Deserialize<FileWrapper>(File.ReadAllText(VaultPath));
            byte[] salt = Convert.FromBase64String(wrapper.SaltBase64);
            byte[] iv = Convert.FromBase64String(wrapper.IVBase64);
            byte[] cipher = Convert.FromBase64String(wrapper.CipherTextBase64);

            byte[] key = Generate(masterPass, salt);
            string json = Decrypt(cipher, key, iv);

            return JsonSerializer.Deserialize<List<PassRecord>>(json) ?? new List<PassRecord>();
        }

        static void SaveRecordsToFile(List<PassRecord> records, string masterPass, byte[] salt = null)
        {
            if (salt == null)
                salt = SaltGeneration();

            byte[] key = Generate(masterPass, salt);
            string json = JsonSerializer.Serialize(records);
            byte[] encrypted = Encrypt(json, key, out byte[] iv);

            var wrapper = new FileWrapper
            {
                SaltBase64 = Convert.ToBase64String(salt),
                IVBase64 = Convert.ToBase64String(iv),
                CipherTextBase64 = Convert.ToBase64String(encrypted)
            };

            File.WriteAllText(VaultPath, JsonSerializer.Serialize(wrapper));
        }

        static void AddRecord(List<PassRecord> records)
        {
            Console.Write("Enter Title: ");
            string title = Console.ReadLine();
            Console.Write("Enter Username/Email: ");
            string username = Console.ReadLine();
            Console.Write("Enter Password: ");
            string password = Console.ReadLine();

            records.Add(new PassRecord { Title = title, username = username, password = password });
            Console.WriteLine("Password added!");
        }

        static void ListRecords(List<PassRecord> records)
        {
            Console.WriteLine("Saved Records:");
            foreach (var r in records)
            {
                Console.WriteLine($"- {r.Title} | {r.username} | Password: {new string('*', r.password.Length)}");
            }
        }

        static void SearchRecord(List<PassRecord> records)
        {
            Console.Write("Enter Title to search: ");
            string search = Console.ReadLine();
            var results = records.FindAll(r => r.Title.ToLower().Contains(search.ToLower()));
            if (results.Count == 0) Console.WriteLine("No records found.");
            else
            {
                foreach (var r in results)
                {
                    Console.WriteLine($"- {r.Title} | {r.username} | Password: {new string('*', r.password.Length)}");
                }
            }
        }

        static void DeleteRecord(List<PassRecord> records)
        {
            Console.Write("Enter Title to delete: ");
            string title = Console.ReadLine();
            int removed = records.RemoveAll(r => r.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
            Console.WriteLine(removed > 0 ? "Record deleted!" : "No matching record found.");
        }

        static string GenerateRandomPassword(int length = 12)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+-=[]{}";
            var sb = new StringBuilder();
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] buf = new byte[4];
                for (int i = 0; i < length; i++)
                {
                    rng.GetBytes(buf);
                    int idx = (int)BitConverter.ToUInt32(buf, 0) % chars.Length;
                    sb.Append(chars[idx]);
                }
            }
            return sb.ToString();
        }

        static void Main(string[] args)
        {
            int attempts = 3;
            bool success = false;
            List<PassRecord> records = null;
            string password ="";

            while (attempts > 0 && !success)
            {
                Console.Write("Enter your Master Password: ");
                password = Console.ReadLine();

                try
                {
                    if (File.Exists(VaultPath))
                    {
                        records = LoadRecordsFromFile(password);
                        Console.WriteLine($"Vault loaded with {records.Count} record(s).");
                    }
                    else
                    {
                        Console.WriteLine("No vault found. Creating a new one.");
                        records = new List<PassRecord>();
                    }
                    success = true;
                }
                catch
                {
                    attempts--;
                    Console.WriteLine($"Incorrect password! You have {attempts} attempt(s) left.");
                }
            }

            if (!success)
            {
                Console.WriteLine("You have entered wrong password 3 times. Exiting...");
                return;
            }

            byte[] existingSalt = File.Exists(VaultPath) ? Convert.FromBase64String(JsonSerializer.Deserialize<FileWrapper>(File.ReadAllText(VaultPath)).SaltBase64) : null;

            bool running = true;
            while (running)
            {
                Console.WriteLine("\nMenu:");
                Console.WriteLine("1 - Add Record");
                Console.WriteLine("2 - List Records");
                Console.WriteLine("3 - Search Record");
                Console.WriteLine("4 - Delete Record");
                Console.WriteLine("5 - Generate Random Password");
                Console.WriteLine("0 - Exit");
                Console.Write("Choice: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1": AddRecord(records); break;
                    case "2": ListRecords(records); break;
                    case "3": SearchRecord(records); break;
                    case "4": DeleteRecord(records); break;
                    case "5":
                        Console.Write("Enter length of password: ");
                        if (int.TryParse(Console.ReadLine(), out int len))
                        {
                            string pw = GenerateRandomPassword(len);
                            Console.WriteLine($"Generated Password: {pw}");
                        }
                        else Console.WriteLine("Invalid length.");
                        break;
                    case "0": running = false; break;
                    default: Console.WriteLine("Invalid choice."); break;
                }
            }

            SaveRecordsToFile(records, password, existingSalt);
            Console.WriteLine("Vault saved successfully.");
        }
    }
}
