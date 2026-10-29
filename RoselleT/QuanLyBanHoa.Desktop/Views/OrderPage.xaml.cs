using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using QuanLyBanHoa.Desktop.Data;
using QuanLyBanHoa.Desktop.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Windows.UI;

namespace QuanLyBanHoa.Desktop.Views
{
    public sealed partial class OrderPage : Page
    {
        private List<OrderRowViewModel> _allOrders = new();
        private static List<string> _dbValidStatuses = new();
        private bool _isLoadingUI = false;
        private bool _isFilterInitialized = false;

        public OrderPage()
        {
            this.InitializeComponent();

            // Chặn không cho phép chọn ngày sau ngày hôm nay
            DpFromDate.MaxDate = DateTimeOffset.Now.Date;
            DpToDate.MaxDate = DateTimeOffset.Now.Date;

            LoadOrders();
        }

        // HÀM CHUẨN HÓA ĐỊA CHỈ THÀNH "(Phường, TP)" TỪ DELIVERYADDRESS / ADDRESS TRONG DB
        public static string ExtractWardAndCity(string? deliveryAddr, string? customerAddr = null)
        {
            string source = !string.IsNullOrWhiteSpace(deliveryAddr) ? deliveryAddr : (customerAddr ?? string.Empty);
            if (string.IsNullOrWhiteSpace(source)) return "Chưa cập nhật";

            var parts = source.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(p => p.Trim())
                              .Where(p => !string.IsNullOrEmpty(p))
                              .ToList();

            if (parts.Count >= 2)
            {
                string ward = parts[^2];
                string city = parts[^1];
                return $"({ward}, {city})";
            }

            // Nếu trong DB chỉ có 1 cụm nhưng khách hàng có địa chỉ đầy đủ (Phường, TP)
            if (!string.IsNullOrWhiteSpace(customerAddr) && customerAddr.Contains(','))
            {
                var cParts = customerAddr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(p => p.Trim())
                                         .ToList();
                if (cParts.Count >= 2)
                {
                    return $"({cParts[^2]}, {cParts[^1]})";
                }
            }

            return $"({parts[0]})";
        }

        public static List<string> GetValidStatusesFromDb(ShopDbContext db)
        {
            if (_dbValidStatuses.Count > 0) return _dbValidStatuses;

            var extracted = new List<string>();
            try
            {
                var conn = db.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT cc.definition 
                        FROM sys.check_constraints cc
                        INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
                        WHERE t.name = 'SALES_ORDER' AND cc.definition LIKE '%OrderStatus%'";

                    var defObj = cmd.ExecuteScalar();
                    if (defObj != null)
                    {
                        string definition = defObj.ToString() ?? string.Empty;
                        var matches = Regex.Matches(definition, @"N?'([^']+)'");
                        foreach (Match m in matches)
                        {
                            string val = m.Groups[1].Value.Trim();
                            if (!string.IsNullOrEmpty(val) && !extracted.Contains(val))
                            {
                                extracted.Add(val);
                            }
                        }
                    }
                }
            }
            catch (Exception) { }

            if (extracted.Count > 0)
            {
                _dbValidStatuses = extracted.OrderBy(s => GetStatusOrderWeight(s)).ToList();
                return _dbValidStatuses;
            }

            var existingInTable = db.SalesOrders
                .AsNoTracking()
                .ToList()
                .Select(o => GetStringProp(o, "OrderStatus", "Status"))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .OrderBy(s => GetStatusOrderWeight(s))
                .ToList();

            _dbValidStatuses = existingInTable.Count > 0
                ? existingInTable
                : new List<string> { "Mới", "Đang xử lý", "Đã giao", "Đã hủy" };

            return _dbValidStatuses;
        }

