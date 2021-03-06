﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DoomLauncher
{
    public partial class StatsInfo : Form
    {
        public StatsInfo()
        {
            InitializeComponent();

            pbInfo1.Image = DoomLauncher.Properties.Resources.bon2b;

            lblZdoom.Text = "For all ZDoom based ports. Uses save games to parse statistics. This means statistics cannot be read for the last level of an episode. " +
                "Items are not available on ZDoom, Zandronum, and GZDoom pre-3.5.0. Statistics will be recorded when the game is saved or when an auto save is generated.";
            lblBoom.Text = "Uses the -levelstat parameter and parses the generated levelstat.txt. All statistics will be recorded when the game has exited.";
            lblCNDoom.Text = "Uses the -printstats parameter and parses the generated stdout.txt. All statistics will be recorded when the game has exited.";
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
