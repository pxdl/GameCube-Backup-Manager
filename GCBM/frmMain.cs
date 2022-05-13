﻿#region Using
using GCBM.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using AutoUpdaterDotNET;
using sio = System.IO;
using ste = System.Text.Encoding;
using System.Reflection;
#endregion

namespace GCBM
{
    public partial class frmMain : Form
    {
        #region Properties
        private static string GET_CURRENT_PATH       = Directory.GetCurrentDirectory();
        private static string TEMP_DIR               = @"\temp";
        private static string COVERS_DIR             = @"\covers\cache";
        private static string MEDIA_DIR              = @"\media\covers";
        private static string CULTURE_CURRENT        = "pt-BR";
        private static string PROG_UPDATE            = "08/05/2022";
        //private static string CURRENT_DIRECTORY;
        //private static string STANDARD_DIRECTORY;
        //private static string FILE_TDBXML;
        //private static string LOG_LEVEL;
        //private static string CULTURE_LANG;
        //private static string TRANSLATOR;
        private static string RES_PATH;
        private static string IMAGE_PATH;
        private static string LINK_DOMAIN;
        private static string FLUSH_SD;
        private static string SCRUB_ALIGN;
        private static char REGION                   = 'n';
        //private const string MIN_DB_VERSION          = "1.2.0.0";
        private const string INI_FILE                = "config.ini";
        //private const string GLOBAL_INI_FILE         = "gc_global.ini";
        //private const string LOG_FILE                = "gcbm.log";
        //private const string CACHE_DIR               = "cache";
        //private const string LOCAL_FILES_DB          = "gcbm_Local.xml";
        private const string WIITDB_FILE             = "wiitdb.xml";
        private const string WIITDB_DOWNLOAD_SITE    = "https://www.gametdb.com/";
        private const string en_US = "en-US";

        //private const string TITLES_FILE             = "titles.txt";
        //private bool ENABLE_INTERNET                 = true;
        //private bool ENABLE_UPDATE_PROGRAM           = true;
        //private bool UPDATE_LOG                      = true;
        private bool ERROR                           = false;
        //private bool NETWORK_CHECK                   = true;
        //private bool EXPORT_LOG_CHECK                = true;
        //private bool CLEAR_TEMP_CHECK                = true;
        //private bool WIITDBXML_CHECK                 = true;
        private bool SPLASH_SCREEN_DONE              = false;
        private bool ROOT_OPENED                     = true;
        private bool FILENAME_SORT                   = true;
        private bool RETRIEVE_FILES_INFO             = true;
        //private int Reserved;
        private readonly Assembly assembly           = Assembly.GetExecutingAssembly();
        private readonly IniFile CONFIG_INI_FILE     = new IniFile(INI_FILE);
        private readonly CultureInfo MY_CULTURE      = new CultureInfo(CULTURE_CURRENT, false);
        private readonly ProcessStartInfo START_INFO = new ProcessStartInfo();
        private readonly WebClient NET_CLIENT        = new WebClient();
        private HttpWebResponse NET_RESPONSE         = null;
        private frmSplashScreen SPLASH_SCREEN;

        [DllImport("kernel32.dll")]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
        #endregion

        #region Flag Attributes Screensaver
        /// <summary>
        /// Flag Attributes Screensaver
        /// </summary>
        [FlagsAttribute]
        enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }
        #endregion

        #region Main Form
        /// <summary>
        /// Main constructor method of the class.
        /// No argument parameters.
        /// </summary>
        public frmMain()
        {
            InitializeComponent();

            this.Text = "GameCube Backup Manager 2022 - " + VERSION() + " - 64-bit";

            // Splash Screen
            if (CONFIG_INI_FILE.IniReadBool("SEVERAL", "Welcome") == false)
            {
                // Splash Screen
                this.Load += new EventHandler(HandleFormLoad);
                this.SPLASH_SCREEN = new frmSplashScreen();
            }
            else
            {
                NetworkCheck();
            }

            if (!File.Exists(INI_FILE))
            {
                DefaultConfigSave();
            }

            LoadConfigFile();
            AboutTranslator();
            //DetectOSLanguage();
            AdjustLanguage();
            UpdateProgram();
            LoadDatabaseXML();
            DisabeScreensaver();
            GetAllDrives();
            CurrentYear();
            RegisterHeaderLog();
            RequiredDirectories();
            DisableOptionsGame(dgvGameList);
            tscbDiscDrive.SelectedIndex = 0;
            cbFilterDatabase.SelectedIndex = 0;

            if (!File.Exists(WIITDB_FILE))
            {
                CheckWiiTdbXml();
            }

            // DISABLED
            tsmiExportCSV.Enabled = false;
            tsmiExportHTML.Enabled = false;
            //tsmiElfDol.Enabled = false;
            tsmiDolphinEmulator.Enabled = false;
            tsmiBurnMedia.Enabled = false;
            tsmiManageApp.Enabled = false;
            tsmiCreatePackage.Enabled = false;
            // HIDE
            tsmiExportCSV.Visible = false;
            tsmiExportHTML.Visible = false;
            //tsmiElfDol.Visible = false;
            tsmiDolphinEmulator.Visible = false;
            tsmiBurnMedia.Visible = false;
            tsmiManageApp.Visible = false;
            tsmiCreatePackage.Visible = false;
        }
        #endregion

