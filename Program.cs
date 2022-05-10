using AForge.Video.DirectShow;
using ObjectDetection;
using System.Drawing.Imaging;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

//Инициализация камеры
var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
var videoSource = new VideoCaptureDevice(new FilterInfoCollection(FilterCategory.VideoInputDevice)[0].MonikerString);
videoSource.NewFrame += VideoSource_NewFrame;

//Инициализация нейросети
var neiro = new Neiroweb(); 

//Инициализация и данные бота
string token = "";
var isWorking = false;
var botClient = new TelegramBotClient(token);
using var cts = new CancellationTokenSource();


// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = { } // receive all update types
};
botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    receiverOptions,
    cancellationToken: cts.Token);

var me = await botClient.GetMeAsync();
Console.WriteLine($"Start listening for @{me.Username}");

var chatId = 1066726563;
var isPersonOnPhoto = false;
var rootPath = new FileInfo(typeof(Program).Assembly.Location).Directory.FullName;
var pathToPhoto = rootPath.Substring(0, rootPath.Length - 16) + @"assets\images\output\photo.jpg";

while (true)
{
    if (isWorking)
    {
        GC.Collect();
        Thread.Sleep(2000);
        videoSource.Start(); //сделать кадр
        Thread.Sleep(1000);
        neiro = new Neiroweb();
        MovePhoto("photo.jpg");
        isPersonOnPhoto = neiro.Initialize();
        if (isPersonOnPhoto && isWorking)
        {
            
            using (var fileStream = new FileStream(pathToPhoto, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await botClient.SendPhotoAsync(
                    chatId: chatId,
                    photo: new InputOnlineFile(fileStream),
                    caption: "Тревога! Посторонний в охраняемой зоне!",
                    replyMarkup: new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(
                        new[] { new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Start"),
                            new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Stop") }
                        )
                );
            }
        }
    }
    Thread.Sleep(1000);
}

Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Type != UpdateType.Message)
        return;
    if (update.Message!.Type != MessageType.Text)
        return;

    var chatId = update.Message.Chat.Id;
    var messageText = update.Message.Text;

    switch (messageText)
    {
        case "Start":
            isWorking = true;
            break;

        case "Stop":
            isWorking = false;
            break;
    }
}

Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

void MovePhoto(string filename)
{
    var resoursePath = rootPath + '\\' + filename;
    var resultPath = rootPath.Substring(0,rootPath.Length - 16) + @"assets\images\" + filename;
    var file = new FileInfo(resoursePath);
    if (System.IO.File.Exists(resultPath))
        System.IO.File.Delete(resultPath);
    file.MoveTo(resultPath);
}

static void VideoSource_NewFrame(object sender, AForge.Video.NewFrameEventArgs e)
{
    var filename = "photo.jpg";
    e.Frame.Save(filename, ImageFormat.Jpeg);
}