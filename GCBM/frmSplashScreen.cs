﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GCBM
{
    public partial class frmSplashScreen : Form
    {
        private const string INI_FILE            = "config.ini";
        private readonly IniFile CONFIG_INI_FILE = new IniFile(INI_FILE);
        private delegate void ProgressDelegate(int progress);
        private ProgressDelegate del;

        public frmSplashScreen()
        {
            InitializeComponent();

            AdjustLanguage();
            CurrentYear();
            this.pbSplashScreen.Maximum = 100;
            del = this.UpdateProgressInternal;
        }

        private void AdjustLanguage()
        {
            switch (CONFIG_INI_FILE.IniReadInt("LANGUAGE", "ConfigLanguage"))
            {
                case 0:
                    Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("pt-BR");
                    this.Controls.Clear();
                    InitializeComponent();
                    break;
                case 1:
                    Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
                    this.Controls.Clear();
                    InitializeComponent();
                    break;
                case 2:
                    Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("es");
                    this.Controls.Clear();
                    InitializeComponent();
                    break;
                case 3:
                    Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ko");
                    this.Controls.Clear();
                    InitializeComponent();
                    break;
            }
        }

        private void CurrentYear()
        {
            DateTime _currentYear = DateTime.Now;
            tsslCurrentYear.Text = "Copyright © 2019 - " + _currentYear.Year.ToString() + " Laete Meireles";
        }

        private void UpdateProgressInternal(int progress)
        {
            if (this.Handle == null)
            {
                return;
            }
            this.pbSplashScreen.Value = progress;
        }
        public void UpdateProgress(int progress)
        {
            this.Invoke(del, progress);
        }

    }
}
