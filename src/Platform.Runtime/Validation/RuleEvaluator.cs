using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Platform.Runtime.Validation;

public class RuleResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
}

public static class RuleEvaluator
{
    public static RuleResult Evaluate(object value, string ruleType, string ruleValue, string errorMessage)
    {
        var result = new RuleResult();
        var stringValue = value?.ToString() ?? string.Empty;

        switch (ruleType.ToLower())
        {
            case "regex":
                if (!Regex.IsMatch(stringValue, ruleValue))
                {
                    result.IsValid = false;
                    result.Errors.Add(errorMessage);
                }
                break;

            case "range":
                var parts = ruleValue.Split('-');
                if (parts.Length == 2 && double.TryParse(parts[0], out var min) && double.TryParse(parts[1], out var max))
                {
                    if (double.TryParse(stringValue, out var num))
                    {
                        if (num < min || num > max)
                        {
                            result.IsValid = false;
                            result.Errors.Add(errorMessage);
                        }
                    }
                }
                break;

            case "email":
                var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!Regex.IsMatch(stringValue, emailRegex))
                {
                    result.IsValid = false;
                    result.Errors.Add(errorMessage);
                }
                break;

            case "phone":
                var phoneRegex = @"^\+?[\d\s\-\(\)]{7,20}$";
                if (!Regex.IsMatch(stringValue, phoneRegex))
                {
                    result.IsValid = false;
                    result.Errors.Add(errorMessage);
                }
                break;

            case "minlength":
                if (int.TryParse(ruleValue, out var minLen) && stringValue.Length < minLen)
                {
                    result.IsValid = false;
                    result.Errors.Add(errorMessage);
                }
                break;

            case "minvalue":
                if (double.TryParse(ruleValue, out var minVal) && double.TryParse(stringValue, out var numMin))
                {
                    if (numMin < minVal)
                    {
                        result.IsValid = false;
                        result.Errors.Add(errorMessage);
                    }
                }
                break;

            case "maxvalue":
                if (double.TryParse(ruleValue, out var maxVal) && double.TryParse(stringValue, out var numMax))
                {
                    if (numMax > maxVal)
                    {
                        result.IsValid = false;
                        result.Errors.Add(errorMessage);
                    }
                }
                break;

            case "url":
                var urlRegex = @"^https?://[^\s/$.?#].[^\s]*$";
                if (!Regex.IsMatch(stringValue, urlRegex, RegexOptions.IgnoreCase))
                {
                    result.IsValid = false;
                    result.Errors.Add(errorMessage);
                }
                break;

            case "creditcard":
                // Luhn algorithm validation
                var digits = stringValue.Replace("-", "").Replace(" ", "");
                if (!IsValidLuhn(digits))
                {
                    result.IsValid = false;
                    result.Errors.Add(errorMessage);
                }
                break;
        }

        return result;
    }

    /// <summary>
    /// Validates a credit card number using the Luhn algorithm.
    /// </summary>
    private static bool IsValidLuhn(string digits)
    {
        if (string.IsNullOrEmpty(digits) || digits.Length < 13 || digits.Length > 19)
            return false;

        int sum = 0;
        bool alternate = false;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(digits[i])) return false;
            int n = digits[i] - '0';
            if (alternate)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alternate = !alternate;
        }
        return sum % 10 == 0;
    }
}
