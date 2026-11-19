using System.Text.Json;

namespace Cursoft.Services;

internal static class SystemCursorSources
{
	private static string SourcesFile => Path.Combine(AppPaths.StateDir, "system-cursor-sources.json");
	private sealed record Scheme(Dictionary<string, string> Sources, Dictionary<string, string> Applied);

	// Keep the original scheme available when a later session opens with resized copies installed.
	public static Dictionary<string, string> Read(Dictionary<string, string>? values = null)
	{
		var current = values == null ? RegistryCursorService.ReadCurrentValues() : new Dictionary<string, string>(values);
		Dictionary<string, string>? defaults = null;
		Scheme? saved = null;
		if (File.Exists(SourcesFile))
		{
			try { saved = JsonSerializer.Deserialize<Scheme>(File.ReadAllText(SourcesFile)); }
			catch (Exception e) when (e is JsonException or IOException) { }
		}
		foreach (var (role, path) in current.ToArray())
		{
			if (saved?.Applied != null && saved.Sources != null && saved.Applied.TryGetValue(role, out var applied) &&
				string.Equals(applied, path, StringComparison.OrdinalIgnoreCase) && saved.Sources.TryGetValue(role, out var original))
				current[role] = original;
			else if (string.Equals(Path.GetDirectoryName(Environment.ExpandEnvironmentVariables(path)),
				AppPaths.ScaledCursorsDir, StringComparison.OrdinalIgnoreCase))
				current[role] = (defaults ??= RegistryCursorService.GetWindowsDefaultValues()).GetValueOrDefault(role) ?? "";
		}
		return current;
	}

	public static Dictionary<string, string> Resize(Dictionary<string, string> sources, int size, Models.ScaleMode mode)
	{
		var defaults = RegistryCursorService.GetWindowsDefaultValues();
		var files = new Dictionary<string, string>(sources);
		foreach (var (role, path) in sources)
		{
			if (string.IsNullOrEmpty(path))
			{
				var fallback = defaults.GetValueOrDefault(role);
				files[role] = string.IsNullOrEmpty(fallback) ? PlaceholderCursorDefaults.GetPath(role) ?? "" : fallback;
			}
		}
		var result = CursorScalerService.ScaleValues(files, size, mode);
		Remember(sources, result);
		return result;
	}

	public static void Remember(Dictionary<string, string> sources, Dictionary<string, string> applied)
	{
		var temp = SourcesFile + "." + Guid.NewGuid().ToString("N");
		try
		{
			File.WriteAllText(temp, JsonSerializer.Serialize(new Scheme(sources, applied)));
			File.Move(temp, SourcesFile, overwrite: true);
		}
		finally { File.Delete(temp); }
	}
}
