namespace Canon.Eos.Framework.Interfaces;

public interface IEosLog
{
	bool IsDebugEnabled { get; set; }

	bool IsErrorEnabled { get; set; }

	bool IsWarningEnabled { get; set; }

	string LogProviderName { get; }

	void Debug(string format, params object[] parameters);

	void Error(string format, params object[] parameters);

	void Warn(string format, params object[] parameters);
}
