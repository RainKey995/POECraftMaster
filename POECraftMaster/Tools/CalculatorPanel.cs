using System.Numerics;
using ImGuiNET;

namespace POECraftMaster.Tools;

internal sealed class CalculatorPanel
{
    private string _expression = "";
    private string _result = "";
    private string _error = "";
    private bool _hasResult;
    private static readonly string[] Buttons =
        ["清空", "(", ")", "退格", "7", "8", "9", "÷", "4", "5", "6", "×",
            "1", "2", "3", "-", "0", ".", "=", "+"];

    public void Render()
    {
        ImGui.BeginChild("calculator", Vector2.Zero);
        ImGui.Text("计算器");
        ImGui.TextWrapped("支持加减乘除、小数、负数和括号。输入算式后按 Enter 或点击 = 计算。");
        ImGui.SetNextItemWidth(Math.Min(440, ImGui.GetContentRegionAvail().X));
        string previous = _expression;
        bool submitted = ImGui.InputText("##calculation", ref _expression, 257, ImGuiInputTextFlags.EnterReturnsTrue);
        if (_expression != previous) { _hasResult = false; _error = ""; _result = ""; }
        if (submitted) Calculate();
        if (_hasResult)
        {
            ImGui.TextWrapped($"= {_result}");
            if (ImGui.Button("复制结果")) ImGui.SetClipboardText(_result);
        }
        if (_error.Length > 0) ImGui.TextColored(new Vector4(1, .4f, .3f, 1), _error);
        ImGui.Separator();
        float width = Math.Min(440, ImGui.GetContentRegionAvail().X);
        float buttonWidth = Math.Max(1, (width - ImGui.GetStyle().ItemSpacing.X * 3) / 4);
        for (int i = 0; i < Buttons.Length; i++)
        {
            if (i % 4 != 0) ImGui.SameLine();
            if (!ImGui.Button(Buttons[i], new Vector2(buttonWidth, 42))) continue;
            if (Buttons[i] == "清空")
            {
                _expression = _result = _error = "";
                _hasResult = false;
            }
            else if (Buttons[i] == "退格")
            {
                if (_expression.Length > 0) _expression = _expression[..^1];
                _result = _error = "";
                _hasResult = false;
            }
            else if (Buttons[i] == "=") Calculate();
            else
            {
                string token = Buttons[i];
                string prefix = _hasResult ? (token is "+" or "-" or "×" or "÷" ? _result : "") : _expression;
                if (prefix.Length + token.Length <= 256) _expression = prefix + token;
                _result = _error = "";
                _hasResult = false;
            }
        }
        ImGui.EndChild();
    }

    private void Calculate()
    {
        _hasResult = CalculatorEngine.TryEvaluate(_expression, out decimal result, out _error);
        _result = _hasResult ? CalculatorEngine.Format(result) : "";
    }
}
