using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using QuanLyBanHoa.Desktop.Data;
using QuanLyBanHoa.Desktop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.UI;

namespace QuanLyBanHoa.Desktop.Views
{
    public sealed partial class CustomerEditDialog : ContentDialog
    {
        private readonly int? _accountId;

        // DANH SÁCH ĐẦY ĐỦ 34 TỈNH / THÀNH PHỐ SAU SÁP NHẬP TRÊN TOÀN VIỆT NAM VÀ CÁC PHƯỜNG SAU SÁP NHẬP
        public static readonly Dictionary<string, List<string>> VietnamPostMergerWards = new()
        {
            // --- 6 THÀNH PHỐ TRỰC THUỘC TRUNG ƯƠNG ---
            ["TP.HCM"] = new List<string>
            {
                "Phường Sài Gòn", "Phường Bến Thành", "Phường Tân Định", "Phường Cầu Ông Lãnh",
                "Phường Xuân Hòa", "Phường Bàn Cờ", "Phường Nhiêu Lộc", "Phường Xóm Chiếu",
                "Phường Khánh Hội", "Phường Vĩnh Hội", "Phường Chợ Quán", "Phường An Đông",
                "Phường Chợ Lớn", "Phường Diên Hồng", "Phường Vườn Lài", "Phường Hòa Hưng",
                "Phường Gia Định", "Phường Bình Thạnh", "Phường Thạnh Mỹ Tây", "Phường Phú Nhuận",
                "Phường Đức Nhuận", "Phường Tân Sơn Nhất", "Phường Tân Bình", "Phường Gò Vấp",
                "Phường Hạnh Thông", "Phường Thủ Đức", "Phường Thảo Điền", "Phường An Khánh",
                "Phường Thủ Dầu Một", "Phường Bình Dương", "Phường Thuận An", "Phường Dĩ An",
                "Phường Vũng Tàu", "Phường Tam Thắng", "Phường Bà Rịa", "Phường Phú Mỹ"
            },
            ["Hà Nội"] = new List<string>
            {
                "Phường Hoàn Kiếm", "Phường Cửa Nam", "Phường Ba Đình", "Phường Ngọc Hà",
                "Phường Giảng Võ", "Phường Đống Đa", "Phường Kim Liên", "Phường Văn Miếu - Quốc Tử Giám",
                "Phường Hai Bà Trưng", "Phường Bạch Mai", "Phường Cầu Giấy", "Phường Nghĩa Đô",
                "Phường Tây Hồ", "Phường Phú Thượng", "Phường Thanh Xuân", "Phường Khương Đình",
                "Phường Hoàng Mai", "Phường Định Công", "Phường Long Biên", "Phường Việt Hưng",
                "Phường Hà Đông", "Phường Dương Nội", "Phường Từ Liêm", "Phường Sơn Tây"
            },
            ["Hải Phòng"] = new List<string>
            {
                "Phường Hồng Bàng", "Phường Ngô Quyền", "Phường Lê Chân", "Phường Hải An",
                "Phường Kiến An", "Phường Đồ Sơn", "Phường Dương Kinh", "Phường Thủy Nguyên",
                "Phường An Dương", "Phường Hải Dương", "Phường Thành Đông", "Phường Chí Linh", "Phường Kinh Môn"
            },
            ["Đà Nẵng"] = new List<string>
            {
                "Phường Hải Châu", "Phường Thạch Thang", "Phường Thanh Khê", "Phường An Hải",
                "Phường Sơn Trà", "Phường Ngũ Hành Sơn", "Phường Liên Chiểu", "Phường Cẩm Lệ",
                "Phường Hòa Cường", "Phường Tam Kỳ", "Phường Quảng Phú", "Phường Hội An", "Phường Điện Bàn"
            },
            ["Huế"] = new List<string>
            {
                "Phường Thuận Hóa", "Phường Phú Xuân", "Phường Vỹ Dạ", "Phường An Cựu",
                "Phường Kim Long", "Phường Thủy Xuân", "Phường Hương Thủy", "Phường Hương Trà", "Phường Phong Điền"
            },
            ["Cần Thơ"] = new List<string>
            {
                "Phường Ninh Kiều", "Phường Tân An", "Phường Cái Khế", "Phường An Hòa",
                "Phường Bình Thủy", "Phường Cái Răng", "Phường Ô Môn", "Phường Thốt Nốt",
                "Phường Vị Thanh", "Phường Ngã Bảy", "Phường Sóc Trăng", "Phường Phú Lợi", "Phường Vĩnh Châu"
            },

            // --- 28 TỈNH SAU SÁP NHẬP TRÊN TOÀN QUỐC ---
            ["An Giang"] = new List<string>
            {
                "Phường Long Xuyên", "Phường Mỹ Thới", "Phường Châu Đốc", "Phường Tân Châu",
                "Phường Rạch Giá", "Phường Vĩnh Thanh", "Phường Hà Tiên", "Phường Phú Quốc"
            },
            ["Bắc Ninh"] = new List<string>
            {
                "Phường Kinh Bắc", "Phường Vũ Ninh", "Phường Suối Hoa", "Phường Võ Cường",
                "Phường Từ Sơn", "Phường Quế Võ", "Phường Thuận Thành", "Phường Bắc Giang", "Phường Việt Yên"
            },
            ["Cà Mau"] = new List<string>
            {
                "Phường Tân Thành", "Phường An Xuyên", "Phường Lý Văn Lâm", "Phường Hòa Thành",
                "Phường Bạc Liêu", "Phường Vĩnh Trạch", "Phường Giá Rai"
            },
            ["Cao Bằng"] = new List<string>
            {
                "Phường Thục Phán", "Phường Sông Bằng", "Phường Tân Giang", "Phường Ngọc Xuân"
            },
            ["Đắk Lắk"] = new List<string>
            {
                "Phường Buôn Ma Thuột", "Phường Tân Lập", "Phường Tân An", "Phường Thành Nhất",
                "Phường Buôn Hồ", "Phường Tuy Hòa", "Phường Phú Lâm", "Phường Sông Cầu", "Phường Đông Hòa"
            },
            ["Điện Biên"] = new List<string>
            {
                "Phường Điện Biên Phủ", "Phường Mường Thanh", "Phường Him Lam", "Phường Mường Lay"
            },
            ["Đồng Nai"] = new List<string>
            {
                "Phường Biên Hòa", "Phường Trấn Biên", "Phường Long Bình", "Phường Tam Hiệp",
                "Phường Long Khánh", "Phường Đồng Xoài", "Phường Bình Phước", "Phường Phước Long", "Phường Bình Long"
            },
            ["Đồng Tháp"] = new List<string>
            {
                "Phường Cao Lãnh", "Phường Mỹ Phú", "Phường Sa Đéc", "Phường Hồng Ngự",
                "Phường Mỹ Tho", "Phường Đạo Thạnh", "Phường Gò Công", "Phường Cai Lậy"
            },
            ["Gia Lai"] = new List<string>
            {
                "Phường Pleiku", "Phường Diên Hồng", "Phường Hội Phú", "Phường An Khê", "Phường Ayun Pa",
                "Phường Quy Nhơn", "Phường Quy Nhơn Nam", "Phường An Nhơn", "Phường Hoài Nhơn"
            },
            ["Hà Tĩnh"] = new List<string>
            {
                "Phường Thành Sen", "Phường Trần Phú", "Phường Hà Huy Tập", "Phường Bắc Hồng Lĩnh",
                "Phường Nam Hồng Lĩnh", "Phường Sông Trí", "Phường Kỳ Anh"
            },
            ["Hưng Yên"] = new List<string>
            {
                "Phường Phố Hiến", "Phường Sơn Nam", "Phường Hồng Châu", "Phường Mỹ Hào",
                "Phường Thái Bình", "Phường Trần Lãm", "Phường Trần Hưng Đạo", "Phường Trà Lý"
            },
            ["Khánh Hòa"] = new List<string>
            {
                "Phường Nha Trang", "Phường Bắc Nha Trang", "Phường Nam Nha Trang", "Phường Tây Nha Trang",
                "Phường Cam Ranh", "Phường Ninh Hòa", "Phường Phan Rang", "Phường Đông Hải", "Phường Bảo An"
            },
            ["Lai Châu"] = new List<string>
            {
                "Phường Đoàn Kết", "Phường Tân Phong", "Phường Quyết Thắng"
            },
            ["Lâm Đồng"] = new List<string>
            {
                "Phường Xuân Hương - Đà Lạt", "Phường Cam Ly - Đà Lạt", "Phường Lâm Viên - Đà Lạt",
                "Phường Xuân Trường - Đà Lạt", "Phường Bảo Lộc", "Phường B'Lao",
                "Phường Gia Nghĩa", "Phường Phan Thiết", "Phường Mũi Né", "Phường Phú Thủy", "Phường La Gi"
            },
            ["Lạng Sơn"] = new List<string>
            {
                "Phường Lương Văn Tri", "Phường Hoàng Văn Thụ", "Phường Đông Kinh", "Phường Kỳ Lừa", "Phường Tam Thanh"
            },
            ["Lào Cai"] = new List<string>
            {
                "Phường Lào Cai", "Phường Cam Đường", "Phường Sa Pa", "Phường Yên Bái",
                "Phường Nam Cường", "Phường Nghĩa Lộ"
            },
            ["Nghệ An"] = new List<string>
            {
                "Phường Trường Vinh", "Phường Thành Vinh", "Phường Vinh Hưng", "Phường Vinh Phú",
                "Phường Cửa Lò", "Phường Hoàng Mai", "Phường Thái Hòa"
            },
            ["Ninh Bình"] = new List<string>
            {
                "Phường Hoa Lư", "Phường Nam Hoa Lư", "Phường Bắc Hoa Lư", "Phường Tam Điệp",
                "Phường Nam Định", "Phường Thiên Trường", "Phường Phủ Lý", "Phường Hà Nam", "Phường Duy Tiên"
            },
            ["Phú Thọ"] = new List<string>
            {
                "Phường Việt Trì", "Phường Nông Trang", "Phường Thanh Miếu", "Phường Phú Thọ",
                "Phường Vĩnh Yên", "Phường Phúc Yên", "Phường Hòa Bình", "Phường Kỳ Sơn"
            },
            ["Quảng Ngãi"] = new List<string>
            {
                "Phường Cẩm Thành", "Phường Nghĩa Lộ", "Phường Trương Quang Trọng", "Phường Đức Phổ",
                "Phường Kon Tum", "Phường Đăk Bla", "Phường Đăk Cấm"
            },
            ["Quảng Ninh"] = new List<string>
            {
                "Phường Hạ Long", "Phường Hồng Gai", "Phường Bãi Cháy", "Phường Tuần Châu",
                "Phường Cẩm Phả", "Phường Uông Bí", "Phường Móng Cái", "Phường Đông Triều", "Phường Quảng Yên"
            },
            ["Quảng Trị"] = new List<string>
            {
                "Phường Đông Hà", "Phường Nam Đông Hà", "Phường Quảng Trị",
                "Phường Đồng Hới", "Phường Đồng Thuận", "Phường Ba Đồn"
            },
            ["Sơn La"] = new List<string>
            {
                "Phường Tô Hiệu", "Phường Chiềng Lề", "Phường Chiềng Cơi", "Phường Mộc Châu", "Phường Thảo Nguyên"
            },
            ["Tây Ninh"] = new List<string>
            {
                "Phường Tân Ninh", "Phường Hòa Thành", "Phường Trảng Bàng", "Phường Gò Dầu",
                "Phường Long An", "Phường Tân An", "Phường Kiến Tường"
            },
            ["Thái Nguyên"] = new List<string>
            {
                "Phường Phan Đình Phùng", "Phường Gia Sàng", "Phường Tích Lương", "Phường Sông Công",
                "Phường Phổ Yên", "Phường Bắc Kạn", "Phường Đức Xuân"
            },
            ["Thanh Hóa"] = new List<string>
            {
                "Phường Hạc Thành", "Phường Quảng Phú", "Phường Đông Quang", "Phường Đông Sơn",
                "Phường Hàm Rồng", "Phường Sầm Sơn", "Phường Bỉm Sơn", "Phường Nghi Sơn"
            },
            ["Tuyên Quang"] = new List<string>
            {
                "Phường Minh Xuân", "Phường Mỹ Lâm", "Phường Nông Tiến", "Phường Hà Giang", "Phường Quang Trung"
            },
            ["Vĩnh Long"] = new List<string>
            {
                "Phường Long Châu", "Phường Phước Hậu", "Phường Tân Hạnh", "Phường Bình Minh",
                "Phường Bến Tre", "Phường Phú Khương", "Phường Trà Vinh", "Phường Nguyệt Hóa"
            }
        };

