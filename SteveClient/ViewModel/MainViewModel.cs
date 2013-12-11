using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Net.Sockets;
using System.IO;
using SteveClientCore;
using Newtonsoft.Json;
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using Ionic.Zip;
using Communication;

namespace SteveClient.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            IP = "localhost";
            Port = 51343;
            Task = "run";
            Desire = "out";
            Exec = "python";
            Files = "";

            m_cids = new List<string>();
        }


        //Properties

        public string IP { get; set; }
        public int Port { get; set; }
        public string ClientId
        {
            get { return m_id; }

            set
            {
                m_id = value;
                RaisePropertyChanged("ClientId");
            }

        }
        public string Task { get; set; }
        public string Desire { get; set; }
        public string Exec { get; set; }

        public string Files { get; set; }
        public string Command { get; set; }

        public TcpClient Client
        {
            get
            {
                if (m_tcpClient == null) { m_tcpClient = new TcpClient(IP, Port); }

                return m_tcpClient;
            }

            set
            {
                m_tcpClient = value;
            }
        }

        public StreamReader Reader
        {
            get
            {
                if (m_reader == null) { m_reader = new StreamReader(NetStream); }

                return m_reader;
            }

            set
            {
                m_reader = value;
            }
        }

        public StreamWriter Writer
        {
            get
            {
                if (m_writer == null) { m_writer = new StreamWriter(NetStream); }

                return m_writer;
            }

            set
            {
                m_writer = value;
            }
        }

        public NetworkStream NetStream
        {
            get
            {
                if (m_stream == null) { m_stream = Client.GetStream(); }

                return m_stream;
            }

            set
            {
                m_stream = value;
            }
        }

        // Button

        public RelayCommand ConnectBtn
        {
            get
            {
                return new RelayCommand(Connect);
            }
        }

        public RelayCommand BrowseBtn
        {
            get
            {
                return new RelayCommand(GetFiles);
            }
        }

        public RelayCommand SendRequestBtn
        {
            get
            {
                return new RelayCommand(SendRequest);
            }
        }

        public RelayCommand CommandDoneBtn
        {
            get
            {
                return new RelayCommand(MakeFile);
            }
        }

        public RelayCommand CancelCommand
        {
            get
            {
                return new RelayCommand(CancelPrompt);
            }
        }

        //Methods

        private void Connect()
        {
            Client = new TcpClient(IP, Port);
            NetStream = m_tcpClient.GetStream();
            Writer = new StreamWriter(NetStream);
            Reader = new StreamReader(NetStream);

            Writer.WriteLine(JsonConvert.SerializeObject(new ReqDef()));
            Writer.Flush();

            //Wait for response
            ReqDefResp resp = JsonConvert.DeserializeObject<ReqDefResp>(Reader.ReadLine());
            ClientId = resp.id;
        }

        private void GetFiles()
        {
            OpenFileDialog o = new OpenFileDialog();
            o.Multiselect = true;
            o.Title = "Select Files to Send";

            DialogResult dr = o.ShowDialog();

            if (dr == DialogResult.OK)
            {
                Files = "";
                foreach (String file in o.FileNames)
                {
                    Files += file + "\n";
                }
                RaisePropertyChanged("Files");
            }
        }

        private void SendRequest()
        {
            //Check for Makefile
            if (!Files.Contains("Makefile"))
            {
                //Prompt for command
                cp = new CommandPrompt();
                cp.Show();
                return;
            }

            Writer.WriteLine(JsonConvert.SerializeObject(new Request(Files.Length > 0)));
            Writer.Flush();

            ReqResp resp = JsonConvert.DeserializeObject<ReqResp>(Reader.ReadLine());

            m_cids.Add(resp.cid);
            cid = resp.cid;

            //Wait for Response to see if Steve can handle the request
            //Todo: actually use objects instead of this way
            string ack = Reader.ReadLine();

            if (ack.Contains("CompAck") && ack.Contains(resp.cid))
            {
                //TODO: Change when security is implemented
                Writer.WriteLine(ack.Replace("CompAck", "CompAccept"));
                Writer.Flush();
            }
            else
            {
                //Dunno
            }

            m_TftpClient = new TFTPClient(resp.port, IP);

            
            DoFileCreate();


        }

        private void DoFileCreate()
        {
            if (cp != null && cp.IsActive)
            {
                cp.Close();
            }

            string[] filesArray = Files.Split(new string[] {"\n", "\r\n"}, StringSplitOptions.None);
            List<String> files = new List<string>(filesArray);

            files.RemoveAll( x => x.Equals(""));

            using (ZipFile zip = new ZipFile())
            {
                foreach (string s in files)
                {
                    zip.AddFile(s,@"\");
                }

                //zip.AddFiles(files);
                string name = cid + ".zip";
                zip.Save(name);
            }



            m_TftpClient.PutFile(cid + ".zip");


            string foo1 = Reader.ReadLine();
            m_TftpClient.GetFile("result_" + cid + ".zip");

            Directory.CreateDirectory(cid);

            using (ZipFile zip1 = ZipFile.Read("result_" + cid + ".zip"))
            {
                // here, we extract every entry, but we could extract conditionally
                // based on entry name, size, date, checkbox status, etc.  
                foreach (ZipEntry e in zip1)
                {
                    e.Extract(cid, ExtractExistingFileAction.OverwriteSilently);
                }
            }


            DialogResult res = MessageBox.Show("Result Recieved, Do you want to open folder?", "Result", MessageBoxButtons.YesNo);
            if (res == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start(cid);
            }

            Client.Close();
            NetStream.Close();
            Reader.Close();
            Writer.Close();

            Client = null;
            NetStream = null;
            Reader = null;
            Writer = null;
        }

        //Generates a makefile from a command
        private void MakeFile()
        {
            cp.Close();

            StreamWriter write = new StreamWriter("Makefile");
            write.WriteLine("all:");
            write.WriteLine("\t" + Command);
            write.Flush();
            write.Close();

            Files += Environment.CurrentDirectory + Path.DirectorySeparatorChar + "Makefile\n";

            SendRequest();
        }

        //Just closes prompt box
        private void CancelPrompt()
        {
            cp.Close();
        }


        private TcpClient m_tcpClient;
        private NetworkStream m_stream;
        private StreamReader m_reader;
        private StreamWriter m_writer;
        private List<String> m_cids;
        private string cid;

        private TFTPClient m_TftpClient;
        private CommandPrompt cp;

        private String m_id;
    }
}