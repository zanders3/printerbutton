using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PrinterButton
{
    public class Poem
    {
        void ParseConfig(string line, string match, ref float val)
        {
            if (line.StartsWith(match))
            {
                float newVal;
                if (float.TryParse(line.Split(' ').Last(), out newVal))
                    val = newVal;
            }
        }

        public Poem()
        {
            if (!File.Exists("poem.txt"))
            {
                MessageBox.Show("Failed to find poem.txt in the same folder so can't generate poems :(");
                return;
            }

            try
            {
                List<string> currentList = new List<string>();
                string headerLine = null;
                using (StreamReader reader = new StreamReader("poem.txt"))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        ParseConfig(line, "WIDTH", ref PageWidth);
                        ParseConfig(line, "FONTSIZE", ref FontSize);
                        ParseConfig(line, "HEADER", ref HeaderHeight);
                        ParseConfig(line, "WEBSITE", ref WebsiteHeight);
                        if (line.StartsWith("FONTFAMILY "))
                            FontFamily = line.Substring("FONTFAMILY ".Length);
                        if (line.StartsWith("WORDS") || line.StartsWith("PATTERNS") || line.StartsWith("LINES"))
                        {
                            if (headerLine != null)
                            {
                                if (headerLine.StartsWith("WORDS"))
                                    words.Add(currentList);
                                else if (headerLine.StartsWith("PATTERNS"))
                                    patterns = currentList;
                                else if (headerLine.StartsWith("LINES"))
                                    lines = currentList;
                            }

                            currentList = new List<string>();
                            headerLine = line;
                        }
                        else if (!string.IsNullOrEmpty(line))
                            currentList.Add(line);
                    }

                    if (headerLine != null)
                    {
                        if (headerLine.StartsWith("WORDS"))
                            words.Add(currentList);
                        else if (headerLine.StartsWith("PATTERNS"))
                            patterns = currentList;
                        else if (headerLine.StartsWith("LINES"))
                            lines = currentList;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to read poem.txt :(\n" + e.Message + "\n" + e.StackTrace);
                return;
            }

            if (words.Count == 0 || words.Any(word => word.Count == 0))
                MessageBox.Show("There is an empty word list in the poem.txt");
            if (patterns.Count == 0)
                MessageBox.Show("The patterns list is empty in the poem.txt");
            if (lines.Count == 0)
                MessageBox.Show("The lines list is empty in the poem.txt");
        }

        List<List<string>> words = new List<List<string>>();
        List<string> patterns = new List<string>(), lines = new List<string>();
        public float PageWidth = 120f, HeaderHeight = 10f, FontSize = 10f, WebsiteHeight = 10f;
        public string FontFamily = "Times New Roman";

        static string Pick(Random random, List<string> array)
        {
            return array.Count == 0 ? string.Empty : array[random.Next(array.Count - 1)];
        }

        public string GeneratePoem(Random random, out string selectedImage)
        {
            selectedImage = null;
            int numLines;
            if (!int.TryParse(Pick(random, lines), out numLines))
                numLines = 3;

            StringBuilder finalPoem = new StringBuilder();
            for (int line = 0; line<numLines; line++)
            {
                foreach (char c in Pick(random, patterns))
                {
                    if (c >= '0' && c <= '9')
                    {
                        string word = Pick(random, words[System.Math.Min(c - '1', words.Count - 1)]);
                        if (selectedImage == null && File.Exists(word + ".png"))
                        {
                            selectedImage = word + ".png";
                        }
                        finalPoem.Append(word);
                    }
                    else
                        finalPoem.Append(c);
                }
                finalPoem.AppendLine();
            }

            return finalPoem.ToString();
        }
    }
}
