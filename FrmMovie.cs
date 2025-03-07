using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MovieProjectTest
{
    public partial class FrmMovie : Form
    {
        public FrmMovie()
        {
            InitializeComponent();
        }

        private void FrmMovie_Load(object sender, EventArgs e)
        {
            btSaveAddEdit.Enabled = false;
            btEdit.Enabled = false;
            btDel.Enabled = false;
            groupBox2.Enabled = false;
            btAdd.Enabled = true;
            lbMovieId.Text = "";
            tbMovieName.Clear();
            tbMovieDetail.Clear();
            
        }

        private void btAdd_Click(object sender, EventArgs e)
        {
            FrmMovie_Load(sender, e);
            groupBox2.Enabled = true;
            btSaveAddEdit.Enabled = true;
            btAdd.Enabled = false;
        }

        private void btSaveAddEdit_Click(object sender, EventArgs e)
        {
            FrmMovie_Load(sender, e);
            groupBox2.Enabled = true;
            btSaveAddEdit.Enabled = true;
            btAdd.Enabled = false;
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            FrmMovie_Load(sender, e);
        }

        private void btDel_Click(object sender, EventArgs e)
        {

        }

        private void btEdit_Click(object sender, EventArgs e)
        {

        }
    }
}
