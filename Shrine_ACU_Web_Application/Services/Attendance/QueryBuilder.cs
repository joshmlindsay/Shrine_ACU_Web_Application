using System.Text;

namespace Shrine_ACU_Web_Application.Services.Attendance;

public sealed class QueryBuilder
{
    private readonly List<KeyValuePair<string, string>> _pairs = [];

    public QueryBuilder Add(string key, object? value)
    {
        if (value is null)
        {
            return this;
        }

        if (value is string text && string.IsNullOrWhiteSpace(text))
        {
            return this;
        }

        _pairs.Add(new KeyValuePair<string, string>(key, value.ToString()!));
        return this;
    }

    public string Build()
    {
        if (_pairs.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder("?");
        for (var i = 0; i < _pairs.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('&');
            }

            builder.Append(Uri.EscapeDataString(_pairs[i].Key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(_pairs[i].Value));
        }

        return builder.ToString();
    }
}
