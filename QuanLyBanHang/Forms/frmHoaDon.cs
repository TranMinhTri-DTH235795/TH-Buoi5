using ClosedXML.Excel;
using QuanLyBanHang.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static QuanLyBanHang.Data.HoaDon;

namespace QuanLyBanHang.Forms
{
    public partial class frmHoaDon : Form
    {
        QLBHDbContext context = new QLBHDbContext();    // Khởi tạo biến ngữ cảnh CSDL 
        int id;                                         // Lấy mã hóa đơn (dùng cho Sửa và Xóa)

        public frmHoaDon()
        {
            InitializeComponent();
        }

        private void frmHoaDon_Load(object sender, EventArgs e)
        {
            dataGridView.AutoGenerateColumns = false;

            List<DanhSachHoaDon> hd = new List<DanhSachHoaDon>();
            hd = context.HoaDon.Select(r => new DanhSachHoaDon
            {
                ID = r.ID,
                NhanVienID = r.NhanVienID,
                HoVaTenNhanVien = r.NhanVien.HoVaTen,
                KhachHangID = r.KhachHangID,
                HoVaTenKhachHang = r.KhachHang.HoVaTen,
                NgayLap = r.NgayLap,
                GhiChuHoaDon = r.GhiChuHoaDon,
                TongTienHoaDon = r.HoaDon_ChiTiet.Sum(ct => (long)ct.SoLuongBan * ct.DonGiaBan),
                XemChiTiet = "Xem chi tiết"
            }).ToList();

            dataGridView.DataSource = hd;
        }

        private void btnLapHoaDon_Click(object sender, EventArgs e)
        {
            using (frmHoaDon_ChiTiet chiTiet = new frmHoaDon_ChiTiet())
            {
                chiTiet.ShowDialog();
            }
            frmHoaDon_Load(sender, e); // Tải lại dữ liệu sau khi thêm mới hóa đơn
        }

        private void btnSua_Click(object sender, EventArgs e)
        {
            id = Convert.ToInt32(dataGridView.CurrentRow.Cells["STT"].Value.ToString());
            using (frmHoaDon_ChiTiet chiTiet = new frmHoaDon_ChiTiet(id))
            {
                chiTiet.ShowDialog();
            }
            frmHoaDon_Load(sender, e); // Tải lại dữ liệu sau khi sửa hóa đơn
        }

