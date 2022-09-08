﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace GCBM;

public partial class frmRenameISO : Form
{
    #region Rename ISO

    private void RenameISO()
    {
        var _directoryName = Path.GetDirectoryName(NEW_NAME);
        var _fileName = Path.GetFileName(NEW_NAME);
        var ret = Regex.Replace(_fileName,
                @"[^0-9a-zA-ZéúíóáÉÚÍÓÁèùìòàÈÙÌÒÀõãñÕÃÑêûîôâÊÛÎÔÂëÿüïöäËYÜÏÖÄçÇ[\]\-().]+?", string.Empty)
            .Replace("/", "&");

        if (chkRenameISO.Checked)
        {
            if (File.Exists(NEW_NAME))
            {
                if (!string.IsNullOrEmpty(ret))
                {
                    //File.Move(_oldName + Path.DirectorySeparatorChar + _newName, _oldName + Path.DirectorySeparatorChar + newFilename);
                    File.Move(NEW_NAME, _directoryName + Path.DirectorySeparatorChar + ret);
                    if (File.Exists(_directoryName + Path.DirectorySeparatorChar + ret))
                    {
                        ConfirmRename(ret);

                        DialogResult = DialogResult.OK;
                        RETURN_CONFIRM = 1;
                        Close();
                    }
                }
            }
        }
        else
        {
            if (File.Exists(NEW_NAME))
            {
                var newFilename = tbRenameISO.Text;
                if (!string.IsNullOrEmpty(newFilename))
                {
                    File.Move(NEW_NAME, _directoryName + Path.DirectorySeparatorChar + newFilename);
                    if (File.Exists(_directoryName + Path.DirectorySeparatorChar + newFilename))
                    {
                        ConfirmRename(newFilename);

                        DialogResult = DialogResult.OK;
                        RETURN_CONFIRM = 1;
                        Close();
                    }
                }
                else
                {
                    NameNeeded();
                }
            }
        }
    }

    #endregion

    #region chkRenameISO_CheckedChanged

    private void chkRenameISO_CheckedChanged(object sender, EventArgs e)
    {
        tbRenameISO.Enabled = !chkRenameISO.Checked;
    }

    #endregion

    #region Properties

    public string NEW_NAME { get; }
    public string OLD_NAME { get; }
    public int RETURN_CONFIRM { get; set; }

    #endregion

    #region Main Form

    public frmRenameISO()
    {
        InitializeComponent();
    }

    public frmRenameISO(string fbd, string pathImage)
    {
        InitializeComponent();

        NEW_NAME = pathImage;
        OLD_NAME = fbd;
        tbRenameISO.Text = Path.GetFileName(NEW_NAME);
    }

    #endregion

    #region Notifications

    private void ConfirmRename(string newFilename)
    {
        _ = MessageBox.Show("Arquivo renomeado para: " +
                            Environment.NewLine + Environment.NewLine +
                            newFilename, "AVISO", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void NameNeeded()
    {
        _ = MessageBox.Show("Por favor, digite um nome para o arquivo!",
            "AVISO", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    #endregion

    #region Buttons

    private void btnConfirm_Click(object sender, EventArgs e)
    {
        RenameISO();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        Close();
        Dispose();
    }

    #endregion
}