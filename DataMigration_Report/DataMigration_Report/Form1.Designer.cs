namespace DataMigration_Report
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
            this.dataGroup = new System.Windows.Forms.GroupBox();
            this.dataOpenFolder = new System.Windows.Forms.Button();
            this.dataLabel = new System.Windows.Forms.Label();
            this.dataButton = new System.Windows.Forms.Button();
            this.dataPath = new System.Windows.Forms.TextBox();
            this.execGroup = new System.Windows.Forms.GroupBox();
            this.execLabelBetween = new System.Windows.Forms.Label();
            this.dateTimePickerFrom = new System.Windows.Forms.DateTimePicker();
            this.dateTimePickerTo = new System.Windows.Forms.DateTimePicker();
            this.execButton = new System.Windows.Forms.Button();
            this.execLabel = new System.Windows.Forms.Label();
            this.resultCount = new System.Windows.Forms.Label();
            this.quitButton = new System.Windows.Forms.Button();
            this.dataGroup.SuspendLayout();
            this.execGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGroup
            // 
            this.dataGroup.Controls.Add(this.dataOpenFolder);
            this.dataGroup.Controls.Add(this.dataLabel);
            this.dataGroup.Controls.Add(this.dataButton);
            this.dataGroup.Controls.Add(this.dataPath);
            this.dataGroup.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dataGroup.Location = new System.Drawing.Point(13, 12);
            this.dataGroup.Name = "dataGroup";
            this.dataGroup.Size = new System.Drawing.Size(776, 111);
            this.dataGroup.TabIndex = 11;
            this.dataGroup.TabStop = false;
            this.dataGroup.Text = "【一次テーブル取込】";
            // 
            // dataOpenFolder
            // 
            this.dataOpenFolder.Location = new System.Drawing.Point(531, 36);
            this.dataOpenFolder.Name = "dataOpenFolder";
            this.dataOpenFolder.Size = new System.Drawing.Size(90, 45);
            this.dataOpenFolder.TabIndex = 2;
            this.dataOpenFolder.Text = "参照";
            this.dataOpenFolder.UseVisualStyleBackColor = true;
            this.dataOpenFolder.Click += new System.EventHandler(this.dataOpenFolder_Click);
            // 
            // dataLabel
            // 
            this.dataLabel.AutoSize = true;
            this.dataLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.dataLabel.Location = new System.Drawing.Point(19, 50);
            this.dataLabel.Name = "dataLabel";
            this.dataLabel.Size = new System.Drawing.Size(134, 20);
            this.dataLabel.TabIndex = 1;
            this.dataLabel.Text = "移行データ格納先：";
            // 
            // dataButton
            // 
            this.dataButton.Location = new System.Drawing.Point(676, 36);
            this.dataButton.Name = "dataButton";
            this.dataButton.Size = new System.Drawing.Size(90, 45);
            this.dataButton.TabIndex = 3;
            this.dataButton.Text = "取込";
            this.dataButton.UseVisualStyleBackColor = true;
            this.dataButton.Click += new System.EventHandler(this.dataButton_Click);
            // 
            // dataPath
            // 
            this.dataPath.Location = new System.Drawing.Point(159, 46);
            this.dataPath.Name = "dataPath";
            this.dataPath.Size = new System.Drawing.Size(370, 26);
            this.dataPath.TabIndex = 1;
            // 
            // execGroup
            // 
            this.execGroup.Controls.Add(this.execLabelBetween);
            this.execGroup.Controls.Add(this.dateTimePickerFrom);
            this.execGroup.Controls.Add(this.dateTimePickerTo);
            this.execGroup.Controls.Add(this.execButton);
            this.execGroup.Controls.Add(this.execLabel);
            this.execGroup.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.execGroup.Location = new System.Drawing.Point(13, 129);
            this.execGroup.Name = "execGroup";
            this.execGroup.Size = new System.Drawing.Size(776, 113);
            this.execGroup.TabIndex = 10;
            this.execGroup.TabStop = false;
            this.execGroup.Text = "【データ移行】";
            // 
            // execLabelBetween
            // 
            this.execLabelBetween.AutoSize = true;
            this.execLabelBetween.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.execLabelBetween.Location = new System.Drawing.Point(328, 47);
            this.execLabelBetween.Name = "execLabelBetween";
            this.execLabelBetween.Size = new System.Drawing.Size(25, 20);
            this.execLabelBetween.TabIndex = 14;
            this.execLabelBetween.Text = "～";
            // 
            // dateTimePickerFrom
            // 
            this.dateTimePickerFrom.Location = new System.Drawing.Point(159, 44);
            this.dateTimePickerFrom.Name = "dateTimePickerFrom";
            this.dateTimePickerFrom.Size = new System.Drawing.Size(160, 26);
            this.dateTimePickerFrom.TabIndex = 4;
            this.dateTimePickerFrom.ValueChanged += new System.EventHandler(this.dateTimePickerFrom_ValueChanged);
            // 
            // dateTimePickerTo
            // 
            this.dateTimePickerTo.Location = new System.Drawing.Point(361, 44);
            this.dateTimePickerTo.Name = "dateTimePickerTo";
            this.dateTimePickerTo.Size = new System.Drawing.Size(160, 26);
            this.dateTimePickerTo.TabIndex = 5;
            this.dateTimePickerTo.ValueChanged += new System.EventHandler(this.dateTimePickerTo_ValueChanged);
            // 
            // execButton
            // 
            this.execButton.Location = new System.Drawing.Point(676, 37);
            this.execButton.Name = "execButton";
            this.execButton.Size = new System.Drawing.Size(90, 45);
            this.execButton.TabIndex = 6;
            this.execButton.Text = "移行実行";
            this.execButton.UseVisualStyleBackColor = true;
            this.execButton.Click += new System.EventHandler(this.execButton_Click);
            // 
            // execLabel
            // 
            this.execLabel.AutoSize = true;
            this.execLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.execLabel.Location = new System.Drawing.Point(56, 47);
            this.execLabel.Name = "execLabel";
            this.execLabel.Size = new System.Drawing.Size(97, 20);
            this.execLabel.TabIndex = 8;
            this.execLabel.Text = "対象検査日：";
            // 
            // resultCount
            // 
            this.resultCount.AutoSize = true;
            this.resultCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.resultCount.Location = new System.Drawing.Point(552, 279);
            this.resultCount.Name = "resultCount";
            this.resultCount.Size = new System.Drawing.Size(106, 20);
            this.resultCount.TabIndex = 9;
            this.resultCount.Text = "移行件数：0件";
            // 
            // quitButton
            // 
            this.quitButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.quitButton.Location = new System.Drawing.Point(689, 267);
            this.quitButton.Name = "quitButton";
            this.quitButton.Size = new System.Drawing.Size(90, 45);
            this.quitButton.TabIndex = 10;
            this.quitButton.Text = "終了";
            this.quitButton.UseVisualStyleBackColor = true;
            this.quitButton.Click += new System.EventHandler(this.quitButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 334);
            this.Controls.Add(this.dataGroup);
            this.Controls.Add(this.execGroup);
            this.Controls.Add(this.resultCount);
            this.Controls.Add(this.quitButton);
            this.Name = "Form1";
            this.Text = "レポートデータ移行ツール";
            this.dataGroup.ResumeLayout(false);
            this.dataGroup.PerformLayout();
            this.execGroup.ResumeLayout(false);
            this.execGroup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label dataLabel;
        private System.Windows.Forms.Button dataButton;
        private System.Windows.Forms.TextBox dataPath;
        private System.Windows.Forms.Button quitButton;
        private System.Windows.Forms.Button execButton;
        private System.Windows.Forms.Label execLabel;
        private System.Windows.Forms.Label resultCount;
        private System.Windows.Forms.GroupBox execGroup;
        private System.Windows.Forms.GroupBox dataGroup;
        private System.Windows.Forms.Button dataOpenFolder;
        private System.Windows.Forms.DateTimePicker dateTimePickerFrom;
        private System.Windows.Forms.DateTimePicker dateTimePickerTo;
        private System.Windows.Forms.Label execLabelBetween;
    }
}

