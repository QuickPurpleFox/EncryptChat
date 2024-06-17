using System;
using System.Xml.Linq;

namespace EncryptChat.Models;

public class XmlHelper
{
    public static string GetModulusFromXml(string xmlString)
    {
        XDocument doc = XDocument.Parse(xmlString);
        XElement modulusElement = doc.Root.Element("Modulus");
        if (modulusElement != null)
        {
            return modulusElement.Value;
        }
        throw new Exception("Modulus element not found");
    }
}