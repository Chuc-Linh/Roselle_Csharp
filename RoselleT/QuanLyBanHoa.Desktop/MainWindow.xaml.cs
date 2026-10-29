using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace QuanLyBanHoa.Desktop
{
    public sealed partial class MainWindow : Window
    {
        public static MainWindow? Current { get; private set; }
        private bool _isSidebarCollapsed = false;

        public MainWindow()
        {
            this.InitializeComponent();
            Current = this;

            // Khi vừa mở ứng dụng -> Hiện trang Đăng nhập
            ContentFrame.Navigate(typeof(Views.LoginPage));
        }

        // Mở khóa Sidebar & Header sau khi đăng nhập thành công
        public void UnlockAdminSession(string username)
        {
            TxtAdminName.Text = username;
            SidebarPanel.Visibility = Visibility.Visible;
            TopHeaderBar.Visibility = Visibility.Visible;

            // Mặc định chuyển vào trang Dashboard
            HighlightMenuButton(BtnNavDashboard, "Trang chủ & Thống kê");
            ContentFrame.Navigate(typeof(Views.DashboardPage));
        }

        // XỬ LÝ THU GỌN / MỞ RỘNG SIDEBAR MƯỢT MÀ
        private void BtnToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            _isSidebarCollapsed = !_isSidebarCollapsed;

            if (_isSidebarCollapsed)
            {
                SidebarPanel.Width = 65;
                TxtLogoMain.Visibility = Visibility.Collapsed;
                TxtLogoSub.Visibility = Visibility.Collapsed;
                ToggleMenuAppearance(Visibility.Collapsed, HorizontalAlignment.Center);
            }
            else
            {
                SidebarPanel.Width = 250;
                TxtLogoMain.Visibility = Visibility.Visible;
                TxtLogoSub.Visibility = Visibility.Visible;
                ToggleMenuAppearance(Visibility.Visible, HorizontalAlignment.Left);
            }
        }

        private void ToggleMenuAppearance(Visibility textVisibility, HorizontalAlignment alignment)
        {
            SetButtonAppearance(BtnNavDashboard, textVisibility, alignment);
            SetButtonAppearance(BtnNavProductTypes, textVisibility, alignment);
            SetButtonAppearance(BtnNavProducts, textVisibility, alignment);
            SetButtonAppearance(BtnNavPrices, textVisibility, alignment);
            SetButtonAppearance(BtnNavReceipts, textVisibility, alignment);
            SetButtonAppearance(BtnNavStockReport, textVisibility, alignment);
            SetButtonAppearance(BtnNavStockWarning, textVisibility, alignment);
            SetButtonAppearance(BtnNavOrders, textVisibility, alignment);
            SetButtonAppearance(BtnNavCustomers, textVisibility, alignment);
            SetButtonAppearance(BtnLogout, textVisibility, alignment);
        }

        private void SetButtonAppearance(Button btn, Visibility textVisibility, HorizontalAlignment alignment)
        {
            btn.HorizontalContentAlignment = alignment;
            if (btn.Content is StackPanel sp && sp.Children.Count > 1 && sp.Children[1] is TextBlock tb)
            {
                tb.Visibility = textVisibility;
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedBtn && clickedBtn.Tag is string tag)
            {
                switch (tag)
                {
                    case "Dashboard":
                        HighlightMenuButton(clickedBtn, "Trang chủ & Thống kê");
                        ContentFrame.Navigate(typeof(Views.DashboardPage));
                        break;
                    case "ProductTypes":
                        HighlightMenuButton(clickedBtn, "Quản lý Loại sản phẩm");
                        ContentFrame.Navigate(typeof(Views.ProductTypePage));
                        break;
                    case "Products":
                        HighlightMenuButton(clickedBtn, "Quản lý Sản phẩm");
                        ContentFrame.Navigate(typeof(Views.ProductPage));
                        break;
                    case "Prices":
                        HighlightMenuButton(clickedBtn, "Quản lý Giá bán & Lợi nhuận");
                        ContentFrame.Navigate(typeof(Views.PriceManagementPage));
                        break;
                    case "Receipts":
                        HighlightMenuButton(clickedBtn, "Nhập hàng & Tồn kho");
                        ContentFrame.Navigate(typeof(Views.ReceiptPage));
                        break;
                    case "StockReport":
                        HighlightMenuButton(clickedBtn, "Báo cáo Nhập - Xuất - Tồn");
                        ContentFrame.Navigate(typeof(Views.StockReportPage));
                        break;
                    case "StockWarning":
                        HighlightMenuButton(clickedBtn, "Cảnh báo Sắp hết hàng");
                        ContentFrame.Navigate(typeof(Views.StockWarningPage));
                        break;
                    case "Orders":
                        HighlightMenuButton(clickedBtn, "Quản lý Đơn hàng");
                        ContentFrame.Navigate(typeof(Views.OrderPage));
                        break;
                    case "Customers":
                        HighlightMenuButton(clickedBtn, "Khách hàng & Tài khoản");
                        ContentFrame.Navigate(typeof(Views.CustomerPage));
                        break;
                }
            }
        }

        private void HighlightMenuButton(Button activeButton, string pageTitle)
        {
            TxtPageTitle.Text = pageTitle;

            Button[] allButtons = {
                BtnNavDashboard,
                BtnNavProductTypes,
                BtnNavProducts,
                BtnNavPrices,
                BtnNavReceipts,
                BtnNavStockReport,
                BtnNavStockWarning,
                BtnNavOrders,
                BtnNavCustomers
            };

            var activeBg = new SolidColorBrush(Color.FromArgb(255, 45, 58, 79)); // #2D3A4F
            var transparentBg = new SolidColorBrush(Colors.Transparent);
            var activeFg = new SolidColorBrush(Colors.White);
            var inactiveFg = new SolidColorBrush(Color.FromArgb(255, 203, 213, 225)); // #CBD5E1

            foreach (var btn in allButtons)
            {
                btn.Background = (btn == activeButton) ? activeBg : transparentBg;
                btn.Foreground = (btn == activeButton) ? activeFg : inactiveFg;
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            SidebarPanel.Visibility = Visibility.Collapsed;
            TopHeaderBar.Visibility = Visibility.Collapsed;
            ContentFrame.Navigate(typeof(Views.LoginPage));
        }
    }
}