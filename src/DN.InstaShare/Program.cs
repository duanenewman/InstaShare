using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MetadataExtractor;
using Newtonsoft.Json;

namespace DN.InstaShare
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var settings = GetSettings();
            
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

			//fallback to "Windows XP *" exif tags allows for exif entered in explorer, rather than IPTC generated through Lightroom, etc.
            var title = ContentWithNewLineIfNotNull(tags.FirstOrDefault(t => t.Name == "Object Name" || t.Name == "Windows XP Title")?.Description, settings.Separater);
            var caption = ContentWithNewLineIfNotNull(tags.FirstOrDefault(t => t.Name == "Caption/Abstract" || t.Name == "Windows XP Subject")?.Description, settings.Separater);
            var keywords = (tags.FirstOrDefault(t => t.Name == "Keywords" || t.Name == "Windows XP Keywords")?.Description ?? "")
                .Replace(" ", "").Split(new char[] { ';' })
                .Except(settings.ExcludedKeywords, StringComparer.InvariantCultureIgnoreCase)
                .Union(settings.StandardKeywords, StringComparer.InvariantCultureIgnoreCase)
	            .ToArray();

            var hashtags = !keywords.Any() 
	            ? string.Empty 
	            : "#" + string.Join(" #", keywords).Trim();

	        var photographer = CreditWithNewLineIfNotNull(settings.Photographer, settings.Separater);

			var clipText = $"{title}{caption}{photographer}{hashtags} {settings.Footer}".Trim();
			Clipboard.Clear();
			Clipboard.SetDataObject(clipText);
			Clipboard.SetText(clipText);

            Process.Start(file);

            //TODO: figure out how to use DataTransferManager.GetForCurrentView to share image directly..
            //possibly by using nuget: uwp-desktop

        }

        private static Settings GetSettings()
        {
            var appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var configPath = Path.Combine(Path.GetDirectoryName(appPath), "settings");
            if (File.Exists(configPath))
            {
                var data = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<Settings>(data);
            }
            return new Settings();
        }


        private static void ShowMessageAndWaitForUser(string message)
        {
            MessageBox.Show(message);
        }

        private static IReadOnlyList<Tag> GetExifTags(string file)
        {
            const string windowsExifDirectoryName = "Exif IFD0";
            const string iptcExifDirectoryName = "IPTC";

            var directories = ImageMetadataReader.ReadMetadata(file);

            var directory = directories.FirstOrDefault(d => d.Name == iptcExifDirectoryName) 
                ?? directories.FirstOrDefault(d => d.Name == windowsExifDirectoryName);

            return directory?.Tags;
        }

        private static string GetFilePathFromArgsOrClipboard(string[] args)
        {
            var clipboardFiles = Clipboard.GetFileDropList();
            return args?.Length > 0 ? args[0] : clipboardFiles.Count > 0 ? clipboardFiles[0] : string.Empty;
        }

        private static string ContentWithNewLineIfNotNull(string content, string separater)
        {
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;

	        var separaterWithNewLine = string.IsNullOrWhiteSpace(separater)
		        ? string.Empty
		        : $"{Environment.NewLine}{separater}";

			return $"{content.Trim()}{separaterWithNewLine}{Environment.NewLine}";
        }

        private static string CreditWithNewLineIfNotNull(string credit, string separater)
        {
            if (string.IsNullOrWhiteSpace(credit)) return string.Empty;

	        var separaterWithNewLine = string.IsNullOrWhiteSpace(separater)
		        ? string.Empty
		        : $"{Environment.NewLine}{separater}";

			return $"📷 {credit.Trim()}{separaterWithNewLine}{Environment.NewLine}";
        }
    }
}
