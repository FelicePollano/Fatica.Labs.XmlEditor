using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.XmlEditor;
using System.IO;
using System.Reflection;

namespace Fatica.Labs.XmlEditor.Gui
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            xmlEditorControl1.Font = new Font("Consolas", 10);

            xmlEditorControl1.Text = "<hibernate-mapping xmlns=\"urn:nhibernate-mapping-2.2\">\n\n</hibernate-mapping>";
            xmlEditorControl1.SchemaCompletionDataItems.Add(new XmlSchemaCompletionData(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Fatica.Labs.XmlEditor.Gui.nhibernate-mapping.xsd"))));
            xmlEditorControl1.SchemaCompletionDataItems.Add(new XmlSchemaCompletionData(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Fatica.Labs.XmlEditor.Gui.nhibernate-configuration.xsd"))));
            xmlEditorControl1.DefaultSchemaCompletionData = new XmlSchemaCompletionData(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Fatica.Labs.XmlEditor.Gui.nhibernate-mapping.xsd")));
            
        }
    }
}
