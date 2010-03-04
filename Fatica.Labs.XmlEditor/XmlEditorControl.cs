// <file>
//     <license GNU LESSER GENERAL PUBLIC LICENSE Version 3, 29 June 2007 />
//     <original-owner name="Matthew Ward" email="mrward@users.sourceforge.net"/>
//     <owner name="Felice Pollano" email="felice@felicepollano.com"/>
// </file>

using System;
using System.Windows.Forms;

using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Actions;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using Fatica.Labs.XmlEditor;
using System.Diagnostics;
using System.Drawing;
using System.Xml.Schema;

namespace ICSharpCode.XmlEditor
{
	/// <summary>
	/// Xml editor derived from the SharpDevelop TextEditor control.
	/// </summary>
	public class XmlEditorControl : ICSharpCode.TextEditor.TextEditorControl
	{
		CodeCompletionWindow codeCompletionWindow;
		XmlSchemaCompletionDataCollection schemaCompletionDataItems = new XmlSchemaCompletionDataCollection();
		XmlSchemaCompletionData defaultSchemaCompletionData = null;
		string defaultNamespacePrefix = String.Empty;
		ContextMenuStrip contextMenuStrip;
		TextAreaControl primaryTextAreaControl;
        Timer validationTimer;
        bool validated = false;
		public XmlEditorControl()
		{
            validationTimer = new Timer();
            validationTimer.Interval = 1500;
            validationTimer.Enabled = true;
            validationTimer.Tick += new EventHandler(validationTimer_Tick);
			XmlFormattingStrategy strategy = new XmlFormattingStrategy();
			Document.FormattingStrategy = (IFormattingStrategy)strategy;
			
			Document.HighlightingStrategy = HighlightingManager.Manager.FindHighlighter("XML");
			Document.FoldingManager.FoldingStrategy = new XmlFoldingStrategy();
			
			//Document.BookmarkManager.Factory = new SDBookmarkFactory(Document.BookmarkManager);
			Document.BookmarkManager.Added   += new ICSharpCode.TextEditor.Document.BookmarkEventHandler(BookmarkAdded);
			Document.BookmarkManager.Removed += new ICSharpCode.TextEditor.Document.BookmarkEventHandler(BookmarkRemoved);
            Document.DocumentChanged += new DocumentEventHandler(Document_DocumentChanged);
		}

        void Document_DocumentChanged(object sender, DocumentEventArgs e)
        {
            validated = false;
            validationTimer.Stop();
            validationTimer.Start();
            if (e.Text != null)
            {
                if (-1 != e.Text.IndexOfAny("<>".ToCharArray()))
                    Document.FoldingManager.UpdateFoldings(string.Empty, null);
            }
        }

        void validationTimer_Tick(object sender, EventArgs e)
        {
            if (!validated)
            {
                XmlSchemaSet schemaset = new XmlSchemaSet();
                foreach (XmlSchemaCompletionData xs in schemaCompletionDataItems)
                {
                    schemaset.Add(xs.Schema);
                }
                XmlSchemaSquiggleValidator validator = new XmlSchemaSquiggleValidator(schemaset);
                validator.Validate(primaryTextAreaControl.TextArea.Document.TextContent, primaryTextAreaControl.TextArea);
                validated = true;
                primaryTextAreaControl.TextArea.Invalidate();
            }
        }
        
       

        
		
		/// <summary>
		/// Gets the schemas that the xml editor will use.
		/// </summary>
		/// <remarks>
		/// Probably should NOT have a 'set' property, but allowing one
		/// allows us to share the completion data amongst multiple
		/// xml editor controls.
		/// </remarks>
		public XmlSchemaCompletionDataCollection SchemaCompletionDataItems {
			get {
				return schemaCompletionDataItems;
			}
			set {
				schemaCompletionDataItems = value;
			}
		}
		
		/// <summary>
		/// Gets or sets the default namespace prefix.
		/// </summary>
		public string DefaultNamespacePrefix {
			get {
				return defaultNamespacePrefix;
			}
			set {
				defaultNamespacePrefix = value;
			}
		}
		
		/// <summary>
		/// Gets or sets the default schema completion data associated with this
		/// view.
		/// </summary>
		public XmlSchemaCompletionData DefaultSchemaCompletionData {
			get {
				return defaultSchemaCompletionData;
			}
			set {
				defaultSchemaCompletionData = value;
			}
		}
		
		/// <summary>
		/// Called when the user hits Ctrl+Space.
		/// </summary>
		public void ShowCompletionWindow()
		{
			if (!IsCaretAtDocumentStart) {
				// Find character before cursor.
				
				char ch = GetCharacterBeforeCaret();
				
				HandleKeyPress(ch);
			}
		}
		
