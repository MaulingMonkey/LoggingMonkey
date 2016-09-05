using System.IO;
using System.Resources;
using System.Text;

namespace LoggingMonkey {
	static partial class __Extension_Methods {
		public static string GetText( this ResourceManager resourceManager, string name ) {
			var o = resourceManager.GetObject(name);
			if (o is string) return (string)o;

			if (o is byte[]) {
				var reader = new StreamReader(new MemoryStream((byte[])o));
				return reader.ReadToEnd();
			}

			return null;
		}

		public static byte[] GetBinary( this ResourceManager resourceManager, string name ) {
			var o = resourceManager.GetObject(name);
			if (o is byte[]) return (byte[])o;

			if (o is string) return Encoding.UTF8.GetBytes((string)o);

			return null;
		}
	}
}
