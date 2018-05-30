using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using clup.Options;
using JetBrains.Annotations;

namespace clup.Core
{
    internal static class ClupEngine
    {
        public static void Foo([NotNull] DeleteOptions options)
        {
            options.Validate();

            string[] extensions = options.FileExtensions.ToArray();
            string pattern = $"*.{extensions[0]}{extensions.Skip(1).Aggregate(string.Empty, (seed, value) => $"{seed} OR *.{value}")}";
            string[] files = Directory.EnumerateFiles(options.SourceDirectory, pattern, SearchOption.AllDirectories).ToArray();

            ConcurrentDictionary<string, List<string>> map = new ConcurrentDictionary<string, List<string>>();
            Parallel.ForEach(files, file =>
            {
                using (MD5 md5 = MD5.Create())
                using (FileStream stream = File.OpenRead(file))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    string hex = BitConverter.ToString(hash);
                    map.AddOrUpdate(hex, new List<string> { file }, (_, list) =>
                    {
                        list.Add(file);
                        return list;
                    });
                }
            });

            Parallel.ForEach(map.Values, duplicates =>
            {
                if (duplicates.Count < 2) return;
                (long ticks, string path) = (File.GetCreationTimeUtc(duplicates[0]).Ticks, duplicates[0]);
                foreach (string duplicate in duplicates.Skip(1))
                {
                    long creation = File.GetCreationTimeUtc(duplicate).Ticks;
                    if (creation >= ticks) File.Delete(duplicate);
                    else
                    {
                        File.Delete(path);
                        (ticks, path) = (creation, duplicate);
                    }
                }
            });
        }
    }
}