		/// <summary>
		/// Adds edit actions to the xml editor.
		/// </summary>
		public void AddEditActions(IEditAction[] actions)
		{
			foreach (IEditAction action in actions) {
				foreach (Keys key in action.Keys) {
					editactions[key] = action;
				}
			}
		}
		
		/// <summary>
		/// Gets or sets the right click menu associated with the
		/// xml editor.
		/// </summary>
		public ContextMenuStrip TextAreaContextMenuStrip {
			get {
				return contextMenuStrip;
			}
			set {
				contextMenuStrip = value;
				if (primaryTextAreaControl != null) {
					primaryTextAreaControl.ContextMenuStrip = value;
				}
			}
		}
		
		protected override void InitializeTextAreaControl(TextAreaControl newControl)
		{
			base.InitializeTextAreaControl(newControl);
			
			primaryTextAreaControl = newControl;
		
			newControl.TextArea.KeyEventHandler += new ICSharpCode.TextEditor.KeyEventHandler(HandleKeyPress);

			newControl.ContextMenuStrip = contextMenuStrip;
			newControl.SelectionManager.SelectionChanged += new EventHandler(SelectionChanged);
			newControl.Document.DocumentChanged += new DocumentEventHandler(DocumentChanged);
			newControl.TextArea.ClipboardHandler.CopyText += new CopyTextEventHandler(ClipboardHandlerCopyText);
			
			newControl.MouseWheel += new MouseEventHandler(TextAreaMouseWheel);
			newControl.DoHandleMousewheel = false;
		}
		
		/// <summary>
		/// Captures the user's key presses.
		/// </summary>
		/// <remarks>
		/// <para>The code completion window ProcessKeyEvent is not perfect
		/// when typing xml.  If enter a space or ':' the text is
		/// autocompleted when it should not be.</para>
		/// <para>The code completion window has one predefined width,
		/// which cuts off any long namespaces that we show.</para>
		/// <para>The above issues have been resolved by duplicating
		/// the code completion window and fixing the problems in the
		/// duplicated class.</para>
		/// </remarks>
		protected bool HandleKeyPress(char ch)
		{
			if (IsCodeCompletionWindowOpen) {
				if (codeCompletionWindow.ProcessKeyEvent(ch)) {
					return false;
				}
			}
			
			try {
				switch (ch) {
					case '<':
					case ' ':
					case '=':
                    case '\"':
						ShowCompletionWindow(ch);
						return false;
					default:
						if (XmlParser.IsAttributeValueChar(ch)) {
							if (IsInsideQuotes(ActiveTextAreaControl.TextArea)) {
								// Have to insert the character ourselves since
								// it is not actually inserted yet.  If it is not
								// inserted now the code completion will not work
								// since the completion data provider attempts to
								// include the key typed as the pre-selected text.
								InsertCharacter(ch);
								ShowCompletionWindow(ch);
								return true;
							}
						}
						break;
				}
			} catch (Exception e) {
				//MessageService.ShowError(e);
                Debug.Assert(false);
			}
			
			return false;
		}
		
		bool IsCodeCompletionEnabled {
			get {
				return true;
			}
		}
		
		void CodeCompletionWindowClosed(object sender, EventArgs e)
		{
			codeCompletionWindow.Closed -= new EventHandler(CodeCompletionWindowClosed);
			codeCompletionWindow.Dispose();
			codeCompletionWindow = null;
		}
		
		bool IsCodeCompletionWindowOpen {
			get {
				return ((codeCompletionWindow != null) && (!codeCompletionWindow.IsDisposed));
			}
		}
		
		void ShowCompletionWindow(char ch)
		{
			if (IsCodeCompletionWindowOpen)
            {
				codeCompletionWindow.Close();
			}
			
			if (IsCodeCompletionEnabled)
            {
				XmlCompletionDataProvider completionDataProvider = new XmlCompletionDataProvider(schemaCompletionDataItems, defaultSchemaCompletionData, defaultNamespacePrefix);
				codeCompletionWindow = CodeCompletionWindow.ShowCompletionWindow(ParentForm, this, FileName, completionDataProvider, ch, true/*PROPERTY*/, false);
				if (codeCompletionWindow != null) 
                {
                    
					codeCompletionWindow.Closed += new EventHandler(CodeCompletionWindowClosed);
				}
			}
		}
		
		void DocumentChanged(object sender, DocumentEventArgs e)
		{
		}
		
		void SelectionChanged(object sender, EventArgs e)
		{
		}
		
		void ClipboardHandlerCopyText(object sender, CopyTextEventArgs e)
		{
//			TextEditorSideBar.PutInClipboardRing(e.Text);
		}
	
