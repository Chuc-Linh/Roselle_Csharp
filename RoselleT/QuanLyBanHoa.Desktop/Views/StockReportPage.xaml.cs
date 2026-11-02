using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using QuanLyBanHoa.Desktop.Data;
using System;
using System.Linq;
using Windows.UI;

namespace QuanLyBanHoa.Desktop.Views
{
    public sealed partial class StockReportPage : Page
    {
        private bool _isShowingDialog = false;
        private bool _isInternalChange = false;

        public StockReportPage()
        {
            this.InitializeComponent();

            _isInternalChange = true;
            DateTime today = DateTime.Today;
            DpFromDate.MaxDate = new DateTimeOffset(today);
            DpToDate.MaxDate = new DateTimeOffset(today);

            DpFromDate.Date = new DateTimeOffset(new DateTime(today.Year, today.Month, 1));
            DpToDate.Date = new DateTimeOffset(today);
            _isInternalChange = false;

            LoadReportData();
        }

        private async void DpFromDate_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (_isInternalChange) return;

            DateTime today = DateTime.Today;
            if (DpFromDate.Date.HasValue && DpFromDate.Date.Value.Date > today)
            {
                _isInternalChange = true;
                DpFromDate.Date = new DateTimeOffset(today);
                _isInternalChange = false;
                await ShowAlert("Từ ngày không được chọn sau ngày hôm nay!");
            }

            if (DpFromDate.Date.HasValue)
            {
                DpToDate.MinDate = DpFromDate.Date.Value;
                if (DpToDate.Date.HasValue && DpToDate.Date.Value.Date < DpFromDate.Date.Value.Date)
                {
                    _isInternalChange = true;
                    DpToDate.Date = DpFromDate.Date.Value;
                    _isInternalChange = false;
                }
            }
            else
            {
                DpToDate.MinDate = DateTimeOffset.MinValue;
            }
        }

        private async void DpToDate_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (_isInternalChange) return;

