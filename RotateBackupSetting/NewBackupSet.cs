using LiteDB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RotateBackupSetting
{
    
    public partial class NewBackupSet : Form
    {
        public NewBackupSet()
        {
            InitializeComponent();
            radioButtonDirectory.Checked = true;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            bool cont = true;

            if (textBoxCommand.Text == "")
            {
                label1.ForeColor = Color.Red;
                cont = false;
            }
            else
            {
                if (textBoxCommand.Text.Contains(" "))
                {
                    label1.ForeColor = Color.Red;
                    cont = false;
                }
                else
                {
                    label1.ForeColor = SystemColors.ControlText;
                }
            }


            if (cont)
            {
                using (var db = new LiteDatabase(@"BackupSet.db"))
                {
                    // Get a collection (or create, if doesn't exist)
                    var col = db.GetCollection<BackupSetting>("backup");
                    var _item = col.FindOne(Query.EQ("Command", textBoxCommand.Text));

                    if (_item == null)
                    {
                        var bsetting = new BackupSetting
                        {
                            Command = textBoxCommand.Text,
                            Remark = textBoxRemark.Text,
                            isDirectory = (radioButtonDirectory.Checked) ? true : false,
                            // recordId = RandomString(16),
                            _id = RandomString(16),
                            maxPath = 5
                        };

                        col.Insert(bsetting);
                        MessageBox.Show("Backup Set Saved");

                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Command Name Already Used!");
                    }
                }
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private string RandomString(int length)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            var finalString = new String(stringChars);
            return finalString;
        }

    }
}
