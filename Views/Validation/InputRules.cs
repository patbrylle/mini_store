using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace mini_store.Validation
{
    public static class InputRules
    {
        // Loose patterns for the browser only. The methods below are the real check.
        // (escaped for the HTML "v" regex mode used by current browsers)
        public const string PhoneHtmlPattern = @"[0-9+\(\)\s\-.]{10,20}";
        public const string EmailHtmlPattern = @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,24}";

        private static readonly Regex EmailRx = new(
            @"^[A-Za-z0-9._%+\-]+@(?:[A-Za-z0-9](?:[A-Za-z0-9\-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z]{2,24}$",
            RegexOptions.Compiled);
        private static readonly Regex Mobile = new(@"^09\d{9}$", RegexOptions.Compiled);
        private static readonly Regex Landline = new(@"^0[2-8]\d{8}$", RegexOptions.Compiled);
        private static readonly Regex NameChars = new(@"^[\p{L}\p{N}][\p{L}\p{N} .,'’&\-]*$", RegexOptions.Compiled);
        private static readonly Regex AddressChars = new(@"^[\p{L}\p{N} .,#'’&()/\-]+$", RegexOptions.Compiled);
        private static readonly Regex Repeats = new(@"(.)\1{4,}", RegexOptions.Compiled);
        private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex PhoneJunk = new(@"[\s\-().]", RegexOptions.Compiled);

        // ---------- normalizers (call these before saving) ----------

        /// trims, collapses spaces, returns null when blank
        public static string? Clean(string? v)
        {
            var s = Spaces.Replace((v ?? "").Trim(), " ");
            return s.Length == 0 ? null : s;
        }

        public static string NormalizeEmail(string? v) => (v ?? "").Trim().ToLowerInvariant();

        /// "+63 917-123-4567" / "639171234567" -> "09171234567"; blank -> null
        public static string? NormalizePhone(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return null;
            var s = PhoneJunk.Replace(v.Trim(), "");
            if (s.StartsWith("+63")) s = "0" + s[3..];
            else if (s.StartsWith("63") && s.Length >= 11) s = "0" + s[2..];
            return s;
        }

        // ---------- validators ----------

        public static bool IsValidEmail(string? v)
        {
            var s = (v ?? "").Trim();
            if (s.Length == 0 || s.Length > 100 || s.Contains("..") || !EmailRx.IsMatch(s)) return false;
            var local = s[..s.IndexOf('@')];
            return local.Length <= 64 && !local.StartsWith('.') && !local.EndsWith('.');
        }

        // PH mobile (09XXXXXXXXX) or landline with area code (0 + 9 digits)
        public static bool IsValidPhone(string? v)
        {
            var p = NormalizePhone(v);
            if (p == null) return false;
            if (!Mobile.IsMatch(p) && !Landline.IsMatch(p)) return false;
            return p[3..].Distinct().Count() > 1;          // rejects 09111111111 style numbers
        }

        public static bool IsValidName(string? v)
        {
            var s = Clean(v);
            return s != null && s.Length is >= 2 and <= 100 && NameChars.IsMatch(s) && s.Any(char.IsLetter);
        }

        public static bool IsValidAddress(string? v)
        {
            var s = Clean(v);
            return s != null && s.Length is >= 5 and <= 200 && AddressChars.IsMatch(s)
                   && s.Any(char.IsLetter) && !Repeats.IsMatch(s);
        }

        public static bool IsBlank(object? v) => v is null || (v is string s && string.IsNullOrWhiteSpace(s));
    }

    // Blank values pass: pair them with [Required] when the field is mandatory.

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class StrongEmailAttribute : ValidationAttribute
    {
        public StrongEmailAttribute() : base("Enter a valid email address, e.g. name@example.com.") { }
        public override bool IsValid(object? value) => InputRules.IsBlank(value) || InputRules.IsValidEmail(value as string);
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PhPhoneAttribute : ValidationAttribute
    {
        public PhPhoneAttribute() : base("Enter a valid Philippine number, e.g. 0917 123 4567 or +63 917 123 4567.") { }
        public override bool IsValid(object? value) => InputRules.IsBlank(value) || InputRules.IsValidPhone(value as string);
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PersonNameAttribute : ValidationAttribute
    {
        public PersonNameAttribute() : base("Name may only contain letters, numbers, spaces and . , ' & - (2 to 100 characters).") { }
        public override bool IsValid(object? value) => InputRules.IsBlank(value) || InputRules.IsValidName(value as string);
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SafeAddressAttribute : ValidationAttribute
    {
        public SafeAddressAttribute() : base("Enter a real address (5 to 200 characters, letters, numbers and . , # ' & ( ) / - only).") { }
        public override bool IsValid(object? value) => InputRules.IsBlank(value) || InputRules.IsValidAddress(value as string);
    }
}