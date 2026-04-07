using System.Text;
using System.Collections.Generic;

namespace Majal.Templates;

public abstract class BaseTemplate
{
    private readonly StringBuilder _builder = new();
    private string _currentIndent = string.Empty;
    private readonly List<int> _indents = [];

    public abstract string TransformText();

    protected void Write(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (_builder.Length == 0 || _builder[_builder.Length - 1] == '\n')
            _builder.Append(_currentIndent);
        _builder.Append(text);
    }

    protected void WriteLine(string text)
    {
        Write(text);
        _builder.AppendLine();
    }

    protected void PushIndent(string indent)
    {
        _indents.Add(indent.Length);
        _currentIndent += indent;
    }

    protected void PopIndent()
    {
        if (_indents.Count <= 0) return;
        var lastIndentLength = _indents[_indents.Count - 1];
        _indents.RemoveAt(_indents.Count - 1);
        _currentIndent = _currentIndent.Substring(0, _currentIndent.Length - lastIndentLength);
    }

    protected void Clear()
    {
        _builder.Clear();
        _currentIndent = string.Empty;
        _indents.Clear();
    }

    public override string ToString() => _builder.ToString();
}