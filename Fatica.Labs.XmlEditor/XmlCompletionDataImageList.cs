// <file>
//     <license GNU LESSER GENERAL PUBLIC LICENSE Version 3, 29 June 2007 />
//     <original-owner name="Matthew Ward" email="mrward@users.sourceforge.net"/>
//     <owner name="Felice Pollano" email="felice@felicepollano.com"/>
// </file>

using System;
using System.Windows.Forms;
using Fatica.Labs.XmlEditor;

namespace ICSharpCode.XmlEditor
{
	public class XmlCompletionDataImageList
	{
        static ImageListProvider provider;
        static XmlCompletionDataImageList()
        {
            provider = new ImageListProvider();
        }
		XmlCompletionDataImageList()
		{
            
		}
		
		public static ImageList GetImageList()
		{
            return provider.ImageList;
		}
	}
}
