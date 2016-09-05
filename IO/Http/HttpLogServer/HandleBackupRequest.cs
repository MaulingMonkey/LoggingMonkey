using System;
using System.IO;
using System.IO.Packaging;
using System.Net;
using System.Text;

namespace LoggingMonkey {
	partial class HttpLogServer {
		private void HandleBackupRequest( HttpListenerContext context, AccessControlStatus acs )
		{
			if( !Allow( acs ) )
			{
				context.Response.StatusCode = 401;
				context.Response.ContentEncoding = Encoding.UTF8;
				context.Response.ContentType = "text/html";
				using ( var writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8) )
				{
					writer.Write
						( "<html><head>\n"
						+ "	<title>" + acs.ToString() + "</title>\n"
						+ "</head><body>\n"
						+ "	Your ID has " + ((acs==AccessControlStatus.Blacklisted) ? "been blacklisted" : "not yet been whitelisted") + "<br>\n"
						+ "</body></html>\n"
						);
				}
				return;// Require auth
			}

			// TODO: Handle multiple backup.zip requests
			Stream zip = null;
			try {
				zip = File.Open( Paths.BackupZip, FileMode.Create, FileAccess.ReadWrite, FileShare.None );
			} catch ( IOException ) {
				context.Response.ContentEncoding = Encoding.UTF8;
				context.Response.ContentType = "text/plain";
				context.Response.StatusCode = 503;
				using ( var writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8) ) {
					writer.Write("Couldn't open backup zip (already backing up?)\n");
				}
				return; // EARLY BAIL
			}

			using ( zip ) {
				using ( var package = ZipPackage.Open(zip,FileMode.Create) ) {
					foreach ( var logfile in Directory.GetFiles(Paths.LogsDirectory,"*.log",SearchOption.TopDirectoryOnly) ) {
						var relfile = Uri.EscapeDataString( Path.GetFileName(logfile) );
						var uri = PackUriHelper.CreatePartUri( new Uri(relfile,UriKind.Relative) );
						var part = package.CreatePart( uri, System.Net.Mime.MediaTypeNames.Text.Plain, CompressionOption.Maximum );
						using ( var fstream = File.Open(logfile,FileMode.Open,FileAccess.Read,FileShare.ReadWrite) ) using ( var partstream = part.GetStream() ) fstream.CopyTo(partstream);
						package.Flush();
					}
					package.Close();
				}
				zip.Flush();
				zip.Position = 0;

				context.Response.ContentType = "application/zip";
				context.Response.ContentLength64 = zip.Length;
				zip.CopyTo(context.Response.OutputStream);
			}
			return; // EARLY BAIL
		}
	}
}
