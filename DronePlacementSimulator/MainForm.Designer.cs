namespace DronePlacementSimulator
{
    partial class MainForm
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelOverallSurvivalRate = new System.Windows.Forms.Label();
            this.labelOverallSurvivalRateValue = new System.Windows.Forms.Label();
            this.labelDeliveryMissValue = new System.Windows.Forms.Label();
            this.labelDeliveryMiss = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileFToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.savePlacementToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.placementToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.kMeansToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pulverToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.boutilierToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rubisToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sImulationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runF10ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripComboBoxStations = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripComboBoxPolicy = new System.Windows.Forms.ToolStripComboBox();
            this.menuStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelOverallSurvivalRate
            // 
            this.labelOverallSurvivalRate.AutoSize = true;
            this.labelOverallSurvivalRate.Font = new System.Drawing.Font("Times New Roman", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.labelOverallSurvivalRate.ForeColor = System.Drawing.Color.Red;
            this.labelOverallSurvivalRate.Location = new System.Drawing.Point(12, 77);
            this.labelOverallSurvivalRate.Name = "labelOverallSurvivalRate";
            this.labelOverallSurvivalRate.Size = new System.Drawing.Size(133, 18);
            this.labelOverallSurvivalRate.TabIndex = 0;
            this.labelOverallSurvivalRate.Text = "Survival Rate:";
            this.labelOverallSurvivalRate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelOverallSurvivalRateValue
            // 
            this.labelOverallSurvivalRateValue.AutoSize = true;
            this.labelOverallSurvivalRateValue.Font = new System.Drawing.Font("Times New Roman", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.labelOverallSurvivalRateValue.ForeColor = System.Drawing.Color.Red;
            this.labelOverallSurvivalRateValue.Location = new System.Drawing.Point(151, 77);
            this.labelOverallSurvivalRateValue.Name = "labelOverallSurvivalRateValue";
            this.labelOverallSurvivalRateValue.Size = new System.Drawing.Size(85, 18);
            this.labelOverallSurvivalRateValue.TabIndex = 1;
            this.labelOverallSurvivalRateValue.Text = "100.00%";
            this.labelOverallSurvivalRateValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelDeliveryMissValue
            // 
            this.labelDeliveryMissValue.AutoSize = true;
            this.labelDeliveryMissValue.Font = new System.Drawing.Font("Times New Roman", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.labelDeliveryMissValue.ForeColor = System.Drawing.Color.Blue;
            this.labelDeliveryMissValue.Location = new System.Drawing.Point(151, 105);
            this.labelDeliveryMissValue.Name = "labelDeliveryMissValue";
            this.labelDeliveryMissValue.Size = new System.Drawing.Size(19, 18);
            this.labelDeliveryMissValue.TabIndex = 3;
            this.labelDeliveryMissValue.Text = "0";
            this.labelDeliveryMissValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelDeliveryMiss
            // 
            this.labelDeliveryMiss.AutoSize = true;
            this.labelDeliveryMiss.Font = new System.Drawing.Font("Times New Roman", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.labelDeliveryMiss.ForeColor = System.Drawing.Color.Blue;
            this.labelDeliveryMiss.Location = new System.Drawing.Point(12, 105);
            this.labelDeliveryMiss.Name = "labelDeliveryMiss";
            this.labelDeliveryMiss.Size = new System.Drawing.Size(135, 18);
            this.labelDeliveryMiss.TabIndex = 2;
            this.labelDeliveryMiss.Text = "Delivery Miss:";
            this.labelDeliveryMiss.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileFToolStripMenuItem,
            this.placementToolStripMenuItem,
            this.sImulationToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1120, 33);
            this.menuStrip1.TabIndex = 4;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileFToolStripMenuItem
            // 
            this.fileFToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.savePlacementToolStripMenuItem,
            this.openToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileFToolStripMenuItem.Name = "fileFToolStripMenuItem";
            this.fileFToolStripMenuItem.ShortcutKeyDisplayString = "F";
            this.fileFToolStripMenuItem.Size = new System.Drawing.Size(76, 29);
            this.fileFToolStripMenuItem.Text = "File (F)";
            // 
            // savePlacementToolStripMenuItem
            // 
            this.savePlacementToolStripMenuItem.Name = "savePlacementToolStripMenuItem";
            this.savePlacementToolStripMenuItem.ShortcutKeyDisplayString = "S";
            this.savePlacementToolStripMenuItem.Size = new System.Drawing.Size(248, 30);
            this.savePlacementToolStripMenuItem.Text = "Save placement";
            this.savePlacementToolStripMenuItem.Click += new System.EventHandler(this.savePlacementToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeyDisplayString = "L";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(248, 30);
            this.openToolStripMenuItem.Text = "Load placement";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeyDisplayString = "X";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(248, 30);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ClickExit);
            // 
            // placementToolStripMenuItem
            // 
            this.placementToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.kMeansToolStripMenuItem,
            this.pulverToolStripMenuItem,
            this.boutilierToolStripMenuItem,
            this.rubisToolStripMenuItem});
            this.placementToolStripMenuItem.Name = "placementToolStripMenuItem";
            this.placementToolStripMenuItem.ShortcutKeyDisplayString = "P";
            this.placementToolStripMenuItem.Size = new System.Drawing.Size(134, 29);
            this.placementToolStripMenuItem.Text = "Placement (P)";
            // 
            // kMeansToolStripMenuItem
            // 
            this.kMeansToolStripMenuItem.Name = "kMeansToolStripMenuItem";
            this.kMeansToolStripMenuItem.ShortcutKeyDisplayString = "K";
            this.kMeansToolStripMenuItem.Size = new System.Drawing.Size(191, 30);
            this.kMeansToolStripMenuItem.Text = "K-Means";
            this.kMeansToolStripMenuItem.Click += new System.EventHandler(this.ClickPlacementItems);
            // 
            // pulverToolStripMenuItem
            // 
            this.pulverToolStripMenuItem.Name = "pulverToolStripMenuItem";
            this.pulverToolStripMenuItem.ShortcutKeyDisplayString = "V";
            this.pulverToolStripMenuItem.Size = new System.Drawing.Size(191, 30);
            this.pulverToolStripMenuItem.Text = "Pulver";
            this.pulverToolStripMenuItem.Click += new System.EventHandler(this.ClickPlacementItems);
            // 
            // boutilierToolStripMenuItem
            // 
            this.boutilierToolStripMenuItem.Name = "boutilierToolStripMenuItem";
            this.boutilierToolStripMenuItem.ShortcutKeyDisplayString = "B";
            this.boutilierToolStripMenuItem.Size = new System.Drawing.Size(191, 30);
            this.boutilierToolStripMenuItem.Text = "Boutilier";
            this.boutilierToolStripMenuItem.Click += new System.EventHandler(this.ClickPlacementItems);
            // 
            // rubisToolStripMenuItem
            // 
            this.rubisToolStripMenuItem.Name = "rubisToolStripMenuItem";
            this.rubisToolStripMenuItem.ShortcutKeyDisplayString = "R";
            this.rubisToolStripMenuItem.Size = new System.Drawing.Size(191, 30);
            this.rubisToolStripMenuItem.Text = "RUBIS";
            this.rubisToolStripMenuItem.Click += new System.EventHandler(this.ClickPlacementItems);
            // 
            // sImulationToolStripMenuItem
            // 
            this.sImulationToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runF10ToolStripMenuItem});
            this.sImulationToolStripMenuItem.Name = "sImulationToolStripMenuItem";
            this.sImulationToolStripMenuItem.ShortcutKeyDisplayString = "S";
            this.sImulationToolStripMenuItem.Size = new System.Drawing.Size(135, 29);
            this.sImulationToolStripMenuItem.Text = "SImulation (S)";
            // 
            // runF10ToolStripMenuItem
            // 
            this.runF10ToolStripMenuItem.Name = "runF10ToolStripMenuItem";
            this.runF10ToolStripMenuItem.Size = new System.Drawing.Size(172, 30);
            this.runF10ToolStripMenuItem.Text = "Run (F10)";
            this.runF10ToolStripMenuItem.Click += new System.EventHandler(this.ClickRunSimulation);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.toolStripComboBoxStations,
            this.toolStripLabel2,
            this.toolStripComboBoxPolicy});
            this.toolStrip1.Location = new System.Drawing.Point(0, 33);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1120, 33);
            this.toolStrip1.TabIndex = 5;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(76, 30);
            this.toolStripLabel1.Text = "Stations";
            // 
            // toolStripComboBoxStations
            // 
            this.toolStripComboBoxStations.AutoSize = false;
            this.toolStripComboBoxStations.Items.AddRange(new object[] {
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30"});
            this.toolStripComboBoxStations.Name = "toolStripComboBoxStations";
            this.toolStripComboBoxStations.Size = new System.Drawing.Size(60, 33);
            this.toolStripComboBoxStations.SelectedIndexChanged += new System.EventHandler(this.toolStripComboBoxStations_SelectedIndexChanged);
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(59, 30);
            this.toolStripLabel2.Text = "Policy";
            // 
            // toolStripComboBoxPolicy
            // 
            this.toolStripComboBoxPolicy.DropDownWidth = 200;
            this.toolStripComboBoxPolicy.Items.AddRange(new object[] {
            "Nearest Station First",
            "Highest Survival Rate Station First"});
            this.toolStripComboBoxPolicy.Name = "toolStripComboBoxPolicy";
            this.toolStripComboBoxPolicy.Size = new System.Drawing.Size(275, 33);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1120, 902);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.labelDeliveryMissValue);
            this.Controls.Add(this.labelDeliveryMiss);
            this.Controls.Add(this.labelOverallSurvivalRateValue);
            this.Controls.Add(this.labelOverallSurvivalRate);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Drone Placement Simulator";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.MainForm_Paint);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion
        private System.Windows.Forms.Label labelOverallSurvivalRate;
        private System.Windows.Forms.Label labelOverallSurvivalRateValue;
        private System.Windows.Forms.Label labelDeliveryMissValue;
        private System.Windows.Forms.Label labelDeliveryMiss;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileFToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem placementToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem kMeansToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pulverToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem boutilierToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rubisToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sImulationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem runF10ToolStripMenuItem;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBoxStations;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem savePlacementToolStripMenuItem;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBoxPolicy;
    }
}