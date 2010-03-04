using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Schema;
using System.Xml;
using System.IO;
using System.Diagnostics;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using System.Drawing;

// <file>
//     <license GNU LESSER GENERAL PUBLIC LICENSE Version 3, 29 June 2007 />
//     <owner name="Felice Pollano" email="felice@felicepollano.com"/>
// </file>


namespace Fatica.Labs.XmlEditor
{
    public class XmlSchemaSquiggleValidator
    {
        XmlSchemaSet schemaset;
        public XmlSchemaSquiggleValidator(XmlSchemaSet schemaset)
        {
            this.schemaset = schemaset;
        }
        protected void OnValidate(object _, ValidationEventArgs vae)
        {
            var offset = textArea.Document.PositionToOffset(new TextLocation(vae.Exception.LinePosition-1,vae.Exception.LineNumber-1));

            var mk = new TextMarker(offset, GetWordLen(offset), TextMarkerType.WaveLine, vae.Severity == XmlSeverityType.Error ? Color.DarkBlue : Color.Green);
            mk.ToolTip = vae.Message;
            textArea.Document.MarkerStrategy.AddMarker(mk);
        }

        private int GetWordLen(int offset)
        {
            int len = 0;
            while ((len+offset)<textArea.Document.TextLength &&  char.IsLetterOrDigit(textArea.Document.GetCharAt(offset + len)))
                len++;
            return len;
        }
        TextArea textArea;
        public void Validate(String doc,TextArea textArea)
        {
            this.textArea = textArea;
            textArea.Document.MarkerStrategy.RemoveAll(p => true);
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.CloseInput = true;
            settings.ValidationEventHandler += new ValidationEventHandler(settings_ValidationEventHandler);  // Your callback...
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas.Add(schemaset);
            settings.ValidationFlags =
              XmlSchemaValidationFlags.ReportValidationWarnings |
              XmlSchemaValidationFlags.ProcessIdentityConstraints |
              XmlSchemaValidationFlags.ProcessInlineSchema |
              XmlSchemaValidationFlags.ProcessSchemaLocation;

            // Wrap document in an XmlNodeReader and run validation on top of that
            try
            {
                using (XmlReader validatingReader = XmlReader.Create(new StringReader(doc), settings))
                {
                    while (validatingReader.Read()) { /* just loop through document */ }
                }
            }
            catch (XmlException e)
            {
                var offset = textArea.Document.PositionToOffset(new TextLocation(e.LinePosition, e.LineNumber));

                var mk = new TextMarker(offset, 5, TextMarkerType.WaveLine,  Color.DarkBlue );
                mk.ToolTip = e.Message;
                textArea.Document.MarkerStrategy.AddMarker(mk);
            }
        }

        void settings_ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            OnValidate(this, e);
        }
        
        
        
    }
}