		void TextAreaMouseWheel(object sender, MouseEventArgs e)
		{
			TextAreaControl textAreaControl = (TextAreaControl)sender;

			if (IsCodeCompletionWindowOpen && codeCompletionWindow.Visible) {
				codeCompletionWindow.HandleMouseWheel(e);
			} else {
				textAreaControl.HandleMouseWheel(e);
			}
		}
		
		char GetCharacterBeforeCaret()
		{
			string text = Document.GetText(ActiveTextAreaControl.TextArea.Caret.Offset - 1, 1);
			if (text.Length > 0) {
				return text[0];
			}
			
			return '\0';
		}
		
		bool IsCaretAtDocumentStart {
			get {
				return ActiveTextAreaControl.TextArea.Caret.Offset == 0;
			}
		}
		
		/// <summary>
		/// Checks whether the caret is inside a set of quotes (" or ').
		/// </summary>
		bool IsInsideQuotes(TextArea textArea)
		{
			bool inside = false;
			
			LineSegment line = textArea.Document.GetLineSegment(textArea.Document.GetLineNumberForOffset(textArea.Caret.Offset));
			if (line != null) {
				if ((line.Offset + line.Length > textArea.Caret.Offset) &&
				    (line.Offset < textArea.Caret.Offset)){
					
					char charAfter = textArea.Document.GetCharAt(textArea.Caret.Offset);
					char charBefore = textArea.Document.GetCharAt(textArea.Caret.Offset - 1);
					
					if (((charBefore == '\'') && (charAfter == '\'')) ||
					    ((charBefore == '\"') && (charAfter == '\"'))) {
						inside = true;
					}
				}
			}
			
			return inside;
		}
		
		/// <summary>
		/// Inserts a character into the text editor at the current offset.
		/// </summary>
		/// <remarks>
		/// This code is copied from the TextArea.SimulateKeyPress method.  This
		/// code is needed to handle an issue with code completion.  What if
		/// we want to include the character just typed as the pre-selected text
		/// for autocompletion?  If we do not insert the character before
		/// displaying the autocompletion list we cannot set the pre-selected text
		/// because it is not actually inserted yet.  The autocompletion window
		/// checks the offset of the pre-selected text and closes the window
		/// if the range is wrong.  The offset check is always wrong since the text
		/// does not actually exist yet.  The check occurs in
		/// CodeCompletionWindow.CaretOffsetChanged:
		/// <code>[[!CDATA[	int offset = control.ActiveTextAreaControl.Caret.Offset;
		///
		///	if (offset < startOffset || offset > endOffset) {
		///		Close();
		///	} else {
		///		codeCompletionListView.SelectItemWithStart(control.Document.GetText(startOffset, offset - startOffset));
		///	}]]
		/// </code>
		/// The Close method is called because the offset is out of the range.
		/// </remarks>
		void InsertCharacter(char ch)
		{
			ActiveTextAreaControl.TextArea.BeginUpdate();
			Document.UndoStack.StartUndoGroup();
			
			switch (ActiveTextAreaControl.TextArea.Caret.CaretMode)
			{
				case CaretMode.InsertMode:
					ActiveTextAreaControl.TextArea.InsertChar(ch);
					break;
				case CaretMode.OverwriteMode:
					ActiveTextAreaControl.TextArea.ReplaceChar(ch);
					break;
			}
			int currentLineNr = ActiveTextAreaControl.TextArea.Caret.Line;
			Document.FormattingStrategy.FormatLine(ActiveTextAreaControl.TextArea, currentLineNr, Document.PositionToOffset(ActiveTextAreaControl.TextArea.Caret.Position), ch);
			
			ActiveTextAreaControl.TextArea.EndUpdate();
			Document.UndoStack.EndUndoGroup();
		}
		
		/// <summary>
		/// Have to remove the bookmark from the document otherwise the text will
		/// stay marked in red if the bookmark is a breakpoint.  This is because
		/// there are two bookmark managers, one in SharpDevelop itself and one
		/// in the TextEditor library.  By default, only the one in the text editor's
		/// bookmark manager will be removed, so SharpDevelop will not know about it.
		/// Removing it from the SharpDevelop BookMarkManager informs the debugger
		/// service that the breakpoint has been removed so it triggers the removal
		/// of the text marker.
		/// </summary>
		void BookmarkRemoved(object sender, ICSharpCode.TextEditor.Document.BookmarkEventArgs e)
		{
			
		}
		
		/// <summary>
		/// Have to add the bookmark to the BookmarkManager otherwise the bookmark is
		/// not remembered when re-opening the file and does not show in the 
		/// bookmark manager.
		/// </summary>
		void BookmarkAdded(object sender, ICSharpCode.TextEditor.Document.BookmarkEventArgs e)
		{
			
		}

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // XmlEditorControl
            // 
            this.Name = "XmlEditorControl";
            this.ResumeLayout(false);

        }
	}
}
