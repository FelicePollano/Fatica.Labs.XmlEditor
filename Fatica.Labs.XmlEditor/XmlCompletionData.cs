// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Matthew Ward" email="mrward@users.sourceforge.net"/>
//     <version>$Revision: 2932 $</version>
// </file>

using System;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using ICSharpCode.TextEditor.Document;
using System.Drawing;

namespace ICSharpCode.XmlEditor
{
	/// <summary>
	/// Holds the text for  namespace, child element or attribute 
	/// autocomplete (intellisense).
	/// </summary>
	public class XmlCompletionData : ICompletionData
	{
		string text;
		DataType dataType = DataType.XmlElement;
		string description = String.Empty;
		
		/// <summary>
		/// The type of text held in this object.
		/// </summary>
		public enum DataType {
			XmlElement = 1,
			XmlAttribute = 2,
			NamespaceUri = 3,
			XmlAttributeValue = 4
		}
        public bool Mandatory { get; set; }
		public XmlCompletionData(string text)
			: this(text, String.Empty, DataType.XmlElement)
		{
		}
		
		public XmlCompletionData(string text, string description)
			: this(text, description, DataType.XmlElement)
		{
		}

		public XmlCompletionData(string text, DataType dataType)
			: this(text, String.Empty, dataType)
		{
		}		

		public XmlCompletionData(string text, string description, DataType dataType)
		{
			this.text = text;
			this.description = description;
			this.dataType = dataType;  
		}		
		
		public int ImageIndex {
			get {
                switch (dataType)
                {
                    case DataType.NamespaceUri:
                        return 0;
                    case DataType.XmlAttribute:
                        return Mandatory?6:1;
                    case DataType.XmlAttributeValue:
                        return 2;
                    case DataType.XmlElement:
                        return 3;
                }
                return 0;
			}
		}
		
		public string Text {
			get {
				return text;
			}
			set {
				text = value;
			}
		}
		
		/// <summary>
		/// Returns the xml item's documentation as retrieved from
		/// the xs:annotation/xs:documentation element.
		/// </summary>
		public string Description {
			get {
				return description;
			}
		}
		
		public double Priority {
			get {
				return 0;
			}
		}
		
		public bool InsertAction(TextArea textArea, char ch)
		{
			if ((dataType == DataType.XmlElement))
            {
				textArea.InsertString(text);
			}
            else if (dataType == DataType.XmlAttributeValue)
            {
                if( XmlParser.IsInsideAttributeValue(textArea.Document.TextContent,textArea.Caret.Offset))
                {
                    int first, last;
                    XmlParser.GetCurrentAttributeValueSpan(textArea.Document.TextContent, textArea.Caret.Offset, out first, out last);
                    if (last > first && last > 0)
                    {
                        textArea.SelectionManager.SetSelection(textArea.Document.OffsetToPosition(first)
                                                               , textArea.Document.OffsetToPosition(last)
                                                               );
                        textArea.SelectionManager.RemoveSelectedText();
                    }
                }
                textArea.InsertString(text);
                Caret caret = textArea.Caret;
                // Move caret outside of the attribute quotes.
                caret.Position = textArea.Document.OffsetToPosition(caret.Offset + 1);
            }
			else if (dataType == DataType.NamespaceUri) {
				textArea.InsertString(String.Concat("\"", text, "\""));
			} else {
				// Insert an attribute.
				Caret caret = textArea.Caret;
				textArea.InsertString(String.Concat(text, "=\""));	
				
				// Move caret into the middle of the attribute quotes.
				caret.Position = textArea.Document.OffsetToPosition(caret.Offset - 1);
                textArea.SimulateKeyPress('\"');
			}
			return false;
		}
        
       
    }
}
