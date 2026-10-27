using System.Globalization;
using System.Text;

namespace Roselle.Web.Helpers
{
    public static class StringHelper
    {
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text ?? "";

            // Chuyển đổi sang Unicode dựng sẵn (NFC) để xử lý đồng nhất
            text = text.Normalize(NormalizationForm.FormC);

            var sb = new StringBuilder();
            foreach (var c in text)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            // Xử lý các ký tự đặc biệt tiếng Việt (đ, Đ) và chuyển về chữ thường
            return sb.ToString()
                .Replace('đ', 'd')
                .Replace('Đ', 'D')
                .ToLower();
        }
    }
}