using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace MovieProjectTest
{
    public partial class FrmMovie : Form
    {
        // Fields
        private readonly string conStr = "Server=MixHouse\\SQLEXPRESS;Database=movie_record_db;Integrated Security=True;Connect Timeout=30";
        private byte[] movieImg;
        private byte[] movieDirImg;

        public FrmMovie()
        {
            InitializeComponent();
        }

        // Utility Methods
        private static void ShowWarningMsg(string msg)
        {
            MessageBox.Show(msg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private bool TestConnection()
        {
            try
            {
                using (var conn = new SqlConnection(conStr))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (SqlException ex)
            {
                ShowWarningMsg($"การเชื่อมต่อล้มเหลว: {ex.Message}");
                return false;
            }
        }

        // Database Methods
        private bool IsNewMovie(string movieId)
        {
            try
            {
                using (var conn = new SqlConnection(conStr))
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM movie_tb WHERE movieId = @movieId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@movieId", movieId);
                    return (int)cmd.ExecuteScalar() == 0;
                }
            }
            catch (Exception ex)
            {
                ShowWarningMsg($"เกิดข้อผิดพลาด: {ex.Message}");
                return true;
            }
        }

        private string GenerateNewMovieId()
        {
            const string prefix = "mv";
            string newMovieId = $"{prefix}001";

            try
            {
                // ทดสอบการเชื่อมต่อก่อน
                using (var conn = new SqlConnection(conStr))
                {
                    conn.Open();
                    if (conn.State != ConnectionState.Open)
                    {
                        ShowWarningMsg("ไม่สามารถเชื่อมต่อฐานข้อมูลได้");
                        return newMovieId;
                    }

                    // ดึงรหัสสูงสุดจากตาราง
                    using (var cmd = new SqlCommand("SELECT MAX(movieId) FROM movie_tb WITH (UPDLOCK)", conn))
                    {
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value && !string.IsNullOrEmpty(result.ToString()))
                        {
                            string lastMovieId = result.ToString().Trim();
                            if (lastMovieId.StartsWith(prefix))
                            {
                                string numberPart = lastMovieId.Substring(2);
                                if (int.TryParse(numberPart, out int number))
                                {
                                    newMovieId = $"{prefix}{++number:D3}";
                                }
                                else
                                {
                                    ShowWarningMsg($"ไม่สามารถแปลง '{numberPart}' เป็นตัวเลขได้");
                                }
                            }
                            else
                            {
                                ShowWarningMsg($"รูปแบบ ID '{lastMovieId}' ไม่เริ่มต้นด้วย '{prefix}'");
                            }
                        }
                        // หากไม่มีข้อมูลในตาราง หรือเกิดข้อผิดพลาดในการแปลง ให้ใช้ค่าเริ่มต้น mv001
                    }
                }
            }
            catch (SqlException ex)
            {
                ShowWarningMsg($"เกิดข้อผิดพลาด SQL: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowWarningMsg($"เกิดข้อผิดพลาดในการสร้าง Movie ID: {ex.Message}");
            }

            return newMovieId;
        }

        private void LoadDataIntoComboBox()
        {
            try
            {
                using (var conn = new SqlConnection(conStr))
                using (var cmd = new SqlCommand("SELECT movieTypeName FROM movie_type_tb", conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        cbbMovieType.Items.Clear();
                        while (reader.Read())
                        {
                            cbbMovieType.Items.Add(reader["movieTypeName"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowWarningMsg($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        private void GetMovieFromDBToDGV()
        {
            try
            {
                using (var conn = new SqlConnection(conStr))
                using (var adapter = new SqlDataAdapter(
                    @"SELECT m.movieId, m.movieName, m.movieDetail, m.movieDateSale, mt.movieTypeName 
                      FROM movie_tb m INNER JOIN movie_type_tb mt ON m.movieTypeId = mt.movieTypeId", conn))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dgvMovieShowAll.Rows.Clear();
                    var thaiCulture = new System.Globalization.CultureInfo("th-TH");

                    foreach (DataRow row in dt.Rows)
                    {
                        int rowIndex = dgvMovieShowAll.Rows.Add(
                            row["movieId"],
                            row["movieName"],
                            row["movieDetail"],
                            Convert.ToDateTime(row["movieDateSale"]).ToString("d MMMM yyyy", thaiCulture),
                            row["movieTypeName"]
                        );
                        // เพิ่ม Tag ให้แต่ละแถวเพื่อเก็บ movieId สำหรับการไฮไลต์
                        dgvMovieShowAll.Rows[rowIndex].Tag = row["movieId"];
                    }

                    dgvMovieShowAll.ClearSelection();
                    dgvMovieShowAll.EnableHeadersVisualStyles = false;
                }
            }
            catch (Exception ex)
            {
                ShowWarningMsg($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        private void SearchByMovieId(string movieId)
        {
            try
            {
                using (var conn = new SqlConnection(conStr))
                using (var cmd = new SqlCommand("SELECT movieId, movieName FROM movie_tb WHERE movieId = @movieId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@movieId", movieId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var item = new ListViewItem("1")
                            {
                                Tag = reader["movieId"].ToString()
                            };
                            item.SubItems.Add(reader["movieName"].ToString());
                            lsMovieShow.Items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowWarningMsg($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        private void SearchByMovieName(string movieName)
        {
            try
            {
                using (var conn = new SqlConnection(conStr))
                using (var cmd = new SqlCommand("SELECT movieId, movieName FROM movie_tb WHERE movieName LIKE @movieName", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@movieName", $"%{movieName}%");
                    using (var reader = cmd.ExecuteReader())
                    {
                        int count = 1;
                        while (reader.Read())
                        {
                            var item = new ListViewItem(count++.ToString())
                            {
                                Tag = reader["movieId"].ToString()
                            };
                            item.SubItems.Add(reader["movieName"].ToString());
                            lsMovieShow.Items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowWarningMsg($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        // Form Management Methods
        private void ClearForm()
        {
            rdMovieId.Checked = true;
            btAdd.Enabled = true;
            btEdit.Enabled = false;
            btDel.Enabled = false;
            btSaveAddEdit.Enabled = false;
            groupBox2.Enabled = false;
            lbMovieId.Text = string.Empty;
            tbMovieSearch.Clear();
            tbMovieName.Clear();
            tbMovieDetail.Clear();
            tbMovieDVDTotal.Clear();
            tbMovieDVDPrice.Clear();
            lsMovieShow.Items.Clear();
            nudMovieHour.Value = 0;
            nudMovieMinute.Value = 0;
            cbbMovieType.SelectedIndex = 0;
            dtpMovieDateSale.Value = DateTime.Now;
            pcbMovieImg.Image = null;
            pcbDirMovie.Image = null;
            movieImg = null;
            movieDirImg = null;
        }

        // Event Handlers
        private void FrmMovie_Load(object sender, EventArgs e)
        {
            if (!TestConnection()) return;
            ClearForm();
            GetMovieFromDBToDGV();
            LoadDataIntoComboBox();

            // ตั้งค่าเริ่มต้นสำหรับการไฮไลต์
            dgvMovieShowAll.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            lsMovieShow.View = View.Details;
            lsMovieShow.FullRowSelect = true;
        }

        private void BtAdd_Click(object sender, EventArgs e)
        {
            ClearForm();
            btAdd.Enabled = false;
            btSaveAddEdit.Enabled = true;
            groupBox2.Enabled = true;
            lbMovieId.Text = GenerateNewMovieId();
        }

        private void BtEdit_Click(object sender, EventArgs e)
        {
            groupBox2.Enabled = true;
            btSaveAddEdit.Enabled = true;
            btAdd.Enabled = btEdit.Enabled = btDel.Enabled = false;
        }

        private void BtDel_Click(object sender, EventArgs e)
        {
            if (lsMovieShow.SelectedItems.Count == 0)
            {
                MessageBox.Show("กรุณาเลือกภาพยนตร์ที่ต้องการลบ");
                return;
            }

            string movieId = lsMovieShow.SelectedItems[0].Tag.ToString();
            if (MessageBox.Show("คุณต้องการลบข้อมูลภาพยนตร์นี้ใช่หรือไม่?", "ยืนยัน", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                using (var conn = new SqlConnection(conStr))
                using (var cmd = new SqlCommand("DELETE FROM movie_tb WHERE movieId = @movieId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@movieId", movieId);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("ลบข้อมูลภาพยนตร์สำเร็จ");
                        FrmMovie_Load(sender, e);
                    }
                    else
                    {
                        MessageBox.Show("ไม่พบข้อมูลที่ต้องการลบ");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowWarningMsg($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        private void BtSaveAddEdit_Click(object sender, EventArgs e)
        {
            var validations = new (bool Condition, string Message)[]
            {
                (string.IsNullOrWhiteSpace(tbMovieName.Text), "ป้อนชื่อภาพยนต์ด้วย"),
                (string.IsNullOrWhiteSpace(tbMovieDetail.Text), "ป้อนรายละเอียดภาพยนต์ด้วย"),
                (nudMovieHour.Value <= 0, "ชั่วโมงต้องมากกว่า 0"),
                (string.IsNullOrWhiteSpace(tbMovieDVDTotal.Text), "ป้อนจำนวน DVD ด้วย"),
                (string.IsNullOrWhiteSpace(tbMovieDVDPrice.Text), "ป้อนราคา DVD ด้วย"),
                (movieImg == null, "เลือกรูปตัวอย่างภาพยนต์ด้วย"),
                (movieDirImg == null, "เลือกรูปผู้กำกับภาพยนต์ด้วย")
            };

            var error = validations.FirstOrDefault(v => v.Condition);
            if (error.Condition)
            {
                ShowWarningMsg(error.Message);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(conStr))
                using (var cmd = new SqlCommand("", conn))
                {
                    conn.Open();
                    var tran = conn.BeginTransaction();
                    cmd.Transaction = tran;

                    cmd.CommandText = IsNewMovie(lbMovieId.Text)
                        ? "INSERT INTO movie_tb (movieId, movieName, movieDetail, movieDateSale, movieLengthHour, movieLengthMinute, movieTypeId, movieDVDTotal, movieDVDPrice, movieImg, movieDirImg) " +
                          "VALUES (@movieId, @movieName, @movieDetail, @movieDateSale, @movieLengthHour, @movieLengthMinute, @movieTypeId, @movieDVDTotal, @movieDVDPrice, @movieImg, @movieDirImg)"
                        : "UPDATE movie_tb SET movieName=@movieName, movieDetail=@movieDetail, movieDateSale=@movieDateSale, movieLengthHour=@movieLengthHour, " +
                          "movieLengthMinute=@movieLengthMinute, movieTypeId=@movieTypeId, movieDVDTotal=@movieDVDTotal, movieDVDPrice=@movieDVDPrice, movieImg=@movieImg, movieDirImg=@movieDirImg " +
                          "WHERE movieId=@movieId";

                    cmd.Parameters.AddRange(new[]
                    {
                        new SqlParameter("@movieId", lbMovieId.Text.Trim()),
                        new SqlParameter("@movieName", tbMovieName.Text.Trim()),
                        new SqlParameter("@movieDetail", tbMovieDetail.Text.Trim()),
                        new SqlParameter("@movieDateSale", dtpMovieDateSale.Value),
                        new SqlParameter("@movieLengthHour", nudMovieHour.Value),
                        new SqlParameter("@movieLengthMinute", nudMovieMinute.Value),
                        new SqlParameter("@movieTypeId", cbbMovieType.SelectedIndex + 1),
                        new SqlParameter("@movieDVDTotal", Convert.ToInt32(tbMovieDVDTotal.Text)),
                        new SqlParameter("@movieDVDPrice", Convert.ToDecimal(tbMovieDVDPrice.Text)),
                        new SqlParameter("@movieImg", movieImg),
                        new SqlParameter("@movieDirImg", movieDirImg)
                    });

                    cmd.ExecuteNonQuery();
                    tran.Commit();
                    ShowWarningMsg("บันทึกข้อมูลสำเร็จ!");
                    ClearForm();
                    FrmMovie_Load(sender, e);
                }
            }
            catch (Exception ex)
            {
                ShowWarningMsg($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        private void BtCancel_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("ต้องการล้างข้อมูลหรือไม่", "ยืนยัน", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ClearForm();
            }
        }

        private void BtExit_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("ต้องการออกจากแอปหรือไม่", "ยืนยัน", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void BtMovieSearch_Click(object sender, EventArgs e)
        {
            string searchText = tbMovieSearch.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show("กรุณาป้อนคำค้นหา");
                return;
            }

            lsMovieShow.Items.Clear();
            groupBox2.Enabled = false;

            if (rdMovieId.Checked)
            {
                SearchByMovieId(searchText);
            }
            else if (rdMovieName.Checked)
            {
                SearchByMovieName(searchText);
            }

            if (lsMovieShow.Items.Count == 0)
            {
                MessageBox.Show("ไม่พบข้อมูลที่ค้นหา");
            }
        }

        private void RdMovieId_Click(object sender, EventArgs e)
        {
            lsMovieShow.Items.Clear();
            tbMovieSearch.Clear();
        }

        private void RdMovieName_Click(object sender, EventArgs e)
        {
            lsMovieShow.Items.Clear();
            tbMovieSearch.Clear();
        }

        private void LsMovieShow_SelectedIndexChanged(object sender, EventArgs e)
        {
            // รีเซ็ตสีของทุกแถวใน ListView
            foreach (ListViewItem item in lsMovieShow.Items)
            {
                item.BackColor = SystemColors.Window;
                item.ForeColor = SystemColors.WindowText;
            }

            if (lsMovieShow.SelectedItems.Count == 0) return;

            // ไฮไลต์แถวที่เลือกใน ListView
            var selectedItem = lsMovieShow.SelectedItems[0];
            selectedItem.BackColor = Color.LightBlue;
            selectedItem.ForeColor = Color.Black;

            btAdd.Enabled = false;
            btEdit.Enabled = btDel.Enabled = true;
            btSaveAddEdit.Enabled = false;
            string movieId = selectedItem.Tag.ToString();

            // ไฮไลต์แถวที่เกี่ยวข้องใน DataGridView
            foreach (DataGridViewRow row in dgvMovieShowAll.Rows)
            {
                row.DefaultCellStyle.BackColor = Color.White;
                row.DefaultCellStyle.ForeColor = Color.Black;
                if (row.Tag?.ToString() == movieId)
                {
                    row.DefaultCellStyle.BackColor = Color.LightYellow;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                }
            }

            try
            {
                using (var conn = new SqlConnection(conStr))
                using (var cmd = new SqlCommand("SELECT * FROM movie_tb WHERE movieId = @movieId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@movieId", movieId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            lbMovieId.Text = movieId;
                            tbMovieName.Text = reader["movieName"].ToString();
                            tbMovieDetail.Text = reader["movieDetail"].ToString();
                            dtpMovieDateSale.Value = Convert.ToDateTime(reader["movieDateSale"]);
                            nudMovieHour.Value = Convert.ToInt32(reader["movieLengthHour"]);
                            nudMovieMinute.Value = Convert.ToInt32(reader["movieLengthMinute"]);
                            cbbMovieType.SelectedIndex = Convert.ToInt32(reader["movieTypeId"]) - 1;
                            tbMovieDVDTotal.Text = reader["movieDVDTotal"].ToString();
                            tbMovieDVDPrice.Text = reader["movieDVDPrice"].ToString();

                            movieImg = reader["movieImg"] as byte[];
                            pcbMovieImg.Image = movieImg != null
                                ? Image.FromStream(new MemoryStream(movieImg))
                                : null;

                            movieDirImg = reader["movieDirImg"] as byte[];
                            pcbDirMovie.Image = movieDirImg != null
                                ? Image.FromStream(new MemoryStream(movieDirImg))
                                : null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowWarningMsg($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        private void DgvMovieShowAll_SelectionChanged(object sender, EventArgs e)
        {
            // รีเซ็ตสีของทุกแถวใน DataGridView
            foreach (DataGridViewRow row in dgvMovieShowAll.Rows)
            {
                row.DefaultCellStyle.BackColor = Color.White;
                row.DefaultCellStyle.ForeColor = Color.Black;
            }

            // รีเซ็ตสีของทุกแถวใน ListView
            foreach (ListViewItem item in lsMovieShow.Items)
            {
                item.BackColor = SystemColors.Window;
                item.ForeColor = SystemColors.WindowText;
            }

            if (dgvMovieShowAll.SelectedRows.Count == 0) return;

            // ไฮไลต์แถวที่เลือกใน DataGridView
            var selectedRow = dgvMovieShowAll.SelectedRows[0];
            selectedRow.DefaultCellStyle.BackColor = Color.LightYellow;
            selectedRow.DefaultCellStyle.ForeColor = Color.Black;

            // ไฮไลต์แถวที่เกี่ยวข้องใน ListView
            string movieId = selectedRow.Tag?.ToString();
            foreach (ListViewItem item in lsMovieShow.Items)
            {
                if (item.Tag?.ToString() == movieId)
                {
                    item.BackColor = Color.LightBlue;
                    item.ForeColor = Color.Black;
                }
            }
        }

        private void BtSelectImgMv_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = "Image Files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string extFile = Path.GetExtension(ofd.FileName).ToLower();
                    pcbMovieImg.Image = Image.FromFile(ofd.FileName);
                    using (var ms = new MemoryStream())
                    {
                        pcbMovieImg.Image.Save(ms, extFile == ".jpg" || extFile == ".jpeg"
                            ? System.Drawing.Imaging.ImageFormat.Jpeg
                            : System.Drawing.Imaging.ImageFormat.Png);
                        movieImg = ms.ToArray();
                    }
                }
            }
        }

        private void BtSelectImgDir_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = "Image Files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string extFile = Path.GetExtension(ofd.FileName).ToLower();
                    pcbDirMovie.Image = Image.FromFile(ofd.FileName);
                    using (var ms = new MemoryStream())
                    {
                        pcbDirMovie.Image.Save(ms, extFile == ".jpg" || extFile == ".jpeg"
                            ? System.Drawing.Imaging.ImageFormat.Jpeg
                            : System.Drawing.Imaging.ImageFormat.Png);
                        movieDirImg = ms.ToArray();
                    }
                }
            }
        }

        private void TbMovieDVDTotal_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void TbMovieDVDPrice_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (char.IsDigit(e.KeyChar)) return;
            if (e.KeyChar == '.' && !tbMovieDVDPrice.Text.Contains(".") && tbMovieDVDPrice.Text.Length > 0) return;
            e.Handled = true;
        }
    }
}