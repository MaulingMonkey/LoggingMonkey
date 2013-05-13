namespace LoggingMonkey.Web.Models
{
    public class IndexViewModel
    {
        public IndexViewModel()
        {
            Search = new SearchModel();
            DisplayOptions = new DisplayOptionsModel();
        }

        public SearchModel Search { get; set; }
        public DisplayOptionsModel DisplayOptions { get; set; }
        public MessagesModel Messages { get; set; }
    }
}
