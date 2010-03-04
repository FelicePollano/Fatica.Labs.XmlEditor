// <file>
//     <license GNU LESSER GENERAL PUBLIC LICENSE Version 3, 29 June 2007 />
//     <owner name="Felice Pollano" email="felice@felicepollano.com"/>
// </file>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Fatica.Labs.XmlEditor
{
    public partial class ImageListProvider : UserControl
    {
        public ImageListProvider()
        {
            InitializeComponent();
        }
        public ImageList ImageList
        {
            get { return imageListCompletion; }
        }
    }
}