        // Bảng ánh xạ các tên Tỉnh/TP cũ trong DB về Tỉnh/TP và Phường mới sau sáp nhập
        private static readonly Dictionary<string, (string NewCity, string DefaultWard)> _legacyLocationMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Bình Dương"] = ("TP.HCM", "Phường Bình Dương"),
            ["Bà Rịa - Vũng Tàu"] = ("TP.HCM", "Phường Vũng Tàu"),
            ["Vũng Tàu"] = ("TP.HCM", "Phường Vũng Tàu"),
            ["Hải Dương"] = ("Hải Phòng", "Phường Hải Dương"),
            ["Quảng Nam"] = ("Đà Nẵng", "Phường Tam Kỳ"),
            ["Hậu Giang"] = ("Cần Thơ", "Phường Vị Thanh"),
            ["Sóc Trăng"] = ("Cần Thơ", "Phường Sóc Trăng"),
            ["Kiên Giang"] = ("An Giang", "Phường Rạch Giá"),
            ["Bắc Giang"] = ("Bắc Ninh", "Phường Bắc Giang"),
            ["Bạc Liêu"] = ("Cà Mau", "Phường Bạc Liêu"),
            ["Phú Yên"] = ("Đắk Lắk", "Phường Tuy Hòa"),
            ["Bình Phước"] = ("Đồng Nai", "Phường Bình Phước"),
            ["Tiền Giang"] = ("Đồng Tháp", "Phường Mỹ Tho"),
            ["Bình Định"] = ("Gia Lai", "Phường Quy Nhơn"),
            ["Thái Bình"] = ("Hưng Yên", "Phường Thái Bình"),
            ["Ninh Thuận"] = ("Khánh Hòa", "Phường Phan Rang"),
            ["Đắk Nông"] = ("Lâm Đồng", "Phường Gia Nghĩa"),
            ["Bình Thuận"] = ("Lâm Đồng", "Phường Phan Thiết"),
            ["Yên Bái"] = ("Lào Cai", "Phường Yên Bái"),
            ["Vinh"] = ("Nghệ An", "Phường Trường Vinh"),
            ["Nam Định"] = ("Ninh Bình", "Phường Nam Định"),
            ["Hà Nam"] = ("Ninh Bình", "Phường Hà Nam"),
            ["Vĩnh Phúc"] = ("Phú Thọ", "Phường Vĩnh Yên"),
            ["Hòa Bình"] = ("Phú Thọ", "Phường Hòa Bình"),
            ["Kon Tum"] = ("Quảng Ngãi", "Phường Kon Tum"),
            ["Quảng Bình"] = ("Quảng Trị", "Phường Đồng Hới"),
            ["Long An"] = ("Tây Ninh", "Phường Long An"),
            ["Bắc Kạn"] = ("Thái Nguyên", "Phường Bắc Kạn"),
            ["Hà Giang"] = ("Tuyên Quang", "Phường Hà Giang"),
            ["Bến Tre"] = ("Vĩnh Long", "Phường Bến Tre"),
            ["Trà Vinh"] = ("Vĩnh Long", "Phường Trà Vinh")
        };

