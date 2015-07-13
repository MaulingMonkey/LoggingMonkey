using System;
using System.Collections.Generic;
using System.Linq;

namespace LoggingMonkey
{
	class AccessControlCommand
	{
		public string Description;
		public readonly List< FileAccessList > InvokerRequires = new List<FileAccessList>( );
		public readonly List< FileAccessList > InvokerProhibits = new List<FileAccessList>( );

		public readonly List< FileAccessList > TargetAddedTo = new List<FileAccessList>( );
		public readonly List< FileAccessList > TargetRemovedFrom = new List<FileAccessList>( );
		public readonly List< FileAccessList > TargetExpectedInWarning = new List<FileAccessList>( );

		public AccessControlCommand( string description )
		{
			Description = description;
		}

		public void Invoke( string invokerId, string targetId )
		{
			Debug.WriteLine( "{0} attempted to {1} {2}", invokerId, Description, targetId );

			if( InvokerRequires.Count > 0 && !InvokerRequires.Any( l => l.ContainsUser( invokerId ) ) )
			{
				Debug.WriteLine
					( "\tFailed: {0} was not in any of the required lists: {1}"
					, invokerId
					, string.Join( ", ", InvokerRequires.Select( l => l.Description ).ToArray( ) )
					);
				return;
			}

			var deniedBy = InvokerProhibits.FirstOrDefault( l => l.ContainsUser( invokerId ) );
			if( deniedBy != null )
			{
				Debug.WriteLine
					( "\tFailed: {0} was in a prohibited list: {1}"
					, invokerId
					, deniedBy.Description );
				return;
			}

			if( TargetExpectedInWarning.Count > 0 && !TargetExpectedInWarning.Any( l => l.ContainsLine( targetId ) ) )
				Debug.WriteLine
					( "\tWarning: {0} not found in any of the following lists: {1}"
					, targetId
					, string.Join( ", ", TargetExpectedInWarning.Select( l => l.Description ).ToArray( ) )
					);

			foreach( var list in TargetAddedTo )
			if( !list.ContainsLine( targetId ) )
			{
				Debug.WriteLine( "\tAdded to: {0}", list.Description );
				list.AppendLine( targetId );
			}
			else
			{
				Debug.WriteLine( "\tAlready in: {0}", list.Description );
			}

			foreach( var list in TargetRemovedFrom )
			if( list.ContainsLine( targetId ) )
			{
				Debug.WriteLine( "\tRemoved from: {0}", list.Description );
				list.RemoveLines( targetId );
			}
		}
	}
}
