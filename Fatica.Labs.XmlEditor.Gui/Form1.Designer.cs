namespace Fatica.Labs.XmlEditor.Gui
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.xmlEditorControl1 = new ICSharpCode.XmlEditor.XmlEditorControl();
            this.SuspendLayout();
            // 
            // xmlEditorControl1
            // 
            this.xmlEditorControl1.DefaultNamespacePrefix = "";
            this.xmlEditorControl1.DefaultSchemaCompletionData = null;
            this.xmlEditorControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.xmlEditorControl1.IsReadOnly = false;
            this.xmlEditorControl1.Location = new System.Drawing.Point(0, 0);
            this.xmlEditorControl1.Name = "xmlEditorControl1";
            this.xmlEditorControl1.SchemaCompletionDataItems = ((ICSharpCode.XmlEditor.XmlSchemaCompletionDataCollection)(resources.GetObject("xmlEditorControl1.SchemaCompletionDataItems")));
            this.xmlEditorControl1.Size = new System.Drawing.Size(636, 510);
            this.xmlEditorControl1.TabIndex = 0;
            this.xmlEditorControl1.Text = "xmlEditorControl1";
            this.xmlEditorControl1.TextAreaContextMenuStrip = null;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(636, 510);
            this.Controls.Add(this.xmlEditorControl1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private ICSharpCode.XmlEditor.XmlEditorControl xmlEditorControl1;
    }
}

