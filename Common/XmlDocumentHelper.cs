using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Common
{
	public static class XmlDocumentHelper
	{
		public static XmlDocument Create(string xml)
		{
			var document = new XmlDocument();
			document.LoadXml(xml);
			return document;
		}

		public static void Save(XmlDocument document, string filePath, string lineEndings)
		{
            var xmlContent = new StringBuilder();
		    using (TextWriter tempWriter = new StringWriter(xmlContent))
		        document.Save(tempWriter);

		    var contentLines = xmlContent
                .ToString()
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            contentLines[0] = contentLines[0].Replace("utf-16", "utf-8");
            File.WriteAllText(filePath, string.Join(lineEndings, contentLines), new UTF8Encoding(true));
		}
	}
}