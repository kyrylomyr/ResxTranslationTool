using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ResxTranslationTool.Models;

namespace ResxTranslationTool.Services
{
    internal sealed class TranslationFileService
    {
        private readonly string _fileName;

        public TranslationFileService(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("The file name should not be empty.", "fileName");
            
            _fileName = fileName;
        }

        public void Save(IEnumerable<Translation> translations)
        {
            using (var writer = new StreamWriter(_fileName))
            {
                var solution = new Solution { Translations = translations.ToList() };
                var serializer = new XmlSerializer(typeof(Solution));
                serializer.Serialize(writer, solution);
            }
        }

        public IEnumerable<Translation> Read()
        {
            if (!File.Exists(_fileName))
                throw new FileNotFoundException("The file doesn't exist.", _fileName);

            using (var reader = new StreamReader(_fileName))
            {
                var serializer = new XmlSerializer(typeof(Solution));
                var solution = (Solution)serializer.Deserialize(reader);
                return solution.Translations;
            }
        }
    }
}
