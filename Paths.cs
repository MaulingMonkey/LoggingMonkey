
namespace LoggingMonkey
{
	public static class Paths
	{
		public static readonly string PasswordTxt    = Platform.IsOnUnix ? @"/home/loggingmonkey/password.txt"    : @"C:\home\scripts\bmpass.txt";
		public static readonly string BackupZip      = Platform.IsOnUnix ? @"/home/loggingmonkey/logs-backup.zip" : @"C:\home\logs-backup.zip";
		public static readonly string LogsDirectory  = Platform.IsOnUnix ? @"/home/loggingmonkey/logs/"           : @"C:\home\logs\";
		public static readonly string ExceptionsTxt  = Platform.IsOnUnix ? @"/home/loggingmonkey/exceptions.txt"  : null;
		public static readonly string RsaKey         = Platform.IsOnUnix ? @"/home/loggingmonkey/key.dsa"      : @"C:\home\configs\lm-key.dsa";

		public static readonly string AdminTxt       = Platform.IsOnUnix ? @"/home/loggingmonkey/admin.txt"     : @"C:\home\configs\lm-admin.txt";
		public static readonly string BlacklistTxt   = Platform.IsOnUnix ? @"/home/loggingmonkey/blacklist.txt" : @"C:\home\configs\lm-blacklist.txt";
		public static readonly string PendingTxt     = Platform.IsOnUnix ? @"/home/loggingmonkey/pending.txt"   : @"C:\home\configs\lm-pending.txt";
		public static readonly string WhitelistTxt   = Platform.IsOnUnix ? @"/home/loggingmonkey/whitelist.txt" : @"C:\home\configs\lm-whitelist.txt";
		public static readonly string AutoAllowTxt   = Platform.IsOnUnix ? @"/home/loggingmonkey/blacklist.txt" : @"C:\home\configs\lm-autoallow.txt";
	}
}
