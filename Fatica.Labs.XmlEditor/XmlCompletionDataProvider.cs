// <file>
//     <license GNU LESSER GENERAL PUBLIC LICENSE Version 3, 29 June 2007 />
//     <original-owner name="Matthew Ward" email="mrward@users.sourceforge.net"/>
//     <owner name="Felice Pollano" email="felice@felicepollano.com"/>
// </file>

using System;
using System.Windows.Forms;

using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using Fatica.Labs.XmlEditor;
using System.Collections.Generic;

namespace ICSharpCode.XmlEditor
{
	/// <summary>
	/// Provides the autocomplete (intellisense) data for an
	/// xml document that specifies a known schema.
	/// </summary>
	public class XmlCompletionDataProvider : AbstractCompletionDataProvider
	{
		XmlSchemaCompletionDataCollection schemaCompletionDataItems;
		XmlSchemaCompletionData defaultSchemaCompletionData;
		string defaultNamespacePrefix = String.Empty;
		
		public XmlCompletionDataProvider(XmlSchemaCompletionDataCollection schemaCompletionDataItems, XmlSchemaCompletionData defaultSchemaCompletionData, string defaultNamespacePrefix)
		{
			this.schemaCompletionDataItems = schemaCompletionDataItems;
			this.defaultSchemaCompletionData = defaultSchemaCompletionData;
			this.defaultNamespacePrefix = defaultNamespacePrefix;
			DefaultIndex = 0;
		}
		
		public override ImageList ImageList {
			get {
				return XmlCompletionDataImageList.GetImageList();
			}
		}

		/// <summary>
		/// Overrides the default behaviour and allows special xml
		/// characters such as '.' and ':' to be used as completion data.
		/// </summary>
		public override CompletionDataProviderKeyResult ProcessKey(char key)
		{
			if (key == '\r' || key == '\t') {
				return CompletionDataProviderKeyResult.InsertionKey;
			}
			return CompletionDataProviderKeyResult.NormalKey;
		}
		
		public override ICompletionData[] GenerateCompletionData(string fileName, TextArea textArea, char charTyped)
		{
			preSelection = null;
			string text = String.Concat(textArea.Document.GetText(0, textArea.Caret.Offset), charTyped);
			
			switch (charTyped) {
				case '=':
					// Namespace intellisense.
					if (XmlParser.IsNamespaceDeclaration(text, text.Length)) {
						return schemaCompletionDataItems.GetNamespaceCompletionData();;
					}
					break;
				case '<':
					// Child element intellisense.
					XmlElementPath parentPath = XmlParser.GetParentElementPath(text);
                    List<ICompletionData> childElements = new List<ICompletionData>();
                    if (!XmlParser.IsInsideComment(text, text.Length))
                    {
                        childElements.Add(new XmlCompletionData("!--", XmlCompletionData.DataType.XmlElement));
                        string unclosed = XmlParser.GetCurrentUnclosedElement(text, text.Length);
                        if (null != unclosed)
                        {
                            childElements.Add(new XmlCompletionData("/"+unclosed, XmlCompletionData.DataType.XmlElement));
                        }
                        if (parentPath.Elements.Count > 0)
                        {
                            childElements.AddRange(GetChildElementCompletionData(parentPath));
                        }
                        else if (defaultSchemaCompletionData != null)
                        {
                            childElements.AddRange(defaultSchemaCompletionData.GetElementCompletionData(defaultNamespacePrefix));
                        }
                    }
                    
                    return childElements.ToArray();
					
				case ' ':
					// Attribute intellisense.
					if (!XmlParser.IsInsideAttributeValue(text, text.Length)) {
						XmlElementPath path = XmlParser.GetActiveElementStartPath(text, text.Length);
                        string[] currentAttributes = XmlParser.GetPresentAttributes(textArea.Document.TextContent, textArea.Caret.Offset);
						if (path.Elements.Count > 0) {
							return GetAttributeCompletionData(path,currentAttributes);
						}
					}
					break;
					
				default:
                    
					// Attribute value intellisense.
					if (XmlParser.IsAttributeValueChar(charTyped)) {
						string attributeName = XmlParser.GetAttributeName(text, text.Length);
						if (attributeName.Length > 0) {
							XmlElementPath elementPath = XmlParser.GetActiveElementStartPath(text, text.Length);
							if (elementPath.Elements.Count > 0) {
								
								var datas = GetAttributeValueCompletionData(elementPath, attributeName);
                                preSelection = null;
                                return datas;
							}
						}
					}
					break;
			}
			
			return null;
		}
		
		/// <summary>
		/// Finds the schema given the xml element path.
		/// </summary>
		public XmlSchemaCompletionData FindSchema(XmlElementPath path)
		{
			if (path.Elements.Count > 0) {
				string namespaceUri = path.Elements[0].Namespace;
				if (namespaceUri.Length > 0) {
					return schemaCompletionDataItems[namespaceUri];
				} else if (defaultSchemaCompletionData != null) {
					
					// Use the default schema namespace if none
					// specified in a xml element path, otherwise
					// we will not find any attribute or element matches
					// later.
					foreach (QualifiedName name in path.Elements) {
						if (name.Namespace.Length == 0) {
							name.Namespace = defaultSchemaCompletionData.NamespaceUri;
						}
					}
					return defaultSchemaCompletionData;
				}
			}
			return null;
		}
		
		/// <summary>
		/// Finds the schema given a namespace URI.
		/// </summary>
		public XmlSchemaCompletionData FindSchema(string namespaceUri)
		{
			return schemaCompletionDataItems[namespaceUri];
		}
		
		/// <summary>
		/// Gets the schema completion data that was created from the specified 
		/// schema filename.
		/// </summary>
		public XmlSchemaCompletionData FindSchemaFromFileName(string fileName)
		{
			return schemaCompletionDataItems.GetSchemaFromFileName(fileName);
		}
		
		ICompletionData[] GetChildElementCompletionData(XmlElementPath path)
		{
			ICompletionData[] completionData = new ICompletionData[0];
			
			XmlSchemaCompletionData schema = FindSchema(path);
			if (schema != null) {
				completionData = schema.GetChildElementCompletionData(path);
			}
			
			return completionData;
		}
		
		ICompletionData[] GetAttributeCompletionData(XmlElementPath path,string[] toExclude)
		{
            ICompletionData[] completionData = new ICompletionData[0];
			
			XmlSchemaCompletionData schema = FindSchema(path);
			if (schema != null) {
				completionData = schema.GetAttributeCompletionData(path,toExclude);
			}
			
			return completionData;
		}
		
		ICompletionData[] GetAttributeValueCompletionData(XmlElementPath path, string name)
		{
            ICompletionData[] completionData = new ICompletionData[0];
			
			XmlSchemaCompletionData schema = FindSchema(path);
			if (schema != null) {
				completionData = schema.GetAttributeValueCompletionData(path, name);
			}
			
			return completionData;
		}
	}
}
