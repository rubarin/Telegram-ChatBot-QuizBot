using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using File = System.IO.File;
using ChatBotTelegram;

namespace TelegramBot
{
    class Program
    {
        private static Quiz quiz;
        private static TelegramBotClient Bot;
        private static Dictionary<long, QuestionState> States;
        private static Dictionary<long, int> PlayerScore;
        static void Main(string[] args)
        {
            quiz = new Quiz("Quiz.txt");

            States = new Dictionary<long, QuestionState>();

            PlayerScore = new Dictionary<long, int>(); 

            Bot = new TelegramBotClient("YOUR_TOKEN_HERE");

            //Bot.on += BotOnCallbackQueryReceived;

            var me = Bot.GetMeAsync().Result;

            Console.WriteLine(me.FirstName);

            StartReceivingMessagesAsync().Wait();

            Console.ReadLine();
        }

        private static async Task StartReceivingMessagesAsync()
        {
            var offset = 0;
            while (true)
            {
                var updates = await Bot.GetUpdatesAsync(offset);

                foreach (var update in updates)
                {
                    if (update.Message != null)
                    {
                        var message = update.Message;

                        if (message.Type != MessageType.Text) { return; }

                        Console.WriteLine($"New message received from {message.Chat.FirstName}: {message.Text}");

                        switch (message.Text)
                        {
                            case "/start":
                                string text =
@"List of commands:
/start - run the bot
/keyboard - display keyboard
/callback - display menu
/quiz - start a quiz";
                                await Bot.SendTextMessageAsync(message.From.Id, text);
                                break;
                            case "/callback":
                                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                                {
                                    new[]
                                    {
                                        InlineKeyboardButton.WithUrl("Centria UAS", "https://net.centria.fi/"),
                                        InlineKeyboardButton.WithUrl("Weather in Kokkola", "https://en.ilmatieteenlaitos.fi/local-weather/kokkola"),
                                    },
                                    new[]
                                    {
                                        InlineKeyboardButton.WithCallbackData("Item 1"),
                                        InlineKeyboardButton.WithCallbackData("Item 2"),
                                    }
                                });
                                await Bot.SendTextMessageAsync(message.From.Id, "Choose one of the options:", replyMarkup: inlineKeyboard);
                                break;
                            case "/keyboard":
                                var replyKeyboard = new ReplyKeyboardMarkup(new[]
                                {
                                    new[]
                                    {
                                        new KeyboardButton("Hello!"),
                                        new KeyboardButton("How are you doing?"),
                                    },
                                    new[]
                                    {
                                        new KeyboardButton("Contact info") { RequestContact = true},
                                        new KeyboardButton("Geolocation") { RequestLocation = true}
                                    }
                                });
                                await Bot.SendTextMessageAsync(message.Chat.Id, "Message", replyMarkup: replyKeyboard);
                                break;
                            case var answer when new[] { "Hello!" }.Contains(answer):
                                {
                                    await Bot.SendTextMessageAsync(message.From.Id, "Hello!");
                                    break;
                                }
                            case var answer when new[] { "How are you doing?" }.Contains(answer):
                                {
                                    await Bot.SendTextMessageAsync(message.From.Id, "I'm doing fine!");
                                    break;
                                }
                            case "/quiz":
                                var chatId = message.Chat.Id;
                                offset = update.Id + 1;
                                var playerId = message.From.Id;
                                if (!States.TryGetValue(chatId, out var state))
                                {
                                    state = new QuestionState();
                                    States[chatId] = state;
                                }

                                if (state.CurrentItem == null)
                                {
                                    state.CurrentItem = quiz.NextQuestion();
                                    state.questionIndex = 0;
                                }

                                while (state.questionIndex < quiz.Questions.Count)
                                {
                                    var question = state.CurrentItem;
                                    await Bot.SendTextMessageAsync(chatId, question.Question);

                                    var answer = "";
                                    while (string.IsNullOrEmpty(answer))
                                    {
                                        updates = await Bot.GetUpdatesAsync(offset: offset, timeout: 100);
                                        message = updates.FirstOrDefault()?.Message;

                                        if (message != null && !string.IsNullOrEmpty(message.Text))
                                        {
                                            answer = message.Text.ToLower();
                                        }
                                    }

                                    offset = updates.LastOrDefault().Id + 1;


                                    if (answer == question.Answer.ToLower())
                                    {
                                        await Bot.SendTextMessageAsync(chatId, "Correct!");
                                        if (PlayerScore.ContainsKey(playerId))
                                        {
                                            PlayerScore[playerId]++;
                                        }
                                        else
                                        {
                                            PlayerScore[playerId] = 1;
                                        }

                                    }
                                    else
                                    {
                                        await Bot.SendTextMessageAsync(chatId, "Wrong");
                                    }

                                    state.CurrentItem = quiz.NextQuestion();
                                    state.questionIndex++;
                                }

                                await Bot.SendTextMessageAsync(chatId, "Quiz complete!");
                                await Bot.SendTextMessageAsync(chatId,  $"You got {PlayerScore[playerId]}/{quiz.Questions.Count} points for the quiz");
                                break;
      
                            default:
                                break;
                        }
                    }
                    else if (update.CallbackQuery != null)
                    {
                        var callbackQuery = update.CallbackQuery;
                        string buttonText = callbackQuery.Data;

                        Console.WriteLine($"Callback query received from {callbackQuery.From.FirstName}: {buttonText}");

                        await Bot.AnswerCallbackQueryAsync(
                            callbackQueryId: callbackQuery.Id,
                            text: $"You pressed {buttonText}");
                    }

                    offset = update.Id + 1;
                }

                await Task.Delay(1000);
            }
        }
    }
}

