
namespace LoggingMonkey {
	interface IIrcMessageReactor {
		bool TryReact( Network network, string message );
	}
}
