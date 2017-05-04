using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MetadataExtractor;

namespace DN.InstaShare
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var standardKeywords = new List<string>()
            {
                "dldadventures",
                "diverslife",
                "scuba",
                "canon7dmarkii",
                "ikelite",
                "uw",
                "uwlife",
                "uwpics",
                "uwphotography",
                "underwater",
                "underwaterlife",
                "underwaterpics",
                "underwaterphoto",
                "underwaterphotography",
                "scubadiving",
                "scubadive",
                "scubalife",
                "padi",
                "divepix"
            };

            var ignoredKeywords = new List<string>()
            {
                "flickr"
            };

            var file = GetFilePathFromArgsOrClipboard(args);

            if (string.IsNullOrWhiteSpace(file))
            {
                ShowMessageAndWaitForUser("Must either supply a filename on the command line or have a file copied to the clipboard.");
                return;
            }

            var tags = GetExifTags(file);

            if (tags == null)
            {
                ShowMessageAndWaitForUser("No usable EXIF data.");
                return;
            }

            var title = ContentWithNewLineIfNotNull(tags.FirstOrDefault(t => t.Name == "Object Name" || t.Name == "Windows XP Title")?.Description);
            var caption = ContentWithNewLineIfNotNull(tags.FirstOrDefault(t => t.Name == "Caption/Abstract" || t.Name == "Windows XP Subject")?.Description);
            var keywords = (tags.FirstOrDefault(t => t.Name == "Keywords" || t.Name == "Windows XP Keywords")?.Description ?? "")
                .Replace(" ", "").Split(new char[] { ';' })
                .Except(ignoredKeywords)
                .Union(standardKeywords, StringComparer.InvariantCultureIgnoreCase);

            var hashtags = "#" + string.Join(" #", keywords).Trim();

            var clipText = $"{title}{caption}{hashtags}";
            System.Windows.Clipboard.SetText(clipText);

            Process.Start(file);

            //TODO: figure out how to use DataTransferManager.GetForCurrentView to share image directly..
            //possibly by using nuget: uwp-desktop

        }


        private static void ShowMessageAndWaitForUser(string message)
        {
            MessageBox.Show(message);
            //Console.WriteLine(message);
            //Console.WriteLine("Press any key to continue...");
            //Console.ReadKey();
        }

        private static IReadOnlyList<Tag> GetExifTags(string file)
        {
            const string windowsExifDirectoryName = "Exif IFD0";
            const string iptcExifDirectoryName = "IPTC";

            var directories = ImageMetadataReader.ReadMetadata(file);

            var directory = directories.FirstOrDefault(d => d.Name == windowsExifDirectoryName) 
                ?? directories.FirstOrDefault(d => d.Name == iptcExifDirectoryName);

            if (directory != null) return directory.Tags;

            //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(directories));
            return null;
        }

        private static string GetFilePathFromArgsOrClipboard(string[] args)
        {
            var clipboardFiles = System.Windows.Clipboard.GetFileDropList();
            return args?.Length > 0 ? args[0] : clipboardFiles.Count > 0 ? clipboardFiles[0] : string.Empty;
        }

        private static string ContentWithNewLineIfNotNull(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;

            return content.Trim() + Environment.NewLine;
        }
    }
}
