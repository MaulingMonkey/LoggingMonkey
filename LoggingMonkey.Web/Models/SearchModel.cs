using System;
using System.ComponentModel.DataAnnotations;
using LoggingMonkey.Web.Helpers;

namespace LoggingMonkey.Web.Models
{
	public class SearchModel
	{
		public static readonly int DefaultChannelId;
		public static readonly int DefaultMatchTypeId;

        public string Nickname { get; set; }
        public string Username { get; set; }
        public string Hostname { get; set; }

        public string Message { get; set; }
        public int ChannelId { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        [Display(Name = "From")]
        public DateTime? FromDate { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        [Display(Name = "To")]
        public DateTime? ToDate { get; set; }

        public MatchTypes MatchType { get; set; }
        public bool IsCaseSensitive { get; set; }
	}
}