        public CustomerEditDialog(int? accountId)
        {
            this.InitializeComponent();
            _accountId = accountId;

            // Nạp đầy đủ 34 Tỉnh/Thành phố sau sáp nhập
            CbCity.ItemsSource = VietnamPostMergerWards.Keys.ToList();
            CbCity.SelectedIndex = 0;

            if (_accountId.HasValue)
            {
                this.Title = "Chỉnh Sửa Người Dùng";

                TxtUsername.IsReadOnly = true;
                TxtUsername.Background = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240));
                TxtUsername.Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139));

                LblPassword.Text = "Mật khẩu (Đã khóa bảo mật):";
                TxtPassword.Password = string.Empty;
                TxtPassword.PlaceholderText = "Đã khóa - Không được phép đổi mật khẩu";
                TxtPassword.IsEnabled = false;

                LoadExistingUser(_accountId.Value);
            }
            else
            {
                this.Title = "Thêm Người Dùng Mới";

                TxtUsername.IsReadOnly = false;
                TxtUsername.Background = new SolidColorBrush(Colors.White);

                LblPassword.Text = "Mật khẩu (*):";
                TxtPassword.Password = string.Empty;
                TxtPassword.PlaceholderText = "Nhập mật khẩu...";
                TxtPassword.IsEnabled = true;

                CbRole.SelectedIndex = 1; // Mặc định KhachHang
            }
        }

        private void TxtPhone_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = args.NewText.Any(c => !char.IsDigit(c));
        }

        private void CbCity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbCity.SelectedItem is string selectedCity && VietnamPostMergerWards.TryGetValue(selectedCity, out var wards))
            {
                CbWard.ItemsSource = wards;
                if (wards.Count > 0) CbWard.SelectedIndex = 0;
            }
        }

        private void LoadExistingUser(int accountId)
        {
            try
            {
                using (var db = new ShopDbContext())
                {
                    var acc = db.Accounts.FirstOrDefault(a => a.AccountID == accountId);
                    if (acc == null) return;

                    var cus = db.Customers.FirstOrDefault(c => c.AccountID == accountId);

                    TxtUsername.Text = acc.Username;
                    TxtFullName.Text = cus?.FullName ?? acc.Username;
                    TxtEmail.Text = cus?.Email ?? string.Empty;
                    TxtPhone.Text = cus?.PhoneNumber ?? string.Empty;

                    ParseAndBindAddress(cus?.Address);

                    foreach (ComboBoxItem item in CbRole.Items)
                    {
                        if (item.Content?.ToString() == acc.Role)
                        {
                            CbRole.SelectedItem = item;
                            break;
                        }
                    }
                    if (CbRole.SelectedItem == null && CbRole.Items.Count > 0)
                        CbRole.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ShowError("Lỗi tải thông tin: " + ex.Message);
            }
        }

        private void ParseAndBindAddress(string? rawAddress)
        {
            if (string.IsNullOrWhiteSpace(rawAddress)) return;

            var parts = rawAddress.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(p => p.Trim())
                                  .ToList();

            if (parts.Count >= 3)
            {
                string city = parts[^1];
                string ward = parts[^2];
                string street = string.Join(", ", parts.Take(parts.Count - 2));

                if (VietnamPostMergerWards.ContainsKey(city))
                {
                    CbCity.SelectedItem = city;
                    CbWard.ItemsSource = VietnamPostMergerWards[city];
                    if (VietnamPostMergerWards[city].Contains(ward))
                        CbWard.SelectedItem = ward;
                }
                else if (_legacyLocationMap.TryGetValue(city, out var mapped))
                {
                    CbCity.SelectedItem = mapped.NewCity;
                    CbWard.ItemsSource = VietnamPostMergerWards[mapped.NewCity];
                    CbWard.SelectedItem = VietnamPostMergerWards[mapped.NewCity].Contains(ward) ? ward : mapped.DefaultWard;
                }
                TxtStreet.Text = street;
            }
            else if (parts.Count == 2)
            {
                string ward = parts[0];
                string city = parts[1];
                if (VietnamPostMergerWards.ContainsKey(city))
                {
                    CbCity.SelectedItem = city;
                    CbWard.ItemsSource = VietnamPostMergerWards[city];
                    if (VietnamPostMergerWards[city].Contains(ward))
                        CbWard.SelectedItem = ward;
                }
                TxtStreet.Text = string.Empty;
            }
            else if (parts.Count == 1)
            {
                string singleLoc = parts[0];
                if (VietnamPostMergerWards.ContainsKey(singleLoc))
                {
                    CbCity.SelectedItem = singleLoc;
                    TxtStreet.Text = string.Empty;
                }
                else if (_legacyLocationMap.TryGetValue(singleLoc, out var mapped))
                {
                    CbCity.SelectedItem = mapped.NewCity;
                    CbWard.ItemsSource = VietnamPostMergerWards[mapped.NewCity];
                    CbWard.SelectedItem = mapped.DefaultWard;
                    TxtStreet.Text = string.Empty;
                }
                else
                {
                    TxtStreet.Text = rawAddress;
                }
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            string username = TxtUsername.Text.Trim();
            string fullName = TxtFullName.Text.Trim();
            string email = TxtEmail.Text.Trim();
            string phone = TxtPhone.Text.Trim();
            string street = TxtStreet.Text.Trim();
            string? ward = CbWard.SelectedItem as string;
            string? city = CbCity.SelectedItem as string;
            string selectedRole = (CbRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "KhachHang";

            // 1. Kiểm tra trống Tên đăng nhập & Họ tên
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(fullName))
            {
                ShowError("Vui lòng nhập đầy đủ Tên đăng nhập và Họ và tên!");
                return;
            }

            // 2. Kiểm tra chuẩn định dạng Email
            if (string.IsNullOrEmpty(email) || !Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                ShowError("Email không hợp lệ! Vui lòng nhập đúng định dạng (VD: ten@gmail.com).");
                return;
            }

            // 3. Kiểm tra chuẩn Số điện thoại Việt Nam (10 số, đầu 03, 05, 07, 08, 09)
            if (!Regex.IsMatch(phone, @"^(03|05|07|08|09)\d{8}$"))
            {
                ShowError("Số điện thoại không hợp lệ! Phải gồm đúng 10 chữ số và thuộc đầu số mạng Việt Nam (03, 05, 07, 08, 09).");
                return;
            }

            // 4. Kiểm tra Địa chỉ (Số nhà tên đường + Phường + Tỉnh/Thành phố)
            if (string.IsNullOrEmpty(street) || string.IsNullOrEmpty(ward) || string.IsNullOrEmpty(city))
            {
                ShowError("Vui lòng nhập Số nhà/Tên đường và chọn đầy đủ Phường, Tỉnh/Thành phố!");
                return;
            }

            string fullAddress = $"{street}, {ward}, {city}";

            try
            {
                using (var db = new ShopDbContext())
                {
                    if (_accountId == null)
                    {
                        string rawPass = TxtPassword.Password.Trim();
                        if (string.IsNullOrEmpty(rawPass))
                        {
                            ShowError("Vui lòng nhập mật khẩu khởi tạo cho tài khoản mới!");
                            return;
                        }

                        if (db.Accounts.Any(a => a.Username == username))
                        {
                            ShowError("Tên đăng nhập này đã tồn tại!");
                            return;
                        }

                        var newAcc = new Account
                        {
                            Username = username,
                            PasswordHash = rawPass,
                            Role = selectedRole,
                            Status = true
                        };
                        db.Accounts.Add(newAcc);
                        db.SaveChanges();

                        var newCus = new Customer
                        {
                            AccountID = newAcc.AccountID,
                            FullName = fullName,
                            Email = email,
                            PhoneNumber = phone,
                            Address = fullAddress
                        };
                        db.Customers.Add(newCus);
                        db.SaveChanges();
                    }
                    else
                    {
                        var acc = db.Accounts.FirstOrDefault(a => a.AccountID == _accountId.Value);
                        if (acc != null)
                        {
                            acc.Role = selectedRole;
                        }

                        var cus = db.Customers.FirstOrDefault(c => c.AccountID == _accountId.Value);
                        if (cus != null)
                        {
                            cus.FullName = fullName;
                            cus.Email = email;
                            cus.PhoneNumber = phone;
                            cus.Address = fullAddress;
                        }
                        else if (acc != null)
                        {
                            db.Customers.Add(new Customer
                            {
                                AccountID = acc.AccountID,
                                FullName = fullName,
                                Email = email,
                                PhoneNumber = phone,
                                Address = fullAddress
                            });
                        }

                        db.SaveChanges();
                    }

                    args.Cancel = false;
                }
            }
            catch (Exception ex)
            {
                ShowError("Lỗi lưu CSDL: " + ex.Message);
            }
        }

        private void ShowError(string msg)
        {
            TxtError.Text = msg;
            TxtError.Visibility = Visibility.Visible;
        }
    }
}