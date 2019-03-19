public static class MethodExtensions {
	public static string GetDescription (this TempMailAPI.Enums.Charset value) {
		return GetDescription <TempMailAPI.Enums.Charset> (value);
	}
	public static string GetDescription (this TempMailAPI.Enums.ContentType value) {
		return GetDescription <TempMailAPI.Enums.ContentType> (value);
	}
	public static string GetDescription (this TempMailAPI.Enums.XPriority value) {
		return GetDescription <TempMailAPI.Enums.XPriority> (value);
	}
	private static string GetDescription <T> (this T value) {
		System.ComponentModel.DescriptionAttribute [] attributes = (System.ComponentModel.DescriptionAttribute [])value.GetType ().GetField (value.ToString ()).GetCustomAttributes (typeof (System.ComponentModel.DescriptionAttribute), false);
		return attributes != null && attributes.Length > 0 ? attributes [0].Description : value.ToString ();
	}
}