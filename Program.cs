// See https://aka.ms/new-console-template for more information
using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using GBX.NET.LZO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Net;

internal class ModSigns
{

    private static void Main(string[] args)
    {
        ILoggerFactory loggerFactory =
            LoggerFactory.Create(builder =>
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                }).SetMinimumLevel(LogLevel.Trace));

        ILogger<ModSigns> logger = loggerFactory.CreateLogger<ModSigns>();
        // using (logger.BeginScope("[scope is enabled]"))
        // {
        //     logger.LogInformation("Hello World!");
        //     logger.LogInformation("Logs contain timestamp and log level.");
        //     logger.LogInformation("Each log message is fit in a single line.");
        // }

        logger.LogInformation(">> Find / Remove Signs & Skins <<");

        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            var exeName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            logger.LogInformation($"usage: {exeName}.exe MAP_FILE");
            logger.LogInformation($"Pass map file as argument");
            logger.LogInformation($"Output file will be written to MAP_FILE_signsrem.Map.Gbx");
            // load local GBX.NET dependency version
            var version = System.Reflection.Assembly.GetAssembly(typeof(GBX.NET.GameBox)).GetName().Version;
            logger.LogInformation($"\n\n  GBX.NET version: {version}");

            logger.LogInformation($"-------------\n\n\nPress any key to exit.");
            Console.ReadKey();
            logger.LogInformation("Cya, friend o/.");
            return;
        }

        var mapFile = args[0];

        logger.LogInformation($"Map: {mapFile}");

        var mapFileName = Path.GetFileName(mapFile);
        var mapFileNoExt = Path.GetFileNameWithoutExtension(mapFileName);
        mapFileNoExt = Path.GetFileNameWithoutExtension(mapFileNoExt);


        // GBX.NET.Lzo.SetLzo(typeof(GBX.NET.LZO.MiniLZO));
        Gbx.LZO = new Lzo();

        CGameCtnChallenge map = GameBox.ParseNode<CGameCtnChallenge>(mapFile);

        logger.LogInformation($"Parsed map: {map.MapName} by {map.AuthorNickname} ({map.AuthorLogin})\n");

        Console.CursorVisible = true;
        while (RunMainLoop(logger, map, mapFile)) {
            System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(250));
        }


        // logger.LogInformation("Press any key to exit.");
        // Console.ReadKey();
        logger.LogInformation("Cya, friend o/.");
        System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(500));
        return;
    }

    private static bool RunMainLoop(ILogger<ModSigns> logger, CGameCtnChallenge map, string mapFile)
    {
        Console.WriteLine(">> Please select an option <<\n");
        Console.WriteLine("  1. List All Used Skins");
        Console.WriteLine("  2. Find Blocks/Items with Skin (match FileName or URL, Contains)");
        Console.WriteLine("  3. Remove skins matching (FileName Contains)");
        Console.WriteLine("  4. Remove skins matching (URL Contains)");
        Console.WriteLine("  5. Remove skins matching (FileName Exact Match)");
        Console.WriteLine("  6. Remove skins matching (URL Exact Match)");
        // Console.WriteLine("  7. Replace skin matching (URL Exact Match)");
        Console.WriteLine("  8. Save Map (and resume)");
        Console.WriteLine("  9. Save Map (and exit)");
        Console.WriteLine("  0/q. Exit without saving");
        Console.WriteLine("\nPlease make a choice.");
        Console.Write("> ");
        Console.CursorVisible = true;
        var read = Console.ReadLine();
        if (read == "q" || read == "0") {
            logger.LogInformation("Exiting...");
            return false;
        }
        int option = -1;
        if (!int.TryParse(read, out option) || option < 1 || option > 9) {
            logger.LogWarning($"`{read}` is not a valid choice.\nPress enter to retry.");
            Console.ReadLine();
            return true;
        }
        switch (option) {
            case 1:
                PrintSignsUnique(logger, map);
                return true;
            case 2:
                PrintSigns(logger, map, GetPattern(logger), false);
                return true;
            case 3:
                RemoveSigns(logger, map, GetPattern(logger), false, false);
                return true;
            case 4:
                RemoveSigns(logger, map, GetPattern(logger), false, true);
                return true;
            case 5:
                RemoveSigns(logger, map, GetPattern(logger), true, false);
                return true;
            case 6:
                RemoveSigns(logger, map, GetPattern(logger), true, true);
                return true;
            // case 7:
            //     ReplaceSigns(logger, map, GetPattern(logger, "Existing URL (Exact):"), GetPattern(logger, "New URL (Exact):"));
            //     return true;
            case 8:
                SaveMap(logger, map, mapFile);
                return true;
            case 9:
                SaveMap(logger, map, mapFile);
                logger.LogInformation("Exiting...");
                return false;
            case 0:
                logger.LogInformation("Exiting...");
                return false;
        }
        return false;
    }

    private static void SaveMap(ILogger<ModSigns> logger, CGameCtnChallenge map, string mapFile)
    {
        var mapFileNoExtension = mapFile.Substring(0, mapFile.Length - 8);
        if (!map.MapName.EndsWith("_signsrem")) {
            map.MapName += "$z_signsrem";
        }
        var outFile = $"{mapFileNoExtension}_signsrem.Map.Gbx";
        map.Save(outFile);
        logger.LogInformation($"Wrote Output Map File: {outFile}");
    }

    private static string GetPattern(ILogger<ModSigns> logger, string? prompt = null) {
        if (prompt == null) {
            prompt = "Enter search pattern:\n> ";
        } else {
            prompt += "\n> ";
        }
        Console.Write(prompt);
        var pattern = Console.ReadLine();
        logger.LogInformation($"Search term: {pattern}");
        if (pattern == null) return "";
        return pattern;
    }

    private static void PrintSignsUnique(ILogger<ModSigns> logger, CGameCtnChallenge map) {
        Dictionary<string, int> signs = new();
        var blocks = map.Blocks?.ToArray();
        var items = map.AnchoredObjects?.ToArray();
        string k;

        if (blocks != null) {
            for (int i = 0; i < blocks.Length; i++) {
                var block = blocks[i];
                if (block.Skin == null) continue;
                k = $"{block.Skin.PackDesc.FilePath} @ URL: {block.Skin.PackDesc.LocatorUrl}";
                if (!signs.ContainsKey(k)) signs.Add(k, 0);
                signs[k] += 1;
                if (block.Skin.ForegroundPackDesc == null) continue;
                k = $"{block.Skin.ForegroundPackDesc.FilePath} @ URL: {block.Skin.ForegroundPackDesc.LocatorUrl}";
                if (!signs.ContainsKey(k)) signs.Add(k, 0);
                signs[k] += 1;
            }
        }
        if (items != null) {
            for (int i = 0; i < items.Length; i++) {
                var item = items[i];
                if (item.ForegroundPackDesc != null) {
                    k = $"{item.ForegroundPackDesc.FilePath} @ URL: {item.ForegroundPackDesc.LocatorUrl}";
                    if (!signs.ContainsKey(k)) signs.Add(k, 0);
                    signs[k] += 1;
                }
                if (item.PackDesc != null) {
                    k = $"{item.PackDesc.FilePath} @ URL: {item.PackDesc.LocatorUrl}";
                    if (!signs.ContainsKey(k)) signs.Add(k, 0);
                    signs[k] += 1;
                }
            }
        }

        logger.LogInformation($"\n>> Found Skins ({signs.Count} total):");
        foreach (var key in signs.Keys) {
            var count = signs[key];
            Console.WriteLine($"{count} | {key}");
        }
        Console.WriteLine("");
    }

    private static void PrintSigns(ILogger<ModSigns> logger, CGameCtnChallenge map, string pattern = "", bool exactMatch = false) {
        var blocks = map.Blocks?.ToArray();
        var items = map.AnchoredObjects?.ToArray();

        if (blocks != null) {
            for (int i = 0; i < blocks.Length; i++) {
                var block = blocks[i];
                if (block.Skin == null) continue;
                var matchedFG = PackDescMatches(block.Skin.ForegroundPackDesc?.FilePath, pattern, exactMatch) || PackDescMatches(block.Skin.ForegroundPackDesc?.LocatorUrl, pattern, exactMatch);
                var matchedBG = PackDescMatches(block.Skin.PackDesc.FilePath, pattern, exactMatch) || PackDescMatches(block.Skin.PackDesc.LocatorUrl, pattern, exactMatch);
                if (matchedBG || matchedFG) {
                    if (block.IsFree) {
                        Console.WriteLine($"[B] {block.Name}: at {block.AbsolutePositionInMap} (rotations: {block.PitchYawRoll})");
                    } else {
                        Console.WriteLine($"[B] {block.Name}: at {block.Coord} (direction: {block.Direction})");
                    }
                    if (matchedFG) {
                        Console.WriteLine($"  FG: {block.Skin.ForegroundPackDesc?.FilePath} / URL: {block.Skin.ForegroundPackDesc?.LocatorUrl}");
                    }
                    if (matchedBG) {
                        Console.WriteLine($"  BG: {block.Skin.PackDesc.FilePath} / URL: {block.Skin.PackDesc.LocatorUrl}");
                    }
                    Console.WriteLine("");
                }
            }
        }

        if (items != null) {
            for (int i = 0; i < items.Length; i++) {
                var item = items[i];
                if (item.ForegroundPackDesc == null && item.PackDesc == null) continue;
                var matchedFG = PackDescMatches(item.ForegroundPackDesc?.FilePath, pattern, exactMatch) || PackDescMatches(item.ForegroundPackDesc?.LocatorUrl, pattern, exactMatch);
                var matchedBG = PackDescMatches(item.PackDesc?.FilePath, pattern, exactMatch) || PackDescMatches(item.PackDesc?.LocatorUrl, pattern, exactMatch);
                if (matchedBG || matchedFG) {
                    Console.WriteLine($"[I] {item.ItemModel.Id}: at {item.AbsolutePositionInMap} (rotations: {item.PitchYawRoll})");
                    if (matchedFG) {
                        Console.WriteLine($"  FG: {item.ForegroundPackDesc?.FilePath} / URL: {item.ForegroundPackDesc?.LocatorUrl}");
                    }
                    if (matchedBG) {
                        Console.WriteLine($"  BG: {item.PackDesc?.FilePath} / URL: {item.PackDesc?.LocatorUrl}");
                    }
                    Console.WriteLine("");
                }
            }
        }
    }

    static bool PackDescMatches(string? Source, string pattern, bool exactMatch) {
        if (Source == null) return false;
        if (pattern.Trim() == "") return true;
        if (exactMatch) return Source == pattern;
        return Source.Contains(pattern);
    }

    private static void RemoveSigns(ILogger<ModSigns> logger, CGameCtnChallenge map, string pattern, bool exactMatch = false, bool matchUrl = false)
    {
        var blocks = map.Blocks?.ToArray();
        var items = map.AnchoredObjects?.ToArray();

        if (blocks != null) {
            for (int i = 0; i < blocks.Length; i++) {
                var block = blocks[i];
                if (block.Skin == null) continue;
                var k = $"{block.Name} at {block.AbsolutePositionInMap.ToString()} with rotations {block.PitchYawRoll.ToString()}";
                if (!block.IsFree) {
                    k = $"{block.Name} at {block.Coord} with dir {block.Direction}";
                }
                var skins = $"FG: {block.Skin.ForegroundPackDesc?.FilePath} @ {block.Skin.ForegroundPackDesc?.LocatorUrl} \nBG: {block.Skin.PackDesc?.FilePath} @ {block.Skin.PackDesc?.LocatorUrl}";
                // logger.LogInformation($"Found block {k}\n{skins}\n");

                if (PackDescMatches(matchUrl ? block.Skin.ForegroundPackDesc?.LocatorUrl : block.Skin.ForegroundPackDesc?.FilePath, pattern, exactMatch)) {
                    logger.LogInformation($"Removed Skin: {(matchUrl ? block.Skin.ForegroundPackDesc?.LocatorUrl : block.Skin.ForegroundPackDesc?.FilePath)}");
                    block.Skin.ForegroundPackDesc = null;
                }
                if (PackDescMatches(matchUrl ? block.Skin.PackDesc?.LocatorUrl : block.Skin.PackDesc?.FilePath, pattern, exactMatch)) {
                    logger.LogInformation($"Removed Skin: {(matchUrl ? block.Skin.PackDesc?.LocatorUrl : block.Skin.PackDesc?.FilePath)}");
                    block.Skin = null;
                }
            }
        }

        if (items != null) {
            for (int i = 0; i < items.Length; i++) {
                var item = items[i];
                if (item.ForegroundPackDesc == null && item.PackDesc == null) continue;
                // var k = $"{item.ItemModel.Id} at {item.AbsolutePositionInMap.ToString()} with rotations {item.PitchYawRoll.ToString()}";
                // var skins = $"FG: {item.ForegroundPackDesc?.FilePath} @ {item.ForegroundPackDesc?.LocatorUrl} \nBG: {item.PackDesc?.FilePath} @ {item.PackDesc?.LocatorUrl}";

                if (PackDescMatches(matchUrl ? item.ForegroundPackDesc?.LocatorUrl : item.ForegroundPackDesc?.FilePath, pattern, exactMatch)) {
                    logger.LogInformation($"Removed Skin: {(matchUrl ? item.ForegroundPackDesc?.LocatorUrl : item.ForegroundPackDesc?.FilePath)}");
                    item.ForegroundPackDesc = null;
                }
                if (PackDescMatches(matchUrl ? item.PackDesc?.LocatorUrl : item.PackDesc?.FilePath, pattern, exactMatch)) {
                    logger.LogInformation($"Removed Skin: {(matchUrl ? item.PackDesc?.LocatorUrl : item.PackDesc?.FilePath)}");
                    item.PackDesc = null;
                }
            }
        }
    }


    static bool NeedsFixingPackDesc(string? FileName) {
        if (FileName == null) return false;
        return FileName.Contains("cdn-discordapp");
    }

    static void PrintProgressBar(int current, int total)
    {
        Console.SetCursorPosition(0, Console.CursorTop); // Reset cursor position to the start of the current line
        double percentage = (double)current / total;
        int progressBarWidth = 50; // Width of the progress bar
        int progress = (int)(percentage * progressBarWidth);

        string bar = new string('=', progress) + new string(' ', progressBarWidth - progress);
        Console.Write($"[{bar}] {current}/{total} ({percentage:P})              ");
    }
}
