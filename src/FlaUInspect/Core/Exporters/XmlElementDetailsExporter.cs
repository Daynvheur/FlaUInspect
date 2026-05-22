using System.Xml.Linq;
using FlaUInspect.Models;

namespace FlaUInspect.Core.Exporters;

public class XmlElementDetailsExporter : IElementDetailsExporter {
	public string Export(IEnumerable<ElementPatternItem> automationElement) {
		XDocument document = new();
		XElement root = new("Root");
		document.Add(root);

		foreach (var elementPatternItem in automationElement.Where(x => x.IsVisible)) {
			XElement xElement = new("pattern", new XAttribute("Name", elementPatternItem.PatternName));

			foreach (var patternItem in elementPatternItem.Children ?? [])
				xElement.Add(new XElement("property", new XAttribute("Name", patternItem.Key), new XAttribute("Value", patternItem.Value ?? "")));

			root.Add(xElement);
		}

		return document.ToString();
	}
}