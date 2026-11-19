using Microsoft.Win32;

namespace Cursoft.Services;

internal static class SignInStartup
{
	private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
	public const string Argument = "--startup";

	public static bool IsEnabled()
	{
		using var key = Registry.CurrentUser.OpenSubKey(RunKey);
		return key?.GetValue(AppInfo.Name) is string command && command.Length > 0;
	}

	public static void SetEnabled(bool enabled)
	{
		using var key = Registry.CurrentUser.CreateSubKey(RunKey);
		if (!enabled)
		{
			key.DeleteValue(AppInfo.Name, throwOnMissingValue: false);
			return;
		}

		var executable = Environment.ProcessPath ?? throw new FileNotFoundException(AppInfo.Name);
		if (string.Equals(Path.GetFileName(executable), "dotnet.exe", StringComparison.OrdinalIgnoreCase))
			executable = Path.Combine(AppContext.BaseDirectory, "Cursoft.exe");
		if (!File.Exists(executable))
			throw new FileNotFoundException(executable);
		key.SetValue(AppInfo.Name, $"\"{executable}\" {Argument}", RegistryValueKind.String);
	}
}
