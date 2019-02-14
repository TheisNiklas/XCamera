﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using XCamera.Util;

namespace XCameraManager
{
    /// <summary>
    /// Interaktionslogik für ConnectWindow.xaml
    /// </summary>
    public partial class ConnectWindow : Window
    {
        WebServer webServer;
        public ConnectWindow()
        {
            InitializeComponent();
            ProjectUtil.szBasePath = Config.current.szBasedir;
            IPAddress[] addrList = LocalIPAddress();
            int sel = 0;
            foreach (var addr in addrList)
            {
                if(addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    string szIP = addr.ToString();
                    if( szIP.Equals(XCamera.Util.Config.current.szIP))
                    {
                        sel = cmbIP.Items.Count;
                    }
                    cmbIP.Items.Add(szIP);
                }
                
            }
            cmbIP.SelectedIndex = sel;
            tbPort.Text = XCamera.Util.Config.current.szPort;
            tbBasedir.Text = XCamera.Util.Config.current.szBasedir;
            btnDisconnect.IsEnabled = false;

            /*for(int i=0;i<100;i++)
            {
                tbLog.Text += i.ToString() + Environment.NewLine;
            }*/

        }
        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            string szIP = cmbIP.SelectedValue.ToString();
            
            webServer = new WebServer(SendResponse, "http://" + szIP + ":" + tbPort.Text.Trim() + "/xcamera/");
            webServer.Run();
            XCamera.Util.Config.current.szIP = szIP;
            XCamera.Util.Config.current.szPort = tbPort.Text.Trim();
            btnDisconnect.IsEnabled = true;
            btnConnect.IsEnabled = false;
            ShowInfo("Server wurde erfolgreich gestartet.");

        }
        public  wsResponse SendResponse(HttpListenerRequest request)
        {
            Dictionary<string, string> postParams = new Dictionary<string, string>();
            wsResponse swRes = new wsResponse();

            string szProjectname = "";
            string szFilename = "";
            string szHasJson = "";

            if (request.QueryString["project"] != null)
            {
                szProjectname = request.QueryString["project"].ToString();
            }
            if (request.QueryString["file"] != null)
            {
                szFilename = request.QueryString["file"].ToString();
            }
            if (request.QueryString["json"] != null)
            {
                szHasJson = request.QueryString["json"].ToString();
            }

            swRes.ba = Encoding.UTF8.GetBytes("{}");
            if (request.HttpMethod.Equals("POST", StringComparison.InvariantCultureIgnoreCase))
            {
                if (string.IsNullOrEmpty(szProjectname) )
                {
                    JsonError je = new JsonError { szMessage = "POST requires a project " };
                    ShowError(je.szMessage);

                    string szJson = Newtonsoft.Json.JsonConvert.SerializeObject(je);
                    swRes.ba = Encoding.UTF8.GetBytes(szJson);
                    swRes.szMinetype = "text/json";
                }
                else if (string.IsNullOrEmpty(szHasJson) &&  string.IsNullOrEmpty(szFilename))
                {
                    JsonError je = new JsonError { szMessage = "POST requires both project and file " };
                    ShowError(je.szMessage);

                    string szJson = Newtonsoft.Json.JsonConvert.SerializeObject(je);
                    swRes.ba = Encoding.UTF8.GetBytes(szJson);
                    swRes.szMinetype = "text/json";
                }
                else if (!string.IsNullOrEmpty(szHasJson) )
                {
                    string szJson = "";
                    System.Text.Encoding encoding = request.ContentEncoding;
                    using (StreamReader sr= new StreamReader(request.InputStream, encoding))
                    {
                        szJson = sr.ReadToEnd();
                    }
                    request.InputStream.Close();
                    try
                    {
                        BildInfo bi = Newtonsoft.Json.JsonConvert.DeserializeObject<BildInfo>(szJson);
                        ProjectSql tmpProject = new ProjectSql(szProjectname);
                        int bildId = tmpProject.GetBildId(bi.BildName);
                        Gebaeude gebauede = tmpProject.EnsureGebaeude(bi.GebaeudeBezeichnung);
                        Etage etage = tmpProject.EnsureEtage(bi.EtageBezeichnung);
                        Wohnung wohnung = tmpProject.EnsureWohnung(bi.WohnungBezeichnung);
                        Zimmer zimmer = tmpProject.EnsureZimmer(bi.ZimmerBezeichnung);
                        if( gebauede != null )
                        {
                            tmpProject.SetGebaeude(bildId, gebauede.ID);
                        }
                        if (etage != null)
                        {
                            tmpProject.SetEtage(bildId, etage.ID);
                        }
                        if (wohnung != null)
                        {
                            tmpProject.SetWohnung(bildId, wohnung.ID);
                        }
                        if (zimmer != null)
                        {
                            tmpProject.SetZimmer(bildId, zimmer.ID);
                        }
                        tmpProject.SetComment(bildId, bi.KommentarBezeichnung);
                    }
                    catch (Exception ex)
                    {
                        JsonError je = new JsonError { szMessage = "POST " + ex.ToString() };
                        ShowError(je.szMessage);

                        string szErrJson = Newtonsoft.Json.JsonConvert.SerializeObject(je);
                        swRes.ba = Encoding.UTF8.GetBytes(szErrJson);
                        swRes.szMinetype = "text/json";

                    }
                }
                else
                {
                    try
                    {
                        string szFullPath = System.IO.Path.Combine(XCamera.Util.Config.current.szBasedir, szProjectname);
                        if (!Directory.Exists(szFullPath))
                        {
                            Directory.CreateDirectory(szFullPath);
                        }
                        string szFullFilename = System.IO.Path.Combine(szFullPath, szFilename);
                        ShowInfo("recieving " + szFullFilename);

                        using (FileStream fs = new FileStream(szFullFilename,FileMode.Create, FileAccess.Write))
                        {
                            request.InputStream.CopyTo(fs);
                        }
                        request.InputStream.Close();
                        ProjectSql tmpProject = new ProjectSql(szProjectname);
                        tmpProject.AddBild(szFilename);
                    }
                    catch (Exception ex)
                    {
                        JsonError je = new JsonError { szMessage = "POST " + ex.ToString() };
                        ShowError(je.szMessage);

                        string szJson = Newtonsoft.Json.JsonConvert.SerializeObject(je);
                        swRes.ba = Encoding.UTF8.GetBytes(szJson);
                        swRes.szMinetype = "text/json";
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(szProjectname))
                {
                    ShowInfo("sending project list");
                    string[] szProjects = Directory.GetDirectories(XCamera.Util.Config.current.szBasedir, "*.", SearchOption.TopDirectoryOnly);
                    List<JsonProject> jProjects = new List<JsonProject>();
                    foreach (var szProject in szProjects)
                    {
                        jProjects.Add(new JsonProject {
                            szProjectName = System.IO.Path.GetFileNameWithoutExtension(szProject),
                            lSize = GetDirectorySize(0, System.IO.Path.Combine(XCamera.Util.Config.current.szBasedir, szProject))
                        });
                    }

                    string szJson = Newtonsoft.Json.JsonConvert.SerializeObject(jProjects);
                    swRes.ba = Encoding.UTF8.GetBytes(szJson);
                    swRes.szMinetype = "text/json";
                }
                else if (!string.IsNullOrWhiteSpace(szFilename))
                {
                    string szFullFilename = System.IO.Path.Combine(XCamera.Util.Config.current.szBasedir, szProjectname, szFilename);
                    ShowInfo("sending  " + szFullFilename);
                    if (File.Exists(szFullFilename))
                    {
                        if (System.IO.Path.GetExtension(szFullFilename).Equals(".jpg", StringComparison.InvariantCultureIgnoreCase))
                        {
                            swRes.ba = System.IO.File.ReadAllBytes(szFullFilename);
                            swRes.szMinetype = "image/jpeg";
                        }
                        else
                        {
                            // swRes.szMinetype = "Application/x-sqlite3";
                            ProjectSql tmpProject = new ProjectSql(szProjectname);
                            var metaData = tmpProject.GetMetaData();
                            string szJson = Newtonsoft.Json.JsonConvert.SerializeObject(metaData);
                            swRes.ba = Encoding.UTF8.GetBytes(szJson);
                            swRes.szMinetype = "text/json";

                        }
                    }
                    else
                    {
                        JsonError je = new JsonError { szMessage = "File not found " + szFullFilename };
                        ShowError(je.szMessage);
                        string szJson = Newtonsoft.Json.JsonConvert.SerializeObject(je);
                        swRes.ba = Encoding.UTF8.GetBytes(szJson);
                        swRes.szMinetype = "text/json";
                    }
                }
                else if ( !string.IsNullOrEmpty(szProjectname))
                {
                    ShowInfo("sending project info " + szProjectname);
                    List<JsonProject> jProjects = new List<JsonProject>();
                    
                        jProjects.Add(new JsonProject
                        {
                            szProjectName = System.IO.Path.GetFileNameWithoutExtension(szProjectname),
                            lSize = GetDirectorySize(0, System.IO.Path.Combine(XCamera.Util.Config.current.szBasedir, szProjectname))
                        });


                    string szJson = Newtonsoft.Json.JsonConvert.SerializeObject(jProjects);
                    swRes.ba = Encoding.UTF8.GetBytes(szJson);
                    swRes.szMinetype = "text/json";
                }
            }

            return swRes; 
        }
        private void ShowText(string szMessage)
        {
            Application.Current.Dispatcher.BeginInvoke(
              DispatcherPriority.Background,
              new Action(() => {
                  tbLog.Text += szMessage + Environment.NewLine;
              }));
        }
        private void ShowError(string szMessage)
        {
            ShowText("ERR  " + szMessage);
        }
        private void ShowInfo(string szMessage)
        {
            ShowText("INFO " + szMessage);
        }
        private long GetDirectorySize(long size, string directory)
        {
            foreach (string dir in Directory.GetDirectories(directory))
            {
                GetDirectorySize(size,dir);
            }

            foreach (FileInfo file in new DirectoryInfo(directory).GetFiles())
            {
                size += file.Length;
            }

            return size;
        }
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                webServer.Stop();

            }
            catch (Exception)
            {
            }
            base.OnClosed(e);
        }
        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private IPAddress[] LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList;
               // .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

        private void BtnSelectBasedir_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderDlg = new System.Windows.Forms.FolderBrowserDialog();
            folderDlg.ShowNewFolderButton = true;
            // Show the FolderBrowserDialog.  
            System.Windows.Forms.DialogResult result = folderDlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                tbBasedir.Text = folderDlg.SelectedPath;
                XCamera.Util.Config.current.szBasedir = folderDlg.SelectedPath;
            }
        }
    }
}