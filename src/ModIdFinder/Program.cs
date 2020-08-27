﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LangPack.Core;
using ModIdFinder.Properties;
using Serilog;

namespace ModIdFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            
        }
        public static async Task<IEnumerable<Mod>> DownloadModAsync(IEnumerable<Mod> mods)
        {
            var tasks = mods.Select(async mod =>
            {
                using var httpClient = new HttpClient();
                var bytes = await httpClient.GetByteArrayAsync(mod.DownloadUrl);
                var oldPath = Path.GetTempFileName();
                var newPath = Path.ChangeExtension(oldPath, Path.GetExtension(mod.DownloadUrl.ToString()));
                File.Move(oldPath, newPath);
                await File.WriteAllBytesAsync(newPath, bytes);
                mod.Path = newPath;
                return mod;
            });
            var result = await Task.WhenAll(tasks);
            return result.ToList();
        }
        public static async Task<IEnumerable<Mod>> GetModIdAsync(IEnumerable<Mod> mods)
        {
            var cfrPath = Path.Combine(Directory.GetCurrentDirectory(), "cfr.jar");
            Log.Information("开始反编译.");
            await File.WriteAllBytesAsync(cfrPath, Resources.cfr_0_150);
            Log.Information("释放了cfr到当前目录.");
            var semaphore = new SemaphoreSlim(10);
            var regex = new Regex("(?<=modid=\").*?(?=\")");
            var tasks = new List<Task<Mod>>();
            foreach (var mod in mods)
            {
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await semaphore.WaitAsync();
                        Log.Information($"开始对 {mod.Name} 进行反编译以获得modid,当前还有{semaphore.CurrentCount}个信号量.");
                        var process = new Process
                        {
                            StartInfo =
                            {
                                FileName = "java",
                                Arguments = $"-jar ./cfr.jar {mod.Path}",
                                RedirectStandardOutput = true,
                                UseShellExecute = false
                            }
                        };
                        process.Start();
                        var modid = String.Empty;
                        while (!process.StandardOutput.EndOfStream)
                        {
                            var line = process.StandardOutput.ReadLine();
                            if (!String.IsNullOrEmpty(line))
                            {
                                if (regex.IsMatch(line))
                                {
                                    modid = regex.Match(line).Value;
                                    break;
                                }
                            }
                        }
                        process.Kill();
                        Log.Information($"找到了modid: {modid}");
                        mod.ModId = modid;

                    }
                    finally
                    {
                        semaphore.Release();
                    }
                    return mod;
                });
                tasks.Add(task);

            }
            var result = await Task.WhenAll(tasks);

            return result.ToList();
        }
    }
}
