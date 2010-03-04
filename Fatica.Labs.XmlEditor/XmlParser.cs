// <file>
//     <license GNU LESSER GENERAL PUBLIC LICENSE Version 3, 29 June 2007 />
//     <original-owner name="Matthew Ward" email="mrward@users.sourceforge.net"/>
//     <owner name="Felice Pollano" email="felice@felicepollano.com"/>
// </file>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace ICSharpCode.XmlEditor
{
	/// <summary>
	/// Utility class that contains xml parsing routines used to determine
	/// the currently selected element so we can provide intellisense.
	/// </summary>
	/// <remarks>
	/// All of the routines return <see cref="XmlElementPath"/> objects
	/// since we are interested in the complete path or tree to the
	/// currently active element.
	/// </remarks>
	public class XmlParser
	{
		/// <summary>
		/// Helper class.  Holds the namespace URI and the prefix currently
		/// in use for this namespace.
		/// </summary>
		class NamespaceURI
		{
			string namespaceURI = String.Empty;
			string prefix = String.Empty;
			
			public NamespaceURI()
			{
			}
			
			public NamespaceURI(string namespaceURI, string prefix)
			{
				this.namespaceURI = namespaceURI;
				this.prefix = prefix;
			}
			
			public string Namespace {
				get { return namespaceURI; }
				set { namespaceURI = value; }
			}
			
			public string Prefix {
				get { return prefix; }
				set {
					prefix = value;
					if (prefix == null) {
						prefix = String.Empty;
					}
				}
			}
			
			public override string ToString()
			{
				if (!String.IsNullOrEmpty(prefix)) {
					return prefix + ":" + namespaceURI;
				}
				return namespaceURI;
			}
		}

		static readonly char[] whitespaceCharacters = new char[] {' ', '\n', '\t', '\r'};
		
		XmlParser()
		{
		}

		/// <summary>
		/// Gets path of the xml element start tag that the specified
		/// <paramref name="index"/> is currently inside.
		/// </summary>
		/// <remarks>If the index outside the start tag then an empty path
		/// is returned.</remarks>
		public static XmlElementPath GetActiveElementStartPath(string xml, int index)
		{
			QualifiedNameCollection namespaces = new QualifiedNameCollection();
			return GetActiveElementStartPath(xml, index, namespaces);
		}
		
		/// <summary>
		/// Gets path of the xml element start tag that the specified
		/// <paramref name="index"/> is currently located. This is different to the
		/// GetActiveElementStartPath method since the index can be inside the element
		/// name.
		/// </summary>
		/// <remarks>If the index outside the start tag then an empty path
		/// is returned.</remarks>
		public static XmlElementPath GetActiveElementStartPathAtIndex(string xml, int index)
		{
			QualifiedNameCollection namespaces = new QualifiedNameCollection();
			return GetActiveElementStartPathAtIndex(xml, index, namespaces);
		}

		/// <summary>
		/// Gets the parent element path based on the index position.
		/// </summary>
		public static XmlElementPath GetParentElementPath(string xml)
		{
			QualifiedNameCollection namespaces = new QualifiedNameCollection();
			XmlElementPath path = GetFullParentElementPath(xml, namespaces);
			path.Compact();
			return path;
		}
		
		/// <summary>
		/// Checks whether the attribute at the end of the string is a
		/// namespace declaration.
		/// </summary>
		public static bool IsNamespaceDeclaration(string xml, int index)
		{
			if (String.IsNullOrEmpty(xml)) {
				return false;
			}
			
			index = GetCorrectedIndex(xml.Length, index);
			
			// Move back one character if the last character is an '='
			if (xml[index] == '=') {
				xml = xml.Substring(0, xml.Length - 1);
				--index;
			}
			
			// From the end of the string work backwards until we have
			// picked out the last attribute and reached some whitespace.
			StringBuilder reversedAttributeName = new StringBuilder();
			
			bool ignoreWhitespace = true;
			int currentIndex = index;
			for (int i = 0; i < index; ++i) {
				
				char currentChar = xml[currentIndex];
				
				if (Char.IsWhiteSpace(currentChar)) {
					if (ignoreWhitespace == false) {
						// Reached the start of the attribute name.
						break;
					}
				} else if (Char.IsLetterOrDigit(currentChar) || (currentChar == ':')) {
					ignoreWhitespace = false;
					reversedAttributeName.Append(currentChar);
				} else {
					// Invalid string.
					break;
				}
				
				--currentIndex;
			}
			
			// Did we get a namespace?
			
			bool isNamespace = false;

			if ((reversedAttributeName.ToString() == "snlmx") || (reversedAttributeName.ToString().EndsWith(":snlmx"))) {
				isNamespace = true;
			}
			
			return isNamespace;
		}
		
		/// <summary>
		/// Gets the attribute name and any prefix. The namespace
		/// is not determined.
		/// </summary>
		/// <returns><see langword="null"/> if no attribute name can
		/// be found.</returns>
		public static QualifiedName GetQualifiedAttributeName(string xml, int index)
		{
			string name = GetAttributeName(xml, index);
			return GetQualifiedName(name);
		}
		
		/// <summary>
		/// Gets the name of the attribute inside but before the specified
		/// index.
		/// </summary>
		public static string GetAttributeName(string xml, int index)
		{
			if (String.IsNullOrEmpty(xml)) {
				return String.Empty;
			}
			
			index = GetCorrectedIndex(xml.Length, index);
			return GetAttributeName(xml, index, true, true, true);
		}

		/// <summary>
		/// Gets the name of the attribute and its prefix at the specified index. The index
		/// can be anywhere inside the attribute name or in the attribute value.
		/// The namespace for the element containing the attribute will also be determined
		/// if the includeNamespace flag is set to true.
		/// </summary>
		public static QualifiedName GetQualifiedAttributeNameAtIndex(string xml, int index, bool includeNamespace)
		{
			string name = GetAttributeNameAtIndex(xml, index);
			QualifiedName qualifiedName = GetQualifiedName(name);
			if (qualifiedName != null && String.IsNullOrEmpty(qualifiedName.Namespace) && includeNamespace) {
				QualifiedNameCollection namespaces = new QualifiedNameCollection();
				XmlElementPath path = GetActiveElementStartPathAtIndex(xml, index, namespaces);
				qualifiedName.Namespace = GetNamespaceForPrefix(namespaces, path.Elements.LastPrefix);
			}
			return qualifiedName;
		}
		
		/// <summary>
		/// Gets the name of the attribute and its prefix at the specified index. The index
		/// can be anywhere inside the attribute name or in the attribute value.
		/// </summary>
		public static QualifiedName GetQualifiedAttributeNameAtIndex(string xml, int index)
		{
			return GetQualifiedAttributeNameAtIndex(xml, index, false);
		}
		
		/// <summary>
		/// Gets the name of the attribute at the specified index. The index
		/// can be anywhere inside the attribute name or in the attribute value.
		/// </summary>
		public static string GetAttributeNameAtIndex(string xml, int index)
		{
			if (String.IsNullOrEmpty(xml)) {
				return String.Empty;
			}
			
			index = GetCorrectedIndex(xml.Length, index);
			
			bool ignoreWhitespace = true;
			bool ignoreEqualsSign = false;
			bool ignoreQuote = false;
			
			if (IsInsideAttributeValue(xml, index)) {
				// Find attribute name start.
				int elementStartIndex = GetActiveElementStartIndex(xml, index);
				if (elementStartIndex == -1) {
					return String.Empty;
				}
				
				// Find equals sign.
				for (int i = index; i > elementStartIndex; --i) {
					char ch = xml[i];
					if (ch == '=') {
						index = i;
						ignoreEqualsSign = true;
						break;
					}
				}
			} else {
				// Find end of attribute name.
				for (; index < xml.Length; ++index) {
					char ch = xml[index];
					if (!IsXmlNameChar(ch)) {
						if (ch == '\'' || ch == '\"') {
							ignoreQuote = true;
							ignoreEqualsSign = true;
						}
						break;
					}
				}
				--index;
			}
			
			return GetAttributeName(xml, index, ignoreWhitespace, ignoreQuote, ignoreEqualsSign);
		}
		
		/// <summary>
		/// Checks for valid xml attribute value character
		/// </summary>
		public static bool IsAttributeValueChar(char ch)
		{
			if ((ch == '<') ||
			    (ch == '>')) 
			{
				return false;
			}
			
			return true;
		}
		
		/// <summary>
		/// Checks for valid xml element or attribute name character.
		/// </summary>
		public static bool IsXmlNameChar(char ch)
		{
			if (Char.IsLetterOrDigit(ch) ||
			    (ch == ':') ||
			    (ch == '/') ||
			    (ch == '_') ||
			    (ch == '.') ||
			    (ch == '-'))
			{
				return true;
			}
			
			return false;
		}
		
		/// <summary>
		/// Determines whether the specified index is inside an attribute value.
		/// </summary>
		public static bool IsInsideAttributeValue(string xml, int index)
		{
			if (String.IsNullOrEmpty(xml)) {
				return false;
			}
			
			if (index > xml.Length) {
				index = xml.Length;
			}
			
			int elementStartIndex = GetActiveElementStartIndex(xml, index);
			if (elementStartIndex == -1) {
				return false;
			}
			
			// Count the number of double quotes and single quotes that exist
			// before the first equals sign encountered going backwards to
			// the start of the active element.
			bool foundEqualsSign = false;
			int doubleQuotesCount = 0;
			int singleQuotesCount = 0;
			char lastQuoteChar = ' ';
			for (int i = index - 1; i > elementStartIndex; --i) {
				char ch = xml[i];
				if (ch == '=') {
					foundEqualsSign = true;
					break;
				} else if (ch == '\"') {
					lastQuoteChar = ch;
					++doubleQuotesCount;
				} else if (ch == '\'') {
					lastQuoteChar = ch;
					++singleQuotesCount;
				}
			}
			
			bool isInside = false;
			
			if (foundEqualsSign) {
				// Odd number of quotes?
				if ((lastQuoteChar == '\"') && ((doubleQuotesCount % 2) > 0)) {
					isInside = true;
				} else if ((lastQuoteChar == '\'') && ((singleQuotesCount %2) > 0)) {
					isInside = true;
				}
			}
			
			return isInside;
		}
		
		/// <summary>
		/// Gets the attribute value at the specified index.
		/// </summary>
		/// <returns>An empty string if no attribute value can be found.</returns>
		public static string GetAttributeValueAtIndex(string xml, int index)
		{
			if (!IsInsideAttributeValue(xml, index)) {
				return String.Empty;
			}
			
			index = GetCorrectedIndex(xml.Length, index);
			
			int elementStartIndex = GetActiveElementStartIndex(xml, index);
			if (elementStartIndex == -1) {
				return String.Empty;
			}
			
			// Find equals sign.
			int equalsSignIndex = -1;
			for (int i = index; i > elementStartIndex; --i) {
				char ch = xml[i];
				if (ch == '=') {
					equalsSignIndex = i;
					break;
				}
			}
			
			if (equalsSignIndex == -1) {
				return String.Empty;
			}
			
			// Find attribute value.
			char quoteChar = ' ';
			bool foundQuoteChar = false;
			StringBuilder attributeValue = new StringBuilder();
			for (int i = equalsSignIndex; i < xml.Length; ++i) {
				char ch = xml[i];
				if (!foundQuoteChar) {
					if (ch == '\"' || ch == '\'') {
						quoteChar = ch;
						foundQuoteChar = true;
					}
				} else {
					if (ch == quoteChar) {
						// End of attribute value.
						return attributeValue.ToString();
					} else if (IsAttributeValueChar(ch) || (ch == '\"' || ch == '\'')) {
						attributeValue.Append(ch);
					} else {
						// Invalid character found.
						return String.Empty;
					}
				}
			}
			
			return String.Empty;
		}
		
		/// <summary>
		/// Gets the text of the xml element start tag that the index is
		/// currently inside.
		/// </summary>
		/// <returns>
		/// Returns the text up to and including the start tag &lt; character.
		/// </returns>
		static string GetActiveElementStartText(string xml, int index)
		{
			int elementStartIndex = GetActiveElementStartIndex(xml, index);
			if (elementStartIndex >= 0) {
				if (elementStartIndex < index) {
					int elementEndIndex = GetActiveElementEndIndex(xml, index);
					if (elementEndIndex >= index) {
						return xml.Substring(elementStartIndex, elementEndIndex - elementStartIndex);
					}
				}
			}
			return null;
		}
		
		/// <summary>
		/// Locates the index of the start tag &lt; character.
		/// </summary>
		/// <returns>
		/// Returns the index of the start tag character; otherwise
		/// -1 if no start tag character is found or a end tag
		/// &gt; character is found first.
		/// </returns>
		static int GetActiveElementStartIndex(string xml, int index)
		{
			int elementStartIndex = -1;
			
			int currentIndex = index - 1;

			for (int i = 0; i < index; ++i) {
				
				char currentChar = xml[currentIndex];
				if (currentChar == '<') {
					elementStartIndex = currentIndex;
					break;
				} else if (currentChar == '>') {
					break;
				}
				
				--currentIndex;
			}
			
			return elementStartIndex;
		}
		
		/// <summary>
		/// Locates the index of the end tag character.
		/// </summary>
		/// <returns>
		/// Returns the index of the end tag character; otherwise
		/// -1 if no end tag character is found or a start tag
		/// character is found first.
		/// </returns>
		static int GetActiveElementEndIndex(string xml, int index)
		{
			int elementEndIndex = index;
			
			for (int i = index; i < xml.Length; ++i) {
				
				char currentChar = xml[i];
				if (currentChar == '>') {
					elementEndIndex = i;
					break;
				} else if (currentChar == '<'){
					elementEndIndex = -1;
					break;
				}
			}
			
			return elementEndIndex;
		}
		
		/// <summary>
		/// Gets the element name from the element start tag string.
		/// </summary>
		/// <param name="xml">This string must start at the
		/// element we are interested in.</param>
		static QualifiedName GetElementName(string xml)
		{
			string name = String.Empty;
			
			// Find the end of the element name.
			xml = xml.Replace("\r\n", " ");
			int index = xml.IndexOf(' ');
			if (index > 0) {
				name = xml.Substring(1, index - 1);
			} else {
				name = xml.Substring(1);
			}
			
			return GetQualifiedName(name);
		}
		
		/// <summary>
		/// Gets the element namespace from the element start tag
		/// string.
		/// </summary>
		/// <param name="xml">This string must start at the
		/// element we are interested in.</param>
		static NamespaceURI GetElementNamespace(string xml)
		{
			NamespaceURI namespaceURI = new NamespaceURI();
			
			Match match = Regex.Match(xml, ".*?(xmlns\\s*?|xmlns:.*?)=\\s*?['\\\"](.*?)['\\\"]");
			if (match.Success) {
				namespaceURI.Namespace = match.Groups[2].Value;
				
				string xmlns = match.Groups[1].Value.Trim();
				int prefixIndex = xmlns.IndexOf(':');
				if (prefixIndex > 0) {
					namespaceURI.Prefix = xmlns.Substring(prefixIndex + 1);
				}
			}
			
			return namespaceURI;
		}
		
		static string ReverseString(string text)
		{
			StringBuilder reversedString = new StringBuilder(text);
			
			int index = text.Length;
			foreach (char ch in text) {
				--index;
				reversedString[index] = ch;
			}
			
			return reversedString.ToString();
		}
		
		/// <summary>
		/// Ensures that the index is on the last character if it is
		/// too large.
		/// </summary>
		/// <param name="length">The length of the string.</param>
		/// <param name="index">The current index.</param>
		/// <returns>The index unchanged if the index is smaller than the
		/// length of the string; otherwise it returns length - 1.</returns>
		static int GetCorrectedIndex(int length, int index)
		{
			if (index >= length) {
				index = length - 1;
			}
			return index;
		}
		
		/// <summary>
		/// Gets the active element path given the element text.
		/// </summary>
		static XmlElementPath GetActiveElementStartPath(string xml, int index, string elementText, QualifiedNameCollection namespaces)
		{
			QualifiedName elementName = GetElementName(elementText);
			if (elementName == null) {
				return new XmlElementPath();
			}
			
			NamespaceURI elementNamespace = GetElementNamespace(elementText);
			
			XmlElementPath path = GetFullParentElementPath(xml.Substring(0, index), namespaces);
			
			// Try to get a namespace for the active element's prefix.
			if (elementName.Prefix.Length > 0 && elementNamespace.Namespace.Length == 0) {
				elementName.Namespace = GetNamespaceForPrefix(namespaces, elementName.Prefix);
				elementNamespace.Namespace = elementName.Namespace;
				elementNamespace.Prefix = elementName.Prefix;
			}
			
			if (elementNamespace.Namespace.Length == 0) {
				if (path.Elements.Count > 0) {
					QualifiedName parentName = path.Elements[path.Elements.Count - 1];
					elementNamespace.Namespace = parentName.Namespace;
					elementNamespace.Prefix = parentName.Prefix;
				}
			}
			path.Elements.Add(new QualifiedName(elementName.Name, elementNamespace.Namespace, elementNamespace.Prefix));
			path.Compact();
			return path;
		}
		
		static string GetAttributeName(string xml, int index, bool ignoreWhitespace, bool ignoreQuote, bool ignoreEqualsSign)
		{
			string name = String.Empty;
			
			// From the end of the string work backwards until we have
			// picked out the attribute name.
			StringBuilder reversedAttributeName = new StringBuilder();
			
			int currentIndex = index;
			bool invalidString = true;
			
			for (int i = 0; i <= index; ++i) {
				
				char currentChar = xml[currentIndex];
				
				if (IsXmlNameChar(currentChar)) {
					if (!ignoreEqualsSign) {
						ignoreWhitespace = false;
						reversedAttributeName.Append(currentChar);
					}
				} else if (Char.IsWhiteSpace(currentChar)) {
					if (ignoreWhitespace == false) {
						// Reached the start of the attribute name.
						invalidString = false;
						break;
					}
				} else if ((currentChar == '\'') || (currentChar == '\"')) {
					if (ignoreQuote) {
						ignoreQuote = false;
					} else {
						break;
					}
				} else if (currentChar == '='){
					if (ignoreEqualsSign) {
						ignoreEqualsSign = false;
					} else {
						break;
					}
				} else if (IsAttributeValueChar(currentChar)) {
					if (!ignoreQuote) {
						break;
					}
				} else {
					break;
				}
				
				--currentIndex;
			}
			
			if (!invalidString) {
				name = ReverseString(reversedAttributeName.ToString());
			}
			
			return name;
		}
		
		/// <summary>
		/// Gets the element name at the specified index.
		/// </summary>
		static string GetElementNameAtIndex(string xml, int index)
		{
			int elementStartIndex = GetActiveElementStartIndex(xml, index);
			if (elementStartIndex >= 0 && elementStartIndex < index) {
				int elementEndIndex = GetActiveElementEndIndex(xml, index);
				if (elementEndIndex == -1) {
					elementEndIndex = xml.IndexOfAny(whitespaceCharacters, elementStartIndex);
				}
				if (elementEndIndex >= elementStartIndex) {
					return xml.Substring(elementStartIndex, elementEndIndex - elementStartIndex);
				}
			}
			return null;
		}
		
		/// <summary>
		/// Returns a name and its prefix.
		/// </summary>
		static QualifiedName GetQualifiedName(string name)
		{
			if (name.Length == 0) {
				return null;
			}
			
			QualifiedName qualifiedName = new QualifiedName();
			int prefixIndex = name.IndexOf(':');
			if (prefixIndex > 0) {
				qualifiedName.Prefix = name.Substring(0, prefixIndex);
				qualifiedName.Name = name.Substring(prefixIndex + 1);
			} else {
				qualifiedName.Name = name;
			}
			return qualifiedName;
		}
		
		/// <summary>
		/// Gets the parent element path based on the index position. This
		/// method does not compact the path so it will include all elements
		/// including those in another namespace in the path.
		/// </summary>
		static XmlElementPath GetFullParentElementPath(string xml, QualifiedNameCollection namespaces)
		{
			XmlElementPath path = new XmlElementPath();
			IDictionary<string,string> namespacesInScope = null;
			using (StringReader reader = new StringReader(xml)) {
				using (XmlTextReader xmlReader = new XmlTextReader(reader)) {
					try {
						xmlReader.XmlResolver = null; // prevent XmlTextReader from loading external DTDs
						while (xmlReader.Read()) {
							switch (xmlReader.NodeType) {
								case XmlNodeType.Element:
									if (!xmlReader.IsEmptyElement) {
										QualifiedName elementName = new QualifiedName(xmlReader.LocalName, xmlReader.NamespaceURI, xmlReader.Prefix);
										path.Elements.Add(elementName);
									}
									break;
								case XmlNodeType.EndElement:
									path.Elements.RemoveLast();
									break;
							}
						}
					} catch (XmlException) {
						namespacesInScope = xmlReader.GetNamespacesInScope(XmlNamespaceScope.All);
					}
				}
			}
			
			// Add namespaces in scope for the last element read.
			if (namespacesInScope != null) {
				foreach (KeyValuePair<string, string> ns in namespacesInScope) {
					namespaces.Add(new QualifiedName(String.Empty, ns.Value, ns.Key));
				}
			}
			
			return path;
		}
		
		/// <summary>
		/// Finds the namespace for the specified prefix.
		/// </summary>
		static string GetNamespaceForPrefix(QualifiedNameCollection namespaces, string prefix)
		{
			foreach (QualifiedName name in namespaces) {
				if (name.Prefix == prefix) {
					return name.Namespace;
				}
			}
			return String.Empty;
		}
		
		/// <summary>
		/// Gets path of the xml element start tag that the specified
		/// <paramref name="index"/> is currently inside.
		/// </summary>
		/// <remarks>If the index outside the start tag then an empty path
		/// is returned.</remarks>
		/// <param name="namespaces">Returns the namespaces that are
		/// exist in the xml.</param>
		static XmlElementPath GetActiveElementStartPath(string xml, int index, QualifiedNameCollection namespaces)
		{
			XmlElementPath path = new XmlElementPath();
			string elementText = GetActiveElementStartText(xml, index);
			if (elementText != null) {
				path = GetActiveElementStartPath(xml, index, elementText, namespaces);
			}
			return path;
		}
		
		/// <summary>
		/// Gets path of the xml element start tag that the specified
		/// <paramref name="index"/> is currently located. This is different to the
		/// GetActiveElementStartPath method since the index can be inside the element
		/// name.
		/// </summary>
		/// <remarks>If the index outside the start tag then an empty path
		/// is returned.</remarks>
		static XmlElementPath GetActiveElementStartPathAtIndex(string xml, int index, QualifiedNameCollection namespaces)
		{
			// Find first non xml element name character to the right of the index.
			index = GetCorrectedIndex(xml.Length, index);
			if (index < 0) // can happen when xml.Length==0
				return new XmlElementPath();
			int currentIndex = index;
			for (; currentIndex < xml.Length; ++currentIndex) {
				char ch = xml[currentIndex];
				if (!IsXmlNameChar(ch)) {
					break;
				}
			}
			
			string elementText = GetElementNameAtIndex(xml, currentIndex);
			if (elementText != null) {
				return GetActiveElementStartPath(xml, currentIndex, elementText, namespaces);
			}
			return new XmlElementPath();
		}
        static Regex regexAttributesLookup = new Regex(@"(\w+)\s*=\s*\""\w*\""",RegexOptions.Compiled|RegexOptions.Multiline);
        public static string[] GetPresentAttributes(string xml, int index)
        {
            List<string> attributes = new List<string>();
            int start = GetActiveElementStartIndex(xml, index);
            if (start > 0)
            {
                int end = xml.IndexOfAny("<>".ToCharArray(), start+1);
                if (end == -1)
                {
                    end = xml.Length - 1;
                }
                var matches = regexAttributesLookup.Matches(xml.Substring(start,end-start));
                foreach (Match m in matches)
                {
                    attributes.Add(m.Groups[1].Value);
                }
            }
            return attributes.ToArray();
        }

        static Regex regexProperAttributeEnd=new Regex(@"\s*\w*",RegexOptions.Compiled|RegexOptions.Multiline);
        public static void GetCurrentAttributeValueSpan(string xml, int index, out int first, out int last)
        {
            first = last = index;
            Match m = regexProperAttributeEnd.Match(xml, index);
            if (m.Success)
            {
                last = m.Index+m.Length;
            }
        }

        public static bool IsInsideComment(string text, int p)
        {
            string sub = text.Substring(0, p);
            int closeIndex = sub.LastIndexOf("-->");
            int startIndex = sub.LastIndexOf("<!--");
            return startIndex > closeIndex;
        }
        static Regex regexElement = new Regex(@"</?\s*/?[\w\-._]+.*>", RegexOptions.Multiline | RegexOptions.Compiled);
        static Regex regexComment = new Regex(@"<!--.*-->", RegexOptions.Multiline | RegexOptions.Compiled);
        public static string GetCurrentUnclosedElement(string text, int p)
        {
            Stack<string> elements = new Stack<string>();
            string sub = text.Substring(0, p);
            sub = regexComment.Replace(sub, "");
            foreach (Match m in regexElement.Matches(sub))
            {
                string tag = m.Value.Trim("<>".ToCharArray()).Trim();

                if (!tag.StartsWith("/"))
                {
                    if(!tag.EndsWith("/"))
                        elements.Push(tag.Split(' ')[0]);
                }
                else
                {
                    string last = elements.Pop();
                    if (last != tag.Trim(" /".ToCharArray()))
                    {
                        return null;
                    }
                }
            }
            if (elements.Count > 0)
                return elements.Pop();
            else
                return null;
        }
    }
}

