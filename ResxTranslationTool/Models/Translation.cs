using Microsoft.Practices.Prism.Mvvm;

namespace ResxTranslationTool.Models
{
    public sealed class Translation : BindableBase
    {
        private string _translatedText;
        public string Id { get; set; }

        public string FileName { get; set; }

        public string OriginalText { get; set; }

        public string TranslatedText
        {
            get { return _translatedText; }
            set { SetProperty(ref _translatedText, value); }
        }

        public string Comment { get; set; }
    }
}