        public static int GetStatusOrderWeight(string status)
        {
            string s = (status ?? "").Trim().ToUpperInvariant();
            if (s.Contains("MỚI") || s.Contains("CHỜ") || s.Contains("CHƯA") || s == "PENDING")
                return 1;
            if (s.Contains("ĐANG GIAO") || s.Contains("VẬN CHUYỂN"))
                return 3;
            if (s.Contains("ĐANG") || s.Contains("XỬ LÝ") || s.Contains("XÁC NHẬN") || s.Contains("CHUẨN BỊ") || s == "PROCESSING")
                return 2;
            if (s.Contains("ĐÃ GIAO") || s.Contains("HOÀN THÀNH") || s.Contains("THÀNH CÔNG") || s == "COMPLETED")
                return 4;
            if (s.Contains("HỦY") || s.Contains("HUY") || s.Contains("CANCEL"))
                return 5;
            return 10;
        }

        private void InitializeFilterDropdowns(List<string> validStatuses, List<string> wardAreas)
        {
            if (CmbFilterStatus == null || CmbFilterWard == null) return;

            _isLoadingUI = true;

            if (!_isFilterInitialized)
            {
                CmbFilterStatus.Items.Clear();
                CmbFilterStatus.Items.Add(new ComboBoxItem { Content = "Tất cả đơn hàng", Tag = "ALL" });
                foreach (var st in validStatuses)
                {
                    CmbFilterStatus.Items.Add(new ComboBoxItem { Content = st, Tag = st });
                }
                CmbFilterStatus.SelectedIndex = 0;
                _isFilterInitialized = true;
            }

            string currentWardTag = (CmbFilterWard.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ALL";
            CmbFilterWard.Items.Clear();
            CmbFilterWard.Items.Add(new ComboBoxItem { Content = "Tất cả (Theo ngày mới nhất)", Tag = "ALL" });
            CmbFilterWard.Items.Add(new ComboBoxItem { Content = "Sắp xếp A-Z theo (Phường, TP)", Tag = "SORT_WARD" });

            foreach (var area in wardAreas.OrderBy(a => a))
            {
                CmbFilterWard.Items.Add(new ComboBoxItem { Content = area, Tag = area });
            }

            var matchItem = CmbFilterWard.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Tag?.ToString() == currentWardTag);
            CmbFilterWard.SelectedItem = matchItem ?? CmbFilterWard.Items[0];

            _isLoadingUI = false;
        }

        private async void DpFromDate_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (_isLoadingUI) return;

            DateTime today = DateTime.Today;
            if (DpFromDate.Date.HasValue && DpFromDate.Date.Value.Date > today)
            {
                DpFromDate.Date = new DateTimeOffset(today);
                await ShowAlert("Từ ngày không được chọn sau ngày hôm nay!");
            }

