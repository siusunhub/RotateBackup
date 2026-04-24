using LiteDB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RotateBackupSetting
{


    public partial class Form1 : Form
    {
        List<string> itemkey = new List<string>();
        bool selectedIsDirectory = false;
        string selectedID = "";
        string selectedCommad = "";

        public Form1()
        {
            InitializeComponent();
            updateSelectMenu();

            panel1.Visible = false;
            comboBoxMaxRoll.Enabled = false;
            textBoxPreCommand.Enabled = false;
            textBoxPostCommand.Enabled = false;
            checkBoxDisable.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            NewBackupSet sset = new NewBackupSet();
            sset.ShowDialog();
            updateSelectMenu();
        }

        private void updateMaxPathScreen()
        {
            if (comboBoxMaxRoll.SelectedIndex == -1)
            {
                panel1.Visible = false;

            }
            else
            {
                panel1.Visible = true;
                int _show = comboBoxMaxRoll.SelectedIndex + 1;

                if (selectedIsDirectory)
                {
                    labelMain.Visible = true;
                    textBoxMain.Visible = true;
                    pictureBoxMain.Visible = true;
                    checkBoxKeep.Visible = true;
                    checkBoxKeepFirstDir.Visible = true;

                    labelPath1.Text = "Backup 1:";
                    labelPath2.Text = "Backup 2:";
                    labelPath3.Text = "Backup 3:";
                    labelPath4.Text = "Backup 4:";
                    labelPath5.Text = "Backup 5:";
                    labelPath6.Text = "Backup 6:";
                    labelPath7.Text = "Backup 7:";
                    labelPath8.Text = "Backup 8:";
                    labelPath9.Text = "Backup 9:";
                    labelPath10.Text = "Backup 10:";
                    labelPath11.Text = "Backup 11:";
                    labelPath12.Text = "Backup 12:";
                    labelPath13.Text = "Backup 13:";
                    labelPath14.Text = "Backup 14:";


                    if (_show >= 1)
                    {
                        labelPath1.Visible = true;
                        textBoxPath1.Visible = true;
                        pictureBox1.Visible = true;
                        checkBoxKeep1.Visible = true;
                    }
                    else
                    {
                        labelPath1.Visible = false;
                        textBoxPath1.Visible = false;
                        pictureBox1.Visible = false;
                        checkBoxKeep1.Visible = false;
                    }

                    if (_show >= 2)
                    {
                        labelPath2.Visible = true;
                        textBoxPath2.Visible = true;
                        pictureBox2.Visible = true;
                        checkBoxKeep2.Visible = true;
                    }
                    else
                    {
                        labelPath2.Visible = false;
                        textBoxPath2.Visible = false;
                        pictureBox2.Visible = false;
                        checkBoxKeep2.Visible = false;
                    }
                    if (_show >= 3)
                    {
                        labelPath3.Visible = true;
                        textBoxPath3.Visible = true;
                        pictureBox3.Visible = true;
                        checkBoxKeep3.Visible = true;
                    }
                    else
                    {
                        labelPath3.Visible = false;
                        textBoxPath3.Visible = false;
                        pictureBox3.Visible = false;
                        checkBoxKeep3.Visible = false;
                    }
                    if (_show >= 4)
                    {
                        labelPath4.Visible = true;
                        textBoxPath4.Visible = true;
                        pictureBox4.Visible = true;
                        checkBoxKeep4.Visible = true;
                    }
                    else
                    {
                        labelPath4.Visible = false;
                        textBoxPath4.Visible = false;
                        pictureBox4.Visible = false;
                        checkBoxKeep4.Visible = false;
                    }
                    if (_show >= 5)
                    {
                        labelPath5.Visible = true;
                        textBoxPath5.Visible = true;
                        pictureBox5.Visible = true;
                        checkBoxKeep5.Visible = true;
                    }
                    else
                    {
                        labelPath5.Visible = false;
                        textBoxPath5.Visible = false;
                        pictureBox5.Visible = false;
                        checkBoxKeep5.Visible = false;
                    }

                    if (_show >= 6)
                    {
                        labelPath6.Visible = true;
                        textBoxPath6.Visible = true;
                        pictureBox6.Visible = true;
                        checkBoxKeep6.Visible = true;
                    }
                    else
                    {
                        labelPath6.Visible = false;
                        textBoxPath6.Visible = false;
                        pictureBox6.Visible = false;
                        checkBoxKeep6.Visible = true;
                    }
                    if (_show >= 7)
                    {
                        labelPath7.Visible = true;
                        textBoxPath7.Visible = true;
                        pictureBox7.Visible = true;
                        checkBoxKeep7.Visible = true;
                    }
                    else
                    {
                        labelPath7.Visible = false;
                        textBoxPath7.Visible = false;
                        pictureBox7.Visible = false;
                        checkBoxKeep7.Visible = false;
                    }
                    if (_show >= 8)
                    {
                        labelPath8.Visible = true;
                        textBoxPath8.Visible = true;
                        pictureBox8.Visible = true;
                        checkBoxKeep8.Visible = true;
                    }
                    else
                    {
                        labelPath8.Visible = false;
                        textBoxPath8.Visible = false;
                        pictureBox8.Visible = false;
                        checkBoxKeep8.Visible = false;
                    }

                    if (_show >= 9)
                    {
                        labelPath9.Visible = true;
                        textBoxPath9.Visible = true;
                        pictureBox9.Visible = true;
                        checkBoxKeep9.Visible = true;
                    }
                    else
                    {
                        labelPath9.Visible = false;
                        textBoxPath9.Visible = false;
                        pictureBox9.Visible = false;
                        checkBoxKeep9.Visible = false;
                    }
                    if (_show >= 10)
                    {
                        labelPath10.Visible = true;
                        textBoxPath10.Visible = true;
                        pictureBox10.Visible = true;
                        checkBoxKeep10.Visible = true;
                    }
                    else
                    {
                        labelPath10.Visible = false;
                        textBoxPath10.Visible = false;
                        pictureBox10.Visible = false;
                        checkBoxKeep10.Visible = false;
                    }
                    if (_show >= 11)
                    {
                        labelPath11.Visible = true;
                        textBoxPath11.Visible = true;
                        pictureBox11.Visible = true;
                        checkBoxKeep11.Visible = true;
                    }
                    else
                    {
                        labelPath11.Visible = false;
                        textBoxPath11.Visible = false;
                        pictureBox11.Visible = false;
                        checkBoxKeep11.Visible = false;
                    }
                    if (_show >= 12)
                    {
                        labelPath12.Visible = true;
                        textBoxPath12.Visible = true;
                        pictureBox12.Visible = true;
                        checkBoxKeep12.Visible = true;
                    }
                    else
                    {
                        labelPath12.Visible = false;
                        textBoxPath12.Visible = false;
                        pictureBox12.Visible = false;
                        checkBoxKeep12.Visible = false;
                    }
                    if (_show >= 13)
                    {
                        labelPath13.Visible = true;
                        textBoxPath13.Visible = true;
                        pictureBox13.Visible = true;
                        checkBoxKeep13.Visible = true;
                    }
                    else
                    {
                        labelPath13.Visible = false;
                        textBoxPath13.Visible = false;
                        pictureBox13.Visible = false;
                        checkBoxKeep13.Visible = false;
                    }
                    if (_show >= 14)
                    {
                        labelPath14.Visible = true;
                        textBoxPath14.Visible = true;
                        pictureBox14.Visible = true;
                        checkBoxKeep14.Visible = true;
                    }
                    else
                    {
                        labelPath14.Visible = false;
                        textBoxPath14.Visible = false;
                        pictureBox14.Visible = false;
                        checkBoxKeep14.Visible = false;
                    }
                } else
                {
                    labelMain.Visible = false;
                    textBoxMain.Visible = false;
                    pictureBoxMain.Visible = false;
                    checkBoxKeep.Visible = false;
                    checkBoxKeepFirstDir.Visible = false;

                    labelPath1.Text = "File 1:";
                    labelPath2.Text = "File 2:";
                    labelPath3.Text = "File 3:";
                    labelPath4.Text = "File 4:";
                    labelPath5.Text = "File 5:";
                    labelPath6.Text = "File 6:";
                    labelPath7.Text = "File 7:";
                    labelPath8.Text = "File 8:";
                    labelPath9.Text = "File 9:";
                    labelPath10.Text = "File 10:";
                    labelPath11.Text = "File 11:";
                    labelPath12.Text = "File 12:";
                    labelPath13.Text = "File 13:";
                    labelPath14.Text = "File 14:";

                    labelPath1.Visible = true;
                    textBoxPath1.Visible = true;
                    pictureBox1.Visible = true;
                    labelPath2.Visible = true;
                    textBoxPath2.Visible = true;
                    pictureBox2.Visible = true;
                    labelPath3.Visible = true;
                    textBoxPath3.Visible = true;
                    pictureBox3.Visible = true;
                    labelPath4.Visible = true;
                    textBoxPath4.Visible = true;
                    pictureBox4.Visible = true;
                    labelPath5.Visible = true;
                    textBoxPath5.Visible = true;
                    pictureBox5.Visible = true;
                    labelPath6.Visible = true;
                    textBoxPath6.Visible = true;
                    pictureBox6.Visible = true;
                    labelPath7.Visible = true;
                    textBoxPath7.Visible = true;
                    pictureBox7.Visible = true;
                    labelPath8.Visible = true;
                    textBoxPath8.Visible = true;
                    pictureBox8.Visible = true;
                    labelPath9.Visible = true;
                    textBoxPath9.Visible = true;
                    pictureBox9.Visible = true;

                    labelPath10.Visible = true;
                    textBoxPath10.Visible = true;
                    pictureBox10.Visible = true;

                    labelPath11.Visible = true;
                    textBoxPath11.Visible = true;
                    pictureBox11.Visible = true;

                    labelPath12.Visible = true;
                    textBoxPath12.Visible = true;
                    pictureBox12.Visible = true;
                    labelPath13.Visible = true;
                    textBoxPath13.Visible = true;
                    pictureBox13.Visible = true;
                    labelPath14.Visible = true;
                    textBoxPath14.Visible = true;
                    pictureBox14.Visible = true;
                }
                       
            }
        }


        private void updateSelectMenu()
        {
            using (var db = new LiteDatabase(@"BackupSet.db"))
            {
                // Get a collection (or create, if doesn't exist)
                var col = db.GetCollection<BackupSetting>("backup");

                comboBoxList.Items.Clear();
                itemkey.Clear();


                foreach (var _setting in col.FindAll())
                {
                    string _df = (_setting.isDirectory) ? "[D] " : "[F] ";
                    string _s = _df + _setting.Command + " (" + _setting.Remark + ")";

                    comboBoxList.Items.Add(_s);
                    itemkey.Add(_setting._id);

                }

                comboBoxList.SelectedIndex = -1;
                comboBoxMaxRoll.Enabled = false;
                textBoxPreCommand.Enabled = false;
                textBoxPostCommand.Enabled = false;
                checkBoxDisable.Enabled = false;
                selectedID = "";
                selectedIsDirectory = false;
                panel1.Visible = false;
            }

        }

        private void comboBoxList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxList.SelectedIndex >= 0)
            {
                int _pathindex = comboBoxList.SelectedIndex;
                string _skey = itemkey[_pathindex];

                using (var db = new LiteDatabase(@"BackupSet.db"))
                {
                    // Get a collection (or create, if doesn't exist)
                    var col = db.GetCollection<BackupSetting>("backup");

                    var _item = col.FindOne(Query.EQ("_id", _skey));

                    if (_item == null)
                    {
                        comboBoxMaxRoll.Enabled = false;
                        textBoxPreCommand.Enabled = false;
                        textBoxPostCommand.Enabled = false;
                        checkBoxDisable.Enabled = false;
                        selectedID = "";
                        selectedIsDirectory = false;
                        panel1.Visible = false;
                        return;
                    }

                    comboBoxMaxRoll.Enabled = true;
                    textBoxPreCommand.Enabled = true;
                    textBoxPostCommand.Enabled = true;
                    checkBoxDisable.Enabled = true;

                    selectedIsDirectory = _item.isDirectory;
                    selectedID = _item._id;
                    selectedCommad = _item.Command;
                    int _maxPath = _item.maxPath;

                    comboBoxMaxRoll.SelectedIndex = _maxPath - 1;

                    textBoxMain.Text = (selectedIsDirectory) ? _item.mainPath : "";
                    textBoxPath1.Text = (_maxPath >= 1) ? _item.Path1 : "";
                    textBoxPath2.Text = (_maxPath >= 2) ? _item.Path2 : "";
                    textBoxPath3.Text = (_maxPath >= 3) ? _item.Path3 : "";
                    textBoxPath4.Text = (_maxPath >= 4) ? _item.Path4 : "";
                    textBoxPath5.Text = (_maxPath >= 5) ? _item.Path5 : "";
                    textBoxPath6.Text = (_maxPath >= 6) ? _item.Path6 : "";
                    textBoxPath7.Text = (_maxPath >= 7) ? _item.Path7 : "";
                    textBoxPath8.Text = (_maxPath >= 8) ? _item.Path8 : "";
                    textBoxPath9.Text = (_maxPath >= 9) ? _item.Path9 : "";
                    textBoxPath10.Text = (_maxPath >= 10) ? _item.Path10 : "";
                    textBoxPath11.Text = (_maxPath >= 11) ? _item.Path11 : "";
                    textBoxPath12.Text = (_maxPath >= 12) ? _item.Path12 : "";
                    textBoxPath13.Text = (_maxPath >= 13) ? _item.Path13 : "";
                    textBoxPath14.Text = (_maxPath >= 14) ? _item.Path14 : "";
                    textBoxPreCommand.Text = _item.preCommand;
                    textBoxPostCommand.Text = _item.postCommand;

                    checkBoxKeep.Checked = _item.keepMain;
                    checkBoxKeepFirstDir.Checked = _item.keepFirstDir;
                    checkBoxDisable.Checked = _item.disable;

                    checkBoxKeep1.Checked = _item.keepPath1;
                    checkBoxKeep2.Checked = _item.keepPath2;
                    checkBoxKeep3.Checked = _item.keepPath3;
                    checkBoxKeep4.Checked = _item.keepPath4;
                    checkBoxKeep5.Checked = _item.keepPath5;
                    checkBoxKeep6.Checked = _item.keepPath6;
                    checkBoxKeep7.Checked = _item.keepPath7;
                    checkBoxKeep8.Checked = _item.keepPath8;
                    checkBoxKeep9.Checked = _item.keepPath9;
                    checkBoxKeep10.Checked = _item.keepPath10;
                    checkBoxKeep11.Checked = _item.keepPath11;
                    checkBoxKeep12.Checked = _item.keepPath12;
                    checkBoxKeep13.Checked = _item.keepPath13;
                    checkBoxKeep14.Checked = _item.keepPath14;

                    labelDirFile.Text = (selectedIsDirectory) ? "Directory List:" : "File List:";

                    textBoxCommand.Text = "\"" + AppDomain.CurrentDomain.BaseDirectory + "rotatebackup.exe\" " + _item.Command;

                    updateMaxPathScreen();

                }

            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateMaxPathScreen();
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            DialogResult response = MessageBox.Show("Sure delete ?", "Delete Setting", MessageBoxButtons.YesNo);
            if (response == DialogResult.Yes)
            {
                if (selectedID != "")
                {

                    using (var db = new LiteDatabase(@"BackupSet.db"))
                    {

                        var col = db.GetCollection<BackupSetting>("backup");
                        var _item = col.FindOne(Query.EQ("_id", selectedID));

                        if (_item == null)
                        {

                        }
                        else
                        {
                           var del = col.Delete(_item._id);

                        }

                        comboBoxMaxRoll.Enabled = false;
                        textBoxPreCommand.Enabled = false;
                        textBoxPostCommand.Enabled = false;
                        selectedID = "";
                        selectedIsDirectory = false;
                        panel1.Visible = false;

                        
                    }
                }

                updateSelectMenu();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (selectedID != "")
            {

                using (var db = new LiteDatabase(@"BackupSet.db"))
                {

                    var col = db.GetCollection<BackupSetting>("backup");
                    var _item = col.FindOne(Query.EQ("_id", selectedID));
                    int maxpath = comboBoxMaxRoll.SelectedIndex + 1;

                    _item.mainPath = (selectedIsDirectory) ? textBoxMain.Text : "";

                    _item.Path1 = (maxpath >= 1) ? textBoxPath1.Text : "";
                    _item.Path2 = (maxpath >= 2) ? textBoxPath2.Text : "";
                    _item.Path3 = (maxpath >= 3) ? textBoxPath3.Text : "";
                    _item.Path4 = (maxpath >= 4) ? textBoxPath4.Text : "";
                    _item.Path5 = (maxpath >= 5) ? textBoxPath5.Text : "";
                    _item.Path6 = (maxpath >= 6) ? textBoxPath6.Text : "";
                    _item.Path7 = (maxpath >= 7) ? textBoxPath7.Text : "";
                    _item.Path8 = (maxpath >= 8) ? textBoxPath8.Text : "";
                    _item.Path9 = (maxpath >= 9) ? textBoxPath9.Text : "";
                    _item.Path10 = (maxpath >= 10) ? textBoxPath10.Text : "";
                    _item.Path11 = (maxpath >= 11) ? textBoxPath11.Text : "";
                    _item.Path12 = (maxpath >= 12) ? textBoxPath12.Text : "";
                    _item.Path13 = (maxpath >= 13) ? textBoxPath13.Text : "";
                    _item.Path14 = (maxpath >= 14) ? textBoxPath14.Text : "";
                    _item.preCommand = textBoxPreCommand.Text;
                    _item.postCommand = textBoxPostCommand.Text;
                    _item.maxPath = maxpath;
                    _item.disable = checkBoxDisable.Checked;
                    _item.keepMain = checkBoxKeep.Checked;
                    _item.keepFirstDir = checkBoxKeepFirstDir.Checked;
                    _item.keepPath1 = checkBoxKeep1.Checked;
                    _item.keepPath2 = checkBoxKeep2.Checked;
                    _item.keepPath3 = checkBoxKeep3.Checked;
                    _item.keepPath4 = checkBoxKeep4.Checked;
                    _item.keepPath5 = checkBoxKeep5.Checked;
                    _item.keepPath6 = checkBoxKeep6.Checked;
                    _item.keepPath7 = checkBoxKeep7.Checked;
                    _item.keepPath8 = checkBoxKeep8.Checked;
                    _item.keepPath9 = checkBoxKeep9.Checked;
                    _item.keepPath10 = checkBoxKeep10.Checked;
                    _item.keepPath11 = checkBoxKeep11.Checked;
                    _item.keepPath12 = checkBoxKeep12.Checked;
                    _item.keepPath13 = checkBoxKeep13.Checked;
                    _item.keepPath14 = checkBoxKeep14.Checked;


                    col.Update(_item);

                    MessageBox.Show("Saved");
                }
            }
        }


        private void selectFileBox(TextBox _textbox)
        {

            if (_textbox.Text != "")
            {
                openFileDialog1.FileName = _textbox.Text;
            }

            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                _textbox.Text = openFileDialog1.FileName;
            }
        }

        private void selectDirectoryBox(TextBox _textbox)
        {
            if (_textbox.Text != "")
            {
                folderBrowserDialog1.SelectedPath = _textbox.Text;
            }
                DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                _textbox.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (selectedIsDirectory)
            {
                selectDirectoryBox(textBoxPath1);
            }
            else
            {
                selectFileBox(textBoxPath1);
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            if (selectedIsDirectory)
            {
                selectDirectoryBox(textBoxPath2);
            }
            else
            {
                selectFileBox(textBoxPath2);
            }
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            if (selectedIsDirectory)
            {
                selectDirectoryBox(textBoxPath3);
            }
            else
            {
                selectFileBox(textBoxPath3);
            }
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            if (selectedIsDirectory)
            {
                selectDirectoryBox(textBoxPath4);
            }
            else
            {
                selectFileBox(textBoxPath4);
            }
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            if (selectedIsDirectory)
            {
                selectDirectoryBox(textBoxPath5);
            }
            else
            {
                selectFileBox(textBoxPath5);
            }
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            if (selectedIsDirectory)
            {
                selectDirectoryBox(textBoxPath6);
            }
            else
            {
                selectFileBox(textBoxPath6);
            }
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {
            if (selectedIsDirectory)
            {
                selectDirectoryBox(textBoxPath7);
            }
            else
            {
                selectFileBox(textBoxPath7);
            }
        }

        private void pictureBox8_Click(object sender, EventArgs e)
        {
            if (selectedIsDirectory)
            {
                selectDirectoryBox(textBoxPath8);
            }
            else
            {
                selectFileBox(textBoxPath8);
            }
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            if (selectedIsDirectory)
            {
                selectDirectoryBox(textBoxPath9);
            }
            else
            {
                selectFileBox(textBoxPath9);
            }
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            if (selectedIsDirectory)
            {
                selectDirectoryBox(textBoxPath10);
            }
            else
            {
                selectFileBox(textBoxPath10);
            }
        }

        private void pictureBoxMain_Click(object sender, EventArgs e)
        {
            
                selectDirectoryBox(textBoxMain);
                       
        }

        private void button3_Click(object sender, EventArgs e)
        {
            String command = @"/K RotateBackup.exe " + selectedCommad;
            ProcessStartInfo cmdsi = new ProcessStartInfo("cmd.exe");
            cmdsi.Arguments = command;
            Process cmd = Process.Start(cmdsi);
            cmd.WaitForExit();
        }

        private void textBoxCommand_Click(object sender, EventArgs e)
        {
            textBoxCommand.SelectionStart = 0;
            textBoxCommand.SelectionLength = textBoxCommand.Text.Length;
        }

        private void pictureBox11_Click(object sender, EventArgs e)
        {
            if (selectedIsDirectory)
            {
                selectDirectoryBox(textBoxPath11);
            }
            else
            {
                selectFileBox(textBoxPath11);
            }
        }

        private void pictureBox12_Click(object sender, EventArgs e)
        {
            if (selectedIsDirectory)
            {
                selectDirectoryBox(textBoxPath12);
            }
            else
            {
                selectFileBox(textBoxPath12);
            }
        }

        private void pictureBox13_Click(object sender, EventArgs e)
        {
            if (selectedIsDirectory)
            {
                selectDirectoryBox(textBoxPath13);
            }
            else
            {
                selectFileBox(textBoxPath13);
            }
        }

        private void pictureBox14_Click(object sender, EventArgs e)
        {
            if (selectedIsDirectory)
            {
                selectDirectoryBox(textBoxPath14);
            }
            else
            {
                selectFileBox(textBoxPath14);
            }
        }

        private void buttonAutoFillDigit_Click(object sender, EventArgs e)
        {
            if (textBoxReference.Text == "")
            {
                MessageBox.Show("Please fill in the Reference Path");
            } else
            {
                int maxpath = comboBoxMaxRoll.SelectedIndex + 1;
                string lastpath = textBoxReference.Text;
                if (lastpath.Substring(lastpath.Length - 1) != "\\")
                {
                    lastpath = lastpath + "\\";
                }
                if (maxpath >= 1) textBoxPath1.Text = lastpath + "1";
                if (maxpath >= 2) textBoxPath2.Text = lastpath + "2";
                if (maxpath >= 3) textBoxPath3.Text = lastpath + "3";
                if (maxpath >= 4) textBoxPath4.Text = lastpath + "4";
                if (maxpath >= 5) textBoxPath5.Text = lastpath + "5";
                if (maxpath >= 6) textBoxPath6.Text = lastpath + "6";
                if (maxpath >= 7) textBoxPath7.Text = lastpath + "7";
                if (maxpath >= 8) textBoxPath8.Text = lastpath + "8";
                if (maxpath >= 9) textBoxPath9.Text = lastpath + "9";
                if (maxpath >= 10) textBoxPath10.Text = lastpath + "10";
                if (maxpath >= 11) textBoxPath11.Text = lastpath + "11";
                if (maxpath >= 12) textBoxPath12.Text = lastpath + "12";
                if (maxpath >= 13) textBoxPath13.Text = lastpath + "13";
                if (maxpath >= 14) textBoxPath14.Text = lastpath + "14";
            }
        }


        private void checkAndCreateFolder(string path)
        {
            if (path!= "")
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }
        private void buttonCreateFolder_Click(object sender, EventArgs e)
        {
            int maxpath = comboBoxMaxRoll.SelectedIndex + 1;

            if (maxpath >= 1) checkAndCreateFolder(textBoxPath1.Text);
            if (maxpath >= 2) checkAndCreateFolder(textBoxPath2.Text);
            if (maxpath >= 3) checkAndCreateFolder(textBoxPath3.Text);
            if (maxpath >= 4) checkAndCreateFolder(textBoxPath4.Text);
            if (maxpath >= 5) checkAndCreateFolder(textBoxPath5.Text);
            if (maxpath >= 6) checkAndCreateFolder(textBoxPath6.Text);
            if (maxpath >= 7) checkAndCreateFolder(textBoxPath7.Text);
            if (maxpath >= 8) checkAndCreateFolder(textBoxPath8.Text);
            if (maxpath >= 9) checkAndCreateFolder(textBoxPath9.Text);
            if (maxpath >= 10) checkAndCreateFolder(textBoxPath10.Text);
            if (maxpath >= 11) checkAndCreateFolder(textBoxPath11.Text);
            if (maxpath >= 12) checkAndCreateFolder(textBoxPath12.Text);
            if (maxpath >= 13) checkAndCreateFolder(textBoxPath13.Text);
            if (maxpath >= 14) checkAndCreateFolder(textBoxPath14.Text);
            MessageBox.Show("Folder Created.");
        }
    }
}
