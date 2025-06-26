using MX.ZS.PersonalFinances.BLL;
using MX.ZS.PersonalFinances.Domain.Entities;
using MX.ZS.PersonalFinances.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MX.ZS.PersonalFinances.WinForms
{
    public delegate void UpdateAccountTransactions();
    public partial class TransactionForm : Form
    {
        Transaction _entity;
        IProfileManager _profileManager;
        Account _account;
        event UpdateAccountTransactions _OnUpdateAccountTransactions;
        public TransactionForm(Transaction entity, ref IProfileManager profileManager, ref Account account, UpdateAccountTransactions onUpdateAccountTransactions)
        {
            _OnUpdateAccountTransactions = onUpdateAccountTransactions;
            _entity = entity;
            _profileManager = profileManager;
            _account = account;
            InitializeComponent();
        }

        private void TransactionForm_Load(object sender, EventArgs e)
        {
            comboBox1.DataSource = Enum.GetNames(typeof(eIncomeType));
            switch (_entity)
            {
                case Income:
                    this.Text = "Add Income";
                    break;
                case Expense:
                    this.Text = "Add Expense";
                    label4.Visible = false;
                    comboBox1.Visible = false;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _entity.Name = textBox1.Text;
            _entity.Ammount = decimal.Parse(maskedTextBox1.Text);
            _entity.Applied = checkBox1.Checked;
            _entity.Comments = richTextBox1.Text;
            switch (_entity)
            {
                case Income:
                    ((Income)_entity).IncomeType = (eIncomeType)Enum.Parse(typeof(eIncomeType), comboBox1.SelectedValue.ToString());
                    break;
                case Expense:
                    _entity.Ammount = _entity.Ammount * -1m;
                    break;
            }
            _entity.Date = dateTimePicker1.Value;
            _account.Transactions.Add(_entity);
            _profileManager.UpdateAccount(_account);
            _OnUpdateAccountTransactions.Invoke();
            this.Close();
        }

        private void maskedTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(maskedTextBox1.Text, @"^\d*\.?\d*$"))
            {
                // Remove last entered character or alert
                maskedTextBox1.Text = maskedTextBox1.Text.Remove(maskedTextBox1.Text.Length - 1);
                maskedTextBox1.SelectionStart = maskedTextBox1.Text.Length;
            }
        }

        private void maskedTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            // Allow control keys (backspace, delete, etc.)
            if (char.IsControl(e.KeyChar))
            {
                return;
            }

            // Allow digits
            if (char.IsDigit(e.KeyChar))
            {
                return;
            }

            // Allow one dot (.)
            if (e.KeyChar == '.')
            {
                // If textbox already contains a dot, block input
                if (textBox.Text.Contains("."))
                {
                    e.Handled = true;
                }
                return;
            }

            // Block anything else
            e.Handled = true;
        }
    }
}