            if (DpFromDate.Date.HasValue)
            {
                DpToDate.MinDate = DpFromDate.Date.Value;
                if (DpToDate.Date.HasValue && DpToDate.Date.Value.Date < DpFromDate.Date.Value.Date)
                {
                    DpToDate.Date = DpFromDate.Date.Value;
                }
            }
            else
            {
                DpToDate.MinDate = DateTimeOffset.MinValue;
            }
        }

        private async void DpToDate_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (_isLoadingUI) return;

            DateTime today = DateTime.Today;
            if (DpToDate.Date.HasValue && DpToDate.Date.Value.Date > today)
            {
                DpToDate.Date = new DateTimeOffset(today);
                await ShowAlert("Đến ngày không được phép chọn ngày sau ngày hôm nay!");
            }
        }

        private void LoadOrders()
        {
            _isLoadingUI = true;
            try
            {
                using (var db = new ShopDbContext())
                {
                    var validStatuses = GetValidStatusesFromDb(db);

                    var orders = db.SalesOrders.AsNoTracking().ToList();
                    var customers = db.Customers.AsNoTracking().ToDictionary(c => c.CustomerID);
                    var details = db.OrderDetails.AsNoTracking().ToList();

                    var detailSums = details
                        .GroupBy(d => d.OrderID)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Sum(x => x.Amount > 0 ? x.Amount : (x.UnitPrice * x.Quantity - x.Discount))
                        );

                    _allOrders = orders.Select(o =>
                    {
                        int orderId = o.OrderID;
                        int custId = o.CustomerID;
                        customers.TryGetValue(custId, out var cust);

                        // ĐÃ SỬA: Lấy trực tiếp o.CreatedDate từ SalesOrder.cs
                        DateTime orderDate = o.CreatedDate;
                        string rawStatus = o.OrderStatus;
                        decimal calcTotal = detailSums.ContainsKey(orderId) ? detailSums[orderId] : 0;

                        bool isCanceled = IsOrderCanceled(rawStatus);
                        bool isCompleted = IsOrderCompleted(rawStatus);
                        decimal displayTotal = isCanceled ? 0 : calcTotal;

                        string customerName = cust != null ? cust.FullName : $"Khách #{custId}";
                        string fullAddr = !string.IsNullOrWhiteSpace(o.DeliveryAddress) ? o.DeliveryAddress : (cust?.Address ?? "—");
                        string wardCityArea = ExtractWardAndCity(o.DeliveryAddress, cust?.Address);

                        var rowOptions = new List<string>(validStatuses);
                        if (!string.IsNullOrWhiteSpace(rawStatus) && !rowOptions.Contains(rawStatus))
                        {
                            rowOptions.Add(rawStatus);
                        }

                        return new OrderRowViewModel
                        {
                            OrderID = orderId,
                            OrderCode = $"#{orderId}",
                            OrderDate = orderDate,
                            OrderDateText = orderDate.ToString("dd/MM/yyyy"),
                            CustomerName = customerName,
                            ShippingArea = wardCityArea,
                            FullAddress = fullAddr,
                            TotalAmount = displayTotal,
                            TotalAmountText = $"{displayTotal:N0} VNĐ",
                            CurrentStatusDisplay = rawStatus,
                            CanChangeStatus = !isCanceled && !isCompleted,
                            StatusOptions = rowOptions,
                            AmountFg = new SolidColorBrush(isCanceled ? Color.FromArgb(255, 220, 38, 38) : Color.FromArgb(255, 5, 150, 105)),
                            StatusFg = GetStatusBrush(rawStatus)
                        };
                    })
                    .OrderByDescending(o => o.OrderDate)
                    .ThenByDescending(o => o.OrderID)
                    .ToList();

                    var distinctWardAreas = _allOrders
                        .Select(o => o.ShippingArea)
                        .Where(a => !string.IsNullOrWhiteSpace(a) && a != "Chưa cập nhật")
                        .Distinct()
                        .ToList();

                    InitializeFilterDropdowns(validStatuses, distinctWardAreas);
                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                ShowBanner("Lỗi tải danh sách đơn hàng: " + ex.Message, isError: true);
            }
            finally
            {
                _isLoadingUI = false;
            }
        }

        private async void ApplyFilter()
        {
            if (OrderListView == null) return;

            DateTime today = DateTime.Today;
            if (DpToDate.Date.HasValue && DpToDate.Date.Value.Date > today)
            {
                DpToDate.Date = new DateTimeOffset(today);
                await ShowAlert("Đến ngày không được phép lớn hơn ngày hôm nay!");
                return;
            }

            if (DpFromDate.Date.HasValue && DpFromDate.Date.Value.Date > today)
            {
                DpFromDate.Date = new DateTimeOffset(today);
                await ShowAlert("Từ ngày không được phép lớn hơn ngày hôm nay!");
                return;
            }

            if (DpFromDate.Date.HasValue && DpToDate.Date.HasValue && DpFromDate.Date.Value.Date > DpToDate.Date.Value.Date)
            {
                await ShowAlert("Ngày bắt đầu không được lớn hơn ngày kết thúc!");
                return;
            }

            _isLoadingUI = true;
            try
            {
                var query = _allOrders.AsEnumerable();

                if (DpFromDate.Date.HasValue)
                {
                    DateTime from = DpFromDate.Date.Value.Date;
                    query = query.Where(o => o.OrderDate.Date >= from);
                }

                if (DpToDate.Date.HasValue)
                {
                    DateTime to = DpToDate.Date.Value.Date;
                    query = query.Where(o => o.OrderDate.Date <= to);
                }

                string statusTag = (CmbFilterStatus.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ALL";
                if (statusTag != "ALL")
                {
                    query = query.Where(o => string.Equals(o.CurrentStatusDisplay, statusTag, StringComparison.OrdinalIgnoreCase));
                }

                string wardTag = (CmbFilterWard?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ALL";
                if (wardTag != "ALL" && wardTag != "SORT_WARD")
                {
                    query = query.Where(o => string.Equals(o.ShippingArea, wardTag, StringComparison.OrdinalIgnoreCase));
                }

                string keyword = TxtSearch?.Text.Trim().ToLower() ?? string.Empty;
                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(o =>
                        o.OrderCode.ToLower().Contains(keyword) ||
                        o.CustomerName.ToLower().Contains(keyword) ||
                        o.ShippingArea.ToLower().Contains(keyword) ||
                        o.FullAddress.ToLower().Contains(keyword));
                }

                // Nếu chọn Sắp xếp theo (Phường, TP) hoặc lọc theo Phường
                if (wardTag == "SORT_WARD")
                {
                    query = query.OrderBy(o => o.ShippingArea).ThenByDescending(o => o.OrderDate);
                }
                else
                {
                    query = query.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.OrderID);
                }

                OrderListView.ItemsSource = query.ToList();
            }
            finally
            {
                _isLoadingUI = false;
            }
        }

        private void CmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoadingUI) ApplyFilter();
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e) => ApplyFilter();

        private void BtnResetFilter_Click(object sender, RoutedEventArgs e)
        {
            _isLoadingUI = true;
            DpFromDate.Date = null;
            DpToDate.Date = null;
            DpToDate.MinDate = DateTimeOffset.MinValue;
            DpFromDate.MaxDate = DateTimeOffset.Now.Date;
            DpToDate.MaxDate = DateTimeOffset.Now.Date;
            CmbFilterStatus.SelectedIndex = 0;
            if (CmbFilterWard.Items.Count > 0) CmbFilterWard.SelectedIndex = 0;
            TxtSearch.Text = string.Empty;
            TxtStatusMessage.Visibility = Visibility.Collapsed;
            _isLoadingUI = false;

            LoadOrders();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private async void CmbRowStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingUI) return;
            if (sender is not ComboBox cmb || cmb.Tag is not int orderId) return;
            if (cmb.SelectedItem is not string newStatus) return;

            var rowVm = _allOrders.FirstOrDefault(x => x.OrderID == orderId);
            if (rowVm == null || rowVm.CurrentStatusDisplay == newStatus) return;

            if (!rowVm.CanChangeStatus)
            {
                LoadOrders();
                return;
            }

            if (GetStatusOrderWeight(newStatus) < GetStatusOrderWeight(rowVm.CurrentStatusDisplay))
            {
                await ShowAlert($"Đơn hàng #{orderId} đang ở trạng thái '{rowVm.CurrentStatusDisplay}', không thể quay ngược về '{newStatus}'!");
                LoadOrders();
                return;
            }

            var confirmDialog = new ContentDialog
            {
                Title = "Xác nhận đổi trạng thái",
                Content = $"Bạn có chắc chắn muốn chuyển đơn hàng #{orderId} từ '{rowVm.CurrentStatusDisplay}' sang '{newStatus}' không?",
                PrimaryButtonText = "Đồng ý",
                CloseButtonText = "Hủy",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = ElementTheme.Light
            };
            confirmDialog.Resources["ContentDialogPrimaryButtonBackground"] = new SolidColorBrush(Color.FromArgb(255, 27, 36, 50));
            confirmDialog.Resources["ContentDialogPrimaryButtonForeground"] = new SolidColorBrush(Colors.White);

            if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary)
            {
                LoadOrders();
                return;
            }

            try
            {
                using (var db = new ShopDbContext())
                {
                    var order = db.SalesOrders.FirstOrDefault(o => o.OrderID == orderId);
                    if (order == null) return;

                    order.OrderStatus = newStatus;
                    db.SaveChanges();

                    ShowBanner($"Đã cập nhật trạng thái đơn hàng #{orderId} thành '{newStatus}'!", isError: false);
                    LoadOrders();
                }
            }
            catch (Exception ex)
            {
                string sqlMsg = ex.InnerException?.Message ?? ex.Message;
                ShowBanner("Lỗi cập nhật trạng thái: " + sqlMsg, isError: true);
                LoadOrders();
            }
        }

        private async void BtnViewDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int orderId)
            {
                var dialog = new OrderDetailDialog(orderId) { XamlRoot = this.XamlRoot };
                await dialog.ShowAsync();
            }
        }

        private async System.Threading.Tasks.Task ShowAlert(string msg)
        {
            var errDialog = new ContentDialog
            {
                Title = "Thông báo",
                Content = msg,
                CloseButtonText = "Đã hiểu",
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Light
            };
            await errDialog.ShowAsync();
        }

        private void ShowBanner(string message, bool isError)
        {
            TxtStatusMessage.Text = message;
            TxtStatusMessage.Foreground = new SolidColorBrush(isError
                ? Color.FromArgb(255, 220, 38, 38)
                : Color.FromArgb(255, 5, 150, 105));
            TxtStatusMessage.Visibility = Visibility.Visible;
        }

        public static bool IsOrderCanceled(string rawStatus)
        {
            string s = (rawStatus ?? "").Trim().ToUpperInvariant();
            return s.Contains("HỦY") || s.Contains("HUY") || s.Contains("CANCEL");
        }

        public static bool IsOrderCompleted(string rawStatus)
        {
            string s = (rawStatus ?? "").Trim().ToUpperInvariant();
            return s.Contains("ĐÃ GIAO") || s.Contains("HOÀN THÀNH") || s.Contains("THÀNH CÔNG") || s == "COMPLETED";
        }

        public static SolidColorBrush GetStatusBrush(string status)
        {
            int weight = GetStatusOrderWeight(status);
            return weight switch
            {
                1 => new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)),
                2 => new SolidColorBrush(Color.FromArgb(255, 217, 119, 6)),
                3 => new SolidColorBrush(Color.FromArgb(255, 14, 116, 144)),
                4 => new SolidColorBrush(Color.FromArgb(255, 5, 150, 105)),
                5 => new SolidColorBrush(Color.FromArgb(255, 220, 38, 38)),
                _ => new SolidColorBrush(Color.FromArgb(255, 27, 36, 50))
            };
        }

        public static string GetStringProp(object obj, params string[] names)
        {
            foreach (var name in names)
            {
                var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                {
                    var val = p.GetValue(obj)?.ToString();
                    if (!string.IsNullOrWhiteSpace(val)) return val;
                }
            }
            return string.Empty;
        }

        public static int GetIntProp(object obj, params string[] names)
        {
            foreach (var name in names)
            {
                var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null && p.GetValue(obj) is int val) return val;
            }
            return 0;
        }

        public static decimal GetDecimalProp(object obj, params string[] names)
        {
            foreach (var name in names)
            {
                var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null && p.GetValue(obj) != null)
                {
                    return Convert.ToDecimal(p.GetValue(obj));
                }
            }
            return 0m;
        }

        // ĐÃ SỬA: Bổ sung "CreatedDate" lên đầu để không bao giờ bị lấy nhầm DateTime.Now
        public static DateTime GetDateProp(object obj, params string[] names)
        {
            var allNames = names.Concat(new[] { "CreatedDate", "OrderDate", "CreatedAt" }).Distinct();
            foreach (var name in allNames)
            {
                var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null && p.GetValue(obj) is DateTime dt) return dt;
            }
            return DateTime.Now;
        }
    }

    public class OrderRowViewModel
    {
        public int OrderID { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string OrderDateText { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ShippingArea { get; set; } = string.Empty;
        public string FullAddress { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string TotalAmountText { get; set; } = string.Empty;
        public string CurrentStatusDisplay { get; set; } = string.Empty;
        public bool CanChangeStatus { get; set; } = true;
        public List<string> StatusOptions { get; set; } = new();
        public SolidColorBrush AmountFg { get; set; } = new(Colors.Black);
        public SolidColorBrush StatusFg { get; set; } = new(Colors.Black);
    }
}