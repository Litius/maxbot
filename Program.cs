using maxbot;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;


// 1. Вставьте сюда ваш токен из кабинета MAX для бизнеса
const string AccessToken = "ВАШ ТОКЕН";
const string BaseUrl = "https://platform-api.max.ru";

using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
// Токен передается в заголовке Authorization
client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AccessToken);


using var cts = new CancellationTokenSource();


Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("Остановка бота...");
};

Console.WriteLine("Бот запущен. Нажмите Ctrl+C для выхода.");

long? marker = null; // Указатель на последнее полученное обновление


//цикл опроса полученных сообщений от бота
while (!cts.Token.IsCancellationRequested)
{
    try
    {
        //формируем url для запроса
        var url = "updates?timeout=30&types=message_created";
        if (marker.HasValue)
        {
            url += $"&marker={marker.Value}";
        }

        //запрашиваем ответ передавая сформированный url
        using HttpResponseMessage response = await client.GetAsync(url, cts.Token);

        //декодируем байтовый ответ в строку
        string rawBody = await response.Content.ReadAsStringAsync(cts.Token); 

        //получаем тип ответа из тела ответа
        string? contentType = response.Content.Headers.ContentType?.MediaType;


        // Если ответ НЕ успешный или формат сообщения НЕ json
        if (!response.IsSuccessStatusCode || contentType != "application/json")
        {
            //берём 300 первых символов ответа
            var preview = rawBody.Length > 300 ? rawBody[..300] : rawBody;
            //возвращаем ответ в консоль
            Console.WriteLine($"Неожиданный ответ сервера. HTTP {(int)response.StatusCode} ({contentType}):\n{preview}");
            await Task.Delay(5000, cts.Token);
            continue;
        }

        //парсим строку в дерево объектов json и помещаем корень в root
        JsonNode? root = JsonNode.Parse(rawBody);

        //получаем marker
        if (root?["marker"] != null)
        {
            marker = root["marker"]!.GetValue<long>();
        }

        //получаем "обновления"
        if (root?["updates"] is JsonArray updates)
        {
            //цикл по полученным обновлениям
            foreach (var update in updates)
            {
                //получаем тип обновления
                var updateType = update?["update_type"]?.GetValue<string>();

                //если это НЕ новое сообщение - переходим к следующему обновлению
                if (updateType != "message_created") continue;

                //получаем из обновления сообщение
                var message = update?["message"];

                //получаем из сообщения chat_id  отправителя
                var chatId = message?["recipient"]?["chat_id"]?.GetValue<long>();

                //получем тело вообщения
                var text = message?["body"]?["text"]?.GetValue<string>();

                //отправитель бот?
                var senderIsBot = message?["sender"]?["is_bot"]?.GetValue<bool>() ?? false;

                //если chat_id есть, сообщение не пусктя строка и отправитель НЕ бот
                if (chatId.HasValue && !string.IsNullOrWhiteSpace(text) && !senderIsBot)
                {
                    Console.WriteLine($"[Чат {chatId}]: {text}");


                    //формируем ответ
                    var res =new StringBuilder();

                    //пытаемся распарсить строку отправленную пользователем на предмет трёх чисел
                    if (QuadraticParser.TryParse(text, out double a, out double b, out double c))
                    {
                        //создаём объект решателя уравнений
                        var eq = new QuadraticEquation(a, b, c);
                        //получаем ответ решения уравнения
                        var r = eq.Solve();

                        //добавляем полученное решение в ответ
                        res.Append(eq);
                        res.Append(Environment.NewLine);

                        switch (r.Kind)
                        {
                            case RootsKind.Two:
                                res.Append($"X1 = {r.X1}");
                                res.Append(Environment.NewLine);
                                res.Append($"X2 = {r.X2}");
                                break;
                            case RootsKind.One:
                                res.Append($"X1 = {r.X1}");
                                break;
                            default:
                                res.Append("Не имеет корней!");
                                break;

                        }
                    }
                    else
                    {

                        
                        res.Append("Привет!"); 
                        res.Append("Это бот Антоненко Кати для решения квадратных уравнений: нужно ввести три числа разделяя их пробелами.Например: 2   -5    2");
                    }

                    //отправляем ответ пользователю
                    await SendEchoMessageAsync(client, chatId.Value, res.ToString(), cts.Token);
                }
            }
        }
    }

    catch (OperationCanceledException)
    {
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Сбой цикла опроса: {ex.Message}");
        await Task.Delay(5000, cts.Token);
    }
}

async Task SendEchoMessageAsync(HttpClient client, long chatId, string replyText, CancellationToken ct)
{
    //кладём ответ в промежуточный объект-обёртку (для последующей сериализации json)
    var payload = new { text = replyText };

    //формируем ответ для отправки 
    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    //отправляем ответ 
    var response = await client.PostAsync($"messages?chat_id={chatId}", content, ct);

    //проверяем успешность отправки
    if (!response.IsSuccessStatusCode)
    {
        var err = await response.Content.ReadAsStringAsync(ct);
        Console.WriteLine($"Не удалось отправить ответ: {err}");
    }
}
