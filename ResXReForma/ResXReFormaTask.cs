using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ResXReForma
{
    public class ResXReFormaTask : Task
    {
        [Required]
        public ITaskItem[] Files { get; set; }

        public ITaskItem[] TargetFiles { get; set; }

        public override bool Execute()
        {
            for (var idx = 0; idx < Files.Length; idx++)
            {
                var item = Files[idx];
                var path = item.GetMetadata("FullPath");
                var fileInfo = new FileInfo(path);
                if (fileInfo.IsReadOnly || fileInfo.Extension.ToLower() != ".resx")
                {
                    continue;
                }

                var targetItem = TargetFiles[idx];
                var targetPath = targetItem.GetMetadata("FullPath");

                var time1 = File.GetLastWriteTime(path);
                var time2 = File.GetLastWriteTime(targetPath);
                if (time1 < time2)
                {
                    continue;
                }

                var xml = XDocument.Load(path);
                if (xml.Root == null || xml.Root.FirstNode == null)
                {
                    continue;
                }

                var sb = new StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                sb.AppendLine("<root xml:space=\"preserve\">");
                sb.AppendLine("\t<!--Sorted and reformatted by ResXReForma-->");

                var datas = new List<XElement>();
                foreach (var elt in xml.Root.Elements())
                {
                    var name = elt.Name.LocalName;
                    if (name == "data" || name == "metadata")
                    {
                        if (null == elt.Attribute("name"))
                        {
                            continue;
                        }

                        datas.Add(elt);
                        var value = elt.Element("value");
                        elt.RemoveNodes();
                        elt.Attributes().FirstOrDefault(a => a.Name.LocalName == "space")?.Remove();
                        elt.AddFirst(value);
                    }
                    else if (name != "schema")
                    {
                        sb.AppendLine("\t" + elt.ToString(SaveOptions.DisableFormatting));
                    }
                }

                foreach (var data in datas.OrderBy(data => data.Attribute("name").Value))
                    sb.AppendLine("\t" + data.ToString(SaveOptions.DisableFormatting));

                sb.AppendLine("</root>");

                File.WriteAllText(path, sb.ToString());
                File.SetLastWriteTime(path, time1);
                Log.LogMessage(MessageImportance.High, path + ": sorted and formatted by ResXReForma");

                var targetDir = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                File.WriteAllText(targetPath, ".");
            }

            return true;
        }
    }
}
