using System.Text.RegularExpressions;

namespace Data_Extraction;

public static class InputValidationService
{
    public static bool IsValidUsername(string username)
    {
        return !string.IsNullOrWhiteSpace(username)
               && username.Length >= 3
               && username.Length <= 50;
    }

    public static bool IsValidPassword(string password)
    {
        return !string.IsNullOrWhiteSpace(password)
               && password.Length >= 6
               && password.Length <= 100;
    }

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return true;

        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    public static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return true;

        return Regex.IsMatch(phone, @"^[0-9\-+ ]{7,20}$");
    }

    public static bool IsValidTextLength(string? text, int maxLength)
    {
        return string.IsNullOrWhiteSpace(text) || text.Length <= maxLength;
    }
}