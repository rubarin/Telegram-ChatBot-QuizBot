using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ChatBotTelegram
{
    public class Quiz
    {
        public List<QuestionItem> Questions { get; set; }
        public List<QuestionItem> AvailableQuestion { get; set; }
        private Random random;
        private int count;
        public Quiz(string path = "Quiz.txt")
        {
            var lines = File.ReadAllLines(path);
            Questions = lines
                .Select(line => line.Split('|'))
                .Select(line => new QuestionItem()
                {
                    Question = line[0],
                    Answer = line[1]
                })
                .ToList();
            AvailableQuestion = new List<QuestionItem>();
            random = new Random();
            count = Questions.Count;
        }

        public QuestionItem NextQuestion()
        {
            if (AvailableQuestion.Count == 0)
            {
                AvailableQuestion = new List<QuestionItem>(Questions);
            }

            var index = random.Next(AvailableQuestion.Count);
            var question = AvailableQuestion[index];

            AvailableQuestion.RemoveAt(index);

            return question;
        }
    }
}
