using System;
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
            StitchExteriorMap(@"F:\Anna\Documents\My Games\OpenMW\maps", 512);
        }

        static Coord GetCoords(string path) {
            int startCoord = path.LastIndexOf('(');
            int commaCoord = path.LastIndexOf(',');
            int endCoord = path.LastIndexOf(')');
            int x = int.Parse(path.Substring(startCoord + 1, commaCoord - startCoord - 1));
            int y = int.Parse(path.Substring(commaCoord + 1, endCoord - commaCoord - 1));
            return new Coord() { x = x, y = y };
        }

        static Extents GetExtents(string folder) {
            int minX = int.MaxValue; int maxX = int.MinValue;
            int minY = int.MaxValue; int maxY = int.MinValue;

            foreach (string path in Directory.EnumerateFiles(folder, "*.bmp")) {
                Coord coord = GetCoords(path);
                if (coord.x > maxX) maxX = coord.x; if (coord.x < minX) minX = coord.x;
                if (coord.y > maxY) maxY = coord.y; if (coord.y < minY) minY = coord.y;
            }
            return new Extents() { minX = minX, maxX = maxX, minY = minY, maxY = maxY };
        }

        static void StitchExteriorMap(string folder, int tileSize) {
            StitchExteriorMap(folder, tileSize, GetExtents(folder));
        }

        static void StitchExteriorMap(string folder, int tileSize, Extents extents) {
            Console.WriteLine($"Coords: {extents.minX},{extents.minY} to {extents.maxX},{extents.maxY}");
            int xCount = extents.maxX - extents.minX + 1; int yCount = extents.maxY - extents.minY + 1;
            Console.WriteLine($"Image Resolution: {xCount * tileSize}x{yCount * tileSize}");

            MagickImageCollection montage = new MagickImageCollection();
            for (int i = 0; i < xCount * yCount; i++) montage.Add(new MagickImage(MagickColors.Black, 1, 1));


            //MagickImage map = new MagickImage(MagickColors.Black, (maxX - minX + 1) * tileSize, (maxY - minY + 1) * tileSize);

            foreach (string path in Directory.EnumerateFiles(folder, "*.bmp")) {
                Coord coord = GetCoords(path);
                int xOffset = coord.x - extents.minX;
                int yOffset = extents.maxY - coord.y;
                var image = new MagickImage(path);
                if (image.Width != tileSize) image.Resize(tileSize, tileSize);
                montage[xOffset + yOffset * xCount] = image;
                Console.WriteLine(path);
            }

            MontageSettings montageSettings = new MontageSettings() { Geometry = new MagickGeometry(tileSize), TileGeometry = new MagickGeometry(xCount, yCount) };
            var map = montage.Montage(montageSettings);
            WebPWriteDefines write = new WebPWriteDefines() { Lossless = true, Method = 0 };
            map.Quality = 20;

            int imageCount = 0;
            while (File.Exists($"openmwmap_{imageCount}.webp")) imageCount++;
            map.Write($"openmwmap_{imageCount}.webp", write);

        }
    }
}
