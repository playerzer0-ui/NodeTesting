using System.Collections.Generic;
using System.IO;

namespace NodeTesting.models
{
    /// <summary>
    /// Reads a CSV map file into a grid of numbers. Shared by every map type.
    /// </summary>
    internal static class CsvGrid
    {
        /// <summary>
        /// Loads a CSV as <c>[row, column]</c>. Cells that aren't numbers become 0.
        /// The file is looked up in the working directory, then in <c>Content/</c>.
        /// </summary>
        public static int[,] Load(string csvPath)
        {
            string[] lines = File.ReadAllLines(FindFile(csvPath));
            int height = lines.Length;
            int width = lines[0].Split(',').Length;

            int[,] data = new int[height, width];

            for (int row = 0; row < height; row++)
            {
                string[] values = lines[row].Split(',');
                for (int col = 0; col < width; col++)
                {
                    if (col < values.Length && int.TryParse(values[col].Trim(), out int value))
                        data[row, col] = value;
                }
            }

            return data;
        }

        private static string FindFile(string fileName)
        {
            string cwd = Directory.GetCurrentDirectory();
            var paths = new List<string>
            {
                Path.Combine(cwd, fileName),
                Path.Combine(cwd, "Content", fileName),
            };

            // When run from bin/<config>/<framework>, also try the project's Content folder
            DirectoryInfo projectDir = Directory.GetParent(cwd)?.Parent?.Parent;
            if (projectDir != null)
                paths.Add(Path.Combine(projectDir.FullName, "Content", fileName));

            paths.Add(fileName);

            foreach (string path in paths)
            {
                if (File.Exists(path))
                    return path;
            }

            throw new FileNotFoundException($"Could not find file '{fileName}'");
        }
    }
}
