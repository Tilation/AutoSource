using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BindingSourceTests
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
            DisableEditingControls();
        }
        private void DisableEditingControls()
        {
            tableLayoutPanel2.Enabled = false;
            dataGridView1.Enabled = true;
            buttonAdd.Enabled = true;
            buttonRemove.Enabled = true;
            buttonEdit.Enabled = true;
            buttonApply.Enabled = false;
            buttonCancel.Enabled = false;
        }
        private void EnableEditingControls()
        {
            tableLayoutPanel2.Enabled = true;
            dataGridView1.Enabled = false;
            buttonAdd.Enabled = false;
            buttonRemove.Enabled = false;
            buttonEdit.Enabled = false;
            buttonApply.Enabled = true;
            buttonCancel.Enabled = true;
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            if (testSource1.EmpezarTransaccion())
            {
                testSource1.AddNew();
                EnableEditingControls();
            }
        }


        private void buttonEdit_Click(object sender, EventArgs e)
        {
            if (testSource1.EmpezarTransaccion())
            {
                EnableEditingControls();
            }
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            testSource1.EmpezarTransaccion();
            testSource1.RemoveCurrent();
            testSource1.AplicarTransaccion();
        }

        private void buttonApply_Click(object sender, EventArgs e)
        {
            testSource1.AplicarTransaccion();
            DisableEditingControls();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            testSource1.DescartarTransaccion();
            DisableEditingControls();
        }
    }
}
