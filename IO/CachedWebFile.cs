using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace LoggingMonkey {
    public abstract class CachedWebFile {
		public readonly string LocalPath;
		public readonly string RemotePath;
		public TimeSpan CacheAtLeast = TimeSpan.FromDays(1), Timeout = TimeSpan.FromMinutes(10);
		DateTime LastTry = DateTime.MinValue;

		public CachedWebFile( string local, string remote ) {
			LocalPath = local;
			RemotePath = remote;

			WebClient.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler( WebClient_DownloadFileCompleted );
			RecoverIfNecessary();
			BeginDownloadIfOutOfDate();
		}

		protected abstract void OnDownloadComplete();

		void WebClient_DownloadFileCompleted( object sender, System.ComponentModel.AsyncCompletedEventArgs e ) {
			if( !File.Exists(LocalTempPath) )
				return; // some sort of error

			File.Delete(LocalPath);
			File.Move(LocalTempPath,LocalPath);
			OnDownloadComplete();
		}

		void RecoverIfNecessary() {
			if( File.Exists(LocalTempPath) && !File.Exists(LocalPath) )
				File.Move(LocalTempPath,LocalPath); // Only rename, download might've been interrupted
			OnDownloadComplete();
		}

		void BeginDownloadIfOutOfDate() {
			var now = DateTime.Now;
			var ft = File.GetLastWriteTime(LocalPath);
			if( LastTry+Timeout < now && (!File.Exists(LocalPath) || ft > now || ft+CacheAtLeast < now) ) {
				WebClient.DownloadFileAsync(new Uri(RemotePath),LocalTempPath);
				LastTry = DateTime.Now;
			}
		}

		private string LocalTempPath { get { return LocalPath+"2"; }}
		private readonly WebClient WebClient = new WebClient();
	}

    public class CachedHashedWebCsvFile : CachedWebFile {
		HashSet<string> _Lines = new HashSet<string>();
		public HashSet<string> Lines { get { return _Lines; }}

		public CachedHashedWebCsvFile( string local, string remote ): base( local, remote ) {}

		protected override void OnDownloadComplete() {
			if(!File.Exists(LocalPath)) return;
			var lines = new HashSet<string>(File.ReadAllLines(LocalPath));
			_Lines = lines;
		}
	}
}
