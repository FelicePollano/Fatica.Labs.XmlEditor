namespace Fatica.Labs.XmlEditor
{
    partial class ImageListProvider
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImageListProvider));
            this.imageListCompletion = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            // 
            // imageListCompletion
            // 
            this.imageListCompletion.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListCompletion.ImageStream")));
            this.imageListCompletion.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListCompletion.Images.SetKeyName(0, "09.png");
            this.imageListCompletion.Images.SetKeyName(1, "12.png");
            this.imageListCompletion.Images.SetKeyName(2, "10.png");
            this.imageListCompletion.Images.SetKeyName(3, "39.png");
            this.imageListCompletion.Images.SetKeyName(4, "37.png");
            this.imageListCompletion.Images.SetKeyName(5, "40.png");
            this.imageListCompletion.Images.SetKeyName(6, "11.png");
            // 
            // ImageListProvider
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "ImageListProvider";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ImageList imageListCompletion;
    }
}
