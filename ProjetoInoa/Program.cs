using System;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using System.Net.Mail;
using System.IO;
using System.Threading.Tasks;

namespace ProjetoInoa
{
    public class Program
    {
        //Endereço do .json deve ser alterado, quando instalado em outra máquina/diretório.

        // O símbolo da ação deve incluir a bolsa de valores, por exemplo "PETR4.SA"
        // Algumas ações brasileiras possuem o sufixo ".SA" porque foram originalmente listadas na bolsa de valores de São Paulo


        //Carrega as Configurações do JSON
        public static string _json = File.ReadAllText("C:/Users/bluee/Documents/1 Meus Documentos/INOA/ProjetoInoa/ProjetoInoa/config/config.json");
        private readonly Config _config = JsonConvert.DeserializeObject<Config>(_json);

       
        public static async Task Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("\nErro:\n  Insira os argumentos corretamente: <Ativo> <Preço Venda> <Preço Compra>");
                return;
            }
            if (!double.TryParse(args[1], out double sellPrice) || !double.TryParse(args[2], out double buyPrice))
            {
                Console.WriteLine("\nErro:\n  Preço de Compra e/ou Venda inválidos");
                return;
            }
            string tickerSymbol = args[0];
            var program = new Program();
            await program.CheckPrice(tickerSymbol, sellPrice, buyPrice);
        }
        public void SendEmail(int y, double z)
        {
            //Se y == 0, então estamos enviando um email referente à Venda. Caso contrário, y==1, estamos recomendando a compra.
            //double z é o valor do ativo em questão.

            MailMessage email = new MailMessage();

            //Configuração do Corpo do Email
            email.From = new MailAddress(_config.SenderEmail);
            email.To.Add(new MailAddress(_config.TargetEmail));
            if (y == 0)
            {     
                email.Subject = "Recomendação de Venda de Ativo";
                email.Body = $"Boa Tarde,\n Estamos enviando o E-mail referente a um pedido de alerta de compra e venda de ativo. O ativo em questão atingiu valor acima do selecionado para notificação de venda. O valor atual do mesmo é {z}";
            }
            else if (y == 1)
            {
                email.Subject = "Recomendação de Compra de Ativo";
                email.Body = $"Boa Tarde,\n Estamos enviando o E-mail referente a um pedido de alerta de compra e venda de ativo. O ativo em questão atingiu valor abaixo do selecionado para notificação de compra. O valor atual do mesmo é {z}";
            }

            //Configuração do Cliente SMTP
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                  | SecurityProtocolType.Tls11
                                                  | SecurityProtocolType.Tls12;
            }
            SmtpClient client = new SmtpClient($"{_config.SMTPAddress}", _config.SMTPPort);
            client.EnableSsl = _config.EnableSsl;
            client.UseDefaultCredentials = _config.UseDefaultCredentials;
            client.Credentials = new NetworkCredential(_config.SenderEmail, _config.SenderPW);

            // Envio do Email
            client.Send(email);
        }

        public async Task CheckPrice(string tag, double sellPrice, double buyPrice)
        {
            bool sellE_Sent = false;
            bool buyE_Sent = false;
            while (true)
            {
                // recebimento e leitura de JSON da API
                using (var client = new HttpClient())
                {
                    HttpResponseMessage responseMsg = await client.GetAsync($"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={tag}&apikey={_config.APIKey}");

                    if (responseMsg.IsSuccessStatusCode)
                    {
                        string response = await responseMsg.Content.ReadAsStringAsync();
                        dynamic result = JsonConvert.DeserializeObject(response);
                        if (result["Global Quote"]["05. price"] == null)
                        {
                            Console.WriteLine("\nErro:\n JSON NULL. Você provavelmente errou o Symbol");
                            return;
                        }
                        double price = result["Global Quote"]["05. price"];

                        //Verificação se Email deve ou não ser enviado

                        if ((price > sellPrice) || (price < buyPrice))
                        {

                            if ((price > sellPrice) && (!sellE_Sent))
                            {
                                SendEmail(0, price);
                                Console.WriteLine($"\nEmail de venda enviado, Preço Atual={price}");
                                sellE_Sent = true;
                            }
                            else if ((price < buyPrice) && (!buyE_Sent))
                            {
                                SendEmail(1, price);
                                Console.WriteLine($"\nEmail de compra enviado, Preço Atual={price}");
                                buyE_Sent = true;
                            }
                            else
                            {
                                Console.WriteLine($"\nEmail já foi enviado anteriormente."); //Caso o preço atinja o necessário para o envio de email, mas o cliente já foi avisado, evita-se o Spam.
                            }
                        }
                        else
                        {
                            Console.WriteLine($"\nNenhum Email enviado, Preço Atual={price}"); // Mas caso o preço volte a ficar entre as linhas de Compra/Venda, e atinja novamente o necessário, um novo email é enviado.
                            buyE_Sent = false;
                            sellE_Sent = false;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"HTTP error status code: {responseMsg.StatusCode}");
                    }
                }
                await Task.Delay(20000); //Delay de 20 Segundos entre execuções
            }
        }
    }
}