        #region Main Form Closing
        /// <summary>
        /// Main Form Closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Icon = null;
                notifyIcon.Dispose();
            }
            ClearTemp();
            ExportLOG(1);
        }
        #endregion

        #region Program Version
        /// <summary>
        /// Get the program version directly from the Assembly.
        /// </summary>
        /// <returns></returns>
        private string VERSION()
        {
            string PROG_VERSION = assembly.GetName().Version.ToString();
            return PROG_VERSION;
        }
        #endregion

        #region About Translator
        /// <summary>
        /// About Translator
        /// </summary>
        private void AboutTranslator()
        {
            if (File.Exists("config.ini"))
            {
                if (CONFIG_INI_FILE.IniReadString("GCBM", "ProgVersion", "") != VERSION())
                {
                    CONFIG_INI_FILE.IniWriteString("GCBM", "ProgVersion", VERSION());
                }

                if (CONFIG_INI_FILE.IniReadString("GCBM", "Language", "") != GCBM.Properties.Resources.GCBM_Language)
                {
                    CONFIG_INI_FILE.IniWriteString("GCBM", "Language", GCBM.Properties.Resources.GCBM_Language);
                }

                if (CONFIG_INI_FILE.IniReadString("GCBM", "TranslatedBy", "") != GCBM.Properties.Resources.GCBM_TranslatedBy)
                {
                    CONFIG_INI_FILE.IniWriteString("GCBM", "TranslatedBy", GCBM.Properties.Resources.GCBM_TranslatedBy);
                }
            }
        }
        #endregion

        #region Adjust Language
        /// <summary>
        /// Set the selected language
        /// </summary>
        private void AdjustLanguage()
        {
            switch (CONFIG_INI_FILE.IniReadInt("LANGUAGE", "ConfigLanguage"))
            {
                case 0:
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("pt-BR");
                    this.Controls.Clear();
                    InitializeComponent();
                    break;
                case 1:
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                    this.Controls.Clear();
                    InitializeComponent();
                    break;
                case 2:
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("es");
                    this.Controls.Clear();
                    InitializeComponent();
                    break;
                case 3:
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("ko");
                    this.Controls.Clear();
                    InitializeComponent();
                    break;
            }
        }
        #endregion

        #region Detect OS Language
        private void DetectOSLanguage()
        {
            switch (CultureInfo.InstalledUICulture.IetfLanguageTag)
            {
                case "en-US":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                    this.Controls.Clear();
                    InitializeComponent();
                    break;
                case "es":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("es");
                    this.Controls.Clear();
                    InitializeComponent();
                    break;
                case "ko":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("ko");
                    this.Controls.Clear();
                    InitializeComponent();
                    break;
                default:
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("pt-BR");
                    this.Controls.Clear();
                    InitializeComponent();
                    break;
            }

            // Nome em inlês
            //tbLog.AppendText(System.Globalization.CultureInfo.InstalledUICulture.EnglishName + Environment.NewLine);
            // Nome local
            //tbLog.AppendText(System.Globalization.CultureInfo.InstalledUICulture.NativeName + Environment.NewLine);
            // ISO
            //tbLog.AppendText(System.Globalization.CultureInfo.InstalledUICulture.ThreeLetterISOLanguageName + Environment.NewLine);
            // RFC <-- usar este aqui!!!
            //tbLog.AppendText(System.Globalization.CultureInfo.InstalledUICulture.IetfLanguageTag + Environment.NewLine);
        }
        #endregion

        #region Splash Ssreen
        /// <summary>
        /// Load the Splash Screen form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleFormLoad(object sender, EventArgs e)
        {
            this.Hide();
            Thread thread = new Thread(new ThreadStart(this.ShowSplashScreen));
            thread.Start();
            Hardworker worker = new Hardworker();
            worker.ProgressChanged += (o, ex) =>
            {
                this.SPLASH_SCREEN.UpdateProgress(ex.Progress);
            };
            worker.HardWorkDone += (o, ex) =>
            {
                SPLASH_SCREEN_DONE = true;
                //this.Show();
                if (SPLASH_SCREEN_DONE == true)
                {
                    this.Show();
                    NetworkCheck();
                }
            };
            worker.DoHardWork();
        }

        /// <summary>
        /// Display and close the Splash Screen form.
        /// </summary>
        private void ShowSplashScreen()
        {
            SPLASH_SCREEN.Show();
            while (!SPLASH_SCREEN_DONE)
            {
                Application.DoEvents();
            }
            SPLASH_SCREEN.Close();
            this.SPLASH_SCREEN.Dispose();
        }
        #endregion

        #region Update Program
        /// <summary>
        /// Adjust program update system
        /// </summary>
        private void UpdateProgram()
        {
            if (CONFIG_INI_FILE.IniReadBool("UPDATES", "UpdateServerProxy") == true)
            {
                if (CONFIG_INI_FILE.IniReadString("UPDATES", "ServerProxy", "") != string.Empty &&
                    CONFIG_INI_FILE.IniReadString("UPDATES", "UserProxy", "") != string.Empty &&
                    CONFIG_INI_FILE.IniReadString("UPDATES", "PassProxy", "") != string.Empty)
                {
                    var proxy = new WebProxy(CONFIG_INI_FILE.IniReadString("UPDATES", "ServerProxy", ""), true)
                    {
                        Credentials = new NetworkCredential(CONFIG_INI_FILE.IniReadString("UPDATES", "UserProxy", ""),
                        CONFIG_INI_FILE.IniReadString("UPDATES", "PassProxy", ""))
                    };
                    AutoUpdater.Proxy = proxy;
                }
            }

            // Ativa o suporte as atualizações
            if (CONFIG_INI_FILE.IniReadBool("UPDATES", "UpdateVerifyStart") == true)
            {
                int timeInterval = 0;

                if (CONFIG_INI_FILE.IniReadInt("UPDATES", "VerificationInterval") == 0)
                {
                    timeInterval = 10; // 5 minutos
                }
                else if (CONFIG_INI_FILE.IniReadInt("UPDATES", "VerificationInterval") == 1)
                {
                    timeInterval = 20; // 10 minutos
                }
                else if (CONFIG_INI_FILE.IniReadInt("UPDATES", "VerificationInterval") == 2)
                {
                    timeInterval = 30; // 30 minutos
                }
                else if (CONFIG_INI_FILE.IniReadInt("UPDATES", "VerificationInterval") == 3)
                {
                    timeInterval = 60; // 30 minutos
                }
                else if (CONFIG_INI_FILE.IniReadInt("UPDATES", "VerificationInterval") == 4)
                {
                    timeInterval = 120; // 1 hora
                }
                else if (CONFIG_INI_FILE.IniReadInt("UPDATES", "VerificationInterval") == 5)
                {
                    timeInterval = 240; // 2 horas
                }
                else if (CONFIG_INI_FILE.IniReadInt("UPDATES", "VerificationInterval") == 6)
                {
                    timeInterval = 360; // 3 horas
                }
                else
                {
                    timeInterval = 480; // 4 horas
                }

                // Suporte as atualizações do canal Beta
                if (CONFIG_INI_FILE.IniReadBool("UPDATES", "UpdateBetaChannel") == true)
                {
                    System.Timers.Timer timer = new System.Timers.Timer
                    {
                        Interval = 2 * 15000 * timeInterval,
                        SynchronizingObject = this
                    };

                    timer.Elapsed += delegate
                    {
                        AutoUpdater.Start("https://raw.githubusercontent.com/AxionDrak/GameCube-Backup-Manager/main/BetaChannel/AutoUpdaterBeta.xml");
                        AutoUpdater.ShowRemindLaterButton = false;
                        AutoUpdater.RunUpdateAsAdmin = false;
                        AutoUpdater.ReportErrors = true;
                        //AutoUpdater.UpdateFormSize = new Size(500, 400);
                    };
                    timer.Start();
                }
                else
                {   // Suporte as atualizações do canal Release (Padrão)
                    System.Timers.Timer timer = new System.Timers.Timer
                    {
                        Interval = 2 * 15000 * timeInterval,
                        SynchronizingObject = this
                    };

                    timer.Elapsed += delegate
                    {
                        AutoUpdater.Start("https://raw.githubusercontent.com/AxionDrak/GameCube-Backup-Manager/main/AutoUpdaterRelease.xml");
                        AutoUpdater.ShowRemindLaterButton = false;
                        AutoUpdater.RunUpdateAsAdmin = false;
                        AutoUpdater.ReportErrors = true;
                        //AutoUpdater.UpdateFormSize = new Size(500, 400);
                    };
                    timer.Start();
                }
            }
        }
        #endregion

        #region DateString
        // Get the date and time
        private static string DateString()
        {
            DateTime dt = DateTime.Now;
            //int dts = dt.Millisecond;
            //string dtnew = dt.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss") + "." + dts.ToString();
            string dtnew = dt.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss");
            return dtnew;
        }
        #endregion

        #region Disable Screensaver
        // Disable screen saver
        private void DisabeScreensaver()
        {
            if (CONFIG_INI_FILE.IniReadBool("SEVERAL", "Screensaver") == true)
            {
                // Desativa o screensaver
                SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_CONTINUOUS);
            }
            else
            {
                // Ativa o screensaver
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
            }
        }
        #endregion

        #region Network Check
        /// <summary>
        /// Checks for the existence of a network connection.
        /// </summary>
        private void NetworkCheck()
        {
            if (CONFIG_INI_FILE.IniReadBool("SEVERAL", "NetVerify") == true)
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    pbNetStatus.Image = Properties.Resources.not_conected_16;
                    lblNetStatus.ForeColor = System.Drawing.Color.Red;
                    lblNetStatus.Text = GCBM.Properties.Resources.NetworkCheck_NetStatus_Offline;
                    GlobalNotifications(GCBM.Properties.Resources.NetworkCheck_NetStatus_Offline, ToolTipIcon.Info);
                }
                else
                {
                    pbNetStatus.Image = Properties.Resources.conected_16;
                    lblNetStatus.ForeColor = System.Drawing.Color.Black;
                    lblNetStatus.Text = GCBM.Properties.Resources.NetworkCheck_NetStatus_Online;
                    GlobalNotifications(GCBM.Properties.Resources.NetworkCheck_NetStatus_Online, ToolTipIcon.Info);
                }
                return;
            }
            else
            {
                pbNetStatus.Image = Properties.Resources.not_conected_16;
                lblNetStatus.ForeColor = System.Drawing.Color.Red;
                lblNetStatus.Text = GCBM.Properties.Resources.NetworkCheck_NetStatus_Offline;
                GlobalNotifications(GCBM.Properties.Resources.NetworkCheck_NetStatus_Offline, ToolTipIcon.Info);
            }
        }
        #endregion

        #region Register Log
        /// <summary>
        /// Log record.
        /// </summary>
        private void RegisterHeaderLog()
        {
            // Assembly
            var _version = assembly.GetName();
            // Log
            tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.RegisterHeaderLog_GcbmLogCreated + Environment.NewLine);
            // Version number of the OS
            tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.RegisterHeaderLog_OSVersion + Environment.OSVersion.ToString() + Environment.NewLine);
            // Major, minor, build, and revision numbers of the CLR
            tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.RegisterHeaderLog_CLRVersion + Environment.Version.ToString() + Environment.NewLine);
            // Amount of physical memory mapped to the process context
            tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.RegisterHeaderLog_WorkingSet + Environment.WorkingSet + Environment.NewLine);
            // Array of string containing the names of the logical drives
            tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.RegisterHeaderLog_LogicalUnits + String.Join(", ", Environment.GetLogicalDrives()) + Environment.NewLine);
            // Gets the name of the program.
            tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.RegisterHeaderLog_ProgramName + AssemblyProduct.ToString() + Environment.NewLine);
            // Gets the program version.
            tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.RegisterHeaderLog_ApplicationVersion + _version + Environment.NewLine);
            // Gets the current program directory.
            tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.RegisterHeaderLog_CurrentProgramDirectory + GET_CURRENT_PATH + Environment.NewLine);
        }
        #endregion

        #region Assembly Product
        /// <summary>
        /// Gets the attributes of the Assembly.
        /// </summary>
        private static string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return String.Empty;
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }
        #endregion

        #region Disable Options
        /// <summary>
        /// Disables options on the game selection screen
        /// </summary>
        /// <param name="dgv"></param>
        private void DisableOptionsGame(DataGridView dgv)
        {
            if (dgv == dgvGameList)
            {
                // Main Menu Game
                btnGameInstallExactCopy.Enabled = false;
                btnGameInstallScrub.Enabled = false;
                tsmiReloadGameList.Enabled = false;
                tsmiSelectGameList.Enabled = false;
                //tsmiGameListSelectAll.Enabled = false;
                //tsmiGameListSelectNone.Enabled = false;
                tsmiGameListDeleteAllFiles.Enabled = false;
                tsmiGameListDeleteSelectedFile.Enabled = false;
                tsmiGameListHashSHA1.Enabled = false;
                tsmiDownloadCoversSelectedGame.Enabled = false;
                tsmiSyncDownloadAllDiscOnly3DCovers.Enabled = false;
                //tsmiLanguage.Enabled = false;
                tsmiDownloadCoversSelectedGame.Enabled = false;
                tsmiSyncDownloadDiscOnly3DCovers.Enabled = false;
                tsmiGameInfo.Enabled = false;
                tsmiTransferDeviceCovers.Enabled = false;
            }
            else
            {
                // Main Menu Game Disc
                //tsmiReloadGameListDisc.Enabled = false;
                tsmiSelectGameDisc.Enabled = false;
                tsmiGameDiscDeleteAllFiles.Enabled = false;
                tsmiGameDiscDeleteSelectedFile.Enabled = false;
                tsmiGameDiscExportList.Enabled = false;
                tsmiGameDiscAllHashSHA1.Enabled = false;
                tsmiGameDiscHashSHA1.Enabled = false;
                tsmiGameDiscAllHashMD5.Enabled = false;
                tsmiGameDiscHashMD5.Enabled = false;
                tsmiGameDiscRecalculateAllMD5.Enabled = false;
                tsmiGameDiscRecalculateSelectedGameMD5.Enabled = false;
            }
        }
        #endregion

        #region Enable Options
        /// <summary>
        /// Enable options on the games tab.
        /// </summary>
        private void EnableOptionsGameList()
        {
            // Main Menu Game
            btnGameInstallExactCopy.Enabled = true;
            btnGameInstallScrub.Enabled = true;
            tsmiReloadGameList.Enabled = true;
            tsmiSelectGameList.Enabled = true;
            tsmiGameListDeleteAllFiles.Enabled = true;
            tsmiGameListDeleteSelectedFile.Enabled = true;
            tsmiGameListHashSHA1.Enabled = true;
            tsmiSyncDownloadAllDiscOnly3DCovers.Enabled = true;
            tsmiSyncDownloadAllCovers.Enabled = true;
            //tsmiLanguage.Enabled = true;
            tsmiDownloadCoversSelectedGame.Enabled = true;
            tsmiSyncDownloadDiscOnly3DCovers.Enabled = true;
            tsmiGameInfo.Enabled = true;
            tsmiTransferDeviceCovers.Enabled = true;
        }
        #endregion

        #region Reset Options
        /// <summary>
        /// Reset options game list.
        /// </summary>
        private void ResetOptions()
        {
            // Main Menu Game
            tbIDName.Clear();
            tbIDRegion.Clear();
            tbIDGame.Clear();
            tbIDDiscID.Clear();
            tbIDMakerCode.Clear();
            pbGameDisc.LoadAsync(GET_CURRENT_PATH + @"\media\covers\disc.png");
            pbGameCover3D.LoadAsync(GET_CURRENT_PATH + @"\media\covers\3d.png");
            pbWebGameID.Enabled = false;
            pbWebGameID.Image = Resources.globe_earth_grayscale_64;
            dgvGameList.DataSource = null;
            dgvGameList.Columns.Clear();
            dgvGameList.Rows.Clear();
            dgvGameList.Refresh();
        }
        #endregion

        #region Process Task Delay
        /// <summary>
        /// Process Task Delay
        /// </summary>
        /// <returns></returns>
        async Task ProcessTaskDelay()
        {
            await Task.Delay(5000);
        }
        #endregion

        #region List ISO/GCM Files
        /// <summary>
        /// List existing ISO and GCM files in the directory and subdirectories.
        /// </summary>
        private void ListIsoFile()
        {
            tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.FoundIsoGcmFiles + Environment.NewLine);

            foreach (DataGridViewRow dgvResultRow in dgvGameList.Rows)
            {
                tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.Info + dgvGameList.Rows[dgvResultRow.Index].Cells[1].Value.ToString() + Environment.NewLine);
            }
        }
        #endregion

        #region Load Database XML
        /// <summary>
        /// Load Database XML
        /// </summary>
        private void LoadDatabaseXML()
        {
            if (File.Exists(WIITDB_FILE))
            {
                if (CONFIG_INI_FILE.IniReadBool("SEVERAL", "LoadDatabase") == true)
                {
                    // PERFEITO - NÃO ALTERAR!!!
                    try
                    {
                        lvDatabase.View = View.Details;
                        lvDatabase.GridLines = true;
                        lvDatabase.FullRowSelect = true;
                        lvDatabase.Columns.Add(GCBM.Properties.Resources.LoadDatabase_IDGameCode, 70);
                        lvDatabase.Columns.Add(GCBM.Properties.Resources.LoadDatabase_GameTitle, 210);
                        lvDatabase.Columns.Add(GCBM.Properties.Resources.LoadDatabase_Region, 70);
                        lvDatabase.Columns.Add(GCBM.Properties.Resources.LoadDatabase_Type, 80);
                        lvDatabase.Columns.Add(GCBM.Properties.Resources.LoadDatabase_Developer, 200);
                        lvDatabase.Columns.Add(GCBM.Properties.Resources.LoadDatabase_Editor, 200);

                        using (DataSet ds = new DataSet())
                        {
                            ListViewItem itemXml;
                            ds.ReadXml(@"wiitdb.xml");

                            foreach (DataRow dr in ds.Tables["game"].Rows)
                            {
                                itemXml = new ListViewItem(new string[]
                                {
                                    dr["id"].ToString(),
                                    dr["name"].ToString(),
                                    dr["region"].ToString(),
                                    dr["type"].ToString(),
                                    dr["developer"].ToString(),
                                    dr["publisher"].ToString()
                                });

                                lvDatabase.Items.Add(itemXml);
                            }

                            foreach (DataRow dr in ds.Tables["WiiTDB"].Rows)
                            {
                                lblDatabaseTotal.Text = dr["games"].ToString() + " Total";
                            }

                            ds.Dispose();
                            ds.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        //CheckWiiTdbXml();
                        GlobalNotifications(ex.Message, ToolTipIcon.Error);
                    }   
                }
            }
        }
        #endregion

        #region Current Year
        /// <summary>
        /// Get the current year.
        /// </summary>
        private void CurrentYear()
        {
            DateTime _currentYear = DateTime.Now;
            tsslCurrentYear.Text = "Copyright © 2019 - " + _currentYear.Year.ToString() + " Laete Meireles";
        }
        #endregion

        #region Load Config File
        /// <summary>
        /// Reads and loads the contents of the INI file.
        /// </summary>
        private void LoadConfigFile()
        {
            if (File.Exists(GET_CURRENT_PATH + @"\config.ini"))
            {
                if (CONFIG_INI_FILE.IniReadBool("SEVERAL", "WindowMaximized") == true)
                {
                    this.WindowState = FormWindowState.Maximized;
                }
            }
        }
        #endregion

        #region Default Config Save
        /// <summary>
        /// Default Config Save
        /// </summary>
        private void DefaultConfigSave()
        {
            // GCBM
            CONFIG_INI_FILE.IniWriteString("GCBM", "ProgUpdated", PROG_UPDATE);
            CONFIG_INI_FILE.IniWriteString("GCBM", "ProgVersion", VERSION());
            CONFIG_INI_FILE.IniWriteString("GCBM", "ConfigUpdated", DateTime.Now.ToString("dd/MM/yyyy"));
            CONFIG_INI_FILE.IniWriteString("GCBM", "Language", GCBM.Properties.Resources.GCBM_Language);
            CONFIG_INI_FILE.IniWriteString("GCBM", "TranslatedBy", GCBM.Properties.Resources.GCBM_TranslatedBy);
            // General          
            CONFIG_INI_FILE.IniWriteBool("GENERAL", "DiscClean", true);
            CONFIG_INI_FILE.IniWriteBool("GENERAL", "DiscDelete", false);
            CONFIG_INI_FILE.IniWriteBool("GENERAL", "ExtractZip", false);
            CONFIG_INI_FILE.IniWriteBool("GENERAL", "Extract7z", false);
            CONFIG_INI_FILE.IniWriteBool("GENERAL", "ExtractRar", false);
            CONFIG_INI_FILE.IniWriteBool("GENERAL", "ExtractBZip2", false);
            CONFIG_INI_FILE.IniWriteBool("GENERAL", "ExtractSplitFile", false);
            CONFIG_INI_FILE.IniWriteBool("GENERAL", "ExtractNwb", false);
            CONFIG_INI_FILE.IniWriteInt("GENERAL", "FileSize", 0);
            CONFIG_INI_FILE.IniWriteString("GENERAL", "TemporaryFolder", GET_CURRENT_PATH + TEMP_DIR);
            // Several
            CONFIG_INI_FILE.IniWriteInt("SEVERAL", "AppointmentStyle", 0);
            CONFIG_INI_FILE.IniWriteBool("SEVERAL", "CheckMD5", false);
            CONFIG_INI_FILE.IniWriteBool("SEVERAL", "CheckSHA1", false);
            CONFIG_INI_FILE.IniWriteBool("SEVERAL", "CheckNotify", true);
            CONFIG_INI_FILE.IniWriteBool("SEVERAL", "NetVerify", false);
            CONFIG_INI_FILE.IniWriteBool("SEVERAL", "RecursiveMode", true);
            CONFIG_INI_FILE.IniWriteBool("SEVERAL", "TemporaryBuffer", false);
            CONFIG_INI_FILE.IniWriteBool("SEVERAL", "WindowMaximized", false);
            CONFIG_INI_FILE.IniWriteBool("SEVERAL", "Welcome", false);
            CONFIG_INI_FILE.IniWriteBool("SEVERAL", "Screensaver", false);
            CONFIG_INI_FILE.IniWriteBool("SEVERAL", "LoadDatabase", true);
            CONFIG_INI_FILE.IniWriteBool("SEVERAL", "MultipleInstances", false);
            // TransferSystem
            CONFIG_INI_FILE.IniWriteBool("TRANSFERSYSTEM", "FST", false);
            CONFIG_INI_FILE.IniWriteBool("TRANSFERSYSTEM", "ScrubFlushSD", false);
            CONFIG_INI_FILE.IniWriteInt("TRANSFERSYSTEM", "ScrubAlign", 0);
            CONFIG_INI_FILE.IniWriteString("TRANSFERSYSTEM", "ScrubFormat", "DiscEx");
            CONFIG_INI_FILE.IniWriteInt("TRANSFERSYSTEM", "ScrubFormatIndex", 1);
            CONFIG_INI_FILE.IniWriteBool("TRANSFERSYSTEM", "Wipe", false);
            CONFIG_INI_FILE.IniWriteBool("TRANSFERSYSTEM", "XCopy", true);
            // Covers
            CONFIG_INI_FILE.IniWriteBool("COVERS", "DeleteCovers", false);
            CONFIG_INI_FILE.IniWriteBool("COVERS", "CoverRecursiveSearch", false);
            CONFIG_INI_FILE.IniWriteBool("COVERS", "TransferCovers", false);
            CONFIG_INI_FILE.IniWriteBool("COVERS", "WiiFlowCoverUSBLoader", false);
            CONFIG_INI_FILE.IniWriteBool("COVERS", "GXCoverUSBLoader", true);
            CONFIG_INI_FILE.IniWriteString("COVERS", "CoverDirectoryCache", GET_CURRENT_PATH + COVERS_DIR);
            CONFIG_INI_FILE.IniWriteString("COVERS", "WiiFlowCoverDirectoryDisc", "");
            CONFIG_INI_FILE.IniWriteString("COVERS", "WiiFlowCoverDirectory2D", "");
            CONFIG_INI_FILE.IniWriteString("COVERS", "WiiFlowCoverDirectory3D", "");
            CONFIG_INI_FILE.IniWriteString("COVERS", "WiiFlowCoverDirectoryFull", "");
            CONFIG_INI_FILE.IniWriteString("COVERS", "GXCoverDirectoryDisc", "");
            CONFIG_INI_FILE.IniWriteString("COVERS", "GXCoverDirectory2D", "");
            CONFIG_INI_FILE.IniWriteString("COVERS", "GXCoverDirectory3D", "");
            CONFIG_INI_FILE.IniWriteString("COVERS", "GXCoverDirectoryFull", "");
            // Titles
            CONFIG_INI_FILE.IniWriteBool("TITLES", "GameCustomTitles", false);
            CONFIG_INI_FILE.IniWriteBool("TITLES", "GameTdbTitles", false);
            CONFIG_INI_FILE.IniWriteBool("TITLES", "GameInternalName", true);
            CONFIG_INI_FILE.IniWriteBool("TITLES", "GameXmlName", false);
            CONFIG_INI_FILE.IniWriteString("TITLES", "LocationTitles", @"%APP%\titles.txt");
            CONFIG_INI_FILE.IniWriteString("TITLES", "LocationCustomTitles", @"%APP%\custom-titles.txt");
            CONFIG_INI_FILE.IniWriteInt("TITLES", "TitleLanguage", 0);
            // Updates
            CONFIG_INI_FILE.IniWriteBool("UPDATES", "UpdateVerifyStart", false);
            CONFIG_INI_FILE.IniWriteBool("UPDATES", "UpdateBetaChannel", false);
            CONFIG_INI_FILE.IniWriteBool("UPDATES", "UpdateFileLog", false);
            CONFIG_INI_FILE.IniWriteBool("UPDATES", "UpdateServerProxy", false);
            CONFIG_INI_FILE.IniWriteString("UPDATES", "ServerProxy", "");
            CONFIG_INI_FILE.IniWriteString("UPDATES", "UserProxy", "");
            CONFIG_INI_FILE.IniWriteString("UPDATES", "PassProxy", "");
            CONFIG_INI_FILE.IniWriteInt("UPDATES", "VerificationInterval", 0);
            // Manager Log
            CONFIG_INI_FILE.IniWriteInt("MANAGERLOG", "LogLevel", 0);
            CONFIG_INI_FILE.IniWriteBool("MANAGERLOG", "LogSystemConsole", false);
            CONFIG_INI_FILE.IniWriteBool("MANAGERLOG", "LogDebugConsole", false);
            CONFIG_INI_FILE.IniWriteBool("MANAGERLOG", "LogWindow", false);
            CONFIG_INI_FILE.IniWriteBool("MANAGERLOG", "LogFile", true);
            // Language
            CONFIG_INI_FILE.IniWriteInt("LANGUAGE", "ConfigLanguage", 0);
        }
        #endregion

        // REWRITE FUNCTION - Reload DataGridView List

        #region Reload DataGridView List
        /// <summary>
        /// Reloads the contents of the DataGridView Games List.
        /// </summary>
        private void ReloadDataGridViewGameList()
        {
            if (dgvGameList.RowCount != 0)
            {
                try
                {
                    DirectoryOpenGameList(dgvGameList.CurrentRow.Cells[4].Value.ToString());

                    if (ERROR == false)
                    {
                        LoadCover(tbIDGame.Text);
                    }
                    // pictureBox GameID
                    if (pbWebGameID.Enabled == false)
                    {
                        pbWebGameID.Enabled = true;
                        pbWebGameID.Image = Resources.globe_earth_color_64;
                    }

                }
                catch (Exception ex)
                {
                    tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.Info + ex.Message);
                    GlobalNotifications(ex.Message, ToolTipIcon.Error);
                }
            }
        }

        /// <summary>
        /// Reloads the contents of the DataGridView Disc List.
        /// </summary>
        private void ReloadDataGridViewDiscList()
        {
            if (dgvGameListDisc.RowCount != 0)
            {
                try
                {
                    DirectoryOpenDiscList(dgvGameListDisc.CurrentRow.Cells[4].Value.ToString());

                    if (ERROR == false)
                    {
                        //LoadCover();
                        LoadCover(tbIDGameDisc.Text);
                    }
                    // pictureBox GameID
                    if (pbWebGameDiscID.Enabled == false)
                    {
                        pbWebGameDiscID.Enabled = true;
                        pbWebGameDiscID.Image = Resources.globe_earth_color_64;
                    }

                }
                catch (Exception ex)
                {
                    tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.Info + ex.Message);
                    GlobalNotifications(ex.Message, ToolTipIcon.Error);
                }
            }
        }
        #endregion

        #region Display Files Selected
        /// <summary>
        /// Display Files Selected
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="dgv"></param>
        private void DisplayFilesSelected(string sourceFolder, DataGridView dgv)
        {
            var filters = new String[] { "iso", "gcm" };
            bool isRecursive;

            if (dgv == dgvGameList)
            {
                if (dgv.RowCount == 0)
                {
                    btnGameInstallExactCopy.Enabled = true;
                    btnGameInstallScrub.Enabled = true;
                    tsmiReloadGameList.Enabled = true;
                    tsmiGameListSelectAll.Enabled = true;
                    tsmiGameListSelectNone.Enabled = true;
                    tsmiGameListDeleteAllFiles.Enabled = true;
                    tsmiGameListDeleteSelectedFile.Enabled = true;
                    //tsmiGameListAllHashSHA1.Enabled = true;
                    tsmiGameListHashSHA1.Enabled = true;
                    tsmiDownloadCoversSelectedGame.Enabled = true;
                    tsmiSyncDownloadDiscOnly3DCovers.Enabled = true;
                    tsmiGameInfo.Enabled = true;
                    tsmiTransferDeviceCovers.Enabled = true;
                }
            }

            if (dgv == dgvGameListDisc)
            {
                isRecursive = true;
            }
            else
            {
                isRecursive = false;
            }

            string[] files = GetFilesFolder(sourceFolder, filters, isRecursive);
            
            if (dgv == dgvGameListDisc)
            {
                //tsmiReloadGameListDisc.Enabled = true;
                try
                {
                    if (dgv.RowCount == 0)
                    {
                        tsmiReloadGameListDisc.Enabled = true;
                    }
                }
                catch (Exception ex)
                {

                }
            }

            // Groups files in the folder by extension.
            var filesGroupedByExtension = files.Select(
                arq => Path.GetExtension(arq).TrimStart('.').ToLower(MY_CULTURE)).GroupBy(x => x, (ext, extCnt) =>
                new { _fileExtension = ext, Counter = extCnt.Count() });

            // Scroll through the result and display the values.
            foreach (var _files in filesGroupedByExtension)
            {
                tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.DisplayFilesSelected_Found_String1 + _files.Counter + 
                    GCBM.Properties.Resources.DisplayFilesSelected_Found_String2
                    + _files._fileExtension.ToUpper(MY_CULTURE) + Environment.NewLine);

                GlobalNotifications(GCBM.Properties.Resources.DisplayFilesSelected_Found_String3 + _files.Counter + 
                    GCBM.Properties.Resources.DisplayFilesSelected_Found_String2 + _files._fileExtension.ToUpper(MY_CULTURE), ToolTipIcon.Info);

            }
            //txtLog.AppendText(Environment.NewLine + Environment.NewLine);

            // Creates a DataTable with file data.
            DataTable _table = new DataTable();
            _table.Columns.Add(GCBM.Properties.Resources.DisplayFilesSelected_FileName);
            _table.Columns.Add(GCBM.Properties.Resources.LoadDatabase_Type);
            _table.Columns.Add(GCBM.Properties.Resources.DisplayFilesSelected_Size);
            _table.Columns.Add(GCBM.Properties.Resources.DisplayFilesSelected_FilePath);

            FileInfo _file = null;
            for (int i = 0; i < files.Length; i++)
            {
                //FileInfo _file = new FileInfo(files[i]);
                _file = new FileInfo(files[i]);
                string _getSize = DisplayFormatFileSize(_file.Length, CONFIG_INI_FILE.IniReadInt("GENERAL", "FileSize"));
                //string _getSize = BytesToGB(_file.Length);                
                // 4° coluna
                _table.Rows.Add(_file.Name, _file.Extension.Substring(1, 3).Trim().ToUpper(MY_CULTURE), _getSize, _file.FullName);
                //_table.Rows.Add(_file.Name, _file.Extension.Substring(1, 3).Trim().ToUpper(myCulture), _getSize);
            }

            //if(dgvGameList.SelectionMode == DataGridViewSelectionMode.RowHeaderSelect){
            //    MessageBox.Show("O modo de seleção é RowHeaderSelect");
            //}
            //else
            //{
            //    MessageBox.Show("O modo de seleção NÃO é RowHeaderSelect");
            //}

            // Displays data in DataGridView.
            dgv.DataSource = _table;
            dgv.RowHeadersVisible = false;
            dgv.ColumnHeadersVisible = true;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            //Checkbox
            dgv.Columns[0].ReadOnly = false;
            dgv.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgv.Columns[0].Width = 30;
            //Nome do Arquivo
            dgv.Columns[1].ReadOnly = true;
            dgv.Columns[1].Width = 150;
            //Tipo
            dgv.Columns[2].ReadOnly = true;
            dgv.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgv.Columns[2].Width = 50;
            //Tamanho
            dgv.Columns[3].ReadOnly = true;
            dgv.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgv.Columns[3].Width = 70;
            //Caminho do Arquivo
            dgv.Columns[4].ReadOnly = true;
            //dgvGameList.Columns[4].Width = 100;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            if (dgv == dgvGameList)
            {
                ReloadDataGridViewGameList();
            }   
        }
        #endregion

        #region Get Files Folder
        /// <summary>
        /// Get files from folder origem.
        /// </summary>
        /// <param name="rootFolder"></param>
        /// <param name="filters"></param>
        /// <param name="isRecursive"></param>
        /// <returns></returns>
        private string[] GetFilesFolder(string rootFolder, string[] filters, bool isRecursive)
        {
            List<string> filesFound = new List<string>();
            // Sets options for displaying root folder images.

            if (CONFIG_INI_FILE.IniReadBool("SEVERAL", "RecursiveMode") == true)
            {
                isRecursive = true;
            }
            else
            {
                isRecursive = false;
            }

            var optionSearch = isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var filter in filters)
            {
                try
                {
                    filesFound.AddRange(Directory.GetFiles(rootFolder, string.Format("*.{0}", filter), optionSearch));
                }
                catch (Exception ex)
                {

                }
            }
            return filesFound.ToArray();
        }

        /// <summary>
        ///  Get files from folder destin.
        /// </summary>
        /// <param name="rootFolder"></param>
        /// <param name="filters"></param>
        /// <param name="isRecursive"></param>
        /// <returns></returns>
        private string[] GetFilesFolderDisc(string rootFolder, string[] filters, bool isRecursive)
        {
            List<string> filesFound = new List<string>();
            // Sets options for displaying root folder images.
            //isRecursive = false;

            var optionSearch = isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var filter in filters)
            {
                filesFound.AddRange(Directory.GetFiles(rootFolder, string.Format("*.{0}", filter), optionSearch));
            }
            return filesFound.ToArray();
        }
        #endregion

        #region Get All Drives
        /// <summary>
        /// Gets a list of all connected drives.
        /// </summary>
        private void GetAllDrives()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            tscbDiscDrive.Items.Clear();
            tscbDiscDrive.Items.Add(GCBM.Properties.Resources.GetAllDrives_Inactive);
            tscbDiscDrive.SelectedIndex = 0;

            foreach (DriveInfo d in allDrives)
            {
                if (d.IsReady == true)
                {
                    tscbDiscDrive.Items.Add(d.Name);
                }
            }
        }
        #endregion

        //#region Display Format File Size (Basic Version - Automatic)
        ///// <summary>
        ///// Display Format File Size (Basic Version - Automatic)
        ///// </summary>
        ///// <param name="bytes"></param>
        ///// <returns></returns>
        //public static string BytesToGB(long bytes)
        //{
        //    string result;
        //    double _bytes = bytes;
        //    string[] array_fs = new string[5] { "B", "KB", "MB", "GB", "TB" };
        //    int num2_fs = 0;

        //    while (_bytes >= 1024.0 && num2_fs < array_fs.Length - 1)
        //    {
        //        num2_fs++;
        //        _bytes /= 1024.0;
        //    }
        //    result = $"{_bytes:0.##} {array_fs[num2_fs]}";

        //    return result;
        //}
        //#endregion

        #region Display Format File Size
        /// <summary>
        /// Adjusts the file size display format.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static string DisplayFormatFileSize(long i, int k)
        {
            // Obtém o valor absoluto
            long i_absolute = (i < 0 ? -i : i);
            string suffix;
            double reading;

            if (k == 0)
            {
                if (i_absolute >= 0x1000000000000000) // Exabyte
                {
                    suffix = "EB";
                    reading = (i >> 50);
                }
                else if (i_absolute >= 0x4000000000000) // Petabyte
                {
                    suffix = "PB";
                    reading = (i >> 40);
                }
                else if (i_absolute >= 0x10000000000) // Terabyte
                {
                    suffix = "TB";
                    reading = (i >> 30);
                }
                else if (i_absolute >= 0x40000000) // Gigabyte
                {
                    suffix = "GB";
                    reading = (i >> 20);
                }
                else if (i_absolute >= 0x100000) // Megabyte
                {
                    suffix = "MB";
                    reading = (i >> 10);
                }
                else if (i_absolute >= 0x400) // Kilobyte
                {
                    suffix = "KB";
                    reading = i;
                }
                else
                {
                    return i.ToString("0 bytes"); // Byte
                }
            }
            else if (k == 1) // Kilobyte
            {
                suffix = "KB";
                reading = i;
            }
            else if (k == 2) // Megabyte
            {
                suffix = "MB";
                reading = (i >> 10);
            }
            else if (k == 3) // Gigabyte
            {
                suffix = "GB";
                reading = (i >> 20);
            }
            else if (k == 4) // Terabyte
            {
                suffix = "TB";
                reading = (i >> 30);
            }
            else
            {
                return i.ToString("0 bytes"); // Byte
            }
            // Divide by 1024 to get the fractional value.
            reading = (reading / 1024);
            // Returns the suffix formatted number.
            return reading.ToString("0.## ") + suffix;
        }
        #endregion

        // REWRITE FUNCTION- Directory Open

        #region Directory Open
        /// <summary>
        /// Checks the consistency of the listed ISO and GCM files.       
        /// </summary>
        /// <param name="path"></param>
        private void DirectoryOpenGameList(string path)
        {
            //OpenFileDialog ofd;

            //if (path.Length == 0)
            //{
            //    ofd = new OpenFileDialog() { Filter = "GameCube ISO (*.iso)|*.iso|GameCube Image File (*.gcm)|*.gcm|All files (*.*)|*.*", Title = "Open image" };
            //    if (imgPath != "")
            //        ofd.FileName = imgPath;
            //    if (ofd.ShowDialog() == DialogResult.OK)
            //    {
            //        imgPath = ofd.FileName;
            //        path = imgPath;
            //    }
            //}

            if (path.Length == 0)
                return;

            IMAGE_PATH = path;

            if (CheckImage())
            {
                if (ReadImageTOC())
                {
                    if (CONFIG_INI_FILE.IniReadBool("TITLES", "GameXmlName") == true)
                    {
                        if (File.Exists(WIITDB_FILE))
                        {
                            XElement root = XElement.Load(WIITDB_FILE);
                            IEnumerable<XElement> tests = from el in root.Elements("game") where (string)el.Element("id") == tbIDGame.Text select el;
                            foreach (XElement el in tests)
                            {
                                tbIDName.Text = (string)el.Element("locale").Element("title");
                            }
                        }
                        else
                        {
                            CheckWiiTdbXml();
                        }
                    }
                    //miImageOpen.ToolTipText = imgPath;
                    ROOT_OPENED = false;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        private void DirectoryOpenDiscList(string path)
        {
            if (path.Length == 0)
                return;

            IMAGE_PATH = path;

            if (CheckImage())
            {
                if (ReadImageDiscTOC())
                {
                    if (CONFIG_INI_FILE.IniReadBool("TITLES", "GameXmlName") == true)
                    {
                        if (File.Exists(WIITDB_FILE))
                        {
                            XElement root = XElement.Load(WIITDB_FILE);
                            IEnumerable<XElement> tests = from el in root.Elements("game") where (string)el.Element("id") == tbIDGameDisc.Text select el;
                            foreach (XElement el in tests)
                            {
                                tbIDNameDisc.Text = (string)el.Element("locale").Element("title");
                            }
                        }
                        else
                        {
                            CheckWiiTdbXml();
                        }
                    }
                    //miImageOpen.ToolTipText = imgPath;
                    ROOT_OPENED = false;
                }
            }
        }
        #endregion

        #region Check Image
        /// <summary>
        /// Checks if it is a valid Nintendo GameCube file.
        /// </summary>
        /// <returns></returns>
        private bool CheckImage()
        {
            sio.FileStream fs = null;
            sio.BinaryReader br = null;

            try
            {
                fs = new sio.FileStream(IMAGE_PATH, sio.FileMode.Open, sio.FileAccess.Read, sio.FileShare.Read);
                br = new sio.BinaryReader(fs, ste.Default);

                fs.Position = 0x1c;
                if (br.ReadInt32() != 0x3d9f33c2)
                {
                    tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.NotNintendoGameCubeFile);
                    GlobalNotifications(GCBM.Properties.Resources.NotNintendoGameCubeFile + Environment.NewLine + 
                        GCBM.Properties.Resources.RecommendedDeleteOrMoveFile, ToolTipIcon.Info);

                    pbGameDisc.LoadAsync(GET_CURRENT_PATH + @"\media\covers\disc.png");
                    pbGameCover3D.LoadAsync(GET_CURRENT_PATH + @"\media\covers\3d.png");

                    ERROR = true;
                }
                else
                {
                    ERROR = false;
                }
            }
            catch (Exception ex)
            {
                GlobalNotifications(ex.Message, ToolTipIcon.Error);
                ERROR = true;
            }
            finally
            {
                if (br != null) br.Close();
                if (fs != null) fs.Close();
            }

            return !ERROR;
        }
        #endregion

        #region Load Cover
        /// <summary>
        /// Loads the respective Disk and 2D image files into the loaded ISO/GCM file.
        /// </summary>
        //private void LoadCover()
        private void LoadCover(string _idGame)
        {
            try
            {
                switch (_IDRegionCode)
                {
                    case "e":
                        LINK_DOMAIN = "US";
                        break;
                    case "p":
                        LINK_DOMAIN = "EN";
                        break;
                    case "j":
                        LINK_DOMAIN = "JA";
                        break;
                    default:
                        GlobalNotifications(GCBM.Properties.Resources.UnknownRegion, ToolTipIcon.Info);
                        break;
                }
            }
            catch (Exception ex)
            {
                tbLog.Text = ex.ToString();
            }

            if (File.Exists(GET_CURRENT_PATH + COVERS_DIR + @"\" + LINK_DOMAIN + @"\disc\" + _idGame + ".png"))
            {
                pbGameDisc.LoadAsync(GET_CURRENT_PATH + COVERS_DIR + @"\" + LINK_DOMAIN + @"\disc\" + _idGame + ".png");
            }
            else
            {
                pbGameDisc.LoadAsync(GET_CURRENT_PATH + MEDIA_DIR + @"\disc.png");
            }

            if (File.Exists(GET_CURRENT_PATH + COVERS_DIR + @"\" + LINK_DOMAIN + @"\3d\" + _idGame + ".png"))
            {
                pbGameCover3D.LoadAsync(GET_CURRENT_PATH + COVERS_DIR + @"\" + LINK_DOMAIN + @"\3d\" + _idGame + ".png");
            }
            else
            {
                pbGameCover3D.LoadAsync(GET_CURRENT_PATH + MEDIA_DIR + @"\3d.png");
            }
        }
        #endregion

        // REWRITE FUNCTION - Download Only Disc & 3D Cover

        #region Download Only Disc & 3D Cover
        /// <summary>
        /// Only downloads Disc and 3D files from listed ISO/GCM files.
        /// </summary>
        private void DownloadOnlyDisc3DCover(DataGridView dgv)
        {
            foreach (DataGridViewRow dgvResultRow in dgv.Rows)
            {
                string _IDGameCode = dgv.Rows[dgvResultRow.Index].Cells[4].Value.ToString();
                DirectoryOpenGameList(_IDGameCode);

                //tbLog.AppendText(_IDRegionCode + Environment.NewLine);

                switch (_IDRegionCode)
                {
                    case "e":
                        LINK_DOMAIN = "US";
                        break;
                    case "p":
                        LINK_DOMAIN = "EN";
                        break;
                    case "j":
                        LINK_DOMAIN = "JA";
                        break;
                    default:
                        GlobalNotifications(GCBM.Properties.Resources.UnknownRegion, ToolTipIcon.Info);
                        break;
                }
               
                try
                {
                    // Download Disc cover
                    Uri myLinkCoverDisc = new Uri(@"https://art.gametdb.com/wii/disc/" + LINK_DOMAIN + "/" + _IDMakerCode + ".png");
                    var request = (HttpWebRequest)WebRequest.Create(myLinkCoverDisc);
                    request.Method = "HEAD";
                    NET_RESPONSE = (HttpWebResponse)request.GetResponse();

                    if (NET_RESPONSE.StatusCode == HttpStatusCode.OK)
                    {
                        tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.DownloadDiscCover + _IDMakerCode + ".png" + Environment.NewLine);
                        NET_CLIENT.DownloadFileAsync(myLinkCoverDisc, GET_CURRENT_PATH + COVERS_DIR + @"\" + LINK_DOMAIN + @"\disc\" + _IDMakerCode + ".png");
                        while (NET_CLIENT.IsBusy) { Application.DoEvents(); }
                    }
                }
                catch (WebException ex)
                {
                    tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.DownloadDiscCoverError + Environment.NewLine +
                        GCBM.Properties.Resources.Error + ex.Message + Environment.NewLine);
                }
                finally
                {
                    if (NET_RESPONSE != null)
                    {
                        NET_RESPONSE.Close();
                    }
                }

                try
                {
                    // Download 3D cover
                    Uri myLinkCover3D = new Uri(@"https://art.gametdb.com/wii/cover3D/" + LINK_DOMAIN + "/" + _IDMakerCode + ".png");
                    var request3D = (HttpWebRequest)WebRequest.Create(myLinkCover3D);
                    request3D.Method = "HEAD";
                    NET_RESPONSE = (HttpWebResponse)request3D.GetResponse();

                    if (NET_RESPONSE.StatusCode == HttpStatusCode.OK)
                    {
                        tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.Download3DCover + _IDMakerCode + ".png" + Environment.NewLine);
                        NET_CLIENT.DownloadFileAsync(myLinkCover3D, GET_CURRENT_PATH + COVERS_DIR + @"\" + LINK_DOMAIN + @"\3d\" + _IDMakerCode + ".png");
                        while (NET_CLIENT.IsBusy) { Application.DoEvents(); }
                    }
                }
                catch (WebException ex)
                {
                    tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.Download3DCoverError + Environment.NewLine +
                        GCBM.Properties.Resources.Error + ex.Message + Environment.NewLine);
                }
                finally
                {
                    if (NET_RESPONSE != null)
                    {
                        NET_RESPONSE.Close();
                    }
                }
            }
        }
        #endregion

        // REWRITE FUNCTION - Download Only Disc & 3D Cover Selected Game

        #region Download Only Disc & 3D Cover Selected Game
        /// <summary>
        /// Downloads Disc and 3D files from the selected ISO/GCM file only.
        /// </summary>
        private void DownloadOnlyDisc3DCoverSelectedGame(DataGridView dgv)
        {
            Int32 _selectedGameRowCount = dgv.Rows.GetRowCount(DataGridViewElementStates.Selected);

            if (_selectedGameRowCount == 0)
            {
                SelectGameFromList();
            }
            else
            {
                switch (_IDRegionCode)
                {
                    case "e":
                        LINK_DOMAIN = "US";
                        break;
                    case "p":
                        LINK_DOMAIN = "EN";
                        break;
                    case "j":
                        LINK_DOMAIN = "JA";
                        break;
                    default:
                        GlobalNotifications(GCBM.Properties.Resources.UnknownRegion, ToolTipIcon.Info);
                        break;
                }

                try
                {
                    // Download Disc cover
                    Uri myLinkCoverDisc = new Uri(@"https://art.gametdb.com/wii/disc/" + LINK_DOMAIN + "/" + _IDMakerCode + ".png");
                    var request = (HttpWebRequest)WebRequest.Create(myLinkCoverDisc);
                    request.Method = "HEAD";
                    NET_RESPONSE = (HttpWebResponse)request.GetResponse();

                    if (NET_RESPONSE.StatusCode == HttpStatusCode.OK)
                    {
                        tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.DownloadDiscCover + _IDMakerCode + ".png" + Environment.NewLine);
                        NET_CLIENT.DownloadFileAsync(myLinkCoverDisc, GET_CURRENT_PATH + COVERS_DIR + @"\" + LINK_DOMAIN + @"\disc\" + _IDMakerCode + ".png");
                        while (NET_CLIENT.IsBusy) { Application.DoEvents(); }
                    }
                }
                catch (WebException ex)
                {
                    //MessageBox.Show("ARQUIVO: " + _netResponse.ToString() + " não existe!");
                    tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.DownloadDiscCoverError + Environment.NewLine +
                        "[" + DateString() + "]" + GCBM.Properties.Resources.Error + ex.Message + Environment.NewLine);
                }
                finally
                {
                    if (NET_RESPONSE != null)
                    {
                        NET_RESPONSE.Close();
                    }
                }

                try
                {
                    // Download 3D cover
                    Uri myLinkCover3D = new Uri(@"https://art.gametdb.com/wii/cover3D/" + LINK_DOMAIN + "/" + _IDMakerCode + ".png");
                    var request3D = (HttpWebRequest)WebRequest.Create(myLinkCover3D);
                    request3D.Method = "HEAD";
                    NET_RESPONSE = (HttpWebResponse)request3D.GetResponse();

                    if (NET_RESPONSE.StatusCode == HttpStatusCode.OK)
                    {
                        tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.Download3DCover + _IDMakerCode + ".png" + Environment.NewLine);
                        NET_CLIENT.DownloadFileAsync(myLinkCover3D, GET_CURRENT_PATH + COVERS_DIR + @"\" + LINK_DOMAIN + @"\3d\" + _IDMakerCode + ".png");
                        while (NET_CLIENT.IsBusy) { Application.DoEvents(); }
                    }
                }
                catch (WebException ex)
                {
                    //MessageBox.Show("ARQUIVO: " + _netResponse.ToString() + " não existe!");
                    tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.Download3DCoverError + Environment.NewLine +
                        "[" + DateString() + "]" + GCBM.Properties.Resources.Error + ex.Message + Environment.NewLine);
                }
                finally
                {
                    if (NET_RESPONSE != null)
                    {
                        NET_RESPONSE.Close();
                    }
                }
            }
        }
        #endregion      

        // REWRITE FUNCTION - Clear temporary folder

        #region Clear Temp
        /// <summary>
        /// Clean up the temporary directory.
        /// </summary>
        private void ClearTemp()
        {
            try
            {
                if (Directory.Exists(CONFIG_INI_FILE.IniReadString("GENERAL", "TemporaryFolder", "")))
                {
                    DirectoryInfo dir = new DirectoryInfo(CONFIG_INI_FILE.IniReadString("GENERAL", "TemporaryFolder", ""));
                    foreach (FileInfo fi in dir.GetFiles("*.*", SearchOption.AllDirectories))
                    {
                        fi.Delete();
                        Directory.Delete(dir.ToString(), true);
                    }
                }
                else
                {
                    DirectoryInfo dir = new DirectoryInfo(GET_CURRENT_PATH + TEMP_DIR);
                    foreach (FileInfo fi in dir.GetFiles("*.*", SearchOption.AllDirectories))
                    {
                        fi.Delete();
                        Directory.Delete(dir.ToString(), true);
                    }
                }
            }
            catch (Exception ex)
            {
                tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.ClearTemp_String1 + Environment.NewLine);
            }
        }
        #endregion

        #region Required Directories
        /// <summary>
        /// Directories required for the correct functioning of the program.
        /// </summary>
        private void RequiredDirectories()
        {
            // Temporary directory default
            if (!Directory.Exists(CONFIG_INI_FILE.IniReadString("GENERAL", "TemporaryFolder", "")))
            {
                Directory.CreateDirectory(CONFIG_INI_FILE.IniReadString("GENERAL", "TemporaryFolder", ""));
                tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.RequiredDirectories_String1 + Environment.NewLine);
            }
            else
            {
                tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.RequiredDirectories_String2 + Environment.NewLine);
            }    

            // Cover Directory
            if (!Directory.Exists(GET_CURRENT_PATH + COVERS_DIR))
            {
                // US - Covers Directory
                Directory.CreateDirectory(GET_CURRENT_PATH + COVERS_DIR + @"\US\2d\"); // 2D
                Directory.CreateDirectory(GET_CURRENT_PATH + COVERS_DIR + @"\US\3d\"); // 3D
                Directory.CreateDirectory(GET_CURRENT_PATH + COVERS_DIR + @"\US\disc\"); // Disc
                Directory.CreateDirectory(GET_CURRENT_PATH + COVERS_DIR + @"\US\full\"); // Full
                // JA - Covers Directory
                Directory.CreateDirectory(GET_CURRENT_PATH + COVERS_DIR + @"\JA\2d\"); // 2D
                Directory.CreateDirectory(GET_CURRENT_PATH + COVERS_DIR + @"\JA\3d\"); // 3D
                Directory.CreateDirectory(GET_CURRENT_PATH + COVERS_DIR + @"\JA\disc\"); // Disc
                Directory.CreateDirectory(GET_CURRENT_PATH + COVERS_DIR + @"\JA\full\"); // Full
                // EN - Covers Directory
                Directory.CreateDirectory(GET_CURRENT_PATH + COVERS_DIR + @"\EN\2d\"); // 2D
                Directory.CreateDirectory(GET_CURRENT_PATH + COVERS_DIR + @"\EN\3d\"); // 3D
                Directory.CreateDirectory(GET_CURRENT_PATH + COVERS_DIR + @"\EN\disc\"); // Disc
                Directory.CreateDirectory(GET_CURRENT_PATH + COVERS_DIR + @"\EN\full\"); // Full

                tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.RequiredDirectories_String4 + Environment.NewLine);
            }
            else
            {
                tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.RequiredDirectories_String5 + Environment.NewLine);
            }
        }
        #endregion

        #region External Site
        /// <summary>
        /// Function to load websites in the default browser.
        /// </summary>
        /// <param name="targetLink"></param>
        /// <param name="targetID"></param>
        private void ExternalSite(string targetLink, string targetID)
        {
            try
            {
                Process.Start(targetLink + targetID);
            }
            catch (Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    GlobalNotifications(noBrowser.Message, ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.Error + ex.Message + Environment.NewLine);
                GlobalNotifications(ex.Message, ToolTipIcon.Error);
            }
        }
        #endregion

        // REWRITE FUNCTION - Install Game Exact Copy

        #region Install Game Exact Copy
        /// <summary>
        /// Function to install an exact copy of the file (1:1).
        /// </summary>
        private void InstallGameExactCopy()
        {
            int? selectedRowCount = Convert.ToInt32(dgvGameList.Rows.GetRowCount(DataGridViewElementStates.Selected));

            if (dgvGameList.RowCount == 0)
            {
                EmptyGamesList();
            }
            else if (selectedRowCount > 0)
            {
                try
                {
                    // Removes blank spaces
                    //string ret = Regex.Replace(txtGameTitle.Text, @"[^0-9a-zA-ZéúíóáÉÚÍÓÁèùìòàÈÙÌÒÀõãñÕÃÑêûîôâÊÛÎÔÂëÿüïöäËYÜÏÖÄçÇ\s]+?", string.Empty);
                    // Removes whitespace
                    //string ret = Regex.Replace(txtGameTitle.Text, @"[^0-9a-zA-ZéúíóáÉÚÍÓÁèùìòàÈÙÌÒÀõãñÕÃÑêûîôâÊÛÎÔÂëÿüïöäËYÜÏÖÄçÇ]+?", string.Empty);

                    // Replaces
                    //string _SwapCharacter = tbIDName.Text.Replace(" disc1", "").Replace(" disc2", "").Replace(" 1", "").Replace(" 2", "")
                    //.Replace(" (2)", "").Replace(":", " - ").Replace(";", " - ").Replace(",", " - ")
                    //.Replace(" -  ", " - ").Replace(" FOR NINTENDO GAMECUBE", "").Replace(" GameCube", "");

                    // Nome do jogo
                    string _SwapCharacter = tbIDName.Text.Replace(":", " - ").Replace(";", " - ").Replace(",", " - ").Replace(" -  ", " - ");

                    //string strResult = "";
                    //bool firstCharacter = true;

                    //if (_SwapCharacter.Length > 0)
                    //{
                    //    for (int intCont = 0; intCont <= _SwapCharacter.Length - 1; intCont++)
                    //    {
                    //        if ((firstCharacter) && (!_SwapCharacter.Substring(intCont, 1).Equals(" ")))
                    //        {
                    //            strResult += _SwapCharacter.Substring(intCont, 1).ToUpper();
                    //            firstCharacter = false;
                    //        }
                    //        else
                    //        {
                    //            strResult += _SwapCharacter.Substring(intCont, 1).ToLower();
                    //            if (_SwapCharacter.Substring(intCont, 1).Equals(" "))
                    //            {
                    //                firstCharacter = true;
                    //            }
                    //        }
                    //    }
                    //}

                    var _source = new FileInfo(Path.Combine(fbd1.SelectedPath, dgvGameList.CurrentRow.Cells[4].Value.ToString()));

                    // Disc 1 (0 -> 0) - Title [ID Game]
                    if (tbIDDiscID.Text == "0x00" && CONFIG_INI_FILE.IniReadInt("SEVERAL", "AppointmentStyle") == 0)
                    {
                        Directory.CreateDirectory(tscbDiscDrive.SelectedItem + @"games\" + _SwapCharacter + " [" + tbIDGame.Text + "]");
                        var _destination = new FileInfo(tscbDiscDrive.SelectedItem + @"games\" + _SwapCharacter + " [" + tbIDGame.Text + "]" + @"\" + "game.iso");
                        CopyTask(_source, _destination);
                    } // Disc 2 (1 -> 0) - Title [ID Game]
                    else if (tbIDDiscID.Text == "0x01" && CONFIG_INI_FILE.IniReadInt("SEVERAL", "AppointmentStyle") == 0)
                    {
                        Directory.CreateDirectory(tscbDiscDrive.SelectedItem + @"games\" + _SwapCharacter + " [" + tbIDGame.Text + "]");
                        var _destination = new FileInfo(tscbDiscDrive.SelectedItem + @"games\" + _SwapCharacter + " [" + tbIDGame.Text + "]" + @"\" + "disc2.iso");
                        CopyTask(_source, _destination);
                    }// Disc 1 (0 -> 1) - [ID Game]
                    else if (tbIDDiscID.Text == "0x00" && CONFIG_INI_FILE.IniReadInt("SEVERAL", "AppointmentStyle") == 1)
                    {
                        Directory.CreateDirectory(tscbDiscDrive.SelectedItem + @"games\[" + tbIDGame.Text + "]");
                        var _destination = new FileInfo(tscbDiscDrive.SelectedItem + @"games\[" + tbIDGame.Text + "]" + @"\" + "game.iso");
                        CopyTask(_source, _destination);
                    } // Disc 2 (1 -> 1) - [ID Game]
                    else if (tbIDDiscID.Text == "0x01" && CONFIG_INI_FILE.IniReadInt("SEVERAL", "AppointmentStyle") == 1)
                    {
                        Directory.CreateDirectory(tscbDiscDrive.SelectedItem + @"games\[" + tbIDGame.Text + "]");
                        var _destination = new FileInfo(tscbDiscDrive.SelectedItem + @"games\[" + tbIDGame.Text + "]" + @"\" + "disc2.iso");
                        CopyTask(_source, _destination);
                    }

                    // Título [Código do Jogo] -> 0
                    // [Código do Jogo]        -> 1
                }
                catch (Exception ex)
                {
                    tbLog.AppendText("[" + DateString() + "]" + " [ERRO] " + ex.Message + Environment.NewLine);
                    GlobalNotifications(ex.Message, ToolTipIcon.Error);
                }
            }
        }
        #endregion

        // REWRITE FUNCTION - Install Game Scrub

        #region Install Game Scrub
        /// <summary>
        /// Function to install an copy of the file in Scrub mode.
        /// </summary>
        private void InstallGameScrub()
        {
            const string quote = "\"";
            var _source = dgvGameList.CurrentRow.Cells[4].Value.ToString();

            START_INFO.CreateNoWindow = true;
            START_INFO.UseShellExecute = true;
            // GCIT
            START_INFO.FileName = GET_CURRENT_PATH + @"\bin\gcit.exe ";


            bool boolCaseSwitch = CONFIG_INI_FILE.IniReadBool("TRANSFERSYSTEM", "ScrubFlushSD");
            int intCaseSwitch   = CONFIG_INI_FILE.IniReadInt("TRANSFERSYSTEM", "ScrubAlign");

            switch (boolCaseSwitch)
            {
                case true:
                    FLUSH_SD = " - flush";
                    break;
                case false:
                    FLUSH_SD = "";
                    break;
            }

            switch (intCaseSwitch)
            {
                case 0:
                    SCRUB_ALIGN = "";
                    break;
                case 1:
                    SCRUB_ALIGN = " -a 4";
                    break;
                case 2:
                    SCRUB_ALIGN = " -a 32";
                    break;
                default:
                    SCRUB_ALIGN = " -a 32K";
                    break;
            }

            //if (CONFIG_INI_FILE.IniReadBool("TRANSFERSYSTEM", "ScrubFlushSD") == true)
            //{
            //    FLUSH_SD = " - flush";
            //}
            //else
            //{
            //    FLUSH_SD = "";
            //}

            //if (CONFIG_INI_FILE.IniReadInt("TRANSFERSYSTEM", "ScrubAlign") == 0)
            //{
            //    SCRUB_ALIGN = "";
            //}
            //else if (CONFIG_INI_FILE.IniReadInt("TRANSFERSYSTEM", "ScrubAlign") == 1)
            //{
            //    SCRUB_ALIGN = " -a 4";
            //}
            //else if (CONFIG_INI_FILE.IniReadInt("TRANSFERSYSTEM", "ScrubAlign") == 2)
            //{
            //    SCRUB_ALIGN = " -a 32";
            //}
            //else
            //{
            //    SCRUB_ALIGN = " -a 32K";
            //}

            //startInfo.Arguments = dgvGameList.CurrentRow.Cells[4].Value.ToString() + " -aq " + scrubAlign + flushSD +
            //" -f " + configIniFile.IniReadInt("TRANSFERSYSTEM", "ScrubFormat", "") + " -d " + tscbDiscDrive.SelectedItem + @"games";
            START_INFO.Arguments = quote + _source + quote + " -aq " + SCRUB_ALIGN + FLUSH_SD + " -f " + 
                CONFIG_INI_FILE.IniReadString("TRANSFERSYSTEM", "ScrubFormat", "") + " -d " + tscbDiscDrive.SelectedItem + @"games";
            START_INFO.WindowStyle = ProcessWindowStyle.Hidden;

            using (Process myProcess = Process.Start(START_INFO))
            {
                int i = 0;
                // Display the process statistics until
                // the user closes the program.
                do
                {
                    if (!myProcess.HasExited)
                    {
                        // Refresh the current process property values.
                        myProcess.Refresh();
                        // Display current process statistics.
                        tbLog.AppendText($"{myProcess} -");
                        //toolStripStatusLabel3.Text = $"{myProcess} -";
                        tbLog.AppendText(Environment.NewLine + "-------------------------------------" + Environment.NewLine + Environment.NewLine);

                        if (myProcess.Responding)
                        {
                            lblCopy.Visible = true;
                            lblInstallGame.Visible = true;
                            lblPercent.Visible = true;
                            pbCopy.Visible = true;

                            DisableOptionsGame(dgvGameList);

                            lblCopy.Text = GCBM.Properties.Resources.InstallGameScrub_String1;
                            lblInstallGame.Text = GCBM.Properties.Resources.InstallGameScrub_String2 + i++;
                            pbCopy.PerformStep();
                            var incrementValue = i++ / 2;
                            pbCopy.Value = incrementValue;
                            lblPercent.Text = incrementValue.ToString() + "%"; //i++.ToString() + "%";
                            //progressBarGameCopy.Maximum = i++ * 5;
                        }
                        else
                        {
                            lblInstallGame.Visible = true;
                            lblInstallGame.Text = GCBM.Properties.Resources.InstallGameScrub_String3;
                        }
                    }
                    //progressBar1.Value += 5;
                }
                while (!myProcess.WaitForExit(1000));

                //textLog.AppendText($">> Código de saída do processo : {myProcess.ExitCode}");

                var _StatusExit = myProcess.ExitCode;
                if (_StatusExit == 0)
                {
                    lblInstallGame.Visible = true;
                    lblPercent.Visible = true;
                    pbCopy.Visible = true;

                    EnableOptionsGameList();

                    lblPercent.Text = "100%";
                    pbCopy.Value = 100;
                    lblInstallGame.Text = GCBM.Properties.Resources.InstallGameScrub_String4;

                    if (tbIDDiscID.Text == "0x00")
                    {
                        GlobalNotifications(GCBM.Properties.Resources.InstallGameScrub_String5, ToolTipIcon.Info);
                        //MessageBox.Show(GCBM.Properties.Resources.InstallGameScrub_String5, GCBM.Properties.Resources.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);

                        lblCopy.Visible = false;
                        lblInstallGame.Visible = false;
                        lblPercent.Visible = false;
                        pbCopy.Visible = false;
                    }

                    if (tbIDDiscID.Text == "0x01")
                    {
                        // Usar nome intermo
                        if (CONFIG_INI_FILE.IniReadBool("TITLES", "GameInternalName") == true)
                        {
                            // Renomear game.iso -> disc2.iso
                            //string myOrigem = tscbDiscDrive.SelectedItem + @"games\" + tbIDName.Text + " [" + tbIDGame.Text + "2]" + @"\game.iso";
                            string myOrigem = tscbDiscDrive.SelectedItem + @"games\" + tbIDName.Text + " [" + tbIDGame.Text + "2]" + @"\game.iso";
                            string myDestiny = tscbDiscDrive.SelectedItem + @"games\" + tbIDName.Text.Replace("disc2 ", "") + " [" + tbIDGame.Text + "2]" + @"\disc2.iso";

                            //MessageBox.Show("MYORIGEM: " + Environment.NewLine
                            //    + myOrigem +
                            //    "\n\nMYDESTINY: " + Environment.NewLine
                            //    + myDestiny, "DISC2", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            /*
                            * MYORIGEM:     c:\games\resident evil 4 disc2 (2) [G4BE082]\game.iso
                            * MYDESTINY:    c:\games\resident evil 4 disc2 (2) [G4BE082]\disc2.iso
                            * MYNEWDESTINY: c:\games\resident evil 4 [G4BE08]\
                            */

                            File.Move(myOrigem, myDestiny);

                            GlobalNotifications(GCBM.Properties.Resources.InstallGameScrub_String6, ToolTipIcon.Info);
                            //MessageBox.Show(GCBM.Properties.Resources.InstallGameScrub_String6, GCBM.Properties.Resources.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);

                            lblCopy.Visible = false;
                            lblInstallGame.Visible = false;
                            lblPercent.Visible = false;
                            pbCopy.Visible = false;
                            //GC.Collect();
                        }// Usar WiiTDB.xml
                        else
                        {
                            // Renomear game.iso -> disc2.iso
                            //string myOrigem = tscbDiscDrive.SelectedItem + @"games\" + tbIDName.Text + " [" + tbIDGame.Text + "2]" + @"\game.iso";
                            string myOrigem = tscbDiscDrive.SelectedItem + @"games\" + _oldNameInternal + " [" + tbIDGame.Text + "2]" + @"\game.iso";
                            string myDestiny = tscbDiscDrive.SelectedItem + @"games\" + _oldNameInternal.Replace("disc2 ", "") + " [" + tbIDGame.Text + "2]" + @"\disc2.iso";

                            //MessageBox.Show("MYORIGEM: " + Environment.NewLine
                            //    + myOrigem +
                            //    "\n\nMYDESTINY: " + Environment.NewLine
                            //    + myDestiny, "DISC2", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            /*
                            * MYORIGEM:     c:\games\resident evil 4 disc2 (2) [G4BE082]\game.iso
                            * MYDESTINY:    c:\games\resident evil 4 disc2 (2) [G4BE082]\disc2.iso
                            * MYNEWDESTINY: c:\games\resident evil 4 [G4BE08]\
                            */

                            File.Move(myOrigem, myDestiny);

                            GlobalNotifications(GCBM.Properties.Resources.InstallGameScrub_String6, ToolTipIcon.Info);
                            //MessageBox.Show(GCBM.Properties.Resources.InstallGameScrub_String6, GCBM.Properties.Resources.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);

                            lblCopy.Visible = false;
                            lblInstallGame.Visible = false;
                            lblPercent.Visible = false;
                            pbCopy.Visible = false;
                            //GC.Collect();
                        }
                    }
                }
                //if (_StatusExit == 3)
                //{
                //    //tsslStatusInformation.Text = "Status: ERRO! -> " + "{" + _StatusExit.ToString() + "}" + " Por favor, verifique se exitem espaços no nome do arquivo!";
                //}
            }
        }
        #endregion

        #region Copy Task
        /// <summary>
        /// Function for the copy job.
        /// </summary>
        /// <param name="_source"></param>
        /// <param name="_destination"></param>
        private void CopyTask(FileInfo _source, FileInfo _destination)
        {
            // Disc 1
            //if (textBoxDiscID.Text == "00" && comboBoxSettingsNomenclatureAppointmentStyle.SelectedIndex  == 0)
            if (tbIDDiscID.Text == "0x00")
            {
                if (_destination.Exists)
                {
                    _destination.Delete();
                }
                //Create a tast to run copy file
                Task.Run(() =>
                {
                    //_source.CopyTo(_destination, true);
                    _source.CopyTo(_destination, x => pbCopy.BeginInvoke(new Action(() =>
                    {
                        DisableOptionsGame(dgvGameList);
                        dgvGameList.Enabled = false;
                        pbCopy.Visible = true;
                        lblCopy.Visible = true;
                        lblPercent.Visible = true;
                        lblInstallGame.Visible = true;
                        pbCopy.Value = x;
                        lblCopy.Text = GCBM.Properties.Resources.CopyTask_String1;
                        lblInstallGame.Text = GCBM.Properties.Resources.CopyTask_String2 + tbIDName.Text;
                        lblPercent.Text = x.ToString() + "%";
                    })));
                }).GetAwaiter().OnCompleted(() => pbCopy.BeginInvoke(new Action(() =>
                {
                    pbCopy.Value = 100;
                    lblCopy.Text = GCBM.Properties.Resources.CopyTask_String3;
                    lblInstallGame.Text = GCBM.Properties.Resources.CopyTask_String4;
                    lblPercent.Text = GCBM.Properties.Resources.CopyTask_String5;
                    GlobalNotifications(GCBM.Properties.Resources.InstallGameScrub_String5, ToolTipIcon.Info);
                    EnableOptionsGameList();
                    dgvGameList.Enabled = true;
                    pbCopy.Visible = false;
                    lblCopy.Visible = false;
                    lblPercent.Visible = false;
                    lblInstallGame.Visible = false;
                })));
            }
            // Disc 2
            else if (tbIDDiscID.Text == "0x01")
            {
                if (_destination.Exists)
                {
                    _destination.Delete();
                }
                //Create a tast to run copy file
                Task.Run(() =>
                {
                    _source.CopyTo(_destination, x => pbCopy.BeginInvoke(new Action(() =>
                    {
                        DisableOptionsGame(dgvGameList);
                        pbCopy.Visible = true;
                        lblCopy.Visible = true;
                        lblPercent.Visible = true;
                        lblInstallGame.Visible = true;
                        pbCopy.Value = x;
                        lblCopy.Text = GCBM.Properties.Resources.CopyTask_String1;
                        lblInstallGame.Text = GCBM.Properties.Resources.CopyTask_String2 + tbIDName.Text;
                        lblPercent.Text = x.ToString() + "%";
                    })));
                }).GetAwaiter().OnCompleted(() => pbCopy.BeginInvoke(new Action(() =>
                {
                    pbCopy.Value = 100;
                    lblCopy.Text = GCBM.Properties.Resources.CopyTask_String6;
                    lblInstallGame.Text = GCBM.Properties.Resources.CopyTask_String4;
                    lblPercent.Text = GCBM.Properties.Resources.CopyTask_String5;
                    GlobalNotifications(GCBM.Properties.Resources.InstallGameScrub_String6, ToolTipIcon.Info);
                    EnableOptionsGameList();
                    pbCopy.Visible = false;
                    lblCopy.Visible = false;
                    lblPercent.Visible = false;
                    lblInstallGame.Visible = false;
                })));
            }
        }
        #endregion

        #region Notifications
        /// <summary>
        /// Global Notifications
        /// </summary>
        /// <param name="message"></param>
        /// <param name="icon"></param>
        private void GlobalNotifications(string message, ToolTipIcon icon)
        {
            notifyIcon.ShowBalloonTip(10, "GameCube Backup Manager", message, icon);
        }

        /// <summary>
        /// Informs if the file list is empty.
        /// </summary>
        private static void EmptyGamesList()
        {
            MessageBox.Show(GCBM.Properties.Resources.EmptyGamesList, GCBM.Properties.Resources.Information, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        /// <summary>
        /// Informs if it is necessary to select a game from the list.
        /// </summary>
        private static void SelectGameFromList()
        {
            MessageBox.Show(GCBM.Properties.Resources.SelectGameFromList, GCBM.Properties.Resources.Information, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        /// <summary>
        /// 
        /// </summary>
        private void SelectTargetDrive()
        {
            MessageBox.Show(GCBM.Properties.Resources.SelectTargetDrive, GCBM.Properties.Resources.Information, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        /// <summary>
        /// Informs about deleting files.
        /// This procedure is irreversible.
        /// </summary>
        /// <returns></returns>
        private static DialogResult DialogResultDeleteGame()
        {
            DialogResult dr = MessageBox.Show(GCBM.Properties.Resources.DialogResultDeleteGame_ReallyDeleteFile_String1 + Environment.NewLine + 
                Environment.NewLine + GCBM.Properties.Resources.DialogResultDeleteGame_ReallyDeleteFile_String2,
                GCBM.Properties.Resources.Notice, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            return dr;
        }

        /// <summary>
        /// It informs you about the need to configure the USB Loader GX and WiiFlow cover transfer system.
        /// </summary>
        private static void CheckUSBGXFlow()
        {
            MessageBox.Show(GCBM.Properties.Resources.CheckUSBGXFlow_String1 +
                Environment.NewLine + Environment.NewLine +
                GCBM.Properties.Resources.CheckUSBGXFlow_String2 +
                Environment.NewLine + Environment.NewLine +
                GCBM.Properties.Resources.CheckUSBGXFlow_String3,
                GCBM.Properties.Resources.Notice, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        /// <summary>
        /// 
        /// </summary>
        private async void CheckWiiTdbXml()
        {
            await ProcessTaskDelay();
            MessageBox.Show(GCBM.Properties.Resources.ProcessTaskDelay_String1 + Environment.NewLine +
                GCBM.Properties.Resources.ProcessTaskDelay_String2 +
                Environment.NewLine + Environment.NewLine +
                GCBM.Properties.Resources.ProcessTaskDelay_String3 +
                Environment.NewLine + Environment.NewLine +
                GCBM.Properties.Resources.ProcessTaskDelay_String4,
                GCBM.Properties.Resources.Notice, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeDrive"></param>
        private void InvalidDrive(string typeDrive)
        {
            MessageBox.Show(GCBM.Properties.Resources.InvalidDrive_String1 + Environment.NewLine + 
                GCBM.Properties.Resources.InvalidDrive_String2 + typeDrive +
                Environment.NewLine + Environment.NewLine +
                GCBM.Properties.Resources.InvalidDrive_String3 +
                Environment.NewLine + Environment.NewLine +
                GCBM.Properties.Resources.InvalidDrive_String4 +
                Environment.NewLine + Environment.NewLine +
                GCBM.Properties.Resources.InvalidDrive_String5 + typeDrive +
                GCBM.Properties.Resources.InvalidDrive_String6, 
                GCBM.Properties.Resources.Attention, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typehash"></param>
        /// <param name="listhash"></param>
        private void ListHash(string typehash, string listhash)
        {
            MessageBox.Show(GCBM.Properties.Resources.ListHash_String1 + typehash + GCBM.Properties.Resources.ListHash_String2 + Environment.NewLine + Environment.NewLine 
                + listhash, GCBM.Properties.Resources.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion

        #region Get Hash
        /// <summary>
        /// Get the Hash of the ISO/GCM file.
        /// </summary>
        /// <param name="hashAlgorithm"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();
            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
        #endregion

        #region Verify Hash
        /// <summary>
        /// Check the Hash of the ISO/GCM file.
        /// </summary>
        /// <param name="hashAlgorithm"></param>
        /// <param name="input"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        private static bool VerifyHash(HashAlgorithm hashAlgorithm, string input, string hash)
        {
            // Hash the input.
            var hashOfInput = GetHash(hashAlgorithm, input);
            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(hashOfInput, hash) == 0;
        }
        #endregion

        #region Global Delete Selected Game
        /// <summary>
        /// Global Delete Selected Game
        /// </summary>
        /// <param name="dgv"></param>
        private void GlobalDeleteSelectedGame(DataGridView dgv)
        {
            Int32 _selectedRowCount = dgv.Rows.GetRowCount(DataGridViewElementStates.Selected);

            if (dgv.RowCount == 0)
            {
                EmptyGamesList();
            }
            else
            {
                if (_selectedRowCount == 0)
                {
                    SelectGameFromList();
                }
                else
                {
                    try
                    {   
                        if (DialogResultDeleteGame() == DialogResult.Yes)
                        {
                            if (_IDRegionCode.Equals("e"))
                            {
                                LINK_DOMAIN = "US";
                            }
                            else if (_IDRegionCode.Equals("p"))
                            {
                                LINK_DOMAIN = "EN";
                            }
                            else if (_IDRegionCode.Equals("j"))
                            {
                                LINK_DOMAIN = "JA";
                            }
                            else
                            {
                                GlobalNotifications(GCBM.Properties.Resources.UnknownRegion, ToolTipIcon.Info);
                            }

                            string cover2D   = GET_CURRENT_PATH + @"\covers\cache\" + LINK_DOMAIN + @"\2d\" + tbIDGame.Text + ".png";
                            string cover3D   = GET_CURRENT_PATH + @"\covers\cache\" + LINK_DOMAIN + @"\3d\" + tbIDGame.Text + ".png";
                            string coverDisc = GET_CURRENT_PATH + @"\covers\cache\" + LINK_DOMAIN + @"\disc\" + tbIDGame.Text + ".png";
                            string coverFull = GET_CURRENT_PATH + @"\covers\cache\" + LINK_DOMAIN + @"\full\" + tbIDGame.Text + ".png";

                            // DELETAR JOGO E CAPA DO DISPOSITIVO DE ORIGEM
                            if (dgv == dgvGameList)
                            {
                                File.Delete(dgv.CurrentRow.Cells[4].Value.ToString());

                                // 2D
                                if (File.Exists(cover2D))
                                {
                                    File.Delete(cover2D);
                                }
                                // 3D
                                if (File.Exists(cover3D))
                                {
                                    File.Delete(cover3D);
                                }
                                // Disc
                                if (File.Exists(coverDisc))
                                {
                                    File.Delete(coverDisc);
                                }
                                // Full
                                if (File.Exists(coverFull))
                                {
                                    File.Delete(coverFull);
                                }
                                
                                DisplayFilesSelected(fbd1.SelectedPath, dgvGameList);

                            }// DELETAR JOGO DO DISPOSITIVO DE DESTINO
                            else
                            {
                                string pasta = Path.GetDirectoryName(dgv.CurrentRow.Cells[4].Value.ToString());
                                Directory.Delete(pasta, true);
                                //UpdateGameList(tscbDiscDrive.SelectedItem + @"games\", dgvGameListDisc);
                                DisplayFilesSelected(tscbDiscDrive.SelectedItem + @"games\", dgvGameListDisc);
                            }
                           
                        }

                        if (dgv.RowCount == 0)
                        {
                            DisableOptionsGame(dgvGameList);
                            ResetOptions();
                        }
                    }
                    catch (Exception ex)
                    {
                        tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.Error + ex.Message + Environment.NewLine);
                        GlobalNotifications(ex.Message, ToolTipIcon.Error);
                    }
                }
            }
        }
        #endregion

        #region Global Delete All Games
        /// <summary>
        /// Global Delete All Games
        /// </summary>
        /// <param name="dgv"></param>
        private void GlobalDeleteAllGames(DataGridView dgv)
        {
            var filters = new String[] { "iso", "gcm" };

            if (dgv.RowCount == 0)
            {
                EmptyGamesList();
            }
            else
            {
                try
                {
                    if (DialogResultDeleteGame() == DialogResult.Yes)
                    {
                        // AQUI COMEÇA A DELETAR OS ARQUIVOS

                        if (dgv == dgvGameList)
                        {
                            if (_IDRegionCode.Equals("e"))
                            {
                                LINK_DOMAIN = "US";
                            }
                            else if (_IDRegionCode.Equals("p"))
                            {
                                LINK_DOMAIN = "EN";
                            }
                            else if (_IDRegionCode.Equals("j"))
                            {
                                LINK_DOMAIN = "JA";
                            }
                            else
                            {
                                GlobalNotifications(GCBM.Properties.Resources.UnknownRegion, ToolTipIcon.Info);
                            }

                            DirectoryInfo di2D = new DirectoryInfo(GET_CURRENT_PATH + @"\covers\cache\" + LINK_DOMAIN + @"\2d\");
                            DirectoryInfo di3D = new DirectoryInfo(GET_CURRENT_PATH + @"\covers\cache\" + LINK_DOMAIN + @"\3d\");
                            DirectoryInfo diDisc = new DirectoryInfo(GET_CURRENT_PATH + @"\covers\cache\" + LINK_DOMAIN + @"\disc\");
                            DirectoryInfo diFull = new DirectoryInfo(GET_CURRENT_PATH + @"\covers\cache\" + LINK_DOMAIN + @"\full\");

                            string[] files = GetFilesFolder(fbd1.SelectedPath, filters, false);

                            // Goes through the entire file list and removes all found ISO and GCM files.
                            for (int i = 0; i < files.Length; i++)
                            {
                                //File.Delete(fbd1.SelectedPath + @"\" + dgvGameList.CurrentRow.Cells[1].Value.ToString());
                                File.Delete(dgv.CurrentRow.Cells[4].Value.ToString());

                                DisplayFilesSelected(fbd1.SelectedPath, dgv);
                            }

                            // Delete cover 2D files
                            foreach (FileInfo file in di2D.GetFiles())
                            {
                                file.Delete();
                            }
                            // Delete cover 3D files
                            foreach (FileInfo file in di3D.GetFiles())
                            {
                                file.Delete();
                            }
                            // Delete cover Disc files
                            foreach (FileInfo file in diDisc.GetFiles())
                            {
                                file.Delete();
                            }
                            // Delete cover Full files
                            foreach (FileInfo file in diFull.GetFiles())
                            {
                                file.Delete();
                            }

                            //if (dgv.RowCount == 0)
                            //{
                            //    DisableOptionsGame(dgvGameList);
                            //    ResetOptions();
                            //    MessageBox.Show("Todos os arquivos foram excluídos com sucesso!", "AVISO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            //}

                        }// DELETAR JOGO DO DISPOSITIVO DE DESTINO
                        else if (dgv == dgvGameListDisc)
                        {
                            string[] files = GetFilesFolder(tscbDiscDrive.SelectedItem + @"games\", filters, false);

                            // Goes through the entire file list and removes all found ISO and GCM files.
                            //string pasta = Path.GetDirectoryName(dgv.CurrentRow.Cells[4].Value.ToString());
                            for (int i = 0; i < files.Length; i++)
                            {
                                string pasta = Path.GetDirectoryName(dgv.CurrentRow.Cells[4].Value.ToString());
                                //File.Delete(tscbDiscDrive.SelectedItem + @"games\" + dgv.CurrentRow.Cells[0].Value.ToString());                               
                                Directory.Delete(pasta, true);
                                //MessageBox.Show(pasta);

                                DisplayFilesSelected(tscbDiscDrive.SelectedItem + @"games\", dgv);
                            }

                            //dgvGameListDisc.DataSource = null;
                            dgv.DataSource = null;

                            // Delete cover 2D files
                            //foreach (FileInfo file in di2D.GetFiles())
                            //{
                            //    file.Delete();
                            //}
                            //// Delete cover 3D files
                            //foreach (FileInfo file in di3D.GetFiles())
                            //{
                            //    file.Delete();
                            //}
                            //// Delete cover Disc files
                            //foreach (FileInfo file in diDisc.GetFiles())
                            //{
                            //    file.Delete();
                            //}
                            //// Delete cover Full files
                            //foreach (FileInfo file in diFull.GetFiles())
                            //{
                            //    file.Delete();
                            //}

                            //if (dgvGameListDisc.RowCount == 0)
                            //{
                            //    DisableOptionsGame(dgvGameListDisc);
                            //    ResetOptions();
                            //    MessageBox.Show("Todos os arquivos foram excluídos com sucesso!", "AVISO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            //}

                        }
                    }

                    if (dgv.RowCount == 0)
                    {
                        DisableOptionsGame(dgv);
                        if (dgv == dgvGameList)
                        {
                            ResetOptions();
                        }
                        MessageBox.Show(GCBM.Properties.Resources.AllFilesSuccessfullyDeleted, GCBM.Properties.Resources.Notice, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                }
                catch (Exception ex)
                {
                    tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.Error + ex.Message + Environment.NewLine);
                    GlobalNotifications(ex.Message, ToolTipIcon.Error);
                }
            }
        }
        #endregion

        #region Global Install
        /// <summary>
        /// Global Install
        /// </summary>
        /// <param name="dgv"></param>
        /// <param name="typeInstall"></param>
        private void GlobalInstall(DataGridView dgv, int typeInstall)
        {
            Int32 selectedRowCount = dgv.Rows.GetRowCount(DataGridViewElementStates.Selected);

            if (selectedRowCount == 0)
            {
                SelectGameFromList();
            }
            else
            {
                if (tscbDiscDrive.SelectedIndex == 0)
                {
                    SelectTargetDrive();
                }
                else
                {
                    if (typeInstall == 0)
                    {
                        InstallGameExactCopy();
                    }
                    else
                    {
                        InstallGameScrub();
                    }
                }
            }
        }
        #endregion

        #region Global Check Hashs
        /// <summary>
        /// Global Check Hashs
        /// </summary>
        /// <param name="dgv"></param>
        /// <param name="algorithm"></param>
        private void GlobalCheckHash(DataGridView dgv, string algorithm)
        {
            Int32 selectedRowCount = dgv.Rows.GetRowCount(DataGridViewElementStates.Selected);

            if (selectedRowCount == 0)
            {
                SelectGameFromList();
            }
            else
            {
                try
                {
                    string source = dgv.CurrentRow.Cells[4].Value.ToString();

                    //SHA-1
                    if (algorithm == "SHA-1")
                    {
                        using (SHA1 sha1Hash = SHA1.Create())
                        {
                            string hash = GetHash(sha1Hash, source);

                            if (VerifyHash(sha1Hash, source, hash))
                            {
                                ListHash("SHA-1", hash);
                            }
                            else
                            {
                                tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.HashesAreNotSame + Environment.NewLine);
                            }
                        }
                    }
                    else if (algorithm == "MD5")
                    {
                        //MD5
                        using (MD5 md5Hash = MD5.Create())
                        {
                            string hash = GetHash(md5Hash, source);

                            if (VerifyHash(md5Hash, source, hash))
                            {
                                ListHash("MD5", hash);
                            }
                            else
                            {
                                tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.HashesAreNotSame + Environment.NewLine);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.Error + ex.Message + Environment.NewLine);
                    GlobalNotifications(ex.Message, ToolTipIcon.Error);
                }
            }
        }
        #endregion

        // REWRITE FUNCTION - Transfer Covers

        #region Check Cover Transfer
        /// <summary>
        /// Function to check if the USB Loader GX or WiiFlow directories are configured.
        /// </summary>
        private void CheckCoverTransfer()
        {
            //USB Loader GX
            if (CONFIG_INI_FILE.IniReadBool("COVERS", "GXCoverUSBLoader") == true)
            {
                if (CONFIG_INI_FILE.IniReadString("COVERS", "GXCoverDirectoryDisc", "") == string.Empty || CONFIG_INI_FILE.IniReadString("COVERS", "GXCoverDirectory2D", "") == string.Empty
                    || CONFIG_INI_FILE.IniReadString("COVERS", "GXCoverDirectory3D", "") == string.Empty || CONFIG_INI_FILE.IniReadString("COVERS", "GXCoverDirectoryFull", "") == string.Empty)
                {
                    CheckUSBGXFlow();
                }
                else
                {
                    frmTransferCover _frmTransfer = new frmTransferCover();
                    _frmTransfer.ShowDialog();
                    _frmTransfer.Dispose();
                }
            }// WiiFlow
            else if (CONFIG_INI_FILE.IniReadBool("COVERS", "WiiFlowCoverUSBLoader") == true)
            {
                if (CONFIG_INI_FILE.IniReadString("COVERS", "WiiFlowCoverDirectory2D", "") == string.Empty || CONFIG_INI_FILE.IniReadString("COVERS", "WiiFlowCoverDirectoryFull", "") == string.Empty)
                {
                    CheckUSBGXFlow();
                }
                else
                {
                    frmTransferCover _frmTransfer = new frmTransferCover();
                    _frmTransfer.ShowDialog();
                    _frmTransfer.Dispose();
                }
            }
        }
        #endregion

        #region Additional Information
        /// <summary>
        /// Additional Information.
        /// </summary>
        private void AdditionalInformation()
        {
            Int32 _selectedGameRowCount = dgvGameList.Rows.GetRowCount(DataGridViewElementStates.Selected);

            if (_selectedGameRowCount == 0)
            {
                SelectGameFromList();
            }
            else
            {
                if (File.Exists(WIITDB_FILE))
                {
                    frmInfoAdditional _frmInfo = new frmInfoAdditional(tbIDGame.Text);
                    _frmInfo.ShowDialog();
                    _frmInfo.Dispose();
                }
                else
                {
                    CheckWiiTdbXml();
                }
            }
        }
        #endregion

        #region Export HTML
        /// <summary>
        /// Export game list to HTML.
        /// </summary>
        /// <param name="dgv"></param>
        public static void ExportHTML(DataGridView dgv)
        {
            string caminho = Path.Combine(Application.StartupPath, "gamelist.html");
            StreamWriter r = new StreamWriter(caminho, false);

            Font fonte = dgv.ColumnHeadersDefaultCellStyle.Font;
            int tabSize = 0;
            foreach (DataGridViewColumn col in dgv.Columns)
                if (col.Visible) tabSize += col.Width;

            //string[] conteudo = new string[dgv.Columns.Count];

            r.WriteLine("<html><head>");
            r.WriteLine("<meta http-equiv='Content-Type' content='text/html; charset=utf-8' />");
            r.WriteLine("<title>" + dgv.Name + "</title>");
            r.WriteLine("</head><body>");
            r.WriteLine("<div style='position:static'>");
            r.WriteLine("<table style='border-collapse: collapse; width:" + tabSize.ToString() + "px'>");
            r.WriteLine("<tr>");

            foreach (DataGridViewColumn coluna in dgv.Columns)
            {
                if (coluna.Visible)
                {
                    r.WriteLine("<td style='padding: 2px 2px 2px 2px; font-weight:bold; font-size:"
                        + Convert.ToInt32(fonte.Size + 3).ToString()
                        + "px; border-collapse: collapse; ' align='"
                        + coluna.InheritedStyle.Alignment.ToString().Substring(6,
                            coluna.InheritedStyle.Alignment.ToString().Length - 6)
                        + "' width='" + coluna.Width + "'>");
                    r.WriteLine("<font face='" + fonte.Name + "'>");
                    r.WriteLine(coluna.HeaderText.ToString());
                    r.WriteLine("</font>");
                    r.WriteLine("</td>");
                }
            }
            r.WriteLine("</tr>");
            if (dgv.Rows.Count > 0)
            {
                foreach (DataGridViewRow linha in dgv.Rows)
                {
                    r.WriteLine("<tr>");
                    foreach (DataGridViewCell celula in linha.Cells)
                    {
                        if (celula.Visible)
                        {
                            r.WriteLine("<td style='padding: 2px 2px 2px 2px; font-size:"
                                + Convert.ToInt32(fonte.Size + 3).ToString()
                                + "; border-collapse: collapse; ' align='"
                                + celula.InheritedStyle.Alignment.ToString().Substring(6,
                                    celula.InheritedStyle.Alignment.ToString().Length - 6)
                                + "' width='" + celula.Size.Width + "'>");
                            r.Write("<font face='" + fonte.Name + "'>"
                                + celula.FormattedValue.ToString() + "</font>");
                            r.WriteLine("</td>");
                        }
                    }
                    r.WriteLine("</tr>");
                }
            }
            r.WriteLine("</table></div></body></html>");
            r.Close();

            //Process p = new Process();
            //p.StartInfo = new ProcessStartInfo(caminho);
            //p.StartInfo.UseShellExecute = true;
            //p.Start();
        }
        #endregion

        #region Export CSV
        /// <summary>
        /// Export game list to CSV.
        /// </summary>
        private void ExportCSV(DataGridView dgv)
        {
            if (dgvGameList.Rows.Count > 0)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "CSV (*.csv)|*.csv";
                sfd.FileName = "gamelist.csv";
                bool fileError = false;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    if (File.Exists(sfd.FileName))
                    {
                        try
                        {
                            File.Delete(sfd.FileName);
                        }
                        catch (IOException ex)
                        {
                            fileError = true;
                            GlobalNotifications(GCBM.Properties.Resources.UnableWriteDataDisk, ToolTipIcon.Error);
                        }
                    }
                    if (!fileError)
                    {
                        try
                        {
                            int columnCount = dgv.Columns.Count;
                            string columnNames = "";
                            string[] outputCsv = new string[dgv.Rows.Count + 1];
                            for (int i = 0; i < columnCount; i++)
                            {
                                columnNames += dgv.Columns[i].HeaderText.ToString() + ",";
                            }
                            outputCsv[0] += columnNames;

                            for (int i = 1; (i - 1) < dgv.Rows.Count; i++)
                            {
                                for (int j = 1; j < columnCount; j++)
                                {
                                    outputCsv[i] += dgv.Rows[i - 1].Cells[j].Value.ToString() + ",";
                                }
                            }

                            File.WriteAllLines(sfd.FileName, outputCsv, Encoding.UTF8);
                            GlobalNotifications(GCBM.Properties.Resources.GameListExportedCSV, ToolTipIcon.Info);

                        }
                        catch (Exception ex)
                        {
                            tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.Error + ex.Message + Environment.NewLine);
                            GlobalNotifications(ex.Message, ToolTipIcon.Error);
                        }
                    }
                }
            }
            else
            {
                GlobalNotifications(GCBM.Properties.Resources.RecordExportedFailed, ToolTipIcon.Error);
            }
        }
        #endregion

        #region Export TXT
        /// <summary>
        /// Export game list to TXT.
        /// </summary>
        /// <param name="dgv"></param>
        private void ExportTXT(DataGridView dgv)
        {
            if (dgvGameList.RowCount == 0)
            {
                GlobalNotifications(GCBM.Properties.Resources.RecordExportedFailed, ToolTipIcon.Error);
            }
            else
            {
                try
                {
                    TextWriter writer = new StreamWriter(GET_CURRENT_PATH + @"\gamelist.txt");
                    for (int i = 0; i < dgv.Rows.Count - 1; i++)
                    {
                        for (int j = 1; j < dgv.Columns.Count; j++)
                        {
                            writer.Write("\t" + dgv.Rows[i].Cells[j].Value.ToString() + "\t" + "|");
                        }
                        writer.WriteLine("");
                        writer.WriteLine("--------------------------------------------------------------------------------" +
                            "------------------------------------------------------------------------------------------");
                    }
                    writer.Close();
                    GlobalNotifications(GCBM.Properties.Resources.GameListExportedTXT, ToolTipIcon.Info);
                }
                catch (Exception ex)
                {
                    GlobalNotifications(ex.Message, ToolTipIcon.Error);
                }
            }
        }
        #endregion

        #region Export LOG
        /// <summary>
        /// Export LOG
        /// </summary>
        /// <param name="value"></param>
        private void ExportLOG(int value)
        {
            if (value == 0)
            {
                if (tbLog.Text != string.Empty)
                {
                    File.WriteAllText(GET_CURRENT_PATH + @"\" + "gcbm.log", tbLog.Text);
                    GlobalNotifications(GCBM.Properties.Resources.RecordExportedSuccessfully, ToolTipIcon.Info);
                }
                else
                {
                    GlobalNotifications(GCBM.Properties.Resources.RecordExportedFailed, ToolTipIcon.Error);
                }
            }
            else
            {
                if (tbLog.Text != string.Empty)
                {
                    File.WriteAllText(GET_CURRENT_PATH + @"\" + "gcbm.log", tbLog.Text);
                }
            }
        }
        #endregion

        // Tool Strip Menu Item

        #region tsmiDeleteSelectedFile_Click
        private void tsmiDeleteSelectedFile_Click(object sender, EventArgs e)
        {
            //DeleteSelectedGame(0);
            GlobalDeleteSelectedGame(dgvGameList);
        }
        #endregion

        #region tsmiGameListDeleteAllFiles_Click
        private void tsmiGameListDeleteAllFiles_Click(object sender, EventArgs e)
        {
            //DeleteAllGames(0);
            GlobalDeleteAllGames(dgvGameList);
        }
        #endregion

        #region tsmiReloadGameList_Click
        private void tsmiReloadGameList_Click(object sender, EventArgs e)
        {
            //UpdateGameList(fbd1.SelectedPath, dgvGameList);
            DisplayFilesSelected(fbd1.SelectedPath, dgvGameList);
        }
        #endregion

        #region tsmiEnableCoverPanel_Click
        private void tsmiEnableCoverPanel_Click(object sender, EventArgs e)
        {
            if (!tsmiEnableCoverPanel.Checked)
            {
                tsmiEnableCoverPanel.Checked = true;
                tsmiDisableCoverPanel.Checked = false;
                spcMain.Panel1Collapsed = false;
            }
        }
        #endregion

        #region tsmiDisableCoverPanel_Click
        private void tsmiDisableCoverPanel_Click(object sender, EventArgs e)
        {
            if (!tsmiDisableCoverPanel.Checked)
            {
                tsmiEnableCoverPanel.Checked = false;
                tsmiDisableCoverPanel.Checked = true;
                spcMain.Panel1Collapsed = true;
            }
        }
        #endregion

        #region tsmiExportLog_Click
        private void tsmiExportLog_Click(object sender, EventArgs e)
        {
            ExportLOG(0);
        }
        #endregion

        #region tsmiDownloadCoversSelectedGame_Click
        private void tsmiDownloadCoversSelectedGame_Click(object sender, EventArgs e)
        {
            if (CONFIG_INI_FILE.IniReadBool("SEVERAL", "NetVerify") == true)
            {
                DownloadOnlyDisc3DCoverSelectedGame(dgvGameList);
            }
            else
            {
                GlobalNotifications(GCBM.Properties.Resources.NoInternetConnectionFound_String1 + Environment.NewLine +
                    GCBM.Properties.Resources.NoInternetConnectionFound_String2, ToolTipIcon.Info);
            }
        }
        #endregion

        #region tsmiSyncDownloadDiscOnly3DCovers_Click
        private void tsmiSyncDownloadDiscOnly3DCovers_Click(object sender, EventArgs e)
        {
            if (CONFIG_INI_FILE.IniReadBool("SEVERAL", "NetVerify") == true)
            {
                DownloadOnlyDisc3DCover(dgvGameList);
            }
            else
            {
                GlobalNotifications(GCBM.Properties.Resources.NoInternetConnectionFound_String1 + Environment.NewLine +
                    GCBM.Properties.Resources.NoInternetConnectionFound_String2, ToolTipIcon.Info);
            }
        }
        #endregion

        #region tsmiExit_Click
        private void tsmiExit_Click(object sender, EventArgs e)
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Icon = null;
                notifyIcon.Dispose();
            }           
            ClearTemp();
            Application.Exit();
        }
        #endregion

        #region tsmiDeleteSelectedFileDisc_Click
        private void tsmiDeleteSelectedFileDisc_Click(object sender, EventArgs e)
        {
            //DeleteSelectedGame(1);
            GlobalDeleteSelectedGame(dgvGameListDisc);
        }
        #endregion

        #region tsmiDatabaseUpdateGameTDB_Click
        private void tsmiDatabaseUpdateGameTDB_Click(object sender, EventArgs e)
        {
            if (CONFIG_INI_FILE.IniReadBool("SEVERAL", "NetVerify") == true)
            {
                using (var form = new frmDownloadGameTDB())
                {
                    var _returnRename = form.ShowDialog();
                    if (_returnRename == DialogResult.OK)
                    {
                        int _code = form.RETURN_CONFIRM;
                        if (_code == 1)
                        {
                            if (File.Exists(WIITDB_FILE))
                            {
                                LoadDatabaseXML();
                            }
                        }
                    }
                }
            }
            else
            {
                GlobalNotifications(GCBM.Properties.Resources.NoInternetConnectionFound_String1 + Environment.NewLine +
                    GCBM.Properties.Resources.NoInternetConnectionFound_String2, ToolTipIcon.Info);
            }
        }
        #endregion

        #region tsmiClearLog_Click
        private void tsmiClearLog_Click(object sender, EventArgs e)
        {
            if (tbLog.Text != String.Empty)
            {
                DialogResult dr = MessageBox.Show(GCBM.Properties.Resources.ClearLog, GCBM.Properties.Resources.Notice, 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);

                if (dr == DialogResult.Yes)
                {
                    tbLog.Clear();
                    tabMainLog.Text = "Log";
                }
            }
            else
            {
                MessageBox.Show(GCBM.Properties.Resources.ClearLogNotFound, GCBM.Properties.Resources.Notice, 
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        #endregion

        #region tsmiMenuAbout_Click
        private void tsmiMenuAbout_Click(object sender, EventArgs e)
        {
            frmAboutBox _frmAbout = new frmAboutBox();
            _frmAbout.ShowDialog();
            _frmAbout.Dispose();
        }
        #endregion

        #region tsmiGameInfo_Click
        private void tsmiGameInfo_Click(object sender, EventArgs e)
        {
            AdditionalInformation();
        }
        #endregion

        #region tsmiConfigurationMain_Click
        private void tsmiConfigurationMain_Click(object sender, EventArgs e)
        {               
            using (var form = new frmConfig())
            {
                var _returnRename = form.ShowDialog();
                if (_returnRename == DialogResult.OK)
                {
                    int _code = form.RETURN_CONFIRM;
                    if (_code == 1)
                    {
                        NetworkCheck();
                    }
                }
            }
        }
        #endregion

        #region tsmiTransferDeviceCovers_Click
        private void tsmiTransferDeviceCovers_Click(object sender, EventArgs e)
        {
            CheckCoverTransfer();          
        }
        #endregion

        #region tsmiReloadDeviceDrive_Click
        /// <summary>
        /// Reloads devices connected to the computer and clears the DataGridView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsmiReloadDeviceDrive_Click(object sender, EventArgs e)
        {
            GetAllDrives();
            dgvGameListDisc.DataSource = null;
        }
        #endregion

        #region tsmiReloadGameListDisc_Click
        private void tsmiReloadGameListDisc_Click(object sender, EventArgs e)
        {
            DisplayFilesSelected(tscbDiscDrive.SelectedItem + @"games\", dgvGameListDisc);
        }
        #endregion

        #region tsmiRenameISO_Click
        private void tsmiRenameISO_Click(object sender, EventArgs e)
        {
            Int32 _selectedGameRowCount = dgvGameList.Rows.GetRowCount(DataGridViewElementStates.Selected);

            if (dgvGameList.RowCount == 0)
            {
                EmptyGamesList();
            }
            else
            {
                if (_selectedGameRowCount == 0)
                {
                    SelectGameFromList();
                }
                else
                {
                    string pathImage = dgvGameList.CurrentRow.Cells[4].Value.ToString();

                    using (var form = new frmRenameISO(fbd1.SelectedPath, pathImage))
                    {
                        var _returnRename = form.ShowDialog();
                        if (_returnRename == DialogResult.OK)
                        {
                            int _code = form.RETURN_CONFIRM;
                            if (_code == 1)
                            {
                                DisplayFilesSelected(fbd1.SelectedPath, dgvGameList);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region tsmiNotifyExit_Click
        private void tsmiNotifyExit_Click(object sender, EventArgs e)
        {
            ClearTemp();
            Application.Exit();
        }
        #endregion

        #region tsmiGameDiscDeleteAllFiles_Click
        private void tsmiGameDiscDeleteAllFiles_Click(object sender, EventArgs e)
        {
            GlobalDeleteAllGames(dgvGameListDisc);
        }
        #endregion

        #region tsmiExportTXT_Click
        private void tsmiExportTXT_Click(object sender, EventArgs e)
        {
            ExportTXT(dgvGameList);
        }
        #endregion

        #region tsmiExportHTML_Click
        private void tsmiExportHTML_Click(object sender, EventArgs e)
        {
            ExportHTML(dgvGameList);
        }
        #endregion

        #region tsmiExportCSV_Click
        private void tsmiExportCSV_Click(object sender, EventArgs e)
        {
            ExportCSV(dgvGameList);
        }
        #endregion

        #region tsmiGameSelectAll_Click
        /// <summary>
        /// Selects all files listed in the DataGridView Game List.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsmiGameSelectAll_Click(object sender, EventArgs e)
        {
            dgvGameList.EndEdit();

            foreach (DataGridViewRow dtr in dgvGameList.Rows)
            {
                ((DataGridViewCheckBoxCell)dtr.Cells[0]).Value = true;
            }
        }
        #endregion

        #region tsmiGameSelectNone_Click
        /// <summary>
        /// Deselects all files listed in the DataGridView Game List.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsmiGameSelectNone_Click(object sender, EventArgs e)
        {
            dgvGameList.EndEdit();

            foreach (DataGridViewRow dtr in dgvGameList.Rows)
            {
                ((DataGridViewCheckBoxCell)dtr.Cells[0]).Value = false;
            }
        }
        #endregion

        #region tsmiGameHashSHA1_Click
        /// <summary>
        /// Checks the SHA-1 Hash of the selected file in the DataGridView Game List.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsmiGameHashSHA1_Click(object sender, EventArgs e)
        {
            GlobalCheckHash(dgvGameList, "SHA-1");
        }
        #endregion

        #region tsmiGameDiscHashSHA1_Click
        /// <summary>
        /// Checks the SHA-1 Hash of the selected file in the DataGridView Disc List.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsmiGameDiscHashSHA1_Click(object sender, EventArgs e)
        {
            GlobalCheckHash(dgvGameListDisc, "SHA-1");
        }
        #endregion

        #region tsmiGameDiscHashMD5_Click
        /// <summary>
        /// Checks the MD5 Hash of the selected file in the DataGridView Disc List.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsmiGameDiscHashMD5_Click(object sender, EventArgs e)
        {
            GlobalCheckHash(dgvGameListDisc, "MD5");
        }
        #endregion

        #region tsmiInfoGame_Click
        private void tsmiInfoGame_Click(object sender, EventArgs e)
        {
            Int32 _selecteGameRowCount = dgvGameList.Rows.GetRowCount(DataGridViewElementStates.Selected);

            if (dgvGameList.RowCount == 0)
            {
                EmptyGamesList();
            }
            else
            {
                if (_selecteGameRowCount == 0)
                {
                    SelectGameFromList();
                }
                else
                {
                    // Nome do Arquivo, Formato (Tipo), Tamanho, Local do Arquivo, ID do Jogo
                    frmInformation _frmInfo = new frmInformation(dgvGameList.CurrentRow.Cells[1].Value.ToString(), dgvGameList.CurrentRow.Cells[2].Value.ToString(),
                        dgvGameList.CurrentRow.Cells[3].Value.ToString(), dgvGameList.CurrentRow.Cells[4].Value.ToString(), tbIDGame.Text);
                    _frmInfo.ShowDialog();
                    _frmInfo.Dispose();
                }
            }
        }
        #endregion

        #region tsmiMetaXml_Click
        private void tsmiMetaXml_Click(object sender, EventArgs e)
        {
            GCBM.tools.frmMetaXml metaXml = new tools.frmMetaXml();
            metaXml.ShowDialog();
            metaXml.Dispose();
        }
        #endregion

        #region tsmiManageApp_Click
        private void tsmiManageApp_Click(object sender, EventArgs e)
        {
            GCBM.tools.frmManageApp manageApp = new tools.frmManageApp();
            manageApp.ShowDialog();
            manageApp.Dispose();
        }
        #endregion

        #region tsmiElfDol_Click
        private void tsmiElfDol_Click(object sender, EventArgs e)
        {
            GCBM.tools.frmConvertELFtoDOL elfDol = new tools.frmConvertELFtoDOL();
            elfDol.ShowDialog();
            elfDol.Dispose();
        }
        #endregion

        #region tsmiDolphinEmulator_Click
        private void tsmiDolphinEmulator_Click(object sender, EventArgs e)
        {
            GCBM.tools.frmDolphinEmulator dolphinEmulator = new tools.frmDolphinEmulator();
            dolphinEmulator.ShowDialog();
            dolphinEmulator.Dispose();
        }
        #endregion

        #region tsmiCreatePackage_Click
        private void tsmiCreatePackage_Click(object sender, EventArgs e)
        {
            GCBM.tools.frmCreateAppPackage createPackage = new tools.frmCreateAppPackage();
            createPackage.ShowDialog();
            createPackage.Dispose();
        }
        #endregion

        #region tsmiBurnMedia_Click
        private void tsmiBurnMedia_Click(object sender, EventArgs e)
        {
            GCBM.tools.frmBurnMedia burnMedia = new tools.frmBurnMedia();
            burnMedia.ShowDialog();
            burnMedia.Dispose();
        }
        #endregion

        #region tsmiWebsiteFacebook_Click
        private void tsmiWebsiteFacebook_Click(object sender, EventArgs e)
        {
            ExternalSite("https://www.facebook.com/groups/nintendowiibrasil/", "");
        }
        #endregion

        #region tsmiOfficialGitHub_Click
        private void tsmiOfficialGitHub_Click(object sender, EventArgs e)
        {
            ExternalSite("https://axiondrak.github.io/gcbm.html", "");
        }
        #endregion

        #region tsmiOfficialGBATemp_Click
        private void tsmiOfficialGBATemp_Click(object sender, EventArgs e)
        {
            ExternalSite("https://gbatemp.net/threads/gamecube-backup-manager-official-post.611247/", "");
        }
        #endregion

        #region tsmiVerifyUpdate_Click
        private void tsmiVerifyUpdate_Click(object sender, EventArgs e)
        {
            if (File.Exists("config.ini"))
            {
                // Suporte as atualizações do canal Beta
                if (CONFIG_INI_FILE.IniReadBool("UPDATES", "UpdateBetaChannel") == true)
                {

                    AutoUpdater.Start("https://raw.githubusercontent.com/AxionDrak/GameCube-Backup-Manager/main/BetaChannel/AutoUpdaterBeta.xml");
                    AutoUpdater.ShowRemindLaterButton = false;
                    AutoUpdater.RunUpdateAsAdmin = false;
                    AutoUpdater.ReportErrors = true;
                    //AutoUpdater.UpdateFormSize = new Size(500, 400);
                }
                else
                {
                    AutoUpdater.Start("https://raw.githubusercontent.com/AxionDrak/GameCube-Backup-Manager/main/AutoUpdaterRelease.xml");
                    AutoUpdater.ShowRemindLaterButton = false;
                    AutoUpdater.RunUpdateAsAdmin = false;
                    AutoUpdater.ReportErrors = true;
                    //AutoUpdater.UpdateFormSize = new Size(500, 400);
                }
            }
            else
            {
                AutoUpdater.Start("https://raw.githubusercontent.com/AxionDrak/GameCube-Backup-Manager/main/AutoUpdaterRelease.xml");
                AutoUpdater.ShowRemindLaterButton = false;
                AutoUpdater.RunUpdateAsAdmin = false;
                AutoUpdater.ReportErrors = true;
                //AutoUpdater.UpdateFormSize = new Size(500, 400);
            }
        }
        #endregion

        // Extras Functions

        #region dgvGameList Click
        /// <summary>
        /// Performs an action when clicking on the DataGridView Game List.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvGameList_Click(object sender, EventArgs e)
        {
            ReloadDataGridViewGameList();
        }
        #endregion

        #region Disc Drive Selected Index Changed
        /// <summary>
        /// Selects the index of the changed disk drive.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tscbDiscDrive_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tscbDiscDrive.SelectedIndex != 0)
            {
                tsmiGameDiscSelectAll.Enabled = true;
                tsmiGameDiscSelectNone.Enabled = true;
                tsmiGameDiscDeleteAllFiles.Enabled = true;
                tsmiGameDiscDeleteSelectedFile.Enabled = true;
                //tsmiGameDiscExportList.Enabled = true;
                //tsmiGameDiscAllHashSHA1.Enabled = true;
                tsmiGameDiscHashSHA1.Enabled = true;
                //tsmiGameDiscAllHashMD5.Enabled = true;
                tsmiGameDiscHashMD5.Enabled = true;
            }
            else
            {
                DisableOptionsGame(dgvGameListDisc);
            }

            tabMainDisc.Text = GCBM.Properties.Resources.FilesDestinationUnit + tscbDiscDrive.SelectedItem + ")";

            try
            {
                DriveInfo[] allDrives = DriveInfo.GetDrives();

                foreach (DriveInfo d in allDrives)
                {
                    if (d.IsReady == true)
                    {
                        if (tscbDiscDrive.Text == d.Name)
                        {
                            if (d.DriveFormat == "NTFS") // NTFS
                            {
                                InvalidDrive(d.DriveFormat);

                                if (!Directory.Exists(tscbDiscDrive.Text + @"\games"))
                                {
                                    DialogResult result = MessageBox.Show(GCBM.Properties.Resources.CreateGamesFolder,
                                        GCBM.Properties.Resources.Attention, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);                                    

                                    if (result == DialogResult.Yes)
                                    {
                                        Directory.CreateDirectory(tscbDiscDrive.Text + @"\games");
                                    }
                                }
                                else
                                {
                                    // If the GAMES directory already exists, load the content recursively.
                                    DisplayFilesSelected(tscbDiscDrive.Text + @"games\", dgvGameListDisc);
                                }
                            }
                            else // FAT32 
                            {
                                if (!Directory.Exists(tscbDiscDrive.Text + @"\games"))
                                {
                                    DialogResult result = MessageBox.Show(GCBM.Properties.Resources.CreateGamesFolderNow,
                                        GCBM.Properties.Resources.Attention, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                                    if (result == DialogResult.Yes)
                                    {
                                        Directory.CreateDirectory(tscbDiscDrive.Text + @"\games");
                                    }
                                }
                                else
                                {
                                    // If the GAMES directory already exists, load the content recursively.
                                    DisplayFilesSelected(tscbDiscDrive.Text + @"games\", dgvGameListDisc);
                                }
                            }
                            //label6.Text = "Tamanho Total: " + d.TotalSize / (1024 * 1024) + " MB\nFormato Drive: " + d.DriveFormat + " \nDisponível: " + d.AvailableFreeSpace / (1024 * 1024) + " MB\n" + d.DriveType;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.Error + ex.Message + Environment.NewLine);
                GlobalNotifications(ex.Message, ToolTipIcon.Error);
            }
        }
        #endregion

        #region txtLog Text Changed
        /// <summary>
        /// Just insert an asterisk (*) in the Log tab text.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtLog_TextChanged(object sender, EventArgs e)
        {
            if (tbLog.Lines.Length > 14)
            {
                tabMainLog.Text = "Log *";
                tbLog.SelectionStart = tbLog.TextLength;
                tbLog.ScrollToCaret();
            }

            if (tbLog.Visible)
            {
                tbLog.SelectionStart = tbLog.TextLength;
                tbLog.ScrollToCaret();
            }
        }
        #endregion

        #region pictureBox Game ID Click
        /// <summary>
        /// Function to load websites in the default browser when clicking on a link image.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pbWebGameID_Click(object sender, EventArgs e)
        {
            ExternalSite("https://www.gametdb.com/Wii/", tbIDGame.Text);
        }

        private void pbWebGameDiscID_Click(object sender, EventArgs e)
        {
            ExternalSite("https://www.gametdb.com/Wii/", tbIDGameDisc.Text);
        }
        #endregion

        #region dgvGameListDisc_Click
        private void dgvGameListDisc_Click(object sender, EventArgs e)
        {
            ReloadDataGridViewDiscList();
        }
        #endregion

        #region ComboBox Database Filter
        private void cbFilterDatabase_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbFilterDatabase.SelectedIndex != 0)
            {
                if (lvDatabase.Items.Count == 0)
                {
                    MessageBox.Show(GCBM.Properties.Resources.FilterDatabase_String1 + Environment.NewLine + Environment.NewLine +
                        GCBM.Properties.Resources.FilterDatabase_String2, GCBM.Properties.Resources.Notice, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    foreach (ListViewItem item in lvDatabase.Items)
                    {
                        if (cbFilterDatabase.Text.ToLower() == item.SubItems[3].Text.ToLower())
                        {
                            lvDatabase.Refresh();
                            lvDatabase.Focus();
                            lvDatabase.HideSelection = false;
                            item.Selected = true;
                            lvDatabase.TopItem = item;
                            break;
                        }
                    }
                }
            }
        }
        #endregion

        #region lvDatabase_Click
        private void lvDatabase_Click(object sender, EventArgs e)
        {
            if (lvDatabase.SelectedItems[0].Text.ToString() != string.Empty)
            {
                try
                {
                    string _region;

                    //switch (lvDatabase.SelectedItems[0].SubItems[2].Text.ToString())
                    //{
                    //    case "NTSC-J":
                    //        _region = "JA";
                    //        break;
                    //    case "NTSC-K":
                    //        _region = "JA";
                    //        break;
                    //    case "PAL":
                    //        _region = "EN";
                    //        break;
                    //    default:
                    //        _region = "US";
                    //        break;
                    //}

                    if (lvDatabase.SelectedItems[0].SubItems[2].Text.ToString() == "NTSC-U")
                    {
                        _region = "US";
                    }
                    else if (lvDatabase.SelectedItems[0].SubItems[2].Text.ToString() == "NTSC-J")
                    {
                        _region = "JA";
                    }
                    else if (lvDatabase.SelectedItems[0].SubItems[2].Text.ToString() == "NTSC-K")
                    {
                        _region = "JA";
                    }
                    else //if (lvDatabase.SelectedItems[2].Text.ToString() == "PAL")
                    {
                        _region = "EN";
                    }

                    // Pega as capas Disc do dispositivo
                    if (File.Exists(GET_CURRENT_PATH + @"\covers\cache\" + _region + @"\disc\" + lvDatabase.SelectedItems[0].Text.ToString() + ".png"))
                    {
                        pbGameDisc.LoadAsync(GET_CURRENT_PATH + @"\covers\cache\" + _region + @"\disc\" + lvDatabase.SelectedItems[0].Text.ToString() + ".png");
                    }
                    else
                    {
                        // Pega as capas Disc da internet
                        //HttpWebResponse response = null;
                        var request = (HttpWebRequest)WebRequest.Create(@"https://art.gametdb.com/wii/disc/" + _region + "/" + lvDatabase.SelectedItems[0].Text.ToString() + ".png");
                        request.Method = "HEAD";
                        NET_RESPONSE = (HttpWebResponse)request.GetResponse();

                        try
                        {
                            if (NET_RESPONSE.StatusCode == HttpStatusCode.OK)
                            {
                                pbGameDisc.LoadAsync(@"https://art.gametdb.com/wii/disc/" + _region + "/" + lvDatabase.SelectedItems[0].Text.ToString() + ".png");
                            }
                            //else
                            //{
                            //    GlobalNotifications("O servidor informou: Capa Disco não encontrada!");
                            //    //pbGameDisc.LoadAsync(getCurrentPath + @"\media\covers\disc.png");
                            //}
                        }
                        catch (WebException ex)
                        {
                            //pbGameDisc.LoadAsync(getCurrentPath + @"\media\covers\disc.png");
                            GlobalNotifications(GCBM.Properties.Resources.ServerReportedDiscCoverNotFound, ToolTipIcon.Error);
                        }
                        finally
                        {
                            if (NET_RESPONSE != null)
                            {
                                NET_RESPONSE.Close();
                            }
                        }
                    }

                    //Pega as capas 3D do dispositivo
                    if (File.Exists(GET_CURRENT_PATH + @"\covers\cache\" + _region + @"\3d\" + lvDatabase.SelectedItems[0].Text.ToString() + ".png"))
                    {
                        pbGameCover3D.LoadAsync(GET_CURRENT_PATH + @"\covers\cache\" + _region + @"\3d\" + lvDatabase.SelectedItems[0].Text.ToString() + ".png");
                    }
                    else
                    {
                        // Pega as capas 3D da internet
                        //HttpWebResponse response = null;
                        var request3D = (HttpWebRequest)WebRequest.Create(@"https://art.gametdb.com/wii/cover3D/" + _region + "/" + lvDatabase.SelectedItems[0].Text.ToString() + ".png");
                        request3D.Method = "HEAD";
                        NET_RESPONSE = (HttpWebResponse)request3D.GetResponse();

                        try
                        {
                            if (NET_RESPONSE.StatusCode == HttpStatusCode.OK)
                            {
                                pbGameCover3D.LoadAsync(@"https://art.gametdb.com/wii/cover3D/" + _region + "/" + lvDatabase.SelectedItems[0].Text.ToString() + ".png");
                            }
                            //else
                            //{
                            //    //pbGameCover3D.LoadAsync(getCurrentPath + @"\media\covers\3d.png");
                            //    GlobalNotifications("O servidor informou: Capa 3D não encontrada!");
                            //}
                        }
                        catch (WebException ex)
                        {
                            //pbGameCover3D.LoadAsync(getCurrentPath + @"\media\covers\3d.png");
                            GlobalNotifications(GCBM.Properties.Resources.ServerReported3DCoverNotFound, ToolTipIcon.Error);
                        }
                        finally
                        {
                            if (NET_RESPONSE != null)
                            {
                                NET_RESPONSE.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    GlobalNotifications(GCBM.Properties.Resources.ServerReportedOneCoverOrBothNotFound, ToolTipIcon.Error);
                }
            }
        }
        #endregion

        // Buttons

        #region Button - Install Exact Copy Game
        /// <summary>
        /// Button to install exact copy (1:1) of the file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGameInstallExactCopy_Click(object sender, EventArgs e)
        {
            GlobalInstall(dgvGameList, 0);
        }
        #endregion

        #region Button - Install Scrub Game
        /// <summary>
        /// Button to install scrub copy of the file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGameInstallScrub_Click(object sender, EventArgs e)
        {
            GlobalInstall(dgvGameList, 1);
        }
        #endregion

        #region Button - Select Folder with ISO/GCM Game Files
        /// <summary>
        /// Button to select the directory that contains the ISO/GCM files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            try
            {
                fbd1.Description = GCBM.Properties.Resources.SelectFolderContainingIsoGcmFiles;
                fbd1.ShowNewFolderButton = false;
                if (fbd1.ShowDialog() == DialogResult.OK)
                {
                    DisplayFilesSelected(fbd1.SelectedPath, dgvGameList);
                    ListIsoFile();
                }
            }
            catch (Exception ex)
            {
                tbLog.AppendText("[" + DateString() + "]" + GCBM.Properties.Resources.Error + ex.Message + Environment.NewLine);
                GlobalNotifications(ex.Message, ToolTipIcon.Error);
            }
        }
        #endregion

        //Restarts the application (closes and reopens)
        //Application.Restart();

    } // frmMain Form
} // namespace GCBM
