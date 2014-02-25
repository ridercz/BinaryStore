using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Altairis.BinaryStore;

namespace ConsoleSample {

    internal class Program {
        private const string PATH_BYTE_ARRAY = @"C:\Windows\Web\Wallpaper\Theme1";
        private const string PATH_OUTPUT = @"D:\Downloads\BinaryStoreTest\Output";
        private const string PATH_STREAMS = @"C:\Windows\Web\Wallpaper\Theme2";
        private static List<string> byteArrayNameList = new List<string>();
        private static List<string> streamNameList = new List<string>();

        private static void Main(string[] args) {
            SaveByteArrays();
            SaveStreams();
            LoadByteArrays();
            LoadStreams();
            DeleteAll();

            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
        }

        private static void DeleteAll() {
            Console.WriteLine("Deleting all previously stored data from {0}:", StoreManager.DefaultProvider.Name);
            var nameList = byteArrayNameList.Union(streamNameList);
            foreach (var name in nameList) {
                Console.Write("Deleting {0}...", name);
                var result = StoreManager.Delete(name);
                Console.WriteLine(result ? "OK" : "Failed");
            }
            Console.WriteLine();
        }

        private static void LoadByteArrays() {
            Console.WriteLine("Retrieving data from {0} to {1} using byte arrays:", StoreManager.DefaultProvider.Name, PATH_OUTPUT);
            foreach (var name in byteArrayNameList) {
                var fileName = Path.Combine(PATH_OUTPUT, Path.GetFileName(name));
                Console.Write("Loading {0}...", name);

                byte[] data;
                string contentType;
                var result = StoreManager.Load(name, out data, out contentType);

                if (!result) {
                    Console.WriteLine("Failed!");
                    continue;
                }

                File.WriteAllBytes(fileName, data);
                Console.WriteLine("OK, {0} bytes of {1}", data.Length, contentType);
            }
            Console.WriteLine();
        }

        private static void LoadStreams() {
            Console.WriteLine("Retrieving data from {0} to {1} using streams:", StoreManager.DefaultProvider.Name, PATH_OUTPUT);
            foreach (var name in streamNameList) {
                var fileName = Path.Combine(PATH_OUTPUT, Path.GetFileName(name));
                Console.Write("Loading {0}...", name);

                Stream stream;
                string contentType;
                var result = StoreManager.Load(name, out stream, out contentType);

                if (!result) {
                    Console.WriteLine("Failed!");
                    continue;
                }

                var length = stream.Length;
                using (var f = File.Create(fileName)) {
                    stream.CopyTo(f);
                    stream.Close();
                }

                Console.WriteLine("OK, {0} bytes of {1}", length, contentType);
            }
            Console.WriteLine();
        }
        
        private static void SaveByteArrays() {
            Console.WriteLine("Storing all .jpg files from {0} to {1} using byte arrays:", PATH_BYTE_ARRAY, StoreManager.DefaultProvider.Name);
            foreach (var fileName in Directory.GetFiles(PATH_BYTE_ARRAY, "*.jpg")) {
                var name = "ByteArray/" + Path.GetFileName(fileName);
                var data = File.ReadAllBytes(fileName);
                Console.Write("Saving {0}...", name);

                StoreManager.Save(name, data, "image/jpeg");
                Console.WriteLine("OK");

                byteArrayNameList.Add(name);
            }
            Console.WriteLine();
        }

        private static void SaveStreams() {
            Console.WriteLine("Storing all .jpg files from {0} to {1} using streams:", PATH_STREAMS, StoreManager.DefaultProvider.Name);
            foreach (var fileName in Directory.GetFiles(PATH_STREAMS, "*.jpg")) {
                var name = "Stream/" + Path.GetFileName(fileName);
                Console.Write("Saving {0}...", name);

                using (var f = File.OpenRead(fileName)) {
                    StoreManager.Save(name, f, "image/jpeg");
                }
                Console.WriteLine("OK");

                streamNameList.Add(name);
            }
            Console.WriteLine();
        }
    }
}