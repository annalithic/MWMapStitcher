using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using ImageMagick;
using ImageMagick.Formats;

namespace MWMapStitcher {
    internal class Program {
        struct Coord {
            public int x;
            public int y;
        }

        struct Extents {
            public int minX;
            public int minY;
            public int maxX;
            public int maxY;
        }

        static void Main(string[] args) {
            if(args.Length != 2) StitchExteriorMap(@"E:\maps\6", 128);
            else {
                StitchExteriorMap(args[0], int.Parse(args[1]));
            }
        }

        static Coord GetCoords(string path) {
            int startCoord = path.LastIndexOf('(');
            int commaCoord = path.LastIndexOf(',');
            int endCoord = path.LastIndexOf(')');
            int x = int.Parse(path.Substring(startCoord + 1, commaCoord - startCoord - 1));
            int y = int.Parse(path.Substring(commaCoord + 1, endCoord - commaCoord - 1));
            return new Coord() { x = x, y = y };
        }

        static Extents GetExtents(IEnumerable<string> paths) {
            int minX = int.MaxValue; int maxX = int.MinValue;
            int minY = int.MaxValue; int maxY = int.MinValue;

            foreach (string path in paths) {
                Coord coord = GetCoords(path);
                if (coord.x > maxX) maxX = coord.x; if (coord.x < minX) minX = coord.x;
                if (coord.y > maxY) maxY = coord.y; if (coord.y < minY) minY = coord.y;
            }
            return new Extents() { minX = minX, maxX = maxX, minY = minY, maxY = maxY };
        }

        static void StitchExteriorMap(string folder, int tileSize) {
            List<string> paths = new List<string>();
            foreach(string s in Directory.EnumerateFiles(folder, "*")) { paths.Add(s); }
            Extents extents = GetExtents(paths);
            Console.WriteLine($"Coords: {extents.minX},{extents.minY} to {extents.maxX},{extents.maxY}");
            int xCount = extents.maxX - extents.minX + 1; int yCount = extents.maxY - extents.minY + 1;
            Console.WriteLine($"Image Resolution: {xCount * tileSize}x{yCount * tileSize}");

            MagickImageCollection montage = new MagickImageCollection();
            for (int i = 0; i < xCount * yCount; i++) montage.Add(new MagickImage(MagickColors.Black, 1, 1));

            int progress = 0;

            //MagickImage map = new MagickImage(MagickColors.Black, (maxX - minX + 1) * tileSize, (maxY - minY + 1) * tileSize);
            for(int i = 0; i < paths.Count; i++) {
                string path = paths[i];
                Coord coord = GetCoords(path);
                int xOffset = coord.x - extents.minX;
                int yOffset = extents.maxY - coord.y;
                var image = new MagickImage(path);
                if (image.Width != tileSize) image.Resize(tileSize, tileSize);
                montage[xOffset + yOffset * xCount] = image;
                int newProgress = (int)(((float)i) / paths.Count * 100);
                if (newProgress > progress) {
                    progress = newProgress;
                    Console.WriteLine($"{progress}% {Path.GetFileName(path)}");
                }
            }
            MontageSettings montageSettings = new MontageSettings() { Geometry = new MagickGeometry(tileSize), TileGeometry = new MagickGeometry(xCount, yCount) };
            var map = montage.Montage(montageSettings);
            WebPWriteDefines write = new WebPWriteDefines() { Lossless = true, Method = 0 };
            map.Quality = 50;


            map.Write($"openmwmap_{(long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds}.webp", write);

        }
    }
}
