using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LoggingMonkey
{
	enum AccessControlStatus
	{
		Whitelisted,
		Blacklisted,
		Pending,
		Error,
	}

	static class AccessControl
	{
		static readonly RSACryptoServiceProvider  RSA  = new RSACryptoServiceProvider();
		static readonly SHA1CryptoServiceProvider Hash = new SHA1CryptoServiceProvider();

	    static readonly FileLineList mAdminlist     = new FileLineList(Paths.AdminTxt);
        static readonly FileLineList mBlacklist     = new FileLineList(Paths.BlacklistTxt);
        static readonly FileLineList mPendinglist   = new FileLineList(Paths.PendingTxt);
        static readonly FileLineList mWhitelist     = new FileLineList(Paths.WhitelistTxt);

		//static readonly 
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
			if( mBlacklist.Contains(id) )
				return ""; // No.

			byte[] signed = RSA.SignData(Encoding.UTF8.GetBytes(id),Hash);
			string token = "0|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(id)) + "|" + Convert.ToBase64String(signed);

			if( !mWhitelist.Contains(id) && !mPendinglist.Contains(id) )
				mPendinglist.AppendLine(id);

			return token;
		}

		public static void Blacklist( string id )
		{
			if( !mBlacklist.Contains(id) )
				mBlacklist.AppendLine(id);
		}

        public static void Blacklist(string invokerId, string id)
        {
            if (!mAdminlist.Contains(invokerId))
            {
                Console.WriteLine("{0} attempted to !blacklist {1}, was not found in adminlist.", invokerId, id);
                return;
            }

            if (mBlacklist.Contains(id))
            {
                Console.WriteLine("{0} called !blacklist on blacklisted target {1}", invokerId, id);
                return;
            }

            if (mPendinglist.Contains(id))
            {
                mPendinglist.RemoveLines(id);
            }

            if (mWhitelist.Contains(id))
            {
                mWhitelist.RemoveLines(id);
            }

            Blacklist(id);
            Console.WriteLine("{0} !blacklisted {1}", invokerId, id);
        }

		public static void Whitelist( string id )
		{
			if( !mWhitelist.Contains(id) )
				mWhitelist.AppendLine(id);
		}

        public static void Whitelist( string invokerId, string id )
        {
            if (!mAdminlist.Contains(invokerId))
            {
                Console.WriteLine("{0} attempted to !whitelist {1}, was not found in adminlist.", invokerId, id);
                return;
            }

            if (mWhitelist.Contains(id))
            {
                Console.WriteLine("{0} called !whitelist on a whitelisted target {1}", invokerId, id);
                return;
            }

            if (mBlacklist.Contains(id))
            {
                Console.WriteLine("{0} called !whitelist on a blacklisted target {1}. Removing target from blacklist.", invokerId, id);
                mBlacklist.RemoveLines(id);
            }

            if (!mPendinglist.Contains(id))
            {
                Console.WriteLine("{0} called !whitelist on non-pending target {1}", invokerId, id);
            }
            else
            {
                mPendinglist.RemoveLines(id);
            }

            Console.WriteLine("{0} added {1} to whitelist", invokerId, id);
            Whitelist(id);
        }

		public static string Decode( string data )
		{
			return Encoding.UTF8.GetString(Convert.FromBase64String(data.Split('|')[1]));
		}

		public static AccessControlStatus GetStatus( string data )
		{
			try
			{
				if( string.IsNullOrEmpty(data) )
					return AccessControlStatus.Error;

				var split = data.Split('|');
				if( split.Length != 3 )
					return AccessControlStatus.Error; // bad format

				if( split[0] != "0" )
					return AccessControlStatus.Error; // bad version

				var rawId = Convert.FromBase64String(split[1]);
				var id = Encoding.UTF8.GetString(rawId);

				if( mBlacklist.Contains(id) )
					return AccessControlStatus.Blacklisted;

				// fully verify string before acking as whitelisted
				var rawSignedId = Convert.FromBase64String(split[2]);
				var signedId = Encoding.UTF8.GetString(rawSignedId);

				if( !RSA.VerifyData(rawId,Hash,rawSignedId) )
					return AccessControlStatus.Error; // bad signature

				if( mWhitelist.Contains(id) )
					return AccessControlStatus.Whitelisted;

				if( mPendinglist.Contains(id) )
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
