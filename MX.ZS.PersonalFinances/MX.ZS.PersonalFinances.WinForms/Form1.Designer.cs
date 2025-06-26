namespace MX.ZS.PersonalFinances.WinForms
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new Button();
            button3 = new Button();
            label3 = new Label();
            textBox1 = new TextBox();
            comboBox1 = new ComboBox();
            panel1 = new Panel();
            label1 = new Label();
            panel2 = new Panel();
            button6 = new Button();
            button5 = new Button();
            button4 = new Button();
            button2 = new Button();
            label2 = new Label();
            dataGridView1 = new DataGridView();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(3, 56);
            button1.Name = "button1";
            button1.Size = new Size(163, 23);
            button1.TabIndex = 0;
            button1.Text = "Add new Account";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button3
            // 
            button3.Location = new Point(472, 26);
            button3.Name = "button3";
            button3.Size = new Size(155, 23);
            button3.TabIndex = 2;
            button3.Text = "Delete current Account";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(172, 60);
            label3.Name = "label3";
            label3.Size = new Size(117, 15);
            label3.TabIndex = 5;
            label3.Text = "New Account Name:";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(295, 57);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(332, 23);
            textBox1.TabIndex = 6;
            textBox1.TextChanged += textBox1_Interacted;
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(3, 28);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(463, 23);
            comboBox1.TabIndex = 7;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // panel1
            // 
            panel1.Controls.Add(label1);
            panel1.Controls.Add(comboBox1);
            panel1.Controls.Add(button1);
            panel1.Controls.Add(textBox1);
            panel1.Controls.Add(button3);
            panel1.Controls.Add(label3);
            panel1.Location = new Point(12, 12);
            panel1.Name = "panel1";
            panel1.Size = new Size(634, 100);
            panel1.TabIndex = 8;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 10);
            label1.Name = "label1";
            label1.Size = new Size(57, 15);
            label1.TabIndex = 8;
            label1.Text = "Accounts";
            // 
            // panel2
            // 
            panel2.Controls.Add(button6);
            panel2.Controls.Add(button5);
            panel2.Controls.Add(button4);
            panel2.Controls.Add(button2);
            panel2.Controls.Add(label2);
            panel2.Controls.Add(dataGridView1);
            panel2.Location = new Point(12, 118);
            panel2.Name = "panel2";
            panel2.Size = new Size(634, 550);
            panel2.TabIndex = 9;
            // 
            // button6
            // 
            button6.Location = new Point(185, 7);
            button6.Name = "button6";
            button6.Size = new Size(98, 23);
            button6.TabIndex = 12;
            button6.Text = "Add Expense";
            button6.UseVisualStyleBackColor = true;
            button6.Click += button6_Click;
            // 
            // button5
            // 
            button5.Enabled = false;
            button5.Location = new Point(458, 7);
            button5.Name = "button5";
            button5.Size = new Size(163, 23);
            button5.TabIndex = 11;
            button5.Text = "Delete Transaction";
            button5.UseVisualStyleBackColor = true;
            // 
            // button4
            // 
            button4.Enabled = false;
            button4.Location = new Point(289, 7);
            button4.Name = "button4";
            button4.Size = new Size(163, 23);
            button4.TabIndex = 10;
            button4.Text = "Update Transaction";
            button4.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.Location = new Point(81, 7);
            button2.Name = "button2";
            button2.Size = new Size(98, 23);
            button2.TabIndex = 9;
            button2.Text = "Add Income";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(3, 11);
            label2.Name = "label2";
            label2.Size = new Size(72, 15);
            label2.TabIndex = 9;
            label2.Text = "Transactions";
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(3, 39);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = true;
            dataGridView1.Size = new Size(628, 508);
            dataGridView1.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1713, 680);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "Form1";
            Text = "Form1";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button button1;
        private Button button3;
        private Label label3;
        private TextBox textBox1;
        private ComboBox comboBox1;
        private Panel panel1;
        private Label label1;
        private Panel panel2;
        private Label label2;
        private DataGridView dataGridView1;
        private Button button4;
        private Button button2;
        private Button button5;
        private Button button6;
    }
}
