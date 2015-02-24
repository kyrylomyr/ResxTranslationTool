namespace ResxTranslationTool.Models
{
    public sealed class Translation
    {
        public string Id { get; set; }

        public string FileName { get; set; }

        public string OriginalText { get; set; }

        public string TranslatedText { get; set; }

        public string Comment { get; set; }
    }
}
