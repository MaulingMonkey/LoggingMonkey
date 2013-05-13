
namespace LoggingMonkey
{
	public static class Paths
	{
		public static readonly string PasswordTxt    = Platform.IsOnUnix ? @"/home/loggingmonkey/password.txt"    : @"I:\home\scripts\bmpass.txt";
		public static readonly string BackupZip      = Platform.IsOnUnix ? @"/home/loggingmonkey/logs-backup.zip" : @"I:\home\logs-backup.zip";
		public static readonly string LogsDirectory  = Platform.IsOnUnix ? @"/home/loggingmonkey/logs/"           : @"I:\home\logs\";
		public static readonly string ExceptionsTxt  = Platform.IsOnUnix ? @"/home/loggingmonkey/exceptions.txt"  : null;
		public static readonly string RsaKey         = Platform.IsOnUnix ? @"/home/loggingmonkey/key.dsa"         : @"I:\home\configs\lm-key.dsa";

		public static readonly string AdminTxt       = Platform.IsOnUnix ? @"/home/loggingmonkey/admin.txt"     : @"I:\home\configs\lm-admin.txt";
		public static readonly string BlacklistTxt   = Platform.IsOnUnix ? @"/home/loggingmonkey/blacklist.txt" : @"I:\home\configs\lm-blacklist.txt";
		public static readonly string PendingTxt     = Platform.IsOnUnix ? @"/home/loggingmonkey/pending.txt"   : @"I:\home\configs\lm-pending.txt";
		public static readonly string WhitelistTxt   = Platform.IsOnUnix ? @"/home/loggingmonkey/whitelist.txt" : @"I:\home\configs\lm-whitelist.txt";
		public static readonly string AutoAllowTxt   = Platform.IsOnUnix ? @"/home/loggingmonkey/blacklist.txt" : @"I:\home\configs\lm-autoallow.txt";
	}
}
