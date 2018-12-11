namespace Shape_Detect
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
            this.Plain_Image = new System.Windows.Forms.PictureBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.p1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.p2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.p3ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.camera1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.Image_Shapes = new System.Windows.Forms.PictureBox();
            this.roiBox = new System.Windows.Forms.PictureBox();
            this.labelBoxNumber = new System.Windows.Forms.Label();
            this.labelTriagNumb = new System.Windows.Forms.Label();
            this.serialReturn = new System.Windows.Forms.Label();
            this.comboCOMList = new System.Windows.Forms.ComboBox();
            this.boxOverRideCommand = new System.Windows.Forms.CheckBox();
            this.labelDistFromBot = new System.Windows.Forms.Label();
            this.labelAngleToBot = new System.Windows.Forms.Label();
            this.buttonHome = new System.Windows.Forms.Button();
            this.buttonCalcROI = new System.Windows.Forms.Button();
            this.checkBoxRunRobot = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.Plain_Image)).BeginInit();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Image_Shapes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.roiBox)).BeginInit();
            this.SuspendLayout();
            // 
            // Plain_Image
            // 
            this.Plain_Image.Location = new System.Drawing.Point(12, 27);
            this.Plain_Image.Name = "Plain_Image";
            this.Plain_Image.Size = new System.Drawing.Size(396, 290);
            this.Plain_Image.TabIndex = 0;
            this.Plain_Image.TabStop = false;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1206, 24);
            this.menuStrip1.TabIndex = 3;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.p1ToolStripMenuItem,
            this.p2ToolStripMenuItem,
            this.p3ToolStripMenuItem,
            this.camera1ToolStripMenuItem});
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.openToolStripMenuItem.Text = "Open";
            // 
            // p1ToolStripMenuItem
            // 
            this.p1ToolStripMenuItem.Name = "p1ToolStripMenuItem";
            this.p1ToolStripMenuItem.Size = new System.Drawing.Size(115, 22);
            this.p1ToolStripMenuItem.Text = "P1";
            this.p1ToolStripMenuItem.Click += new System.EventHandler(this.p1ToolStripMenuItem_Click);
            // 
            // p2ToolStripMenuItem
            // 
            this.p2ToolStripMenuItem.Name = "p2ToolStripMenuItem";
            this.p2ToolStripMenuItem.Size = new System.Drawing.Size(115, 22);
            this.p2ToolStripMenuItem.Text = "P2";
            this.p2ToolStripMenuItem.Click += new System.EventHandler(this.p2ToolStripMenuItem_Click);
            // 
            // p3ToolStripMenuItem
            // 
            this.p3ToolStripMenuItem.Name = "p3ToolStripMenuItem";
            this.p3ToolStripMenuItem.Size = new System.Drawing.Size(115, 22);
            this.p3ToolStripMenuItem.Text = "P3";
            this.p3ToolStripMenuItem.Click += new System.EventHandler(this.p3ToolStripMenuItem_Click);
            // 
            // camera1ToolStripMenuItem
            // 
            this.camera1ToolStripMenuItem.Name = "camera1ToolStripMenuItem";
            this.camera1ToolStripMenuItem.Size = new System.Drawing.Size(115, 22);
            this.camera1ToolStripMenuItem.Text = "Camera";
            this.camera1ToolStripMenuItem.Click += new System.EventHandler(this.camera1ToolStripMenuItem_Click);
            // 
            // Image_Shapes
            // 
            this.Image_Shapes.Location = new System.Drawing.Point(802, 27);
            this.Image_Shapes.Name = "Image_Shapes";
            this.Image_Shapes.Size = new System.Drawing.Size(382, 290);
            this.Image_Shapes.TabIndex = 4;
            this.Image_Shapes.TabStop = false;
            // 
            // roiBox
            // 
            this.roiBox.Location = new System.Drawing.Point(414, 27);
            this.roiBox.Name = "roiBox";
            this.roiBox.Size = new System.Drawing.Size(382, 290);
            this.roiBox.TabIndex = 5;
            this.roiBox.TabStop = false;
            // 
            // labelBoxNumber
            // 
            this.labelBoxNumber.AutoSize = true;
            this.labelBoxNumber.Location = new System.Drawing.Point(799, 346);
            this.labelBoxNumber.Name = "labelBoxNumber";
            this.labelBoxNumber.Size = new System.Drawing.Size(94, 13);
            this.labelBoxNumber.TabIndex = 6;
            this.labelBoxNumber.Text = "Number of Boxes: ";
            // 
            // labelTriagNumb
            // 
            this.labelTriagNumb.AutoSize = true;
            this.labelTriagNumb.Location = new System.Drawing.Point(799, 359);
            this.labelTriagNumb.Name = "labelTriagNumb";
            this.labelTriagNumb.Size = new System.Drawing.Size(108, 13);
            this.labelTriagNumb.TabIndex = 7;
            this.labelTriagNumb.Text = "Number of Triangles: ";
            // 
            // serialReturn
            // 
            this.serialReturn.AutoSize = true;
            this.serialReturn.Location = new System.Drawing.Point(9, 399);
            this.serialReturn.Name = "serialReturn";
            this.serialReturn.Size = new System.Drawing.Size(27, 13);
            this.serialReturn.TabIndex = 12;
            this.serialReturn.Text = "N/A";
            // 
            // comboCOMList
            // 
            this.comboCOMList.FormattingEnabled = true;
            this.comboCOMList.Location = new System.Drawing.Point(15, 346);
            this.comboCOMList.Name = "comboCOMList";
            this.comboCOMList.Size = new System.Drawing.Size(121, 21);
            this.comboCOMList.TabIndex = 13;
            this.comboCOMList.Text = "Open COM";
            this.comboCOMList.DropDown += new System.EventHandler(this.comboCOMList_DropDown);
            this.comboCOMList.SelectedIndexChanged += new System.EventHandler(this.comboCOMList_SelectedIndexChanged);
            // 
            // boxOverRideCommand
            // 
            this.boxOverRideCommand.AutoSize = true;
            this.boxOverRideCommand.Location = new System.Drawing.Point(12, 496);
            this.boxOverRideCommand.Name = "boxOverRideCommand";
            this.boxOverRideCommand.Size = new System.Drawing.Size(103, 17);
            this.boxOverRideCommand.TabIndex = 14;
            this.boxOverRideCommand.Text = "Force Run Math";
            this.boxOverRideCommand.UseVisualStyleBackColor = true;
            this.boxOverRideCommand.CheckedChanged += new System.EventHandler(this.boxOverRideCommand_CheckedChanged);
            // 
            // labelDistFromBot
            // 
            this.labelDistFromBot.AutoSize = true;
            this.labelDistFromBot.Location = new System.Drawing.Point(799, 386);
            this.labelDistFromBot.Name = "labelDistFromBot";
            this.labelDistFromBot.Size = new System.Drawing.Size(110, 13);
            this.labelDistFromBot.TabIndex = 16;
            this.labelDistFromBot.Text = "Target Dist to Bot (in):";
            // 
            // labelAngleToBot
            // 
            this.labelAngleToBot.AutoSize = true;
            this.labelAngleToBot.Location = new System.Drawing.Point(799, 399);
            this.labelAngleToBot.Name = "labelAngleToBot";
            this.labelAngleToBot.Size = new System.Drawing.Size(129, 13);
            this.labelAngleToBot.TabIndex = 15;
            this.labelAngleToBot.Text = "Target Angle to Bot (deg):";
            // 
            // buttonHome
            // 
            this.buttonHome.Location = new System.Drawing.Point(12, 467);
            this.buttonHome.Name = "buttonHome";
            this.buttonHome.Size = new System.Drawing.Size(75, 23);
            this.buttonHome.TabIndex = 17;
            this.buttonHome.Text = "Home";
            this.buttonHome.UseVisualStyleBackColor = true;
            this.buttonHome.Click += new System.EventHandler(this.buttonHome_Click);
            // 
            // buttonCalcROI
            // 
            this.buttonCalcROI.Location = new System.Drawing.Point(414, 346);
            this.buttonCalcROI.Name = "buttonCalcROI";
            this.buttonCalcROI.Size = new System.Drawing.Size(95, 23);
            this.buttonCalcROI.TabIndex = 18;
            this.buttonCalcROI.Text = "Recalculate ROI";
            this.buttonCalcROI.UseVisualStyleBackColor = true;
            this.buttonCalcROI.Click += new System.EventHandler(this.buttonCalcROI_Click);
            // 
            // checkBoxRunRobot
            // 
            this.checkBoxRunRobot.AutoSize = true;
            this.checkBoxRunRobot.Location = new System.Drawing.Point(12, 444);
            this.checkBoxRunRobot.Name = "checkBoxRunRobot";
            this.checkBoxRunRobot.Size = new System.Drawing.Size(78, 17);
            this.checkBoxRunRobot.TabIndex = 19;
            this.checkBoxRunRobot.Text = "Run Robot";
            this.checkBoxRunRobot.UseVisualStyleBackColor = true;
            this.checkBoxRunRobot.CheckedChanged += new System.EventHandler(this.checkBoxRunRobot_CheckedChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1206, 569);
            this.Controls.Add(this.checkBoxRunRobot);
            this.Controls.Add(this.buttonCalcROI);
            this.Controls.Add(this.buttonHome);
            this.Controls.Add(this.labelDistFromBot);
            this.Controls.Add(this.labelAngleToBot);
            this.Controls.Add(this.boxOverRideCommand);
            this.Controls.Add(this.comboCOMList);
            this.Controls.Add(this.serialReturn);
            this.Controls.Add(this.labelTriagNumb);
            this.Controls.Add(this.labelBoxNumber);
            this.Controls.Add(this.roiBox);
            this.Controls.Add(this.Image_Shapes);
            this.Controls.Add(this.Plain_Image);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.Plain_Image)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Image_Shapes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.roiBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox Plain_Image;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem p1ToolStripMenuItem;
        private System.Windows.Forms.PictureBox Image_Shapes;
        private System.Windows.Forms.ToolStripMenuItem p2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem camera1ToolStripMenuItem;
        private System.Windows.Forms.PictureBox roiBox;
        private System.Windows.Forms.ToolStripMenuItem p3ToolStripMenuItem;
        private System.Windows.Forms.Label labelBoxNumber;
        private System.Windows.Forms.Label labelTriagNumb;
        private System.Windows.Forms.Label serialReturn;
        private System.Windows.Forms.ComboBox comboCOMList;
        private System.Windows.Forms.CheckBox boxOverRideCommand;
        private System.Windows.Forms.Label labelDistFromBot;
        private System.Windows.Forms.Label labelAngleToBot;
        private System.Windows.Forms.Button buttonHome;
        private System.Windows.Forms.Button buttonCalcROI;
        private System.Windows.Forms.CheckBox checkBoxRunRobot;
    }
}

