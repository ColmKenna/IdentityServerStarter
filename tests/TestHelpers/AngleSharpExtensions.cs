using AngleSharp.Dom;

namespace TestHelpers;

public static class AngleSharpExtensions
{
    public static bool IsEqualTo(this INode node1, INode node2)
    {
        if (node1 == null && node2 == null) return true;
        if (node1 == null || node2 == null) return false;

        // Compare node names
        if (node1.NodeName != node2.NodeName) return false;

        // Compare attributes
        if (node1 is IElement element1 && node2 is IElement element2)
        {
            if (!element1.AreAttributesEqual(element2)) return false;
            if (!element1.AreClassListsEqual(element2)) return false;
        }

        // Compare children
        if (node1.ChildNodes.Length != node2.ChildNodes.Length) return false;
        for (int i = 0; i < node1.ChildNodes.Length; i++)
        {
            if (!node1.ChildNodes[i].IsEqualTo(node2.ChildNodes[i])) return false;
        }

        return true;
    }

    public static bool AreAttributesEqual(this IElement element1, IElement element2)
    {
        var attrs1 = element1.Attributes;
        var attrs2 = element2.Attributes;

        if (attrs1.Length != attrs2.Length) return false;

        foreach (var attr1 in attrs1)
        {
            var attr2 = element2.GetAttribute(attr1.Name);
            if (attr2 == null || attr1.Value != attr2) return false;
        }

        return true;
    }

    public static bool AreClassListsEqual(this IElement element1, IElement element2)
    {
        var classList1 = element1.ClassList;
        var classList2 = element2.ClassList;

        if (classList1.Length != classList2.Length) return false;

        foreach (var cls in classList1)
        {
            if (!classList2.Contains(cls)) return false;
        }

        return true;
    }
}