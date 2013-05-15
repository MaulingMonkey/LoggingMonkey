using System;
using System.ComponentModel.DataAnnotations;
using LoggingMonkey.Web.Helpers;

namespace LoggingMonkey.Web.Models
{
	public class SearchModel
	{
        public string Nickname { get; set; }
        public string Username { get; set; }
        public string Hostname { get; set; }

        public string Message { get; set; }
        public int ChannelId { get; set; }

        [Display(Name = "From")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To")]
        public DateTime? ToDate { get; set; }

        public MatchTypes MatchType { get; set; }
        public bool IsCaseSensitive { get; set; }

	    public bool IsAdvancedSearch
	    {
            get
            {
                return !String.IsNullOrWhiteSpace(Username) || !String.IsNullOrWhiteSpace(Hostname) || FromDate.HasValue ||
                       ToDate.HasValue || MatchType != MatchTypes.PlainText || IsCaseSensitive;
            }
	    }
	}
}
