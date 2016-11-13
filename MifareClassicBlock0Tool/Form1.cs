using MifareClassicBlock0Tool;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MifareClassicBlock0Tool
{
    public partial class Form1 : Form
    {
        MifareTool mifare;
        bool connected = false;
        bool read = true;



        public Form1()
        {
            InitializeComponent();
            try
            {
                mifare = new MifareTool();
                String[] readers = mifare.GetReaders();
                foreach (String reader in readers)
                {
                    comboBoxReader.Items.Add(reader);
                }
                comboBoxReader.SelectedIndex = 0;
                //MessageBox.Show(readers[0]);
                buttonConnect.Enabled = true;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(this, ex.Message, "Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
                AppendText(richTextBoxResponse, ex.Message+"\r\n", Color.Red);
            }
            

        }

        private void AppendText(RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (connected)
            {
                //disconnect
                mifare.Disconnect();
                buttonConnect.Text = "Connect";
                buttonRead.Text = "Read";
                buttonRead.Enabled = false;
                connected = false;
            }
            else
            {
                //connect
                int position = comboBoxReader.SelectedIndex;
                try
                {
                    mifare.Connect(position);
                    buttonConnect.Text = "Disconnect";
                    connected = true;
                    buttonRead.Enabled = true;
                    richTextBoxResponse.AppendText("Connected!\r\n");

                }
                catch(Exception ex)
                {
                    AppendText(richTextBoxResponse, ex.Message + "\r\n", Color.Red);
                }
            }
            
        }

        private void buttonRead_Click(object sender, EventArgs e)
        {
            if (read)
            {
                //read
                try
                {
                    richTextBoxResponse.AppendText("Card UID: ");
                    richTextBoxResponse.AppendText(MifareTool.ByteArrayToString(mifare.GetUid())+"\r\n");
                    richTextBoxResponse.AppendText("Reading sector 0 block 0..\r\n");
                    byte[] block0 = mifare.ReadBlock0();
                    richTextBoxResponse.AppendText("Block 0: "+ MifareTool.ByteArrayToString(block0) +"\r\n");
                    textBoxBlock0.Text = MifareTool.ByteArrayToString(block0,false);
                    mifare.OnbuzzerLED();
                    buttonRead.Text = "Write";
                    read = false;
                }
                catch(Exception ex)
                {
                    AppendText(richTextBoxResponse, ex.Message + "\r\n", Color.Red);
                }
            }
            else
            {
                //write
                try {
                    mifare.OnbuzzerLED();
                    if (mifare.WriteBlock(textBoxBlock0.Text))
                    {
                        AppendText(richTextBoxResponse, "Successfully write to block 0!\r\n", Color.Green);
                    }
                    else
                    {
                        AppendText(richTextBoxResponse, "Failed write to block 0!\r\n", Color.Red);
                    }
                    buttonRead.Text = "Read";
                    read = true;
                }
                catch(Exception ex)
                {
                    richTextBoxResponse.AppendText(ex.Message+"\r\n");
                }
                
            }
        }

        private void textBoxBlock0_Change(object sender, EventArgs e)
        {
            if (textBoxBlock0.Text.Length < 32)
                buttonRead.Enabled = false;
            else
                buttonRead.Enabled = true;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this,"By Mahadir Ahmad (2016)\nhttp://madet.my","Mifare Classic UID/Block 0 Tool V1.0",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
    }
}
