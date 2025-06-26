namespace MX.ZS.PersonalFinances.WinForms
{
    partial class TransactionForm
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
            label1 = new Label();
            textBox1 = new TextBox();
            label2 = new Label();
            label3 = new Label();
            maskedTextBox1 = new MaskedTextBox();
            dateTimePicker1 = new DateTimePicker();
            checkBox1 = new CheckBox();
            richTextBox1 = new RichTextBox();
            comboBox1 = new ComboBox();
            label4 = new Label();
            button1 = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(96, 15);
            label1.TabIndex = 0;
            label1.Text = "Transaction TYPE";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(95, 35);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(138, 23);
            textBox1.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 38);
            label2.Name = "label2";
            label2.Size = new Size(42, 15);
            label2.TabIndex = 2;
            label2.Text = "Name:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 67);
            label3.Name = "label3";
            label3.Size = new Size(65, 15);
            label3.TabIndex = 4;
            label3.Text = "Ammount:";
            // 
            // maskedTextBox1
            // 
            maskedTextBox1.Location = new Point(95, 64);
            maskedTextBox1.Mask = "99999999.99";
            maskedTextBox1.Name = "maskedTextBox1";
            maskedTextBox1.Size = new Size(138, 23);
            maskedTextBox1.TabIndex = 5;
            maskedTextBox1.ValidatingType = typeof(int);
            // 
            // dateTimePicker1
            // 
            dateTimePicker1.Location = new Point(12, 93);
            dateTimePicker1.Name = "dateTimePicker1";
            dateTimePicker1.Size = new Size(221, 23);
            dateTimePicker1.TabIndex = 6;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Checked = true;
            checkBox1.CheckState = CheckState.Checked;
            checkBox1.Location = new Point(12, 122);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(67, 19);
            checkBox1.TabIndex = 7;
            checkBox1.Text = "Applied";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            richTextBox1.Location = new Point(12, 147);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(221, 96);
            richTextBox1.TabIndex = 8;
            richTextBox1.Text = "";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(95, 249);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(138, 23);
            comboBox1.TabIndex = 9;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 252);
            label4.Name = "label4";
            label4.Size = new Size(77, 15);
            label4.TabIndex = 10;
            label4.Text = "Income Type:";
            // 
            // button1
            // 
            button1.Location = new Point(158, 278);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 11;
            button1.Text = "Save";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // TransactionForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(245, 309);
            Controls.Add(button1);
            Controls.Add(label4);
            Controls.Add(comboBox1);
            Controls.Add(richTextBox1);
            Controls.Add(checkBox1);
            Controls.Add(dateTimePicker1);
            Controls.Add(maskedTextBox1);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(textBox1);
            Controls.Add(label1);
            MaximizeBox = false;
            MaximumSize = new Size(261, 348);
            MinimizeBox = false;
            MinimumSize = new Size(261, 348);
            Name = "TransactionForm";
            Text = "IncomeForm";
            Load += TransactionForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox textBox1;
        private Label label2;
        private Label label3;
        private MaskedTextBox maskedTextBox1;
        private DateTimePicker dateTimePicker1;
        private CheckBox checkBox1;
        private RichTextBox richTextBox1;
        private ComboBox comboBox1;
        private Label label4;
        private Button button1;
    }
}