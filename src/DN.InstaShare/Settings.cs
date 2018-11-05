namespace DN.InstaShare
{
    public class Settings
    {
		public string Footer { get; set; }
		public string Separater { get; set; }
		public string Photographer { get; set; }
        public string[] StandardKeywords { get; set; } = new string[0];
        public string[] ExcludedKeywords { get; set; } = new string[0];
    }
}