        private void btnXoa_Click(object sender, EventArgs e)
        {
            // 1. Kiểm tra xem người dùng đã chọn dòng nào trên Grid chưa
            if (dataGridView.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn hóa đơn cần xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Lấy ID hóa đơn từ dòng đang chọn
            int hoaDonId = Convert.ToInt32(dataGridView.CurrentRow.Cells["STT"].Value);

            // 3. Hiển thị hộp thoại xác nhận xóa
            DialogResult result = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa hóa đơn này và tất cả các mặt hàng liên quan không?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // 4. Tìm hóa đơn trong cơ sở dữ liệu
                    var hoaDon = context.HoaDon.Find(hoaDonId);

                    if (hoaDon != null)
                    {
                        // 5. Xóa các chi tiết của hóa đơn này trước (để tránh lỗi khóa ngoại)
                        var chiTiets = context.HoaDon_ChiTiet.Where(ct => ct.HoaDonID == hoaDonId).ToList();
                        context.HoaDon_ChiTiet.RemoveRange(chiTiets);

                        // 6. Xóa hóa đơn chính
                        context.HoaDon.Remove(hoaDon);

                        // Lưu tất cả thay đổi vào CSDL
                        context.SaveChanges();

                        MessageBox.Show("Đã xóa hóa đơn thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // 7. Gọi lại hàm Load để làm mới danh sách hiển thị
                        frmHoaDon_Load(sender, e);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Có lỗi xảy ra khi xóa: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnThoat_Click(object sender, EventArgs e)
        {
            this.Close();

        }

        private void btnXuat_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Xuất dữ liệu ra tập tin Excel";
            saveFileDialog.Filter = "Tập tin Excel|*.xls;*.xlsx";
            saveFileDialog.FileName = "DanhSachHoaDon_" + DateTime.Now.ToShortDateString().Replace("/", "_") + ".xlsx";


            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    DataTable table = new DataTable();

                    table.Columns.AddRange(new DataColumn[5] {
                        new DataColumn("ID", typeof(int)),

                        new DataColumn("NhanVien", typeof(string)),

                        new DataColumn("KhachHang", typeof(string)),
                        new DataColumn("NgayLap", typeof(string)),
                        new DataColumn("ChiTiet", typeof(string))
                    });

                    var hoadon = context.HoaDon.ToList();
                    if (hoadon != null)
                    {
                        foreach (var p in hoadon)
                            table.Rows.Add(p.ID, p.NhanVien, p.KhachHang, p.NgayLap, p.GhiChuHoaDon);
                    }

                    /// Xuat chi tiet hoa hoa

                    DataTable table1 = new DataTable();

                    table1.Columns.AddRange(new DataColumn[5] {
                        new DataColumn("ID", typeof(int)),

                        new DataColumn("TenSanPham", typeof(string)),

                        new DataColumn("DonGiaBan", typeof(string)),

                        new DataColumn("SoLuongBan", typeof(string)),

                        new DataColumn("ThanhTien", typeof(string))
                    });

                    var hoadonct = context.HoaDon_ChiTiet.ToList();
                    if (hoadonct != null)
                    {
                        foreach (var p in hoadonct)
                            table1.Rows.Add(p.ID, p.HoaDonID, p.SanPhamID, p.SoLuongBan, p.DonGiaBan);
                    }

                    using (XLWorkbook wb = new XLWorkbook())
                    {
                        var sheet = wb.Worksheets.Add(table, "HoaDon");
                        sheet.Columns().AdjustToContents();
                        wb.SaveAs(saveFileDialog.FileName);

                        var sheet1 = wb.Worksheets.Add(table1, "HoaDon_ChiTiet");
                        sheet1.Columns().AdjustToContents();
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
                    // Chỉ cần dùng 1 block using cho workbook để đọc cả 2 sheet
                    using (XLWorkbook workbook = new XLWorkbook(openFileDialog.FileName))
                    {
                        // ==========================================================
                        // 1. NHẬP DỮ LIỆU SHEET 1: HÓA ĐƠN
                        // ==========================================================
                        IXLWorksheet wsHoaDon = workbook.Worksheet(1);
                        DataTable dtHoaDon = new DataTable();
                        bool firstRow = true;
                        string readRange = "1:1";

                        foreach (IXLRow row in wsHoaDon.RowsUsed())
                        {
                            if (firstRow)
                            {
                                readRange = string.Format("{0}:{1}", 1, row.LastCellUsed().Address.ColumnNumber);
                                foreach (IXLCell cell in row.Cells(readRange))
                                    dtHoaDon.Columns.Add(cell.Value.ToString().Trim());
                                firstRow = false;
                            }
                            else
                            {
                                dtHoaDon.Rows.Add();
                                int cellIndex = 0;
                                foreach (IXLCell cell in row.Cells(readRange))
                                {
                                    dtHoaDon.Rows[dtHoaDon.Rows.Count - 1][cellIndex] = cell.Value.ToString();
                                    cellIndex++;
                                }
                            }
                        }

                        if (dtHoaDon.Rows.Count > 0)
                        {
                            foreach (DataRow r in dtHoaDon.Rows)
                            {
                                // QUAN TRỌNG: Bỏ qua dòng nếu ID hoặc Nhân viên rỗng (tránh lỗi convert "")
                                if (string.IsNullOrWhiteSpace(r["ID"]?.ToString()) || string.IsNullOrWhiteSpace(r["NhanVien"]?.ToString()))
                                    continue;

                                HoaDon hd = new HoaDon();
                                hd.NhanVienID = Convert.ToInt32(r["NhanVien"]);
                                hd.KhachHangID = Convert.ToInt32(r["KhachHang"]);
                                hd.NgayLap = Convert.ToDateTime(r["NgayLap"]);
                                hd.GhiChuHoaDon = r["ChiTiet"].ToString(); // Sửa lại thành "ChiTiet" cho khớp với file Xuất
                                context.HoaDon.Add(hd);
                            }
                            context.SaveChanges();
                        }

                        // ==========================================================
                        // 2. NHẬP DỮ LIỆU SHEET 2: CHI TIẾT HÓA ĐƠN
                        // ==========================================================
                        IXLWorksheet wsChiTiet = workbook.Worksheet(2); // SỬA LẠI: Đọc Sheet 2
                        DataTable dtChiTiet = new DataTable();
                        firstRow = true; // reset lại biến
                        readRange = "1:1";

                        foreach (IXLRow row in wsChiTiet.RowsUsed())
                        {
                            if (firstRow)
                            {
                                readRange = string.Format("{0}:{1}", 1, row.LastCellUsed().Address.ColumnNumber);
                                foreach (IXLCell cell in row.Cells(readRange))
                                    dtChiTiet.Columns.Add(cell.Value.ToString().Trim());
                                firstRow = false;
                            }
                            else
                            {
                                dtChiTiet.Rows.Add();
                                int cellIndex = 0;
                                foreach (IXLCell cell in row.Cells(readRange))
                                {
                                    dtChiTiet.Rows[dtChiTiet.Rows.Count - 1][cellIndex] = cell.Value.ToString();
                                    cellIndex++;
                                }
                            }
                        }

                        if (dtChiTiet.Rows.Count > 0)
                        {
                            foreach (DataRow r in dtChiTiet.Rows)
                            {
                                // Loại bỏ dòng trống
                                if (string.IsNullOrWhiteSpace(r["ID"]?.ToString()))
                                    continue;

                                HoaDon_ChiTiet hdct = new HoaDon_ChiTiet();


                                hdct.HoaDonID = Convert.ToInt32(r["TenSanPham"]);
                                hdct.SanPhamID = Convert.ToInt32(r["DonGiaBan"]);
                                hdct.SoLuongBan = Convert.ToInt16(r["SoLuongBan"]);
                                hdct.DonGiaBan = Convert.ToInt32(r["ThanhTien"]);

                                context.HoaDon_ChiTiet.Add(hdct);
                            }
                            context.SaveChanges();
                        }

                        MessageBox.Show("Đã nhập dữ liệu thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        frmHoaDon_Load(sender, e);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi nhập dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void dataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            
            // 1. Kiểm tra xem người dùng có click vào một dòng hợp lệ không (tránh click vào tiêu đề cột)
            if (e.RowIndex >= 0)
            {
                // 2. Kiểm tra xem người dùng có đang click vào cột "Xem chi tiết" hay không
                // Chú ý: Thay "XemChiTiet" bằng đúng tên DataPropertyName (hoặc Name) cột chứa chữ "Xem chi tiết" của bạn
                if (dataGridView.Columns[e.ColumnIndex].DataPropertyName == "XemChiTiet" ||
                    dataGridView.Columns[e.ColumnIndex].HeaderText == "Chi tiết")
                {
                    // 3. Lấy ID của hóa đơn ở dòng vừa click
                    // Lưu ý: Ở code cũ nút Sửa bạn dùng tên cột là "STT", nhưng trên hình tôi thấy tiêu đề là "ID". 
                    // Bạn hãy đổi chữ "ID" dưới đây cho khớp với thuộc tính Name của cột ID trong DataGridView nhé.
                    int idHoaDon = Convert.ToInt32(dataGridView.Rows[e.RowIndex].Cells["STT"].Value);


                    // 4. Mở form chi tiết giống hệt nút Sửa
                    using (frmHoaDon_ChiTiet chiTiet = new frmHoaDon_ChiTiet(idHoaDon))
                    {
                        chiTiet.ShowDialog();
                    }

                    // 5. Tải lại dữ liệu sau khi đóng form chi tiết (nếu có chỉnh sửa bên trong)
                    frmHoaDon_Load(sender, e);               
            }
        }
    }
    }


}
