using System.Xml.Linq;
using FlaUI.Core.AutomationElements;
using FlaUInspect.Models;
using FlaUInspect.ViewModels;

namespace FlaUInspect.Core.Exporters;

public class XmlElementDetailsExporter : IElementDetailsExporter {
	public string Export(ProcessViewModel processViewModel, int depthMax = 1) {
		XDocument document = new();

		if (processViewModel.SelectedItem?.AutomationElement is null)
			document.Add((XElement)new("Root"));
		else {
			var currentSelection = processViewModel.SelectedItem.AutomationElement;
			document.Add(CreateBranch(new("Root"), processViewModel.SelectedItem.AutomationElement, processViewModel, depthMax));
			_ = processViewModel.GetElementPatterns(currentSelection); // GetElementPatterns changes SelectedItem.AutomationElement
		}

		return document.ToString();
	}

	public static string Export(AutomationElement automationElement, ProcessViewModel processViewModel, int depthMax = 1) {
		XDocument document = new();

		if (automationElement is null)
			document.Add((XElement)new("Root"));
		else
			document.Add(CreateBranch(new("Root"), automationElement, processViewModel, depthMax));

		return document.ToString();
	}

	private static XElement CreateBranch(XElement parent, AutomationElement automationElement, ProcessViewModel processViewModel, int depthMax, int depth = 0) {
		var elementPatterns = processViewModel.GetElementPatterns(automationElement);
		if (elementPatterns is not null)
			foreach (var elementPatternItem in elementPatterns.Where(x => x.IsVisible && x.Children is not null))
				AddPattern(parent, elementPatternItem, elementPatternItem.Children!, new() { { "Identification", "" }, { "Details", "" }, { "LegacyIAccessible", "" }, { "Window", "False" }, { "Pattern Support", "No" } }); // Where Children is not null

		if ((depth < depthMax || depthMax < 0) && automationElement.FindAllChildren() is AutomationElement[] childrenList && childrenList.Length >= 0)
			AddChildren(parent, processViewModel, childrenList, depthMax, depth);

		return parent;
	}

	private static void AddChildren(XElement parent, ProcessViewModel processViewModel, AutomationElement[] childrenList, int depthMax, int depth)
		=> AddXElement(parent,
			() => new("children"),
			childrenList,
			(children) => CreateBranch(new("child"), children, processViewModel, depthMax, depthMax < 0 ? depth : depth + 1));

	private static void AddPattern(XElement parent, ElementPatternItem elementPatternItem, IEnumerable<PatternItem> patternItems, Dictionary<string, string>? blacklist)
		=> AddXElement(parent,
			() => new("pattern", new XAttribute("Name", elementPatternItem.PatternName)),
			patternItems.Where(c => c.Value is not null && (blacklist is null || !blacklist.ContainsKey(elementPatternItem.PatternIdName) || !blacklist[elementPatternItem.PatternIdName].Contains(c.Value))),
			(patternItem) => new("property",
				new XAttribute("Name", patternItem.Key),
				new XAttribute("Value", patternItem.Value ?? "")));

	private static void AddXElement<T>(XElement parent, Func<XElement> branch, IEnumerable<T> collection, Func<T, XElement> leaf) {
		var xElement = branch();

		foreach (var patternItem in collection)
			xElement.Add(leaf(patternItem));

		if (xElement.HasElements)
			parent.Add(xElement);
	}
}