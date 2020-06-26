using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

using Npgsql;

namespace KursachBankomat
{
    class DatabaseManager
    {
        public async Task<bool> Transfer(Card from, Card to, decimal amount)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                CurrencyConverter converter = new CurrencyConverter();

                NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO public.\"Transfers\" (\"user_id_from\", \"user_id_to\",\"amount\"" +
                    ",\"card_id_from\",\"card_id_to\",\"date_time\",\"currency\") VALUES ((@userIdFrom), (@userIdTo), (@amount)" +
                    ",(@cardIdFrom),(@cardIdTo),(@dt),(@currency))", connection);

                decimal convertedAmount = converter.Convert(from.Currency, to.Currency, amount);

                cmd.Parameters.AddWithValue("userIdFrom", from.HolderUserId);
                cmd.Parameters.AddWithValue("userIdTo", to.HolderUserId);
                cmd.Parameters.AddWithValue("amount", convertedAmount);
                cmd.Parameters.AddWithValue("cardIdFrom", from.Id);
                cmd.Parameters.AddWithValue("cardIdTo", to.Id);
                cmd.Parameters.AddWithValue("dt", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                cmd.Parameters.AddWithValue("currency", to.Currency);

                await cmd.ExecuteNonQueryAsync();

                cmd = new NpgsqlCommand("UPDATE public.\"Cards\" SET balance = balance - (@t_amount) WHERE id = (@id)", connection);
                cmd.Parameters.AddWithValue("t_amount", amount);
                cmd.Parameters.AddWithValue("id", from.Id);

                await cmd.ExecuteNonQueryAsync();

                cmd = new NpgsqlCommand("UPDATE public.\"Cards\" SET balance = balance + (@t_amount) WHERE id = (@id)", connection);
                cmd.Parameters.AddWithValue("t_amount", convertedAmount);
                cmd.Parameters.AddWithValue("id", to.Id);

                await cmd.ExecuteNonQueryAsync();

                return true;
                
            }
            catch(Exception e)
            {
                LastError = e.Message;
                return false;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<bool> Transfer(Card from, string billing_numberTo, decimal amount)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                //находим пользователя, которому переводятся средства, по его номеру счета и получаем его id
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("SELECT id FROM public.\"Users\" WHERE billing_number = (@number)", connection);
                cmd.Parameters.AddWithValue("number", billing_numberTo);

                NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();

                int userIdTo = reader.GetInt32(0);
                reader.Close();
                
                //получаем данные всех его карт
                cmd = new NpgsqlCommand("SELECT * FROM public.\"Cards\" WHERE holder_user_id = (@id)", connection);
                cmd.Parameters.AddWithValue("id", userIdTo);

                reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();
                Card cardTo = new Card(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), (decimal)reader.GetDouble(3), reader.GetString(4)
                        , reader.GetString(5), reader.GetString(6), reader.GetString(7));
                //перебираем каждую карту и ищем карту с той же расчетной валютой, что и карта, с которой списываются средства
                //если такой карты нет, то до цикла в cardTo присваивается первая найденная карта
                do
                {
                    if (reader.GetString(7) == from.Currency)
                    {
                        cardTo = new Card(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), (decimal)reader.GetDouble(3), reader.GetString(4)
                        , reader.GetString(5), reader.GetString(6), reader.GetString(7));
                    }
                }while (await reader.ReadAsync());

                return await Transfer(from, cardTo, amount); //осуществляем перевод в другой перегрузке данного метода
                //можно заметить, что эта перегрузка не осуществляет непосредственно перевод средств, а лишь вычисляет нужную карту для 
                //осуществления перевода в другой перегрузке метода

            }
            catch(Exception e)
            {
                LastError = e.Message;
                return false;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<bool> CardOperation(Card card, int amount, bool isDeposit)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO public.\"WithdrawDepositOperations\" (\"user_id\",\"card_id\"" +
                    ",\"is_deposit\",\"amount\",\"date_time\",\"currency\") VALUES ((@user_id),(@card_id),(@isDeposit),(@amount)" +
                    ",(@dt),(@currency))", connection);

                cmd.Parameters.AddWithValue("user_id", card.HolderUserId);
                cmd.Parameters.AddWithValue("card_id", card.Id);
                cmd.Parameters.AddWithValue("isDeposit", isDeposit);
                cmd.Parameters.AddWithValue("amount", amount);
                cmd.Parameters.AddWithValue("dt",DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                cmd.Parameters.AddWithValue("currency", card.Currency);

                await cmd.ExecuteNonQueryAsync();

                cmd = new NpgsqlCommand("UPDATE public.\"Cards\" SET balance = (@balance) WHERE id = (@id)", connection);

                cmd.Parameters.AddWithValue("balance", isDeposit ? card.Balance + amount : card.Balance - amount);
                cmd.Parameters.AddWithValue("id", card.Id);

                await cmd.ExecuteNonQueryAsync();


                return true;
            }
            catch(Exception e)
            {
                LastError = e.Message;
                return false;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<Card> ReissueCard(Card card)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("UPDATE public.\"Cards\" SET unique_number = (@u_number), valid_through = (@v_th)" +
                    ", cvv2_code = (@cvv2) WHERE id = (@id)", connection);

                DateTime dt = DateTime.Now;
                string unique_number = await GetNextUniqueNumber();
                string valid_th = dt.ToString("MM") + "/" + (Int32.Parse(dt.ToString("yy")) + 4);
                string cvv2 = GenerateCvv2();

                cmd.Parameters.AddWithValue("u_number", unique_number);
                cmd.Parameters.AddWithValue("v_th", valid_th);
                cmd.Parameters.AddWithValue("cvv2", cvv2);
                cmd.Parameters.AddWithValue("id", card.Id);

                await cmd.ExecuteNonQueryAsync();

                return new Card(card.Id, card.HolderUserId, valid_th, card.Balance, unique_number
                    , card.PinCode, cvv2, card.Currency);

            }
            catch (Exception e)
            {
                LastError = e.Message;
                return null;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<bool> RemoveCard(int cardId)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("DELETE FROM public.\"Cards\" WHERE id = (@id)", connection);
                cmd.Parameters.AddWithValue("id", cardId);

                await cmd.ExecuteNonQueryAsync();

                return true;
            }
            catch(Exception e)
            {
                LastError = e.Message;
                return false;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<Card> CreateCard(int holderId, string pin, string currency)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO public.\"Cards\" (\"holder_user_id\",\"valid_through\",\"balance\"" +
                    ",\"unique_number\",\"pin_code\",\"cvv2_code\",\"currency_type\") VALUES ((@holder_id),(@valid_th),(@balance)" +
                    ",(@u_number),(@pin),(@cvv2),(@currency))", connection);

                DateTime dt = DateTime.Now;
                string unique_number = await GetNextUniqueNumber();
                string valid_th = dt.ToString("MM") + "/" + (Int32.Parse(dt.ToString("yy")) + 4);
                string cvv2 = GenerateCvv2();

                cmd.Parameters.AddWithValue("holder_id", holderId);
                cmd.Parameters.AddWithValue("valid_th", valid_th);
                cmd.Parameters.AddWithValue("balance", 0.0);
                cmd.Parameters.AddWithValue("u_number", unique_number);
                cmd.Parameters.AddWithValue("pin", sha256(pin));
                cmd.Parameters.AddWithValue("cvv2", cvv2);
                cmd.Parameters.AddWithValue("currency", currency);

                await cmd.ExecuteNonQueryAsync();

                Card currentCard = new Card(await GetNexCardId(), holderId, valid_th, 0, unique_number, pin, cvv2, currency);

                return currentCard;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return null;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        
        //public async Task<bool> CheckPassportCorrectness(string passport, int userId)
        //{
        //    LastError = "";
        //    NpgsqlConnection connection = new NpgsqlConnection(connectionString);
        //    try
        //    {
        //        await connection.OpenAsync();

        //        NpgsqlCommand cmd = new NpgsqlCommand("SELECT passport_number FROM public.\"Users\" WHERE id = (@u_id)", connection);
        //        cmd.Parameters.AddWithValue("u_id", userId);

        //        NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        //        await reader.ReadAsync();

        //        if (reader.GetString(0) == passport)
        //            return true;

        //        LastError = "Неверный номер паспорта";
        //        return false;

        //    }
        //    catch (Exception e)
        //    {
        //        LastError = e.Message;
        //        return false;
        //    }
        //    finally
        //    {
        //        await connection.CloseAsync();
        //    }
        //}

        public async Task<bool> ChangePassword(string newPassword, int userId)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("UPDATE public.\"Users\" SET password = (@pass) WHERE id = (@id)", connection);
                cmd.Parameters.AddWithValue("pass", sha256(newPassword));
                cmd.Parameters.AddWithValue("id", userId);

                await cmd.ExecuteNonQueryAsync();

                return true;
            }
            catch(Exception e)
            {
                LastError = e.Message;
                return false;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<bool> ChangePin(string newPin, int cardId)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("UPDATE public.\"Cards\" SET pin_code = (@pin) WHERE id = (@card_id)", connection);
                cmd.Parameters.AddWithValue("pin", sha256(newPin));
                cmd.Parameters.AddWithValue("card_id", cardId);

                await cmd.ExecuteNonQueryAsync();

                return true;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return false;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<bool> CheckCardNumberExistence(string cardNumber)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("SELECT COUNT(*) FROM public.\"Cards\" WHERE unique_number = (@number)", connection);
                cmd.Parameters.AddWithValue("number", cardNumber);

                NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();

                if (reader.GetInt32(0) == 1)
                    return true;
                LastError = "Карта не найдена";
                return false;
            }
            catch(Exception e)
            {
                LastError = e.Message;
                return false;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<bool> CheckPasswordCorrectness(string password, int userId)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("SELECT password FROM public.\"Users\" WHERE id = (@u_id)", connection);
                cmd.Parameters.AddWithValue("u_id", userId);

                NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();

                if (reader.GetString(0) == sha256(password))
                    return true;

                LastError = "Неверный пароль";
                return false;

            }
            catch(Exception e)
            {
                LastError = e.Message;
                return false;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<bool> CheckPinCorrectness(string pin, int cardId)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("SELECT pin_code FROM public.\"Cards\" WHERE id = (@card_id)", connection);
                cmd.Parameters.AddWithValue("card_id", cardId);

                NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();

                if (reader.GetString(0) == sha256(pin))
                    return true;

                LastError = "Неверный пин-код";
                return false;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return false;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<bool> CheckBillingNumberCorrectness(string billing_number)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("SELECT COUNT(*) FROM public.\"Users\" WHERE billing_number = (@number)", connection);
                cmd.Parameters.AddWithValue("number", billing_number);

                NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();

                if (reader.GetInt32(0) == 1)
                    return true;

                LastError = "Нет пользователя с таким номером счета";
                return false;
            }
            catch(Exception e)
            {
                LastError = e.Message;
                return false;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<List<Card>> LoadUserCards(int holderId)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM public.\"Cards\" WHERE holder_user_id = (@holderId)", connection);
                cmd.Parameters.AddWithValue("holderId", holderId);

                NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

                List<Card> resultList = new List<Card>();

                while(await reader.ReadAsync())
                {
                    Card card = new Card(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), (decimal)reader.GetDouble(3), reader.GetString(4)
                        , reader.GetString(5), reader.GetString(6), reader.GetString(7));

                    resultList.Add(card);
                }

                return resultList;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return null;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<Card> GetCard(string unique_number)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM public.\"Cards\" WHERE unique_number = (@number)", connection);
                cmd.Parameters.AddWithValue("number", unique_number);

                NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();

                return new Card(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), (decimal)reader.GetDouble(3), reader.GetString(4)
                        , reader.GetString(5), reader.GetString(6), reader.GetString(7));
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return null;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<User> GetUser(int id)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM public.\"Users\" WHERE id = (@id)", connection);
                cmd.Parameters.AddWithValue("id", id);

                NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();

                return new User(reader.GetString(0), reader.GetString(1), reader.GetInt32(5), reader.GetString(2)
                    , reader.GetString(3), reader.GetString(4), reader.GetString(6));

            }
            catch(Exception e)
            {
                LastError = e.Message;
                return null;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<User> GetUser(String login)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM public.\"Users\" WHERE login = (@login)", connection);
                cmd.Parameters.AddWithValue("login", login);

                NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();

                return new User(reader.GetString(0), reader.GetString(1), reader.GetInt32(5), reader.GetString(2)
                    , reader.GetString(3), reader.GetString(4), reader.GetString(6));
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return null;
            }
            finally
            {
                await connection.CloseAsync();
            }

        }
        public async Task<bool> SignIn(String login, String password)
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("SELECT COUNT(*) FROM public.\"Users\" WHERE login = (@login) AND password = (@pass)", connection);
                cmd.Parameters.AddWithValue("login", login);
                cmd.Parameters.AddWithValue("pass",sha256(password));

                NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();


                if (reader.GetInt32(0) == 1)
                    return true;

                LastError = "Неверный логин/пароль";
                return false;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return false;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<List<BankOperation>> LoadOperationHistory(Card card)
        {
            LastError = "";
            NpgsqlConnection connecton = new NpgsqlConnection(connectionString);
            try
            {
                await connecton.OpenAsync();

                List<BankOperation> result = new List<BankOperation>(); //список всех операций по карте, который будет отображен

                //сначала получаем все операции снятия/внесения
                NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM public.\"WithdrawDepositOperations\" " +
                    "WHERE user_id = (@u_id) AND card_id = (@c_id)", connecton);
                cmd.Parameters.AddWithValue("u_id", card.HolderUserId);
                cmd.Parameters.AddWithValue("c_id", card.Id);

                NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

                while(await reader.ReadAsync())
                {
                    result.Add(new BankOperation(reader.GetBoolean(3) ? "Внесение" : "Снятие",reader.GetBoolean(3) 
                        ? (decimal)reader.GetDouble(4) : -(decimal)reader.GetDouble(4),reader.GetString(6),DateTime.Parse(reader.GetString(5))));
                }

                reader.Close();

                //теперь получаем все операции по входящим и исходящим переводам
                cmd = new NpgsqlCommand("SELECT * FROM public.\"Transfers\" WHERE (user_id_from = (@u_id) OR user_id_to = (@u_id))" +
                    " AND (card_id_to = (@c_id) OR card_id_from = (@c_id))", connecton);
                cmd.Parameters.AddWithValue("u_id", card.HolderUserId);
                cmd.Parameters.AddWithValue("c_id", card.Id);

                reader = await cmd.ExecuteReaderAsync();

                while(await reader.ReadAsync())
                {
                    string operationType = "";
                    decimal amount = 0;
                    //если на карту то сумма со знаком "+", если с карты, то "-"
                    if (reader.GetInt32(4) == card.Id)
                    {
                        amount = -(decimal)reader.GetDouble(3);
                    }
                    else
                    {
                        amount = (decimal)reader.GetDouble(3);
                    }

                    //устанавливаем перевод ли это между своими счетами, кому-то другому или от кого-то другого
                    if (reader.GetInt32(1) == reader.GetInt32(2) && reader.GetInt32(1) == card.HolderUserId)
                    {
                        operationType = "Перевод между картами";
                    }
                    else if(reader.GetInt32(1) == card.HolderUserId)
                    {
                        User otherUser = await GetUser(reader.GetInt32(2));
                        operationType = "Перевод " + otherUser.Name + " " + otherUser.Patronymic + " " + otherUser.Surname.Substring(0, 1) + ".";
                    }
                    else if(reader.GetInt32(2) == card.HolderUserId)
                    {
                        User otherUser = await GetUser(reader.GetInt32(1));
                        operationType = "Перевод от " + otherUser.Name + " " + otherUser.Patronymic + " " + otherUser.Surname.Substring(0, 1) + ".";
                    }


                    result.Add(new BankOperation(operationType,amount,reader.GetString(7),DateTime.Parse(reader.GetString(6))));
                }


                return result;
            }
            catch(Exception e)
            {
                LastError = e.Message;
                return null;
            }
            finally
            {
                await connecton.CloseAsync();
            }
        }

        static string sha256(string str)
        {
            var crypt = new SHA256Managed();
            var hash = new StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(str));
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }

        private async Task<string> GetNextUniqueNumber()
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            NpgsqlCommand cmd = new NpgsqlCommand("SELECT unique_number FROM public.\"Cards\" ORDER BY unique_number DESC", connection);

            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return "0000 0000 0000 0001";
            await reader.ReadAsync();

            string rawNumber = reader.GetString(0);
            string formattedNumber = "";
            foreach(char s in rawNumber)
            {
                if (s != ' ')
                {
                    formattedNumber += s;
                }
            }

            string add_str = formattedNumber.Substring(0, LastNonSignificantZeroPos(formattedNumber) + 1);
            string value_num = formattedNumber.Substring(LastNonSignificantZeroPos(formattedNumber) + 1, formattedNumber.Length - LastNonSignificantZeroPos(formattedNumber) - 1);

            value_num = Convert.ToString(Int64.Parse(value_num) + 1);

            await connection.CloseAsync();

            string resultRaw = "";
            string resultFormatted = "";

            if (add_str.Length == 0)
                resultRaw = value_num;
            else
                resultRaw = add_str.Substring(0, 16 - value_num.Length) + value_num;

            for(int i = 0; i < resultRaw.Length; i++)
            {
                if(i%4 == 0 && i!=0)
                {
                    resultFormatted += ' ';
                }
                resultFormatted += resultRaw[i];
            }

            return resultFormatted;
        }
        private async Task<int> GetNexCardId()
        {
            LastError = "";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = new NpgsqlCommand("SELECT id FROM public.\"Cards\" ORDER BY id DESC", connection);

                NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (!reader.HasRows)
                    return 1;
                else
                    await reader.ReadAsync();

                return reader.GetInt32(0);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        private string GenerateCvv2()
        {
            RNGCryptoServiceProvider Rand = new RNGCryptoServiceProvider(); //криптографически устойчивый генератор случайных чисел

            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue) //операция повторяется, пока сгенерированное число максимальному значанию типа uint)
                //на практике вероятность нескольких повторений цикла крайней мала, но вполне возможна ситуация, когда сгенерированное
                //число будет равно MaxValue, что приведет к ошибке в дальнейшем
            {
                byte[] four_bytes = new byte[4];
                Rand.GetBytes(four_bytes); //получаем 4 случайных байта (число типа int/uint)

                scale = BitConverter.ToUInt32(four_bytes, 0); //конвертируем их в число
            }

            int result = (int)(100 + 899 *(scale / (double)uint.MaxValue)); // scale/ (double) uint.MaxValue всегда (0;1), в таком случае
            //границы задаются числами a,b и имеют вид (a,b+a), где в данном случае a = 100, b = 899

            return result.ToString();
        }
        private int LastNonSignificantZeroPos(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] != '0')
                {
                    return i - 1;
                }
            }

            return -1;
        }

        public string LastError { get; private set; } = "";
        private string connectionString { get; set; } = "in project here is correct connection string to DB";
    }
}
