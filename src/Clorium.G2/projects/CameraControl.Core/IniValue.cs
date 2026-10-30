using System;
using System.Globalization;

public struct IniValue
{
	public string Value;

	private static readonly IniValue _default = default;

	public static IniValue Default => _default;

	private static bool TryParseInt(string text, out int value)
	{
		if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var res))
		{
			value = res;
			return true;
		}
		value = 0;
		return false;
	}

	private static bool TryParseDouble(string text, out double value)
	{
		if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var res))
		{
			value = res;
			return true;
		}
		value = double.NaN;
		return false;
	}

	public IniValue(object value)
	{
		if (value is IFormattable formattable)
		{
			Value = formattable.ToString(null, CultureInfo.InvariantCulture);
		}
		else
		{
			Value = value?.ToString();
		}
	}

	public IniValue(string value)
	{
		Value = value;
	}

	public bool ToBool(bool valueIfInvalid = false)
	{
		if (TryConvertBool(out var res))
		{
			return res;
		}
		return valueIfInvalid;
	}

	public bool TryConvertBool(out bool result)
	{
		if (Value == null)
		{
			result = false;
			return false;
		}
		string boolStr = Value.Trim().ToLowerInvariant();
		if (boolStr == "true")
		{
			result = true;
			return true;
		}
		if (boolStr == "false")
		{
			result = false;
			return true;
		}
		result = false;
		return false;
	}

	public int ToInt(int valueIfInvalid = 0)
	{
		if (TryConvertInt(out var res))
		{
			return res;
		}
		return valueIfInvalid;
	}

	public bool TryConvertInt(out int result)
	{
		if (Value == null)
		{
			result = 0;
			return false;
		}
		if (TryParseInt(Value.Trim(), out result))
		{
			return true;
		}
		return false;
	}

	public double ToDouble(double valueIfInvalid = 0.0)
	{
		if (TryConvertDouble(out var res))
		{
			return res;
		}
		return valueIfInvalid;
	}

	public bool TryConvertDouble(out double result)
	{
		if (Value == null)
		{
			result = 0.0;
			return false;
		}
		if (TryParseDouble(Value.Trim(), out result))
		{
			return true;
		}
		return false;
	}

	public string GetString()
	{
		return GetString(allowOuterQuotes: true, preserveWhitespace: false);
	}

	public string GetString(bool preserveWhitespace)
	{
		return GetString(allowOuterQuotes: true, preserveWhitespace);
	}

	public string GetString(bool allowOuterQuotes, bool preserveWhitespace)
	{
		if (Value == null)
		{
			return "";
		}
		string trimmed = Value.Trim();
		if (allowOuterQuotes && trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"')
		{
			string inner = trimmed.Substring(1, trimmed.Length - 2);
			return preserveWhitespace ? inner : inner.Trim();
		}
		return preserveWhitespace ? Value : Value.Trim();
	}

	public override string ToString()
	{
		return Value;
	}

	public static implicit operator IniValue(byte o)
	{
		return new IniValue(o);
	}

	public static implicit operator IniValue(short o)
	{
		return new IniValue(o);
	}

	public static implicit operator IniValue(int o)
	{
		return new IniValue(o);
	}

	public static implicit operator IniValue(sbyte o)
	{
		return new IniValue(o);
	}

	public static implicit operator IniValue(ushort o)
	{
		return new IniValue(o);
	}

	public static implicit operator IniValue(uint o)
	{
		return new IniValue(o);
	}

	public static implicit operator IniValue(float o)
	{
		return new IniValue(o);
	}

	public static implicit operator IniValue(double o)
	{
		return new IniValue(o);
	}

	public static implicit operator IniValue(bool o)
	{
		return new IniValue(o);
	}

	public static implicit operator IniValue(string o)
	{
		return new IniValue(o);
	}
}
