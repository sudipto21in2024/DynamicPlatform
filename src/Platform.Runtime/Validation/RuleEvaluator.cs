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
        }

        return result;
    }
}
