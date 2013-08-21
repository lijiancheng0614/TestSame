using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MainForm
{
    public partial class Form1 : Form
    {
        private bool isFileList = true;
        private double threshold = 0.9;

        public Form1(string initialPath)
            : this()
        {
            this.FolderTree.SelectedPath = initialPath;
        }

        public Form1()
        {
            InitializeComponent();
            this.toolStripComboBox4.Text = this.toolStripComboBox4.Items[0].ToString();
            this.FolderTree.SelectedPath = Path.GetPathRoot(Directory.GetCurrentDirectory());
        }

        private void FolderTree_PathChanged(object sender, EventArgs e)
        {
            isFileList = true;
            string path = FolderTree.SelectedPath;
            string pattern = toolStripComboBox1.Text;
            if (pattern == "") pattern = "*.*";
            fileListBox.Items.Clear();
            foreach (string file in Directory.GetFiles(path, pattern))
            {
                string fileName = file;
                int pos;
                while (fileName.IndexOf("\\") != -1)
                {
                    pos = fileName.IndexOf("\\");
                    fileName = fileName.Substring(pos + 1);
                }
                fileListBox.Items.Add(fileName);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isFileList)
            {
                string path = FolderTree.SelectedPath;
                int n = path.Length;
                if (path[n - 1] != '\\') path += '\\';
                string fileName = (string)fileListBox.SelectedItem;
                try
                {
                    string file = path + fileName;
                    StreamReader sr = new StreamReader(file, System.Text.Encoding.Default);
                    fileTextBox.Text = sr.ReadToEnd();
                    sr.Close();
                    toolStripComboBox2_TextChanged(sender, e);
                }
                catch
                {
                    fileTextBox.Text = "Cannot find such file.";
                }
            }
            else
            {
                string path = FolderTree.SelectedPath;
                int n = path.Length;
                if (path[n - 1] != '\\') path += '\\';
                string directoryName = (string)fileListBox.SelectedItem;
                try
                {
                    string[] file = Directory.GetFiles(path + directoryName + '\\', toolStripComboBox3.Text, SearchOption.AllDirectories);
                    StreamReader sr = new StreamReader(file[0], System.Text.Encoding.Default);
                    fileTextBox.Text = sr.ReadToEnd();
                    sr.Close();
                    toolStripComboBox2_TextChanged(sender, e);
                }
                catch
                {
                    fileTextBox.Text = "Cannot find such file.";
                }
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            isFileList = true;
            string temp = toolStripComboBox1.Text.ToLower();
            if (!toolStripComboBox1.Items.Contains(temp)) toolStripComboBox1.Items.Add(temp);
            FolderTree_PathChanged(sender, e);
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            string searchText = toolStripComboBox2.Text;
            string text = fileTextBox.Text;
            fileTextBox.Select(0, text.Length);
            fileTextBox.SelectionColor = Color.Black;
            if (searchText.Length > 0)
            {
                if (!toolStripComboBox2.Items.Contains(searchText)) toolStripComboBox2.Items.Add(searchText);
                int head = 0;
                int pos = text.IndexOf(searchText);
                while (text.IndexOf(searchText) != -1)
                {
                    fileTextBox.Select(head + pos, searchText.Length);
                    fileTextBox.SelectionColor = Color.Red;
                    text = text.Substring(pos + 1);
                    head += pos + 1;
                    pos = text.IndexOf(searchText);
                }
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            isFileList = false;
            string temp = toolStripComboBox3.Text.ToLower();
            if (!toolStripComboBox3.Items.Contains(temp)) toolStripComboBox3.Items.Add(temp);
            string path = FolderTree.SelectedPath;
            fileListBox.Items.Clear();
            foreach (string directory in Directory.GetDirectories(path))
            {
                string directoryName = directory;
                int pos;
                while (directoryName.IndexOf("\\") != -1)
                {
                    pos = directoryName.IndexOf("\\");
                    directoryName = directoryName.Substring(pos + 1);
                }
                fileListBox.Items.Add(directoryName);
            }
        }

        public static double getSimilarity(string text1, string text2)
        {
            int len1 = text1.Length;
            int len2 = text2.Length;
            len1 = Math.Min(len1, 5000);
            len2 = Math.Min(len2, 5000);
            int[,] dp = new int[len1, len2];
            for (int i = 0; i < len1; ++i)
                for (int j = 0; j < len2; ++j)
                {
                    dp[i, j] = len1 + len2;
                    if (i == 0 && j == 0)
                    {
                        dp[i, j] = 0;
                    }
                    else
                    {
                        if (i > 0) dp[i, j] = Math.Min(dp[i - 1, j] + 1, dp[i, j]);
                        if (j > 0) dp[i, j] = Math.Min(dp[i, j - 1] + 1, dp[i, j]);
                        if (i > 0 && j > 0)
                        {
                            if (text1[i] != text2[j]) dp[i, j] = Math.Min(dp[i - 1, j - 1] + 1, dp[i, j]);
                            else dp[i, j] = Math.Min(dp[i - 1, j - 1], dp[i, j]);
                        }
                    }
                }
            double dif = dp[len1 - 1, len2 - 1];
            dif /= (double)Math.Max(len1, len2);
            return 1 - dif;
        }

        public static double getFrequencySimilarity(string text1, string text2)
        {
            int len = Math.Min(text1.Length, text2.Length);
            text1 = text1.ToLower();
            text2 = text2.ToLower();
            int[] v1 = new int[128];
            int[] v2 = new int[128];
            double dif = 0;
            for (int i = 0; i < text1.Length; ++i)
                if (text1[i] >= 0 && text1[i] < 128) v1[text1[i]]++;
            for (int i = 0; i < text2.Length; ++i)
                if (text2[i] >= 0 && text2[i] < 128) v2[text2[i]]++;
            for (int i = 0; i < 128; i++)
                dif += Math.Abs(v1[i] - v2[i]);
            dif /= len;
            return 1 - dif;
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            isFileList = false;
            string path = FolderTree.SelectedPath;
            fileListBox.Items.Clear();
            int len = Directory.GetDirectories(path).Length;
            string[] fileName = new string[len];
            string[] fileText = new string[len];
            int n = -1;
            foreach (string directory in Directory.GetDirectories(path))
            {
                string directoryName = directory;
                int pos;
                while (directoryName.IndexOf("\\") != -1)
                {
                    pos = directoryName.IndexOf("\\");
                    directoryName = directoryName.Substring(pos + 1);
                }
                fileListBox.Items.Add(directoryName);
                try
                {
                    string[] file = Directory.GetFiles(directory, toolStripComboBox3.Text, SearchOption.AllDirectories);
                    if (file.Length > 0)
                    {
                        StreamReader sr = new StreamReader(file[0], System.Text.Encoding.Default);
                        fileName[++n] = directoryName;
                        fileText[n] = sr.ReadToEnd();
                        sr.Close();
                    }
                }
                catch
                {
                }
            }
            n++;
            fileTextBox.Text = "Testing " + n + " file(s).\r\n";
            for (int i = 1; i < n; i++)
                for (int j = 0; j < i; j++)
                {
                    double sim = 0;
                    if (toolStripComboBox4.Text == "Frequency")
                        sim = getFrequencySimilarity(fileText[i], fileText[j]);
                    else
                        sim = getSimilarity(fileText[i], fileText[j]);
                    if (sim > threshold)
                    {
                        string temp = fileName[j] + "\t" + fileName[i] + "\t" + sim + "\r\n";
                        fileTextBox.Text += temp;
                    }
                }
        }

        private void toolStripTextBox4_TextChanged(object sender, EventArgs e)
        {
            try
            {
                threshold = Double.Parse(toolStripTextBox4.Text);
                if (threshold < 0 || threshold > 1)
                {
                    MessageBox.Show("Please input a number between 0 to 1!");
                    threshold = 0.9;
                    toolStripTextBox4.Text = "0.90";
                    toolStripTextBox4.Focus();
                }
            }
            catch
            {
                MessageBox.Show("Please input a number between 0 to 1!");
                threshold = 0.9;
                toolStripTextBox4.Text = "0.90";
                toolStripTextBox4.Focus();
            }
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Search words can highlight the words you searched.\n" +
                "GetFolder can get the Judge file in the all directories.\n" +
                "About Judge Mode:\n" +
                "Frequency Sim. = 1 - the sum of different chars in two files/all chars\n" +
                "Edit Distance Sim. = 1 - edit distance of two files/all chars\n" +
                "Made By LiJiancheng", "TestSame");
        }

        private void toolStripComboBox3_TextChanged(object sender, EventArgs e)
        {
            listBox1_SelectedIndexChanged(sender, e);
            toolStripComboBox2_TextChanged(sender, e);
        }

        private void toolStripComboBox2_TextChanged(object sender, EventArgs e)
        {
            string searchText = toolStripComboBox2.Text;
            string text = fileTextBox.Text;
            fileTextBox.Select(0, text.Length);
            fileTextBox.SelectionColor = Color.Black;
            if (searchText.Length > 0)
            {
                int head = 0;
                int pos = text.IndexOf(searchText);
                while (text.IndexOf(searchText) != -1)
                {
                    fileTextBox.Select(head + pos, searchText.Length);
                    fileTextBox.SelectionColor = Color.Red;
                    text = text.Substring(pos + 1);
                    head += pos + 1;
                    pos = text.IndexOf(searchText);
                }
            }
        }
    }
}