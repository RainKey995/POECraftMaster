using System.Globalization;

namespace POECraftMaster.Tools;

internal static class CalculatorEngine
{
    public static bool TryEvaluate(string expression, out decimal result, out string error)
    {
        result = 0;
        try
        {
            if (string.IsNullOrWhiteSpace(expression)) throw new FormatException("请输入算式。");
            if (expression.Length > 256) throw new FormatException("算式最多支持 256 个字符。");
            result = new Parser(expression.Replace('×', '*').Replace('÷', '/').Replace('−', '-')).Evaluate();
            error = "";
            return true;
        }
        catch (FormatException e) { error = e.Message; }
        catch (DivideByZeroException) { error = "不能除以零。"; }
        catch (OverflowException) { error = "计算结果超出范围。"; }
        return false;
    }

    public static string Format(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);

    private sealed class Parser(string expression)
    {
        private int _position;

        public decimal Evaluate()
        {
            decimal value = Sum(0);
            SkipSpaces();
            if (_position != expression.Length) throw new FormatException("算式中存在无效字符或缺少运算符。");
            return value;
        }

        private decimal Sum(int depth)
        {
            decimal value = Product(depth);
            while (true)
            {
                if (Take('+')) value += Product(depth);
                else if (Take('-')) value -= Product(depth);
                else return value;
            }
        }

        private decimal Product(int depth)
        {
            decimal value = Operand(depth);
            while (true)
            {
                if (Take('*')) value *= Operand(depth);
                else if (Take('/')) value /= Operand(depth);
                else return value;
            }
        }

        private decimal Operand(int depth)
        {
            if (depth > 32) throw new FormatException("括号或正负号嵌套过多。");
            if (Take('+')) return Operand(depth + 1);
            if (Take('-')) return -Operand(depth + 1);
            if (Take('('))
            {
                decimal value = Sum(depth + 1);
                if (!Take(')')) throw new FormatException("缺少右括号。");
                return value;
            }
            SkipSpaces();
            int start = _position;
            while (_position < expression.Length && (expression[_position] is >= '0' and <= '9' or '.'))
                _position++;
            if (start == _position) throw new FormatException("请输入有效数字，检查算式是否完整。");
            if (!decimal.TryParse(expression.AsSpan(start, _position - start), NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out decimal number))
                throw new FormatException("数字格式无效或数值超出范围。");
            return number;
        }

        private bool Take(char expected)
        {
            SkipSpaces();
            if (_position >= expression.Length || expression[_position] != expected) return false;
            _position++;
            return true;
        }

        private void SkipSpaces()
        {
            while (_position < expression.Length && char.IsWhiteSpace(expression[_position])) _position++;
        }
    }
}
