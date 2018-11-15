using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;


namespace SearchFile
{
    public partial class Form1 : Form
    {
        string fileName;
        string directory;
        string contentFile;
        DateTime startTime;
        DateTime stopTime;
        int countFile;
        List<string> fileList;
        TreeNode node, root;
        CancellationToken token;
        CancellationTokenSource cancelTokenSource;


        public Form1()
        {
            InitializeComponent();
        }

        private void FileSearchFunction(string Dir)
        {
            //  DirectoryInfo DI = new DirectoryInfo((string)Dir);
            DirectoryInfo DI = new DirectoryInfo(Dir);
            DirectoryInfo[] SubDir = null;
            if (token.IsCancellationRequested)
            {
                //  MessageBox.Show("Операция прервана");
                timer1.Enabled = false;
                return;
            }
            try
            {
                SubDir = DI.GetDirectories();
            }
            catch
            {
                return;
            }
           
            for (int i = 0; i < SubDir.Length; ++i)
            {
                if (token.IsCancellationRequested)
                {
                    //   MessageBox.Show("Операция прервана");
                    timer1.Enabled = false;
                    return;
                }
                this.FileSearchFunction(SubDir[i].FullName);
               
            }

            ///////////////нет доступа к системным файлам, выберите другую директорию начала поиска
            try
            {
                FileInfo[] FI = DI.GetFiles(fileName, SearchOption.AllDirectories); //"*.txt",SearchOption.AllDirectories
                if (token.IsCancellationRequested)
                {
                    //   MessageBox.Show("Операция прервана");
                    timer1.Enabled = false;
                    return;
                }
                for (int i = 0; i < FI.Length; ++i)
                    this.Invoke(new ThreadStart(delegate
                    {
                       
                        //выводим имя обрабатываемого файла                 
                        try { label4.Text = FI[i].FullName; }
                        catch { label4.Text = "..." + FI[i].FullName.Substring(FI[i].FullName.Length - 40); }

                        //проверяем содержимое файла 
                        string tmp = File.ReadAllText(FI[i].FullName);


                        if (tmp.IndexOf(contentFile, StringComparison.CurrentCulture) != -1)
                        {
                            richTextBox1.Text += "Совпадение в файле найдено" + FI[i].FullName + "\n";
                            fileList.Add(FI[i].FullName);

                            node = root;
                            node.Expand();
                            foreach (string pathBits in FI[i].FullName.Split('\\'))
                            {
                                node = AddNode(node, pathBits);
                                //   treeView1.ExpandAll();
                                node.Expand();
                            }

                        }
                        else
                        {
                            richTextBox1.Text += "Совпадений в файле не найдено" + FI[i].FullName + "\n";
                        }
                        countFile++;
                        label5.Text = "Обработано файлов: " + countFile;
                    }));
                
            }
            catch
            {
                return; //нет доступа
            }

            stopTime = DateTime.Now;
        }



        private void button1_Click(object sender, EventArgs e)
        {
            countFile = 0;
            fileName = textBox2.Text;
            contentFile = textBox3.Text;
            directory = textBox1.Text;

            if (textBox1.Text != String.Empty && Directory.Exists(textBox1.Text) && fileName != String.Empty && contentFile != String.Empty)
            {
                root = new TreeNode();
                node = root;
                treeView1.Nodes.Add(root);
                fileList = new List<string>(); //список найденных файлов
                // Thread T = new Thread(new ParameterizedThreadStart(FileSearchFunction));
                startTime = DateTime.Now;
                timer1.Enabled = true;

                cancelTokenSource = new CancellationTokenSource();
                token = cancelTokenSource.Token;

                var task = Task.Factory.StartNew(() => {
                   
                    FileSearchFunction(directory);
                   

                },token);

                task.ContinueWith(t => {
                        timer1.Enabled = false;
                        if (fileList == null)
                        {
                        MessageBox.Show("Ничего не найдено");
                    }
                    });         
            }
            else
                MessageBox.Show("Заполните все параметры поиска");
        }

        private TreeNode AddNode(TreeNode node, string key)
        {
            if (node.Nodes.ContainsKey(key))
            {
                return node.Nodes[key];
            }
            else
            {
                return node.Nodes.Add(key, key);
            };
        }

        /*
        foreach (string findedFile in Directory.EnumerateFiles(directory, fileName,
            SearchOption.AllDirectories))
        {
            FileInfo fileInfo;
            try
            {
                fileInfo = new FileInfo(findedFile);
                richTextBox1.Text+=fileInfo.Name + " " + fileInfo.FullName + " " + fileInfo.Length + "_байт" +
                    " Создан: " + fileInfo.CreationTime+"\n";

            }
            catch //слишком длинное имя файла
            {
                continue;
            }
        }*/

        private void button2_Click(object sender, EventArgs e)
        {
            cancelTokenSource.Cancel();
        }

        private void chooseDirectory(object sender, EventArgs e)
        {
            FolderBrowserDialog FBD = new FolderBrowserDialog();
            if (FBD.ShowDialog() == DialogResult.OK)
            {
                directory = FBD.SelectedPath;
                textBox1.Text = directory;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        //  DateTime date1;
        private void timer1_Tick(object sender, EventArgs e)
        {
            var d1 = DateTime.Now - startTime;
            //  date1 = date1.AddSeconds(1);
            //   label6.Text = date1.ToString("mm:ss");
            label6.Text = "" + d1;


        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            label2.Text = "Введите название файла\n(*-любое количество символов,\n? -один любой символ):";
            try
            {
                FileStream stream = File.OpenRead("user.dat");
                BinaryFormatter formatter = new BinaryFormatter();
                Response resp = formatter.Deserialize(stream) as Response;

                textBox1.Text = resp.st1;
                textBox2.Text = resp.st2;
                textBox3.Text = resp.st3;
                stream.Close();
            }
            catch { };
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        [Serializable]
       public class Response
        {
            public string st1;
            public string st2;
            public string st3;

            public Response(string s1, string s2, string s3)
            {
                st1 = s1;
                st2 = s2;
                st3 = s3;
            }
           
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            
            Response resp = new Response(textBox1.Text, textBox2.Text, textBox3.Text);
            BinaryFormatter binFormat = new BinaryFormatter();
            using (Stream fStream = new FileStream("user.dat",
               FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                binFormat.Serialize(fStream, resp);
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            if (cancelTokenSource!=null)
            cancelTokenSource.Cancel();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