            DateTime today = DateTime.Today;
            if (DpToDate.Date.HasValue && DpToDate.Date.Value.Date > today)
            {
                _isInternalChange = true;
                DpToDate.Date = new DateTimeOffset(today);
                _isInternalChange = false;
                await ShowAlert("Đến ngày không được phép chọn ngày sau ngày hôm nay!");
            }
        }

        private async void TxtThreshold_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInternalChange) return;

            string raw = TxtThreshold.Text.Trim();
            if (string.IsNullOrEmpty(raw)) return;

            if (raw.StartsWith("-") || (int.TryParse(raw, out int val) && val < 0))
            {
                _isInternalChange = true;
                TxtThreshold.Text = "10";
                TxtThreshold.SelectionStart = TxtThreshold.Text.Length;
                _isInternalChange = false;

                await ShowAlert("Ngưỡng cảnh báo tồn không được phép nhập số âm! Hệ thống đã đặt lại về 10.");
                return;
            }
        }

        private async void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            DateTime today = DateTime.Today;

            // 1. Kiểm tra chặn ngày tương lai
            if ((DpFromDate.Date.HasValue && DpFromDate.Date.Value.Date > today) ||
                (DpToDate.Date.HasValue && DpToDate.Date.Value.Date > today))
            {
                _isInternalChange = true;
                if (DpFromDate.Date.HasValue && DpFromDate.Date.Value.Date > today)
                    DpFromDate.Date = new DateTimeOffset(today);
                if (DpToDate.Date.HasValue && DpToDate.Date.Value.Date > today)
                    DpToDate.Date = new DateTimeOffset(today);
                _isInternalChange = false;

                await ShowAlert("Không được phép chọn ngày sau ngày hôm nay!");
                return;
            }

            if (DpFromDate.Date.HasValue && DpToDate.Date.HasValue &&
                DpFromDate.Date.Value.Date > DpToDate.Date.Value.Date)
            {
                await ShowAlert("Từ ngày không được lớn hơn Đến ngày!");
                return;
            }

            // 2. Kiểm tra chặn số âm hoặc chữ ở Ngưỡng cảnh báo tồn
            string rawThreshold = TxtThreshold.Text.Trim();
            if (rawThreshold.StartsWith("-") || !int.TryParse(rawThreshold, out int threshold) || threshold < 0)
            {
                _isInternalChange = true;
                TxtThreshold.Text = "10";
                _isInternalChange = false;

                await ShowAlert("Ngưỡng cảnh báo tồn không hợp lệ (không được nhập số âm)!");
                return;
            }

            LoadReportData();
        }

        private async System.Threading.Tasks.Task ShowAlert(string message)
        {
            if (_isShowingDialog || this.XamlRoot == null) return;
            _isShowingDialog = true;
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "Lỗi dữ liệu đầu vào",
                    Content = message,
                    CloseButtonText = "Đã hiểu",
                    XamlRoot = this.XamlRoot,
                    RequestedTheme = ElementTheme.Light
                };
                await dialog.ShowAsync();
            }
            finally
            {
                _isShowingDialog = false;
            }
        }

        private void LoadReportData()
        {
            try
            {
                DateTime fromDate = DpFromDate.Date?.DateTime.Date ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                DateTime toDate = DpToDate.Date?.DateTime.Date ?? DateTime.Now.Date;

                if (fromDate > toDate)
                {
                    DpToDate.Date = new DateTimeOffset(fromDate);
                    toDate = fromDate;
                }

                int threshold = int.TryParse(TxtThreshold.Text.Trim(), out int t) && t >= 0 ? t : 10;

                using (var db = new ShopDbContext())
                {
                    var products = db.Products.AsNoTracking().ToList();

                    var allReceipts = (from d in db.GoodsReceiptDetails
                                       join r in db.GoodsReceipts on d.ReceiptID equals r.ReceiptID
                                       where r.Status == "Đã hoàn thành" || r.Status == "Hoàn thành"
                                       select new { d.ProductID, r.ImportDate, d.Quantity }).ToList();

                    var rawOrders = db.SalesOrders.AsNoTracking().ToList();
                    var allOrderDetails = db.OrderDetails.AsNoTracking().ToList();

                    // ĐÃ SỬA: Lấy trực tiếp o.CreatedDate của SalesOrder
                    var allOrders = (from d in allOrderDetails
                                     join o in rawOrders on d.OrderID equals o.OrderID
                                     let st = o.OrderStatus
                                     where st != null && (st.Contains("Đã giao") || st.Contains("Hoàn thành") || st.Contains("COMPLETED"))
                                     select new { d.ProductID, OrderDate = o.CreatedDate, d.Quantity }).ToList();

                    var reportList = products.Select(p =>
                    {
                        int pId = p.ProductID;

                        var prodReceipts = allReceipts.Where(r => r.ProductID == pId).ToList();
                        int importBefore = prodReceipts.Where(r => r.ImportDate.Date < fromDate).Sum(r => r.Quantity);
                        int importInPeriod = prodReceipts.Where(r => r.ImportDate.Date >= fromDate && r.ImportDate.Date <= toDate).Sum(r => r.Quantity);

                        var prodOrders = allOrders.Where(o => o.ProductID == pId).ToList();
                        int exportBefore = prodOrders.Where(o => o.OrderDate.Date < fromDate).Sum(o => o.Quantity);
                        int exportInPeriod = prodOrders.Where(o => o.OrderDate.Date >= fromDate && o.OrderDate.Date <= toDate).Sum(o => o.Quantity);

                        int openingStock = Math.Max(0, importBefore - exportBefore);
                        int closingStock = Math.Max(0, openingStock + importInPeriod - exportInPeriod);

                        bool isLowStock = closingStock <= threshold;

                        return new StockReportItemViewModel
                        {
                            ProductCode = p.ProductCode,
                            ProductName = p.ProductName,
                            OpeningStock = openingStock,
                            ImportQty = importInPeriod,
                            ExportQty = exportInPeriod,
                            ClosingStock = closingStock,
                            ClosingStockText = closingStock.ToString(),
                            ClosingStockFg = new SolidColorBrush(isLowStock ? Color.FromArgb(255, 220, 38, 38) : Color.FromArgb(255, 27, 36, 50)),
                            StatusText = isLowStock ? "Sắp hết hàng" : "Ổn định",
                            StatusBg = new SolidColorBrush(isLowStock ? Color.FromArgb(255, 254, 226, 226) : Color.FromArgb(255, 236, 253, 245)),
                            StatusFg = new SolidColorBrush(isLowStock ? Color.FromArgb(255, 220, 38, 38) : Color.FromArgb(255, 5, 150, 105))
                        };
                    }).ToList();

                    ReportListView.ItemsSource = reportList;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi load báo cáo tồn kho: " + ex.Message);
            }
        }
    }

    public class StockReportItemViewModel
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int OpeningStock { get; set; }
        public int ImportQty { get; set; }
        public int ExportQty { get; set; }
        public int ClosingStock { get; set; }
        public string ClosingStockText { get; set; } = string.Empty;
        public SolidColorBrush ClosingStockFg { get; set; } = new(Colors.Black);
        public string StatusText { get; set; } = string.Empty;
        public SolidColorBrush StatusBg { get; set; } = new(Colors.Transparent);
        public SolidColorBrush StatusFg { get; set; } = new(Colors.Black);
    }
}