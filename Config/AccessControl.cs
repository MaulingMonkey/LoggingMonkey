using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LoggingMonkey
{
	public enum AccessControlStatus
	{
		// Positive access
		Admin,
		Whitelisted,

		// Possible access
		Pending,
		Error,

		// Negative access
		Blacklisted,
	}

	public static class AccessControl
	{
		static readonly RSACryptoServiceProvider  RSA  = new RSACryptoServiceProvider();
		static readonly SHA1CryptoServiceProvider Hash = new SHA1CryptoServiceProvider();

		static readonly FileAccessList mAdminlist   = new FileAccessList( "Admin List",   Paths.AdminTxt     );
		static readonly FileAccessList mBlacklist   = new FileAccessList( "Blacklist",    Paths.BlacklistTxt );
		static readonly FileAccessList mPendinglist = new FileAccessList( "Pending List", Paths.PendingTxt   );
		static readonly FileAccessList mWhitelist   = new FileAccessList( "Whitelist",    Paths.WhitelistTxt );
		static readonly FileAccessList mTwitlist    = new FileAccessList( "Twit List",    Paths.TwitlistTxt  );

		public static readonly Dictionary<string,FileAccessList> FileAccessLists = new Dictionary<string, FileAccessList>()
		{
			{ "adminlist",		mAdminlist	},
			{ "blacklist",		mBlacklist	},
			{ "pendinglist",	mPendinglist},
			{ "whitelist",		mWhitelist	},
			{ "twitlist",		mTwitlist	},
		};

		static readonly AccessControlCommand
			accWhitelist  = new AccessControlCommand( "!whitelist"  ) { InvokerRequires = { mAdminlist }, TargetAddedTo = { mWhitelist }, TargetRemovedFrom = { mPendinglist, mBlacklist } },
			accBlacklist  = new AccessControlCommand( "!blacklist"  ) { InvokerRequires = { mAdminlist }, TargetAddedTo = { mBlacklist }, TargetRemovedFrom = { mPendinglist, mWhitelist } },
			accTwitlist   = new AccessControlCommand( "!twitlist"   ) { InvokerRequires = { mAdminlist }, TargetAddedTo = { mTwitlist } },
			accUnTwitlist = new AccessControlCommand( "!untwitlist" ) { InvokerRequires = { mAdminlist }, TargetRemovedFrom = { mTwitlist } };

		static AccessControl()
		{
			if( !File.Exists(Paths.RsaKey) )
			{
				// save out newly generated key
				File.WriteAllBytes(Paths.RsaKey,RSA.ExportCspBlob(true));
			}
			else
			{
				// load in existing key
				RSA.ImportCspBlob(File.ReadAllBytes(Paths.RsaKey));
			}
		}

		/// <summary>
		/// Whitelists an ID
		/// </summary>
		/// <returns>Server-signed whitelisted ID</returns>
		public static string RequestToken( string id )
		{
			// Always create token
			byte[] signed = RSA.SignData(Encoding.UTF8.GetBytes(id),Hash);
			string token = "0|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(id)) + "|" + Convert.ToBase64String(signed);

			// But don't re-add them to any list if they're on any of them.
			if( !mBlacklist.ContainsUser(id) && !mWhitelist.ContainsUser(id) && !mPendinglist.ContainsUser(id) )
				mPendinglist.AppendLine(id);

			return token;
		}

		public static void Whitelist ( string invokerId, string targetId ) { accWhitelist .Invoke( invokerId, targetId ); }
		public static void Blacklist ( string invokerId, string targetId ) { accBlacklist .Invoke( invokerId, targetId ); }
		public static void Twitlist  ( string invokerId, string targetId ) { accTwitlist  .Invoke( invokerId, targetId ); }
		public static void Untwitlist( string invokerId, string targetId ) { accUnTwitlist.Invoke( invokerId, targetId ); }

		public static bool InTwitlist( string targetId ) { return mTwitlist.ContainsUser( targetId ); }

		public static string Decode( string data )
		{
			return Encoding.UTF8.GetString(Convert.FromBase64String(data.Split('|')[1]));
		}

		public static AccessControlStatus GetStatus( string data )
		{
			try
			{
				// Check for negative & negative-leaning access first as it overrides positive access.
				if( string.IsNullOrEmpty(data) )
					return AccessControlStatus.Error;

				var split = data.Split('|');
				if( split.Length != 3 )
					return AccessControlStatus.Error; // bad format

				if( split[0] != "0" )
					return AccessControlStatus.Error; // bad version

				var rawId = Convert.FromBase64String(split[1]);
				var id = Encoding.UTF8.GetString(rawId);

				if( mBlacklist.ContainsUser(id) )
					return AccessControlStatus.Blacklisted;

				// fully verify string before acking as whitelisted
				var rawSignedId = Convert.FromBase64String(split[2]);
				var signedId = Encoding.UTF8.GetString(rawSignedId);

				if( !RSA.VerifyData(rawId,Hash,rawSignedId) )
					return AccessControlStatus.Error; // bad signature


				// Now check for positive access as it overrides the remaining possible access
				if( mAdminlist.ContainsUser(id) )
					return AccessControlStatus.Admin;

				if( mWhitelist.ContainsUser(id) )
					return AccessControlStatus.Whitelisted;


				// ...nothing positive or negative, determine "possible" access flavor
				if( mPendinglist.ContainsUser(id) )
					return AccessControlStatus.Pending;

				return AccessControlStatus.Error; // not on any list
			}
			catch( Exception )
			{
				return AccessControlStatus.Error;
			}
		}
	}
}
