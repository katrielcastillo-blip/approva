using System.Globalization;
using Approva.Domain.Entities;
using Approva.Domain.Enums;

namespace Approva.Domain.Services;

/// <summary>Evaluates a single WorkflowCondition against a Request's field values.
/// Numeric comparisons are used when both sides parse as decimal; otherwise falls
/// back to case-insensitive string comparison.</summary>
public static class ConditionEvaluator
{
    public static bool Evaluate(WorkflowCondition condition, Request request)
    {
        var actual = request.GetFieldValue(condition.Field);

        return condition.Operator switch
        {
            ConditionOperator.Equals => Compare(actual, condition.Value) == 0,
            ConditionOperator.NotEquals => Compare(actual, condition.Value) != 0,
            ConditionOperator.GreaterThan => CompareNumeric(actual, condition.Value) > 0,
            ConditionOperator.GreaterThanOrEqual => CompareNumeric(actual, condition.Value) >= 0,
            ConditionOperator.LessThan => CompareNumeric(actual, condition.Value) < 0,
            ConditionOperator.LessThanOrEqual => CompareNumeric(actual, condition.Value) <= 0,
            ConditionOperator.In => SplitList(condition.Value).Any(v => Compare(actual, v) == 0),
            ConditionOperator.NotIn => SplitList(condition.Value).All(v => Compare(actual, v) != 0),
            _ => throw new ArgumentOutOfRangeException(nameof(condition), condition.Operator, "Operador no soportado.")
        };
    }

    private static int Compare(string? actual, string expected)
    {
        if (actual is null)
            return -1;

        if (decimal.TryParse(actual, NumberStyles.Number, CultureInfo.InvariantCulture, out var actualNum) &&
            decimal.TryParse(expected, NumberStyles.Number, CultureInfo.InvariantCulture, out var expectedNum))
        {
            return actualNum.CompareTo(expectedNum);
        }

        return string.Compare(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static decimal CompareNumeric(string? actual, string expected)
    {
        if (actual is null || !decimal.TryParse(actual, NumberStyles.Number, CultureInfo.InvariantCulture, out var actualNum))
            throw new InvalidOperationException($"El campo no tiene un valor numérico comparable: '{actual}'.");
        if (!decimal.TryParse(expected, NumberStyles.Number, CultureInfo.InvariantCulture, out var expectedNum))
            throw new InvalidOperationException($"El valor de la condición no es numérico: '{expected}'.");

        return actualNum - expectedNum;
    }

    private static IEnumerable<string> SplitList(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
