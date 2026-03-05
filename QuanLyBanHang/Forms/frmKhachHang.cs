using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace QuanLyBanHang.Forms
{
    public partial class frmKhachHang : Form
    {
        // Chỉ khai báo 1 lần duy nhất ở đây
        QLBHDbContext context = new QLBHDbContext();
        bool xuLyThem = false;
        int id;

        public frmKhachHang()
        {
            InitializeComponent();
        }

        private void BatTatChucNang(bool giaTri)
        {
            btnLuu.Enabled = giaTri;
            btnHuyBo.Enabled = giaTri;
            txtHoVaTen.Enabled = giaTri;
            txtDiaChi.Enabled = giaTri;

            // Kiểm tra an toàn nếu txtDienThoai tồn tại
            if (txtDienThoai != null) txtDienThoai.Enabled = giaTri;

            btnThem.Enabled = !giaTri;
            btnSua.Enabled = !giaTri;
            btnXoa.Enabled = !giaTri;
        }

        private void frmKhachHang_Load(object sender, EventArgs e)
        {
            try
            {
                BatTatChucNang(false);
                // Load dữ liệu từ Database
                var dsKhachHang = context.KhachHang.ToList();

                BindingSource bs = new BindingSource();
                bs.DataSource = dsKhachHang;

                // Xóa và thiết lập Binding
                txtHoVaTen.DataBindings.Clear();
                txtHoVaTen.DataBindings.Add("Text", bs, "HoVaTen", true, DataSourceUpdateMode.Never);

                txtDiaChi.DataBindings.Clear();
                txtDiaChi.DataBindings.Add("Text", bs, "DiaChi", true, DataSourceUpdateMode.Never);

                if (txtDienThoai != null)
                {
                    txtDienThoai.DataBindings.Clear();
                    txtDienThoai.DataBindings.Add("Text", bs, "DienThoai", true, DataSourceUpdateMode.Never);
                }

                dataGridView1.DataSource = bs;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối CSDL: " + ex.Message);
            }
        }

        private void btnThem_Click(object sender, EventArgs e)
        {
            xuLyThem = true;
            BatTatChucNang(true);
            txtHoVaTen.Clear();
            txtDiaChi.Clear();
            if (txtDienThoai != null) txtDienThoai.Clear();
            txtHoVaTen.Focus();
        }

        private void btnSua_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;
            xuLyThem = false;
            BatTatChucNang(true);

            // Lấy ID từ dòng đang chọn trên lưới
            id = Convert.ToInt32(dataGridView1.CurrentRow.Cells[0].Value);
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHoVaTen.Text))
            {
                MessageBox.Show("Vui lòng nhập họ tên khách hàng!");
                return;
            }

            if (xuLyThem)
            {
                KhachHang kh = new KhachHang
                {
                    HoVaTen = txtHoVaTen.Text,
                    DiaChi = txtDiaChi.Text,
                    DienThoai = txtDienThoai?.Text
                };
                context.KhachHang.Add(kh);
            }
            else
            {
                var kh = context.KhachHang.Find(id);
                if (kh != null)
                {
                    kh.HoVaTen = txtHoVaTen.Text;
                    kh.DiaChi = txtDiaChi.Text;
                    if (txtDienThoai != null) kh.DienThoai = txtDienThoai.Text;
                }
            }

            context.SaveChanges();
            frmKhachHang_Load(sender, e); // Tải lại lưới
        }

        private void btnXoa_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;

            if (MessageBox.Show("Xác nhận xóa khách hàng này?", "Thông báo", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                id = Convert.ToInt32(dataGridView1.CurrentRow.Cells[0].Value);
                var kh = context.KhachHang.Find(id);
                if (kh != null) context.KhachHang.Remove(kh);
                context.SaveChanges();
                frmKhachHang_Load(sender, e);
            }
        }

        private void btnHuyBo_Click(object sender, EventArgs e) => frmKhachHang_Load(sender, e);

        private void btnThoat_Click(object sender, EventArgs e) => this.Close();

        private void btnNhap_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Nhập dữ liệu từ tập tin Excel";
            openFileDialog.Filter = "Tập tin Excel|*.xls;*.xlsx";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    DataTable table = new DataTable();
                    using (XLWorkbook workbook = new XLWorkbook(openFileDialog.FileName))
                    {
                        IXLWorksheet worksheet = workbook.Worksheet(1);
                        bool firstRow = true;
                        string readRange = "1:1";
                        foreach (IXLRow row in worksheet.RowsUsed())
                        {
                            // Đọc dòng tiêu đề (dòng đầu tiên) 
                            if (firstRow)
                            {
                                readRange = string.Format("{0}:{1}", 1, row.LastCellUsed().Address.ColumnNumber);
                                foreach (IXLCell cell in row.Cells(readRange))
                                    table.Columns.Add(cell.Value.ToString());
                                firstRow = false;
                            }
                            else // Đọc các dòng nội dung (các dòng tiếp theo) 
                            {
                                table.Rows.Add();
                                int cellIndex = 0;
                                foreach (IXLCell cell in row.Cells(readRange))
                                {
                                    table.Rows[table.Rows.Count - 1][cellIndex] = cell.Value.ToString();
                                    cellIndex++;
                                }
                            }
                        }
                        if (table.Rows.Count > 0)
                        {
                            foreach (DataRow r in table.Rows)
                            {
                                KhachHang kh = new KhachHang();
                                kh.HoVaTen = r["HoVaTen"].ToString();
                                context.KhachHang.Add(kh);
                                kh.DiaChi = r["DiaChi"].ToString();
                                context.KhachHang.Add(kh);
                                kh.DienThoai = r["DienThoai"].ToString();
                                context.KhachHang.Add(kh);
                            }
                            context.SaveChanges();

                            MessageBox.Show("Đã nhập thành công " + table.Rows.Count + " dòng.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            frmKhachHang_Load(sender, e);
                        }
                        if (firstRow)
                            MessageBox.Show("Tập tin Excel rỗng.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void btnXuat_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Xuất dữ liệu ra tập tin Excel";
            saveFileDialog.Filter = "Tập tin Excel|*.xls;*.xlsx";
            saveFileDialog.FileName = "KhachHang_" + DateTime.Now.ToShortDateString().Replace("/", "_") + ".xlsx";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    DataTable table = new DataTable();

                    table.Columns.AddRange(new DataColumn[4] {
    new DataColumn("ID", typeof(int)),
    new DataColumn("HoVaTen", typeof(string)),
    new DataColumn("DienThoai", typeof(string)),
    new DataColumn("DiaChi", typeof(string))
   });

                    var khachhang = context.KhachHang.ToList();
                    if (khachhang != null)
                    {
                        foreach (var p in khachhang)
                            table.Rows.Add(p.ID, p.HoVaTen, p.DienThoai, p.DiaChi);
                    }

                    using (XLWorkbook wb = new XLWorkbook())
                    {
                        var sheet = wb.Worksheets.Add(table, "KhachHang");
                        sheet.Columns().AdjustToContents();
                        wb.SaveAs(saveFileDialog.FileName);

                        MessageBox.Show("Đã xuất dữ liệu ra tập tin Excel thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void btnThoat_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}